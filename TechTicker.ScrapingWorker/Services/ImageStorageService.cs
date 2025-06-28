using System.Security.Cryptography;

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

            // Check for duplicate by content before saving
            var existingPath = await FindDuplicateByContentAsync(imageData, productId);
            if (!string.IsNullOrEmpty(existingPath))
            {
                _logger.LogInformation("Duplicate image content found for {FileName}, reusing existing path: {Path}", 
                    fileName, existingPath);
                return existingPath;
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

    public async Task<bool> ImageExistsAsync(string relativePath)
    {
        try
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            // Convert relative path to full path
            var fullPath = Path.Combine(_baseImagePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            // Check if file exists
            if (!File.Exists(fullPath))
            {
                _logger.LogDebug("Image file does not exist: {Path}", fullPath);
                return false;
            }

            // Check if file is not empty and is a valid image
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length == 0)
            {
                _logger.LogWarning("Image file is empty: {Path}", fullPath);
                return false;
            }

            // Basic validation - check if file can be read
            try
            {
                await using var stream = File.OpenRead(fullPath);
                var buffer = new byte[8];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead < 4)
                {
                    _logger.LogWarning("Image file is too small to be valid: {Path}", fullPath);
                    return false;
                }

                // Check for common image file signatures
                var isValidImage = IsValidImageSignature(buffer);
                if (!isValidImage)
                {
                    _logger.LogWarning("Image file does not have valid image signature: {Path}", fullPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot read image file: {Path}", fullPath);
                return false;
            }

            _logger.LogDebug("Image file exists and is valid: {Path}", relativePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if image exists: {Path}", relativePath);
            return false;
        }
    }

    public async Task<string?> FindDuplicateByContentAsync(byte[] imageData, Guid productId)
    {
        try
        {
            if (imageData.Length == 0)
            {
                return null;
            }

            var contentHash = GenerateContentHash(imageData);
            var productDir = Path.Combine(_baseImagePath, productId.ToString());

            if (!Directory.Exists(productDir))
            {
                return null;
            }

            // Get all image files in the product directory
            var imageFiles = Directory.GetFiles(productDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => IsImageFile(file))
                .ToList();

            foreach (var imageFile in imageFiles)
            {
                try
                {
                    var fileData = await File.ReadAllBytesAsync(imageFile);
                    var fileHash = GenerateContentHash(fileData);

                    if (fileHash == contentHash)
                    {
                        // Convert to relative path
                        var relativePath = Path.Combine("images", "products", productId.ToString(), Path.GetFileName(imageFile))
                            .Replace(Path.DirectorySeparatorChar, '/');
                        
                        _logger.LogDebug("Found duplicate image by content hash: {Path}", relativePath);
                        return relativePath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading image file for duplicate check: {File}", imageFile);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate images by content for product {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Check if a product already has sufficient valid images
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="minCount">Minimum number of images required</param>
    /// <returns>True if product has sufficient valid images</returns>
    public async Task<bool> HasSufficientImagesAsync(Guid productId, int minCount = 1)
    {
        try
        {
            var imagePaths = await GetProductImagePathsAsync(productId);
            var validCount = 0;

            foreach (var path in imagePaths)
            {
                if (await ImageExistsAsync(path))
                {
                    validCount++;
                    if (validCount >= minCount)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} has sufficient images", productId);
            return false;
        }
    }

    /// <summary>
    /// Get count of valid images for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>Number of valid images</returns>
    public async Task<int> GetValidImageCountAsync(Guid productId)
    {
        try
        {
            var imagePaths = await GetProductImagePathsAsync(productId);
            var validCount = 0;

            foreach (var path in imagePaths)
            {
                if (await ImageExistsAsync(path))
                {
                    validCount++;
                }
            }

            return validCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid image count for product {ProductId}", productId);
            return 0;
        }
    }

    public string GenerateContentHash(byte[] imageData)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(imageData);
        return Convert.ToBase64String(hashBytes);
    }

    public Task<List<string>> GetProductImagePathsAsync(Guid productId)
    {
        try
        {
            var productDir = Path.Combine(_baseImagePath, productId.ToString());
            
            if (!Directory.Exists(productDir))
            {
                return Task.FromResult(new List<string>());
            }

            var imageFiles = Directory.GetFiles(productDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => IsImageFile(file))
                .ToList();

            var relativePaths = new List<string>();
            foreach (var imageFile in imageFiles)
            {
                var relativePath = Path.Combine("images", "products", productId.ToString(), Path.GetFileName(imageFile))
                    .Replace(Path.DirectorySeparatorChar, '/');
                relativePaths.Add(relativePath);
            }

            return Task.FromResult(relativePaths);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image paths for product {ProductId}", productId);
            return Task.FromResult(new List<string>());
        }
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

    private static bool IsValidImageSignature(byte[] buffer)
    {
        if (buffer.Length < 4) return false;

        // Check for common image file signatures
        // JPEG: FF D8 FF
        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            return true;

        // GIF: 47 49 46 38 or 47 49 46 39
        if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46 &&
            (buffer[3] == 0x38 || buffer[3] == 0x39))
            return true;

        // WebP: 52 49 46 46 (RIFF) - need to check further for WebP
        if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
            return true; // Simplified check for WebP

        // BMP: 42 4D
        if (buffer[0] == 0x42 && buffer[1] == 0x4D)
            return true;

        return false;
    }

    private static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" => true,
            _ => false
        };
    }
}
