using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Configuration for supported e-commerce sites and their CSS selectors
/// </summary>
public class SiteConfiguration
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Domain name (e.g., "amazon.com", "newegg.com")
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = null!;

    /// <summary>
    /// Display name for the site
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string SiteName { get; set; } = null!;

    /// <summary>
    /// Whether this site configuration is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// CSS selectors for product name extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ProductNameSelectors { get; set; }

    /// <summary>
    /// CSS selectors for price extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? PriceSelectors { get; set; }

    /// <summary>
    /// CSS selectors for image extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ImageSelectors { get; set; }

    /// <summary>
    /// CSS selectors for description extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? DescriptionSelectors { get; set; }

    /// <summary>
    /// CSS selectors for manufacturer extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ManufacturerSelectors { get; set; }

    /// <summary>
    /// CSS selectors for model number extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ModelNumberSelectors { get; set; }

    /// <summary>
    /// CSS selectors for specifications extraction (JSON array)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? SpecificationSelectors { get; set; }

    /// <summary>
    /// Additional notes about this site configuration
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Sample HTML content for testing selectors
    /// </summary>
    public string? TestHtml { get; set; }

    /// <summary>
    /// Confidence score of the selectors (0.0 to 1.0)
    /// </summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal ConfidenceScore { get; set; } = 0;

    /// <summary>
    /// Last time selectors were tested
    /// </summary>
    public DateTime? LastTestedAt { get; set; }

    /// <summary>
    /// Result of last selector test
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? LastTestResult { get; set; }

    /// <summary>
    /// When this configuration was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User who created this configuration
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User who last updated this configuration
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public ApplicationUser? UpdatedByUser { get; set; }

    // Helper properties for working with JSON arrays
    [NotMapped]
    public string[]? ProductNameSelectorsArray
    {
        get => string.IsNullOrEmpty(ProductNameSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(ProductNameSelectors);
        set => ProductNameSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? PriceSelectorsArray
    {
        get => string.IsNullOrEmpty(PriceSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(PriceSelectors);
        set => PriceSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? ImageSelectorsArray
    {
        get => string.IsNullOrEmpty(ImageSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(ImageSelectors);
        set => ImageSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? DescriptionSelectorsArray
    {
        get => string.IsNullOrEmpty(DescriptionSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(DescriptionSelectors);
        set => DescriptionSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? ManufacturerSelectorsArray
    {
        get => string.IsNullOrEmpty(ManufacturerSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(ManufacturerSelectors);
        set => ManufacturerSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? ModelNumberSelectorsArray
    {
        get => string.IsNullOrEmpty(ModelNumberSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(ModelNumberSelectors);
        set => ModelNumberSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string[]? SpecificationSelectorsArray
    {
        get => string.IsNullOrEmpty(SpecificationSelectors) 
            ? null 
            : JsonSerializer.Deserialize<string[]>(SpecificationSelectors);
        set => SpecificationSelectors = value == null ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, object>? LastTestResultDict
    {
        get => string.IsNullOrEmpty(LastTestResult) 
            ? null 
            : JsonSerializer.Deserialize<Dictionary<string, object>>(LastTestResult);
        set => LastTestResult = value == null ? null : JsonSerializer.Serialize(value);
    }
}
