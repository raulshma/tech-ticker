namespace TechTicker.ScraperService.Models
{
    /// <summary>
    /// Configuration settings for web scraping behavior
    /// </summary>
    public class ScrapingSettings
    {
        public int RequestTimeoutSeconds { get; set; } = 30;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
        public bool EnableCookieManagement { get; set; } = true;
        public string DefaultUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
    }

    /// <summary>
    /// Result of a web scraping operation
    /// </summary>
    public class ScrapingResult
    {
        public bool IsSuccess { get; set; }
        public string? ProductName { get; set; }
        public decimal? Price { get; set; }
        public string? StockStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public int? HttpStatusCode { get; set; }
        public string? RawHtml { get; set; }
    }

    /// <summary>
    /// Error codes for scraping failures
    /// </summary>
    public static class ScrapingErrorCodes
    {
        public const string HTTP_ERROR = "HTTP_ERROR";
        public const string PARSING_ERROR = "PARSING_ERROR";
        public const string TIMEOUT_ERROR = "TIMEOUT_ERROR";
        public const string BLOCKED_BY_CAPTCHA = "BLOCKED_BY_CAPTCHA";
        public const string RATE_LIMITED = "RATE_LIMITED";
        public const string NETWORK_ERROR = "NETWORK_ERROR";
        public const string INVALID_SELECTOR = "INVALID_SELECTOR";
        public const string PRICE_NOT_FOUND = "PRICE_NOT_FOUND";
        public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
    }
}
