using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping and processing product images
/// </summary>
public partial class ImageScrapingService : IImageScrapingService
{
    private readonly ILogger<ImageScrapingService> _logger;
    private readonly ProxyAwareHttpClientService _proxyHttpClient;
    private readonly TechTicker.Shared.Services.IImageStorageService _imageStorageService;
    private readonly IProductImageService _productImageService;

    [GeneratedRegex(@"^https?://", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteUrlRegex();

    public ImageScrapingService(
        ILogger<ImageScrapingService> logger,
        ProxyAwareHttpClientService proxyHttpClient,
        TechTicker.Shared.Services.IImageStorageService imageStorageService,
        IProductImageService productImageService)
    {
        _logger = logger;
        _proxyHttpClient = proxyHttpClient;
        _imageStorageService = imageStorageService;
        _productImageService = productImageService;
    }

    public async Task<ImageScrapingResult> ScrapeImagesAsync(
        IDocument document,
        string? imageSelector,
        string baseUrl,
        Guid productId,
        int maxImages = 5)
    {
        var result = new ImageScrapingResult();

        try
        {
            if (string.IsNullOrEmpty(imageSelector))
            {
                _logger.LogDebug("No image selector provided, skipping image scraping");
                result.IsSuccess = true;
                return result;
            }

            _logger.LogInformation("Starting image scraping with selector: {Selector}", imageSelector);

            var existingImagePaths = await _imageStorageService.GetProductImagePathsAsync(productId);
            var validImageCount = 0;
            foreach (var path in existingImagePaths)
            {
                if (await _imageStorageService.ImageExistsAsync(path))
                {
                    validImageCount++;
                }
            }

            if (validImageCount >= maxImages)
            {
                _logger.LogInformation("Product {ProductId} already has {Count} valid images, skipping scraping.", productId, validImageCount);
                result.IsSuccess = true;
                return result;
            }

            // Extract image URLs from HTML
            var imageUrls = ExtractImageUrls(document, imageSelector, baseUrl);
            
            if (!imageUrls.Any())
            {
                _logger.LogWarning("No images found using selector: {Selector}", imageSelector);
                result.IsSuccess = true;
                return result;
            }

            // Limit number of images
            var urlsToProcess = imageUrls.Take(maxImages).ToList();
            result.OriginalImageUrls = urlsToProcess;
            result.ProcessedCount = urlsToProcess.Count;

            _logger.LogInformation("Found {Count} images to process (limited to {MaxImages})",
                urlsToProcess.Count, maxImages);

            // Get existing image mappings and valid local paths
            var existingImageMappings = await _productImageService.GetExistingImageMappingsAsync(productId);
            var existingPaths = new List<string>();
            var urlsToDownload = new List<string>();

            // First, check if we already have valid local images
            var validLocalPaths = await _imageStorageService.GetProductImagePathsAsync(productId);
            foreach (var path in validLocalPaths)
            {
                if (await _imageStorageService.ImageExistsAsync(path))
                {
                    existingPaths.Add(path);
                }
            }

            // If we already have enough valid images, skip downloading
            if (existingPaths.Count >= maxImages)
            {
                _logger.LogInformation("Product {ProductId} already has {Count} valid local images (>= {MaxImages}), skipping downloads", 
                    productId, existingPaths.Count, maxImages);
                
                result.IsSuccess = true;
                result.PrimaryImageUrl = existingPaths.FirstOrDefault();
                result.AdditionalImageUrls = existingPaths.Skip(1).ToList();
                result.SuccessfulUploads = existingPaths.Count;
                return result;
            }

            // Check which URLs we need to download
            foreach (var imageUrl in urlsToProcess)
            {
                // Check if we have a mapping for this URL and the file still exists
                if (existingImageMappings.TryGetValue(imageUrl, out var existingPath))
                {
                    var fileExists = await _imageStorageService.ImageExistsAsync(existingPath);
                    if (fileExists)
                    {
                        existingPaths.Add(existingPath);
                        _logger.LogDebug("Skipping download for existing image: {Url} -> {Path}", imageUrl, existingPath);
                        continue;
                    }
                    else
                    {
                        _logger.LogDebug("Local file missing or invalid, will re-download: {Url} -> {Path}", imageUrl, existingPath);
                    }
                }

                urlsToDownload.Add(imageUrl);
            }

            _logger.LogInformation("Skipping {ExistingCount} existing images, downloading {DownloadCount} new images",
                existingPaths.Count, urlsToDownload.Count);

            // Download new images with enhanced duplicate detection
            var imageUploadData = new List<TechTicker.Shared.Services.ImageUploadData>();
            var newSavedPaths = new List<string>();

            foreach (var imageUrl in urlsToDownload)
            {
                try
                {
                    var imageData = await DownloadImageAsync(imageUrl);
                    if (imageData != null)
                    {
                        // Check for duplicate by content before adding to upload list
                        var duplicatePath = await _imageStorageService.FindDuplicateByContentAsync(imageData.Data, productId);
                        if (!string.IsNullOrEmpty(duplicatePath))
                        {
                            _logger.LogInformation("Found duplicate image content for URL {Url}, reusing existing path: {Path}", 
                                imageUrl, duplicatePath);
                            existingPaths.Add(duplicatePath);
                            continue;
                        }

                        imageUploadData.Add(imageData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image from {Url}", imageUrl);
                }
            }

            // Save new images to local storage
            if (imageUploadData.Any())
            {
                newSavedPaths = await _imageStorageService.SaveImagesAsync(imageUploadData, productId);
                _logger.LogInformation("Successfully saved {SavedCount}/{DownloadCount} new images",
                    newSavedPaths.Count, imageUploadData.Count);
            }

            // Combine existing and new image paths
            var allImagePaths = new List<string>();
            allImagePaths.AddRange(existingPaths);
            allImagePaths.AddRange(newSavedPaths);

            result.SuccessfulUploads = allImagePaths.Count;

            if (allImagePaths.Count > 0)
            {
                result.PrimaryImageUrl = allImagePaths.First();
                result.AdditionalImageUrls = allImagePaths.Skip(1).ToList();
                result.IsSuccess = true;

                _logger.LogInformation("Successfully processed {TotalCount} images ({ExistingCount} existing, {NewCount} new)",
                    allImagePaths.Count, existingPaths.Count, newSavedPaths.Count);
            }
            else if (urlsToProcess.Any())
            {
                result.ErrorMessage = "Failed to download or find any images";
                result.IsSuccess = false;
            }
            else
            {
                // No images to process, but that's okay
                result.IsSuccess = true;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image scraping");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private List<string> ExtractImageUrls(IDocument document, string selector, string baseUrl)
    {
        var imageUrls = new HashSet<string>(); // Use HashSet to avoid duplicates

        try
        {
            var containers = document.QuerySelectorAll(selector);
            var imageElements = new List<IElement>();

            if (containers.Any(e => e.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase)))
            {
                imageElements.AddRange(containers);
            }
            else
            {
                imageElements.AddRange(containers.SelectMany(c => c.QuerySelectorAll("img")));
            }

            foreach (var element in imageElements.Distinct())
            {
                var bestUrl = FindBestImageUrl(element, baseUrl);
                if (!string.IsNullOrEmpty(bestUrl))
                {
                    imageUrls.Add(bestUrl);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting image URLs with selector {Selector}", selector);
        }

        return imageUrls.ToList();
    }

    private string? FindBestImageUrl(IElement element, string baseUrl)
    {
        var potentialUrls = new List<string?>();

        // Priority 1: High-resolution data attributes on the element itself
        potentialUrls.Add(element.GetAttribute("data-zoom-image"));
        potentialUrls.Add(element.GetAttribute("data-large-image"));
        potentialUrls.Add(element.GetAttribute("data-full-image"));
        potentialUrls.Add(element.GetAttribute("data-highres-image"));

        // Priority 2: Parent element attributes (e.g., link to full image)
        var parent = element.ParentElement;
        if (parent != null)
        {
            potentialUrls.Add(parent.GetAttribute("data-zoom-image"));
            potentialUrls.Add(parent.GetAttribute("data-large-image"));
            if (parent.LocalName.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                potentialUrls.Add(parent.GetAttribute("href"));
            }
        }

        // Priority 3: srcset - find the highest resolution
        var srcset = element.GetAttribute("srcset");
        if (!string.IsNullOrEmpty(srcset))
        {
            potentialUrls.Add(ParseHighestResolutionFromSrcset(srcset));
        }

        // Priority 4: Standard src attribute
        potentialUrls.Add(element.GetAttribute("src"));

        // Find the first valid, absolute URL in the prioritized list
        foreach (var url in potentialUrls)
        {
            if (string.IsNullOrEmpty(url)) continue;

            var absoluteUrl = ConvertToAbsoluteUrl(url, baseUrl);
            if (IsValidImageUrl(absoluteUrl))
            {
                return absoluteUrl;
            }
        }

        return null;
    }

    private string? ParseHighestResolutionFromSrcset(string srcset)
    {
        try
        {
            var sources = srcset.Split(',')
                .Select(s => s.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length > 0)
                .Select(parts => new
                {
                    Url = parts[0],
                    Width = parts.Length > 1 && parts[1].EndsWith("w")
                            ? int.Parse(parts[1].TrimEnd('w'))
                            : 0
                })
                .OrderByDescending(s => s.Width)
                .ToList();

            return sources.FirstOrDefault()?.Url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse srcset: {Srcset}", srcset);
            // Fallback to just taking the first URL
            return srcset.Split(',').FirstOrDefault()?.Trim().Split(' ').FirstOrDefault();
        }
    }

    private string ConvertToAbsoluteUrl(string url, string baseUrl)
    {
        try
        {
            // Handle protocol-relative URLs
            if (url.StartsWith("//"))
            {
                var baseUri = new Uri(baseUrl);
                return new Uri(baseUri, url).ToString();
            }
            
            // Check if it's already absolute
            if (AbsoluteUrlRegex().IsMatch(url))
            {
                return url;
            }

            // Handle relative URLs
            var baseUriForRelative = new Uri(baseUrl);
            var absoluteUri = new Uri(baseUriForRelative, url);
            return absoluteUri.ToString();
        }
        catch
        {
            return url; // Return original if conversion fails
        }
    }

    private static bool IsValidImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Trim().StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".svg" };
        
        try
        {
            // Ensure we have an absolute URL to properly parse the path
            var uri = new Uri(url, UriKind.Absolute);
            var path = uri.AbsolutePath;
            
            var pathWithoutQuery = path.Split('?')[0];
            var extension = Path.GetExtension(pathWithoutQuery);
            
            return !string.IsNullOrEmpty(extension) && imageExtensions.Contains(extension.ToLowerInvariant());
        }
        catch (UriFormatException)
        {
            // This can happen if ConvertToAbsoluteUrl failed and returned a relative path
            try
            {
                var pathWithoutQuery = url.Split('?')[0];
                var extension = Path.GetExtension(pathWithoutQuery);
                return !string.IsNullOrEmpty(extension) && imageExtensions.Contains(extension.ToLowerInvariant());
            }
            catch
            {
                return false;
            }
        }
    }

    private async Task<TechTicker.Shared.Services.ImageUploadData?> DownloadImageAsync(string imageUrl)
    {
        try
        {
            _logger.LogDebug("Downloading image from {Url}", imageUrl);

            var proxyResponse = await _proxyHttpClient.GetBinaryAsync(imageUrl);

            if (!proxyResponse.IsSuccess || proxyResponse.BinaryContent == null)
            {
                _logger.LogWarning("Failed to download image from {Url}: {StatusCode}. Proxy: {Proxy}",
                    imageUrl, proxyResponse.StatusCode, proxyResponse.ProxyUsed ?? "Direct connection");
                return null;
            }

            var imageData = proxyResponse.BinaryContent;
            var contentType = proxyResponse.Headers.ContainsKey("content-type")
                ? proxyResponse.Headers["content-type"]
                : "image/jpeg";

            // Generate filename from URL
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
            {
                fileName = $"image_{Guid.NewGuid():N}.jpg";
            }

            return new TechTicker.Shared.Services.ImageUploadData
            {
                Data = imageData,
                FileName = fileName,
                ContentType = contentType,
                OriginalUrl = imageUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error downloading image from {Url}", imageUrl);
            return null;
        }
    }
}
