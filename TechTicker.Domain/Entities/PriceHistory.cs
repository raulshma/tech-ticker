using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Historical price data for products from various sellers
/// </summary>
public class PriceHistory
{
    [Key]
    public Guid PriceHistoryId { get; set; }

    [Required]
    public DateTimeOffset Timestamp { get; set; }

    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    public Guid MappingId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SellerName { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(50)]
    public string StockStatus { get; set; } = null!;

    [Required]
    [MaxLength(2048)]
    public string SourceUrl { get; set; } = null!;

    [MaxLength(512)]
    public string? ScrapedProductNameOnPage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CanonicalProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(MappingId))]
    public ProductSellerMapping Mapping { get; set; } = null!;
}
