namespace TechTicker.Shared.Constants;

/// <summary>
/// Standard stock status constants used throughout the system
/// </summary>
public static class StockStatus
{
    // Primary stock statuses
    public const string InStock = "IN_STOCK";
    public const string OutOfStock = "OUT_OF_STOCK";
    public const string Unknown = "UNKNOWN";
    public const string LimitedStock = "LIMITED_STOCK";
    public const string PreOrder = "PRE_ORDER";
    public const string Discontinued = "DISCONTINUED";

    // Common variations that should be normalized
    public static readonly Dictionary<string, string> StatusMappings = new()
    {
        // In stock variations
        { "in stock", InStock },
        { "available", InStock },
        { "in-stock", InStock },
        { "instock", InStock },
        { "stock available", InStock },
        { "ready to ship", InStock },
        { "ships today", InStock },
        { "ships immediately", InStock },

        // Out of stock variations
        { "out of stock", OutOfStock },
        { "out-of-stock", OutOfStock },
        { "outofstock", OutOfStock },
        { "unavailable", OutOfStock },
        { "not available", OutOfStock },
        { "sold out", OutOfStock },
        { "temporarily unavailable", OutOfStock },
        { "currently unavailable", OutOfStock },

        // Limited stock variations
        { "limited stock", LimitedStock },
        { "low stock", LimitedStock },
        { "few remaining", LimitedStock },
        { "only a few left", LimitedStock },
        { "limited availability", LimitedStock },

        // Pre-order variations
        { "pre-order", PreOrder },
        { "preorder", PreOrder },
        { "pre order", PreOrder },
        { "coming soon", PreOrder },
        { "backorder", PreOrder },

        // Discontinued variations
        { "discontinued", Discontinued },
        { "no longer available", Discontinued },
        { "product discontinued", Discontinued }
    };

    /// <summary>
    /// Normalizes a stock status string to a standard value
    /// </summary>
    /// <param name="status">Raw stock status from scraping</param>
    /// <returns>Normalized stock status</returns>
    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Unknown;

        // Check if the status is already a standard constant
        var upperStatus = status.Trim().ToUpperInvariant();
        if (GetAllStatuses().Contains(upperStatus))
        {
            return upperStatus;
        }

        var normalizedInput = status.Trim().ToLowerInvariant();

        // Direct mapping
        if (StatusMappings.TryGetValue(normalizedInput, out var mappedStatus))
            return mappedStatus;

        // Partial matching for complex strings
        if (normalizedInput.Contains("in stock") || normalizedInput.Contains("available"))
            return InStock;

        if (normalizedInput.Contains("out of stock") || normalizedInput.Contains("unavailable"))
            return OutOfStock;

        if (normalizedInput.Contains("limited") || normalizedInput.Contains("low stock"))
            return LimitedStock;

        if (normalizedInput.Contains("pre") && normalizedInput.Contains("order"))
            return PreOrder;

        if (normalizedInput.Contains("discontinued"))
            return Discontinued;

        return Unknown;
    }

    /// <summary>
    /// Checks if a status indicates the product is available for purchase
    /// </summary>
    /// <param name="status">Stock status to check</param>
    /// <returns>True if the product is available</returns>
    public static bool IsAvailable(string status)
    {
        return status == InStock || status == LimitedStock;
    }

    /// <summary>
    /// Checks if a status indicates the product is not available
    /// </summary>
    /// <param name="status">Stock status to check</param>
    /// <returns>True if the product is not available</returns>
    public static bool IsUnavailable(string status)
    {
        return status == OutOfStock || status == Discontinued;
    }

    /// <summary>
    /// Checks if a status change represents a transition from unavailable to available
    /// </summary>
    /// <param name="previousStatus">Previous stock status</param>
    /// <param name="currentStatus">Current stock status</param>
    /// <returns>True if the product became available</returns>
    public static bool IsBackInStock(string? previousStatus, string currentStatus)
    {
        var normalizedPrevious = Normalize(previousStatus);
        var normalizedCurrent = Normalize(currentStatus);

        // It is considered back in stock if it was previously out of stock and is now in stock.
        return IsUnavailable(normalizedPrevious) && IsAvailable(normalizedCurrent);
    }

    /// <summary>
    /// Gets all valid stock statuses
    /// </summary>
    /// <returns>Array of all valid stock status constants</returns>
    public static string[] GetAllStatuses()
    {
        return new[] { InStock, OutOfStock, Unknown, LimitedStock, PreOrder, Discontinued };
    }
}
