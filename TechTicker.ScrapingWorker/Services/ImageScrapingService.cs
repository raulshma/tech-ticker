using AngleSharp.Dom;
using System.Text.RegularExpressions;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping and processing product images
/// </summary>
public partial class ImageScrapingService : IImageScrapingService
{
    private readonly ILogger<ImageScrapingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IImageStorageService _imageStorageService;

    [GeneratedRegex(@"^https?://", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteUrlRegex();

    public ImageScrapingService(
        ILogger<ImageScrapingService> logger,
        HttpClient httpClient,
        IImageStorageService imageStorageService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _imageStorageService = imageStorageService;
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

            // Download and upload images
            var imageUploadData = new List<ImageUploadData>();
            
            foreach (var imageUrl in urlsToProcess)
            {
                try
                {
                    var imageData = await DownloadImageAsync(imageUrl);
                    if (imageData != null)
                    {
                        imageUploadData.Add(imageData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image from {Url}", imageUrl);
                }
            }

            if (!imageUploadData.Any())
            {
                result.ErrorMessage = "Failed to download any images";
                result.IsSuccess = false;
                return result;
            }

            // Save images to local storage
            var savedPaths = await _imageStorageService.SaveImagesAsync(imageUploadData, productId);
            result.SuccessfulUploads = savedPaths.Count;

            if (savedPaths.Count > 0)
            {
                result.PrimaryImageUrl = savedPaths.First();
                result.AdditionalImageUrls = savedPaths.Skip(1).ToList();
                result.IsSuccess = true;

                _logger.LogInformation("Successfully processed {SuccessCount}/{TotalCount} images",
                    savedPaths.Count, imageUploadData.Count);
            }
            else
            {
                result.ErrorMessage = "Failed to save any images to local storage";
                result.IsSuccess = false;
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
            var imageElements = document.QuerySelectorAll(selector);
            
            foreach (var element in imageElements)
            {
                // Try different attributes in priority order
                var urls = new List<string?>();
                
                // 1. High-resolution zoom image (common in e-commerce)
                urls.Add(element.GetAttribute("data-zoom-image"));
                urls.Add(element.GetAttribute("data-large-image"));
                
                // 2. Standard src attribute
                urls.Add(element.GetAttribute("src"));
                
                // 3. Parse srcset for highest resolution
                var srcset = element.GetAttribute("srcset");
                if (!string.IsNullOrEmpty(srcset))
                {
                    urls.Add(ParseHighestResolutionFromSrcset(srcset));
                }

                // Process URLs
                foreach (var url in urls.Where(u => !string.IsNullOrEmpty(u)))
                {
                    var absoluteUrl = ConvertToAbsoluteUrl(url!, baseUrl);
                    if (!string.IsNullOrEmpty(absoluteUrl) && IsValidImageUrl(absoluteUrl))
                    {
                        imageUrls.Add(absoluteUrl);
                        break; // Take the first valid URL for this element
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting image URLs with selector {Selector}", selector);
        }

        return imageUrls.ToList();
    }

    private string? ParseHighestResolutionFromSrcset(string srcset)
    {
        try
        {
            // Parse srcset format: "url1 100w, url2 200w, url3 300w"
            var entries = srcset.Split(',')
                .Select(entry => entry.Trim())
                .Where(entry => !string.IsNullOrEmpty(entry))
                .ToList();

            if (!entries.Any()) return null;

            // Find entry with highest width
            string? bestUrl = null;
            int maxWidth = 0;

            foreach (var entry in entries)
            {
                var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var url = parts[0];
                    var widthStr = parts[1].Replace("w", "").Replace("x", "");
                    
                    if (int.TryParse(widthStr, out var width) && width > maxWidth)
                    {
                        maxWidth = width;
                        bestUrl = url;
                    }
                }
            }

            return bestUrl ?? entries.First().Split(' ')[0]; // Fallback to first URL
        }
        catch
        {
            return null;
        }
    }

    private string ConvertToAbsoluteUrl(string url, string baseUrl)
    {
        try
        {
            if (AbsoluteUrlRegex().IsMatch(url))
            {
                return url; // Already absolute
            }

            var baseUri = new Uri(baseUrl);
            var absoluteUri = new Uri(baseUri, url);
            return absoluteUri.ToString();
        }
        catch
        {
            return url; // Return original if conversion fails
        }
    }

    private static bool IsValidImageUrl(string url)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" };
        var lowerUrl = url.ToLower();
        
        // Check if URL contains image extension or common image patterns
        return imageExtensions.Any(ext => lowerUrl.Contains(ext)) ||
               lowerUrl.Contains("image") ||
               lowerUrl.Contains("photo") ||
               lowerUrl.Contains("picture");
    }

    private async Task<ImageUploadData?> DownloadImageAsync(string imageUrl)
    {
        try
        {
            _logger.LogDebug("Downloading image from {Url}", imageUrl);

            using var response = await _httpClient.GetAsync(imageUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", 
                    imageUrl, response.StatusCode);
                return null;
            }

            var imageData = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            
            // Generate filename from URL
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.'))
            {
                fileName = $"image_{Guid.NewGuid():N}.jpg";
            }

            return new ImageUploadData
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
