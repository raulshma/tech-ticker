using AngleSharp.Dom;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping and processing product images
/// </summary>
public interface IImageScrapingService
{
    /// <summary>
    /// Extract and process images from a product page
    /// </summary>
    /// <param name="document">Parsed HTML document</param>
    /// <param name="imageSelector">CSS selector for finding images</param>
    /// <param name="baseUrl">Base URL for resolving relative image URLs</param>
    /// <param name="productId">Product ID for organizing saved images</param>
    /// <param name="maxImages">Maximum number of images to process</param>
    /// <returns>Image scraping result with local file paths</returns>
    Task<ImageScrapingResult> ScrapeImagesAsync(
        IDocument document,
        string? imageSelector,
        string baseUrl,
        Guid productId,
        int maxImages = 5);
}

/// <summary>
/// Result of image scraping operation
/// </summary>
public class ImageScrapingResult
{
    public bool IsSuccess { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<string> AdditionalImageUrls { get; set; } = new();
    public List<string> OriginalImageUrls { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessfulUploads { get; set; }
}
