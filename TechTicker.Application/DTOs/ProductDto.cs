using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for Product responses
/// </summary>
public class ProductDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string? Manufacturer { get; set; }
    public string? ModelNumber { get; set; }
    public string? SKU { get; set; }
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Specifications { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Image properties
    public string? PrimaryImageUrl { get; set; }
    public List<string>? AdditionalImageUrls { get; set; }
    public List<string>? OriginalImageUrls { get; set; }
    public DateTimeOffset? ImageLastUpdated { get; set; }

    // Navigation properties
    public CategoryDto? Category { get; set; }
}

/// <summary>
/// DTO for creating a new product
/// </summary>
public class CreateProductDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? ModelNumber { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    public string? Description { get; set; }

    public Dictionary<string, object>? Specifications { get; set; }

    // Image properties (optional for creation)
    public string? PrimaryImageUrl { get; set; }
    public List<string>? AdditionalImageUrls { get; set; }
}

/// <summary>
/// DTO for updating a product
/// </summary>
public class UpdateProductDto
{
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [MaxLength(100)]
    public string? ModelNumber { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public Guid? CategoryId { get; set; }

    public string? Description { get; set; }

    public Dictionary<string, object>? Specifications { get; set; }

    public bool? IsActive { get; set; }

    // Image properties (optional for updates)
    public string? PrimaryImageUrl { get; set; }
    public List<string>? AdditionalImageUrls { get; set; }
}
