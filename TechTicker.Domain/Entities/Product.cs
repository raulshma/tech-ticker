using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Represents a canonical product in the system
/// </summary>
public class Product
{
    [Key]
    public Guid ProductId { get; set; }

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

    /// <summary>
    /// JSON string containing product specifications (stored as JSONB in PostgreSQL)
    /// </summary>
    public string? Specifications { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public ICollection<ProductSellerMapping> ProductSellerMappings { get; set; } = new List<ProductSellerMapping>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    // Helper property for working with specifications as JSON
    [NotMapped]
    public Dictionary<string, object>? SpecificationsDict
    {
        get => string.IsNullOrEmpty(Specifications) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Specifications);
        set => Specifications = value == null ? null : JsonSerializer.Serialize(value);
    }
}
