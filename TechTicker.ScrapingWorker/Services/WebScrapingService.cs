using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping product data from web pages
/// </summary>
public partial class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IScraperRunLogService _scraperRunLogService;

    [GeneratedRegex(@"[\d,]+\.?\d*")]
    private static partial Regex PriceRegex();

    public WebScrapingService(ILogger<WebScrapingService> logger, HttpClient httpClient, IScraperRunLogService scraperRunLogService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _scraperRunLogService = scraperRunLogService;
    }

    public async Task<ScrapingResult> ScrapeProductPageAsync(ScrapeProductPageCommand command)
    {
        var overallStopwatch = Stopwatch.StartNew();
        Guid? runId = null;

        try
        {
            _logger.LogInformation("Starting scraping for mapping {MappingId} at URL {Url}",
                command.MappingId, command.ExactProductUrl);

            // Create initial run log entry
            var createLogDto = new CreateScraperRunLogDto
            {
                MappingId = command.MappingId,
                TargetUrl = command.ExactProductUrl,
                UserAgent = command.ScrapingProfile.UserAgent,
                AdditionalHeaders = command.ScrapingProfile.Headers,
                Selectors = new ScrapingSelectorsDto
                {
                    ProductNameSelector = command.Selectors.ProductNameSelector,
                    PriceSelector = command.Selectors.PriceSelector,
                    StockSelector = command.Selectors.StockSelector,
                    SellerNameOnPageSelector = command.Selectors.SellerNameOnPageSelector
                },
                AttemptNumber = 1, // TODO: Handle retry logic
                DebugNotes = "Starting scraping operation"
            };

            var createResult = await _scraperRunLogService.CreateRunLogAsync(createLogDto);
            if (createResult.IsSuccess)
            {
                runId = createResult.Data;
            }

            // Configure HTTP request with comprehensive headers
            //_httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", command.ScrapingProfile.UserAgent);

            // Add custom headers from profile (these can override defaults)
            if (command.ScrapingProfile.Headers != null)
            {
                foreach (var header in command.ScrapingProfile.Headers)
                {
                    _httpClient.DefaultRequestHeaders.Remove(header.Key);
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            // Load the page with timing
            var pageLoadStopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(command.ExactProductUrl);
            var htmlContent = await response.Content.ReadAsStringAsync();
            pageLoadStopwatch.Stop();

            // Parse HTML with AngleSharp
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(htmlContent));

            // Update log with page load timing
            if (runId.HasValue)
            {
                await _scraperRunLogService.UpdateRunLogAsync(runId.Value, new UpdateScraperRunLogDto
                {
                    PageLoadTime = pageLoadStopwatch.Elapsed,
                    DebugNotes = $"Page loaded in {pageLoadStopwatch.ElapsedMilliseconds}ms. Status: {response.StatusCode}"
                });
            }

            // Check if HTTP request was successful
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"HTTP request failed with status {response.StatusCode}";
                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "HTTP_ERROR",
                        ErrorCategory = "NETWORK",
                        DebugNotes = $"HTTP status code: {response.StatusCode}, Reason: {response.ReasonPhrase}"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "HTTP_ERROR"
                };
            }

            // Check if we got valid HTML content
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                var errorMessage = "Received empty or null HTML content";
                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "EMPTY_CONTENT",
                        ErrorCategory = "PARSING",
                        DebugNotes = "HTML content was null or whitespace"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "EMPTY_CONTENT"
                };
            }

            var parsingStopwatch = Stopwatch.StartNew();
            var productName = ExtractProductName(document, command.Selectors.ProductNameSelector);
            var price = ExtractPrice(document, command.Selectors.PriceSelector);
            var stockStatus = ExtractStockStatus(document, command.Selectors.StockSelector);
            var sellerNameOnPage = ExtractSellerName(document, command.Selectors.SellerNameOnPageSelector);
            parsingStopwatch.Stop();

            // Get HTML snippet for debugging (first 1000 characters) and sanitize it
            var htmlSnippet = SanitizeString(htmlContent.Length > 1000 ? htmlContent[..1000] : htmlContent);

            // Check if we got a minimal/empty HTML response (likely JavaScript-rendered content)
            var isMinimalHtml = IsMinimalHtmlResponse(document);

            if (price == null)
            {
                var errorMessage = isMinimalHtml
                    ? "Could not extract price from the page - content appears to be JavaScript-rendered"
                    : "Could not extract price from the page";

                var errorCode = isMinimalHtml ? "JAVASCRIPT_CONTENT_DETECTED" : "PRICE_EXTRACTION_FAILED";

                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = errorCode,
                        ErrorCategory = isMinimalHtml ? "JAVASCRIPT_RENDERING" : "SELECTOR",
                        ParsingTime = parsingStopwatch.Elapsed,
                        RawHtmlSnippet = htmlSnippet,
                        DebugNotes = isMinimalHtml
                            ? $"Minimal HTML detected. Consider using browser automation for this site. Price selector: '{command.Selectors.PriceSelector}'"
                            : $"Price selector '{command.Selectors.PriceSelector}' did not match any elements"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = errorCode
                };
            }

            overallStopwatch.Stop();

            // Complete the run log with success
            if (runId.HasValue)
            {
                await _scraperRunLogService.CompleteRunAsync(runId.Value, new CompleteScraperRunDto
                {
                    ExtractedProductName = productName,
                    ExtractedPrice = price.Value,
                    ExtractedStockStatus = stockStatus ?? "Unknown",
                    ExtractedSellerName = sellerNameOnPage,
                    PageLoadTime = pageLoadStopwatch.Elapsed,
                    ParsingTime = parsingStopwatch.Elapsed,
                    RawHtmlSnippet = htmlSnippet,
                    DebugNotes = $"Successfully extracted all data in {overallStopwatch.ElapsedMilliseconds}ms"
                });
            }

            var result = new ScrapingResult
            {
                IsSuccess = true,
                ProductName = productName,
                Price = price.Value,
                StockStatus = stockStatus ?? "Unknown",
                SellerNameOnPage = sellerNameOnPage,
                ScrapedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Successfully scraped product data for mapping {MappingId}: Price={Price}, Stock={Stock}",
                command.MappingId, result.Price, result.StockStatus);

            return result;
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex, "Error scraping product page for mapping {MappingId}", command.MappingId);

            // Log the failure
            if (runId.HasValue)
            {
                await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                {
                    ErrorMessage = SanitizeString(ex.Message) ?? "Unknown error occurred",
                    ErrorCode = "SCRAPING_EXCEPTION",
                    ErrorCategory = DetermineErrorCategory(ex),
                    ErrorStackTrace = SanitizeString(ex.StackTrace),
                    DebugNotes = SanitizeString($"Exception occurred after {overallStopwatch.ElapsedMilliseconds}ms: {ex.GetType().Name}")
                });
            }

            return new ScrapingResult
            {
                IsSuccess = false,
                ErrorMessage = SanitizeString(ex.Message) ?? "Unknown error occurred",
                ErrorCode = "SCRAPING_EXCEPTION"
            };
        }
    }

    private static string DetermineErrorCategory(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "NETWORK",
            TaskCanceledException => "TIMEOUT",
            ArgumentException => "SELECTOR",
            InvalidOperationException => "PARSING",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Sanitizes a string by removing null bytes and other problematic characters that can cause database encoding issues
    /// </summary>
    private static string? SanitizeString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove null bytes (0x00) and other control characters that can cause PostgreSQL UTF-8 encoding issues
        return input.Replace("\0", "")  // Remove null bytes
                   .Replace("\u0001", "") // Remove Start of Heading
                   .Replace("\u0002", "") // Remove Start of Text
                   .Replace("\u0003", "") // Remove End of Text
                   .Replace("\u0004", "") // Remove End of Transmission
                   .Replace("\u0005", "") // Remove Enquiry
                   .Replace("\u0006", "") // Remove Acknowledge
                   .Replace("\u0007", "") // Remove Bell
                   .Replace("\u0008", "") // Remove Backspace
                   .Replace("\u000B", "") // Remove Vertical Tab
                   .Replace("\u000C", "") // Remove Form Feed
                   .Replace("\u000E", "") // Remove Shift Out
                   .Replace("\u000F", "") // Remove Shift In
                   .Replace("\u0010", "") // Remove Data Link Escape
                   .Replace("\u0011", "") // Remove Device Control 1
                   .Replace("\u0012", "") // Remove Device Control 2
                   .Replace("\u0013", "") // Remove Device Control 3
                   .Replace("\u0014", "") // Remove Device Control 4
                   .Replace("\u0015", "") // Remove Negative Acknowledge
                   .Replace("\u0016", "") // Remove Synchronous Idle
                   .Replace("\u0017", "") // Remove End of Transmission Block
                   .Replace("\u0018", "") // Remove Cancel
                   .Replace("\u0019", "") // Remove End of Medium
                   .Replace("\u001A", "") // Remove Substitute
                   .Replace("\u001B", "") // Remove Escape
                   .Replace("\u001C", "") // Remove File Separator
                   .Replace("\u001D", "") // Remove Group Separator
                   .Replace("\u001E", "") // Remove Record Separator
                   .Replace("\u001F", "") // Remove Unit Separator
                   .Trim();
    }

    private string? ExtractProductName(IDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            var rawText = element?.TextContent?.Trim();
            return SanitizeString(rawText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract product name using selector {Selector}", selector);
            return null;
        }
    }

    /// <summary>
    /// Detects if the HTML response is minimal/empty, indicating JavaScript-rendered content
    /// </summary>
    private bool IsMinimalHtmlResponse(IDocument document)
    {
        try
        {
            var html = document.DocumentElement?.OuterHtml ?? "";

            // Check for common patterns of minimal HTML responses
            var bodyNode = document.QuerySelector("body");
            var bodyContent = bodyNode?.TextContent?.Trim() ?? "";
            var bodyInnerHtml = bodyNode?.InnerHtml?.Trim() ?? "";

            // Indicators of minimal/empty content:
            // 1. Very short HTML (less than 200 characters)
            // 2. Empty body
            // 3. Body with only whitespace or minimal content
            // 4. Common empty HTML patterns

            if (html.Length < 200)
                return true;

            if (string.IsNullOrWhiteSpace(bodyContent))
                return true;

            if (bodyInnerHtml.Length < 50)
                return true;

            // Check for common empty HTML patterns
            var emptyPatterns = new[]
            {
                "<html><head></head><body></body></html>",
                "<html><head></head><body> </body></html>",
                "<html><body></body></html>"
            };

            if (emptyPatterns.Any(pattern => html.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Contains(pattern.Replace(" ", ""))))
                return true;

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for minimal HTML response");
            return false;
        }
    }

    private decimal? ExtractPrice(IDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            if (element == null)
                return null;

            var rawPriceText = element.TextContent?.Trim();
            var priceText = SanitizeString(rawPriceText);
            if (string.IsNullOrEmpty(priceText))
                return null;

            // Remove currency symbols and extract numeric value
            var priceMatch = PriceRegex().Match(priceText);
            if (!priceMatch.Success)
                return null;

            var cleanPrice = priceMatch.Value.Replace(",", "");

            if (decimal.TryParse(cleanPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract price using selector {Selector}", selector);
            return null;
        }
    }

    private string? ExtractStockStatus(IDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            var rawStockText = element?.TextContent?.Trim();
            var stockText = SanitizeString(rawStockText);

            if (string.IsNullOrEmpty(stockText))
                return "Unknown";

            // Normalize stock status
            var lowerStock = stockText.ToLower();

            if (lowerStock.Contains("in stock") || lowerStock.Contains("available") || lowerStock.Contains("in-stock"))
                return "In Stock";

            if (lowerStock.Contains("out of stock") || lowerStock.Contains("unavailable") || lowerStock.Contains("sold out"))
                return "Out of Stock";

            if (lowerStock.Contains("limited") || lowerStock.Contains("few left"))
                return "Limited Stock";

            return stockText; // Return original text if no pattern matches
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract stock status using selector {Selector}", selector);
            return "Unknown";
        }
    }

    private string? ExtractSellerName(IDocument document, string? selector)
    {
        if (string.IsNullOrEmpty(selector))
            return null;

        try
        {
            var element = document.QuerySelector(selector);
            var rawText = element?.TextContent?.Trim();
            return SanitizeString(rawText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract seller name using selector {Selector}", selector);
            return null;
        }
    }
}

/// <summary>
/// Result of a web scraping operation
/// </summary>
public class ScrapingResult
{
    public bool IsSuccess { get; set; }
    public string? ProductName { get; set; }
    public decimal Price { get; set; }
    public string? StockStatus { get; set; }
    public string? SellerNameOnPage { get; set; }
    public DateTimeOffset ScrapedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}
