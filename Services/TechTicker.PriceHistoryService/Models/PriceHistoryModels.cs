using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.PriceHistoryService.Models
{
    /// <summary>
    /// Entity representing a price history record in the time-series database
    /// </summary>
    [Table("price_history")]
    public class PriceHistoryRecord
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [Required]
        [Column("canonical_product_id")]
        public Guid CanonicalProductId { get; set; }

        [Required]
        [Column("seller_name")]
        [MaxLength(100)]
        public string SellerName { get; set; } = null!;

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("stock_status")]
        [MaxLength(50)]
        public string StockStatus { get; set; } = null!;

        [Column("source_url")]
        [MaxLength(2048)]
        public string? SourceUrl { get; set; }

        [Column("product_name")]
        [MaxLength(500)]
        public string? ProductName { get; set; }

        [Column("original_stock_status")]
        [MaxLength(200)]
        public string? OriginalStockStatus { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for price history query responses
    /// </summary>
    public class PriceHistoryPointDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal Price { get; set; }
        public string StockStatus { get; set; } = null!;
        public string? SourceUrl { get; set; }
        public string? ProductName { get; set; }
        public string? OriginalStockStatus { get; set; }
    }

    /// <summary>
    /// Request parameters for price history queries
    /// </summary>
    public class PriceHistoryQueryRequest
    {
        public Guid CanonicalProductId { get; set; }
        public string? SellerName { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Response for price history queries with pagination
    /// </summary>
    public class PriceHistoryQueryResponse
    {
        public List<PriceHistoryPointDto> PricePoints { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// Price statistics for a product/seller combination
    /// </summary>
    public class PriceStatisticsDto
    {
        public Guid CanonicalProductId { get; set; }
        public string SellerName { get; set; } = null!;
        public decimal CurrentPrice { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal AveragePrice { get; set; }
        public DateTimeOffset? FirstRecorded { get; set; }
        public DateTimeOffset? LastRecorded { get; set; }
        public int TotalRecords { get; set; }
        public string? CurrentStockStatus { get; set; }
    }
}
