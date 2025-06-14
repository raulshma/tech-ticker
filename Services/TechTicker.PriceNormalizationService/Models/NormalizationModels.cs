using TechTicker.Shared.Utilities;

namespace TechTicker.PriceNormalizationService.Models
{
    /// <summary>
    /// Configuration settings for price normalization
    /// </summary>
    public class NormalizationSettings
    {
        public decimal MinPrice { get; set; } = 0.01m;
        public decimal MaxPrice { get; set; } = 999999.99m;
        public bool StrictValidation { get; set; } = true;
        public string DefaultStockStatus { get; set; } = "UNKNOWN";
    }

    /// <summary>
    /// Normalized stock status enumeration
    /// </summary>
    public static class StockStatusConstants
    {
        public const string InStock = "IN_STOCK";
        public const string OutOfStock = "OUT_OF_STOCK";
        public const string LimitedStock = "LIMITED_STOCK";
        public const string PreOrder = "PRE_ORDER";
        public const string Discontinued = "DISCONTINUED";
        public const string Unknown = "UNKNOWN";
    }

    /// <summary>
    /// Result of price normalization process
    /// </summary>
    public class NormalizationResult
    {
        public decimal NormalizedPrice { get; set; }
        public string NormalizedStockStatus { get; set; } = null!;
        public string? OriginalStockStatus { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? ProductName { get; set; }
    }

    /// <summary>
    /// Error codes for normalization failures
    /// </summary>
    public static class NormalizationErrorCodes
    {
        public const string InvalidPrice = "INVALID_PRICE";
        public const string PriceOutOfRange = "PRICE_OUT_OF_RANGE";
        public const string InvalidStockStatus = "INVALID_STOCK_STATUS";
        public const string MissingRequiredData = "MISSING_REQUIRED_DATA";
        public const string ValidationError = "VALIDATION_ERROR";
    }
}
