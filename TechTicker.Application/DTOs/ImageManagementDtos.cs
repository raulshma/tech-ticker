using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for image upload operations
/// </summary>
public class ImageUploadDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [MaxLength(500)]
    public string? AltText { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO representing an image with metadata
/// </summary>
public class ImageDto
{
    public string Url { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public string? AltText { get; set; }
    public string? Description { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// DTO for reordering images
/// </summary>
public class ImageReorderDto
{
    [Required]
    public List<string> ImageUrls { get; set; } = new();
}

/// <summary>
/// DTO for bulk image operations
/// </summary>
public class BulkImageOperationDto
{
    [Required]
    public List<string> ImageUrls { get; set; } = new();
}

/// <summary>
/// DTO for bulk operation results
/// </summary>
public class BulkImageOperationResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public List<string> SuccessfulUrls { get; set; } = new();
    public List<ImageOperationErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// DTO for image operation errors
/// </summary>
public class ImageOperationErrorDto
{
    public string ImageUrl { get; set; } = null!;
    public string ErrorMessage { get; set; } = null!;
    public string ErrorCode { get; set; } = null!;
}

/// <summary>
/// DTO for setting primary image
/// </summary>
public class SetPrimaryImageDto
{
    [Required]
    public string ImageUrl { get; set; } = null!;
}

/// <summary>
/// DTO for image metadata
/// </summary>
public class ImageMetadataDto
{
    public string Url { get; set; } = null!;
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public string Hash { get; set; } = null!;
    public bool IsValid { get; set; }
}

/// <summary>
/// DTO for image management summary
/// </summary>
public class ProductImageSummaryDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int TotalImages { get; set; }
    public bool HasPrimaryImage { get; set; }
    public long TotalSize { get; set; }
    public DateTimeOffset? LastImageUpdate { get; set; }
} 