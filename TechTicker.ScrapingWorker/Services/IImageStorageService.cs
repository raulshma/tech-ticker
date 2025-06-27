namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for storing images locally
/// </summary>
public interface IImageStorageService
{
    /// <summary>
    /// Save an image to local storage and return the relative path
    /// </summary>
    /// <param name="imageData">Image data as byte array</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">MIME type of the image</param>
    /// <param name="productId">Product ID for organizing images</param>
    /// <returns>Relative path to the saved image</returns>
    Task<string?> SaveImageAsync(byte[] imageData, string fileName, string contentType, Guid productId);

    /// <summary>
    /// Save multiple images to local storage
    /// </summary>
    /// <param name="images">List of image data with metadata</param>
    /// <param name="productId">Product ID for organizing images</param>
    /// <returns>List of relative paths to saved images</returns>
    Task<List<string>> SaveImagesAsync(List<ImageUploadData> images, Guid productId);

    /// <summary>
    /// Check if a local image file exists and is valid
    /// </summary>
    /// <param name="relativePath">Relative path to the image file</param>
    /// <returns>True if file exists and is valid</returns>
    Task<bool> ImageExistsAsync(string relativePath);
}

/// <summary>
/// Data structure for image upload
/// </summary>
public class ImageUploadData
{
    public byte[] Data { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string OriginalUrl { get; set; } = null!;
}
