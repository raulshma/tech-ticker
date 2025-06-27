namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Implementation of local image storage service
/// </summary>
public class ImageStorageService : IImageStorageService
{
    private readonly ILogger<ImageStorageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _baseImagePath;

    public ImageStorageService(
        ILogger<ImageStorageService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Get base path from configuration or use default
        _baseImagePath = _configuration["ImageStorage:BasePath"] ?? "wwwroot/images/products";
        
        // Ensure the base directory exists
        Directory.CreateDirectory(_baseImagePath);
    }

    public async Task<string?> SaveImageAsync(byte[] imageData, string fileName, string contentType, Guid productId)
    {
        try
        {
            _logger.LogDebug("Saving image {FileName} ({Size} bytes) for product {ProductId}", 
                fileName, imageData.Length, productId);

            // Validate image data
            if (imageData.Length == 0)
            {
                _logger.LogWarning("Image data is empty for {FileName}", fileName);
                return null;
            }

            // Check file size limit (10MB)
            const int maxSizeBytes = 10 * 1024 * 1024;
            if (imageData.Length > maxSizeBytes)
            {
                _logger.LogWarning("Image {FileName} is too large ({Size} bytes, max {MaxSize})", 
                    fileName, imageData.Length, maxSizeBytes);
                return null;
            }

            // Validate content type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(contentType.ToLower()))
            {
                _logger.LogWarning("Unsupported image type {ContentType} for {FileName}", 
                    contentType, fileName);
                return null;
            }

            // Create directory structure: /images/products/{productId}/
            var productDir = Path.Combine(_baseImagePath, productId.ToString());
            Directory.CreateDirectory(productDir);

            // Generate unique filename to avoid conflicts
            var fileExtension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(fileExtension))
            {
                fileExtension = GetExtensionFromContentType(contentType);
            }
            
            var uniqueFileName = $"{Guid.NewGuid():N}{fileExtension}";
            var fullPath = Path.Combine(productDir, uniqueFileName);

            // Save the image file
            await File.WriteAllBytesAsync(fullPath, imageData);

            // Return relative path for storage in database
            var relativePath = Path.Combine("images", "products", productId.ToString(), uniqueFileName)
                .Replace(Path.DirectorySeparatorChar, '/'); // Use forward slashes for web URLs

            _logger.LogInformation("Successfully saved image {FileName} to {Path}", 
                fileName, relativePath);
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image {FileName} for product {ProductId}", fileName, productId);
            return null;
        }
    }

    public async Task<List<string>> SaveImagesAsync(List<ImageUploadData> images, Guid productId)
    {
        var savedPaths = new List<string>();

        if (images.Count == 0)
        {
            return savedPaths;
        }

        _logger.LogInformation("Saving {Count} images for product {ProductId}", images.Count, productId);

        // Process images in parallel with limited concurrency
        var semaphore = new SemaphoreSlim(3, 3); // Max 3 concurrent saves
        var saveTasks = images.Select(async image =>
        {
            await semaphore.WaitAsync();
            try
            {
                var path = await SaveImageAsync(image.Data, image.FileName, image.ContentType, productId);
                return path;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(saveTasks);
        
        // Filter out null results
        savedPaths.AddRange(results.Where(path => !string.IsNullOrEmpty(path))!);

        _logger.LogInformation("Successfully saved {SuccessCount}/{TotalCount} images for product {ProductId}", 
            savedPaths.Count, images.Count, productId);

        return savedPaths;
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".jpg" // Default fallback
        };
    }
}
