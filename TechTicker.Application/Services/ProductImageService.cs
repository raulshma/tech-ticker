using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Services;
using TechTicker.Shared.Constants;
using static TechTicker.Shared.Constants.ApplicationConstants;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing product images with comprehensive CRUD operations
/// </summary>
public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<ProductImageService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Maximum number of images per product
    private const int MaxImagesPerProduct = 50;

    public ProductImageService(
        IUnitOfWork unitOfWork,
        IImageStorageService imageStorageService,
        ILogger<ProductImageService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _imageStorageService = imageStorageService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Result<List<ImageDto>>> UploadImagesAsync(Guid productId, List<ImageUploadDto> images)
    {
        try
        {
            if (!await HasImageManagementPermissionAsync(productId))
            {
                return Result<List<ImageDto>>.Failure("Insufficient permissions to manage images for this product", "PERMISSION_DENIED");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<List<ImageDto>>.Failure("Product not found", "PRODUCT_NOT_FOUND");
            }

            // Get current image count
            var currentImages = await GetProductImagesAsync(productId);
            if (currentImages.IsSuccess)
            {
                var currentCount = currentImages.Data!.Count;
                if (currentCount + images.Count > MaxImagesPerProduct)
                {
                    return Result<List<ImageDto>>.Failure(
                        $"Cannot upload {images.Count} images. Product already has {currentCount} images. Maximum allowed: {MaxImagesPerProduct}",
                        "MAX_IMAGES_EXCEEDED");
                }
            }

            var uploadedImages = new List<ImageDto>();
            var uploadDataList = new List<ImageUploadData>();

            // Process and validate each image
            foreach (var imageDto in images)
            {
                var validationResult = await ValidateImageAsync(imageDto.File);
                if (!validationResult.IsSuccess)
                {
                    _logger.LogWarning("Image validation failed for {FileName}: {Error}", 
                        imageDto.File.FileName, validationResult.ErrorMessage);
                    continue;
                }

                // Convert IFormFile to byte array
                using var memoryStream = new MemoryStream();
                await imageDto.File.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                uploadDataList.Add(new ImageUploadData
                {
                    Data = imageData,
                    FileName = imageDto.File.FileName,
                    ContentType = imageDto.File.ContentType,
                    OriginalUrl = $"upload-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{imageDto.File.FileName}"
                });
            }

            if (uploadDataList.Count == 0)
            {
                return Result<List<ImageDto>>.Failure("No valid images to upload", "NO_VALID_IMAGES");
            }

            // Upload images to storage
            var savedPaths = await _imageStorageService.SaveImagesAsync(uploadDataList, productId);
            
            if (savedPaths.Count == 0)
            {
                return Result<List<ImageDto>>.Failure("Failed to save images to storage", "STORAGE_ERROR");
            }

            // Update product with new images
            var existingUrls = product.AdditionalImageUrlsList ?? new List<string>();
            var allImageUrls = new List<string>();
            
            // Keep existing images
            allImageUrls.AddRange(existingUrls);
            
            // Add new images
            allImageUrls.AddRange(savedPaths);

            // Set primary image if product doesn't have one
            if (string.IsNullOrEmpty(product.PrimaryImageUrl) && savedPaths.Count > 0)
            {
                product.PrimaryImageUrl = savedPaths[0];
            }

            // Update the product
            product.AdditionalImageUrlsList = allImageUrls;
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            // Create response DTOs
            for (int i = 0; i < savedPaths.Count; i++)
            {
                var savedPath = savedPaths[i];
                var originalUploadData = uploadDataList[i];
                var metadata = await GetImageMetadataInternalAsync(savedPath);

                uploadedImages.Add(new ImageDto
                {
                    Url = savedPath,
                    IsPrimary = savedPath == product.PrimaryImageUrl,
                    AltText = images[i].AltText,
                    Description = images[i].Description,
                    FileSize = metadata?.FileSize ?? 0,
                    Width = metadata?.Width,
                    Height = metadata?.Height,
                    ContentType = originalUploadData.ContentType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Order = allImageUrls.IndexOf(savedPath)
                });
            }

            _logger.LogInformation("Successfully uploaded {Count} images for product {ProductId}", 
                uploadedImages.Count, productId);

            return Result<List<ImageDto>>.Success(uploadedImages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images for product {ProductId}", productId);
            return Result<List<ImageDto>>.Failure("An error occurred while uploading images", "UPLOAD_ERROR");
        }
    }

    public async Task<Result<List<ImageDto>>> GetProductImagesAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<List<ImageDto>>.Failure("Product not found", "PRODUCT_NOT_FOUND");
            }

            var images = new List<ImageDto>();
            var allImageUrls = new List<string>();

            // Add primary image first
            if (!string.IsNullOrEmpty(product.PrimaryImageUrl))
            {
                allImageUrls.Add(product.PrimaryImageUrl);
            }

            // Add additional images
            if (product.AdditionalImageUrlsList != null)
            {
                allImageUrls.AddRange(product.AdditionalImageUrlsList.Where(url => url != product.PrimaryImageUrl));
            }

            // Create ImageDto objects with metadata
            for (int i = 0; i < allImageUrls.Count; i++)
            {
                var imageUrl = allImageUrls[i];
                var metadata = await GetImageMetadataInternalAsync(imageUrl);

                images.Add(new ImageDto
                {
                    Url = imageUrl,
                    IsPrimary = imageUrl == product.PrimaryImageUrl,
                    FileSize = metadata?.FileSize ?? 0,
                    Width = metadata?.Width,
                    Height = metadata?.Height,
                    ContentType = metadata?.ContentType ?? "image/jpeg",
                    CreatedAt = product.ImageLastUpdated ?? product.CreatedAt,
                    Order = i
                });
            }

            return Result<List<ImageDto>>.Success(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for product {ProductId}", productId);
            return Result<List<ImageDto>>.Failure("An error occurred while retrieving product images", "RETRIEVAL_ERROR");
        }
    }

    public async Task<Result<bool>> DeleteImageAsync(Guid productId, string imageUrl)
    {
        try
        {
            if (!await HasImageManagementPermissionAsync(productId))
            {
                return Result<bool>.Failure("Insufficient permissions to manage images for this product", "PERMISSION_DENIED");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<bool>.Failure("Product not found", "PRODUCT_NOT_FOUND");
            }

            var imageUrls = product.AdditionalImageUrlsList ?? new List<string>();
            if (!imageUrls.Contains(imageUrl) && product.PrimaryImageUrl != imageUrl)
            {
                return Result<bool>.Failure("Image not found for this product", "IMAGE_NOT_FOUND");
            }

            // Remove from storage
            // Note: ImageStorageService doesn't have a delete method, but we can implement it
            // For now, we'll just remove from database and log the file path
            _logger.LogInformation("Image file should be deleted from storage: {ImageUrl}", imageUrl);

            // Remove from product
            if (product.PrimaryImageUrl == imageUrl)
            {
                // If deleting primary image, set a new primary from additional images
                if (imageUrls.Count > 0)
                {
                    product.PrimaryImageUrl = imageUrls.FirstOrDefault(url => url != imageUrl);
                }
                else
                {
                    product.PrimaryImageUrl = null;
                }
            }

            imageUrls.Remove(imageUrl);
            product.AdditionalImageUrlsList = imageUrls;
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted image {ImageUrl} from product {ProductId}", imageUrl, productId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageUrl} from product {ProductId}", imageUrl, productId);
            return Result<bool>.Failure("An error occurred while deleting the image", "DELETE_ERROR");
        }
    }

    public async Task<Result<bool>> SetPrimaryImageAsync(Guid productId, string imageUrl)
    {
        try
        {
            if (!await HasImageManagementPermissionAsync(productId))
            {
                return Result<bool>.Failure("Insufficient permissions to manage images for this product", "PERMISSION_DENIED");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<bool>.Failure("Product not found", "PRODUCT_NOT_FOUND");
            }

            var allImageUrls = new List<string>();
            if (!string.IsNullOrEmpty(product.PrimaryImageUrl))
                allImageUrls.Add(product.PrimaryImageUrl);
            if (product.AdditionalImageUrlsList != null)
                allImageUrls.AddRange(product.AdditionalImageUrlsList);

            if (!allImageUrls.Contains(imageUrl))
            {
                return Result<bool>.Failure("Image not found for this product", "IMAGE_NOT_FOUND");
            }

            // Verify image exists in storage
            var imageExists = await _imageStorageService.ImageExistsAsync(imageUrl);
            if (!imageExists)
            {
                return Result<bool>.Failure("Image file not found in storage", "IMAGE_FILE_NOT_FOUND");
            }

            product.PrimaryImageUrl = imageUrl;
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully set primary image {ImageUrl} for product {ProductId}", imageUrl, productId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary image {ImageUrl} for product {ProductId}", imageUrl, productId);
            return Result<bool>.Failure("An error occurred while setting the primary image", "SET_PRIMARY_ERROR");
        }
    }

    public async Task<Result<bool>> ReorderImagesAsync(Guid productId, List<string> imageUrls)
    {
        try
        {
            if (!await HasImageManagementPermissionAsync(productId))
            {
                return Result<bool>.Failure("Insufficient permissions to manage images for this product", "PERMISSION_DENIED");
            }

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<bool>.Failure("Product not found", "PRODUCT_NOT_FOUND");
            }

            // Validate that all provided URLs belong to this product
            var currentImages = await GetProductImagesAsync(productId);
            if (!currentImages.IsSuccess)
            {
                return Result<bool>.Failure("Error retrieving current product images", "RETRIEVAL_ERROR");
            }

            var currentUrls = currentImages.Data!.Select(img => img.Url).ToHashSet();
            var providedUrls = imageUrls.ToHashSet();

            if (!providedUrls.SetEquals(currentUrls))
            {
                return Result<bool>.Failure("Provided image URLs do not match current product images", "INVALID_IMAGE_URLS");
            }

            // Update the order - first URL becomes primary, rest are additional
            product.PrimaryImageUrl = imageUrls.FirstOrDefault();
            product.AdditionalImageUrlsList = imageUrls.Skip(1).ToList();
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully reordered {Count} images for product {ProductId}", imageUrls.Count, productId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering images for product {ProductId}", productId);
            return Result<bool>.Failure("An error occurred while reordering images", "REORDER_ERROR");
        }
    }

    public async Task<Result<BulkImageOperationResultDto>> BulkDeleteImagesAsync(Guid productId, List<string> imageUrls)
    {
        try
        {
            if (!await HasImageManagementPermissionAsync(productId))
            {
                return Result<BulkImageOperationResultDto>.Failure("Insufficient permissions to manage images for this product", "PERMISSION_DENIED");
            }

            var result = new BulkImageOperationResultDto
            {
                TotalRequested = imageUrls.Count,
                SuccessfulOperations = 0,
                FailedOperations = 0,
                SuccessfulUrls = new List<string>(),
                Errors = new List<ImageOperationErrorDto>()
            };

            foreach (var imageUrl in imageUrls)
            {
                var deleteResult = await DeleteImageAsync(productId, imageUrl);
                if (deleteResult.IsSuccess)
                {
                    result.SuccessfulOperations++;
                    result.SuccessfulUrls.Add(imageUrl);
                }
                else
                {
                    result.FailedOperations++;
                    result.Errors.Add(new ImageOperationErrorDto
                    {
                        ImageUrl = imageUrl,
                        ErrorMessage = deleteResult.ErrorMessage,
                        ErrorCode = deleteResult.ErrorCode
                    });
                }
            }

            _logger.LogInformation("Bulk delete completed for product {ProductId}: {Successful}/{Total} successful", 
                productId, result.SuccessfulOperations, result.TotalRequested);

            return Result<BulkImageOperationResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk delete images for product {ProductId}", productId);
            return Result<BulkImageOperationResultDto>.Failure("An error occurred during bulk delete operation", "BULK_DELETE_ERROR");
        }
    }

    public async Task<Result<ImageMetadataDto>> GetImageMetadataAsync(string imageUrl)
    {
        try
        {
            var metadata = await GetImageMetadataInternalAsync(imageUrl);
            if (metadata == null)
            {
                return Result<ImageMetadataDto>.Failure("Image metadata not available", "METADATA_NOT_FOUND");
            }

            return Result<ImageMetadataDto>.Success(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metadata for image {ImageUrl}", imageUrl);
            return Result<ImageMetadataDto>.Failure("An error occurred while retrieving image metadata", "METADATA_ERROR");
        }
    }

    public async Task<bool> HasImageManagementPermissionAsync(Guid productId)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            // Check if user has Products.Update permission
            var permissions = user.FindAll("permission").Select(c => c.Value);
            if (permissions.Contains(Permissions.ProductsUpdate))
            {
                return true;
            }

            // Check roles - Admin and Moderator have image management permissions
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);
            return roles.Any(role => role == "Admin" || role == "Moderator");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image management permission for product {ProductId}", productId);
            return false;
        }
    }

    private async Task<Result<bool>> ValidateImageAsync(IFormFile file)
    {
        // Check file size
        if (file.Length > FileUpload.MaxFileSizeBytes)
        {
            return Result<bool>.Failure($"File size exceeds maximum limit of {FileUpload.MaxFileSizeBytes / (1024 * 1024)}MB", "FILE_SIZE_EXCEEDED");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!FileUpload.AllowedImageExtensions.Contains(extension))
        {
            return Result<bool>.Failure($"File type {extension} is not allowed", "INVALID_FILE_TYPE");
        }

        // Check content type
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return Result<bool>.Failure($"Content type {file.ContentType} is not allowed", "INVALID_CONTENT_TYPE");
        }

        return Result<bool>.Success(true);
    }

    private async Task<ImageMetadataDto?> GetImageMetadataInternalAsync(string imageUrl)
    {
        try
        {
            // Check if image exists
            var exists = await _imageStorageService.ImageExistsAsync(imageUrl);
            if (!exists)
            {
                return null;
            }

            // For now, return basic metadata
            // In a full implementation, you would read the actual file to get dimensions
            return new ImageMetadataDto
            {
                Url = imageUrl,
                FileSize = 0, // Would need to get actual file size
                ContentType = GetContentTypeFromUrl(imageUrl),
                CreatedAt = DateTimeOffset.UtcNow,
                Hash = _imageStorageService.GenerateContentHash(Array.Empty<byte>()), // Would need actual file data
                IsValid = true
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentTypeFromUrl(string imageUrl)
    {
        var extension = Path.GetExtension(imageUrl).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    public async Task<Dictionary<string, string>> GetExistingImageMappingsAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return new Dictionary<string, string>();
            }

            var imageMappings = new Dictionary<string, string>();

            // Get original URLs list
            var originalUrls = product.OriginalImageUrlsList ?? new List<string>();
            
            // Create a list of local URLs in order: primary first, then additional
            var localUrls = new List<string>();
            
            if (!string.IsNullOrEmpty(product.PrimaryImageUrl))
            {
                localUrls.Add(product.PrimaryImageUrl);
            }
            
            if (product.AdditionalImageUrlsList != null)
            {
                localUrls.AddRange(product.AdditionalImageUrlsList);
            }

            // Map original URLs to local URLs based on position/order
            // Assumes the order in OriginalImageUrls corresponds to the order of local images
            for (int i = 0; i < Math.Min(originalUrls.Count, localUrls.Count); i++)
            {
                var originalUrl = originalUrls[i];
                var localUrl = localUrls[i];
                
                if (!string.IsNullOrEmpty(originalUrl) && !string.IsNullOrEmpty(localUrl))
                {
                    imageMappings[originalUrl] = localUrl;
                }
            }

            return imageMappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving existing image mappings for product {ProductId}", productId);
            return new Dictionary<string, string>();
        }
    }

    public async Task UpdateProductImagesAsync(Guid productId, string? primaryImageUrl, List<string>? additionalImageUrls, List<string>? originalImageUrls)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found for image update", productId);
                return;
            }

            product.PrimaryImageUrl = primaryImageUrl;
            product.AdditionalImageUrlsList = additionalImageUrls ?? new List<string>();
            product.OriginalImageUrlsList = originalImageUrls ?? new List<string>();
            product.ImageLastUpdated = DateTimeOffset.UtcNow;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated images for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating images for product {ProductId}", productId);
        }
    }

    public async Task<bool> HasRecentImagesAsync(Guid productId, TimeSpan maxAge)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            if (product.ImageLastUpdated == null)
            {
                return false;
            }

            var cutoffTime = DateTimeOffset.UtcNow - maxAge;
            return product.ImageLastUpdated > cutoffTime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recent images for product {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> HasAnyImagesAsync(Guid productId)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            var hasPrimary = !string.IsNullOrEmpty(product.PrimaryImageUrl);
            var hasAdditional = product.AdditionalImageUrlsList != null && product.AdditionalImageUrlsList.Any();

            return hasPrimary || hasAdditional;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if product {ProductId} has any images", productId);
            return false;
        }
    }

    public async Task<bool> ShouldSkipImageProcessingAsync(Guid productId, TimeSpan maxAge, int maxImages)
    {
        try
        {
            // Check if the product has recent images
            var hasRecentImages = await HasRecentImagesAsync(productId, maxAge);
            if (hasRecentImages)
            {
                return true;
            }

            // Check if the product already has sufficient images
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                return false;
            }

            var imageCount = 0;
            
            if (!string.IsNullOrEmpty(product.PrimaryImageUrl))
            {
                imageCount++;
            }

            if (product.AdditionalImageUrlsList != null)
            {
                imageCount += product.AdditionalImageUrlsList.Count;
            }

            return imageCount >= maxImages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if image processing should be skipped for product {ProductId}", productId);
            return false;
        }
    }
}

