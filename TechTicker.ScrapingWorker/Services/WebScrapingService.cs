using HtmlAgilityPack;
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
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", command.ScrapingProfile.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

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

            // Parse HTML with HtmlAgilityPack
            var document = new HtmlDocument();
            document.LoadHtml(htmlContent);

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

            // Get HTML snippet for debugging (first 1000 characters)
            var htmlSnippet = htmlContent.Length > 1000 ? htmlContent[..1000] : htmlContent;

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
                    ErrorMessage = ex.Message,
                    ErrorCode = "SCRAPING_EXCEPTION",
                    ErrorCategory = DetermineErrorCategory(ex),
                    ErrorStackTrace = ex.StackTrace,
                    DebugNotes = $"Exception occurred after {overallStopwatch.ElapsedMilliseconds}ms: {ex.GetType().Name}"
                });
            }

            return new ScrapingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
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

    private string? ExtractProductName(HtmlDocument document, string selector)
    {
        try
        {
            var element = document.DocumentNode.SelectSingleNode(selector);
            return element?.InnerText?.Trim();
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
    private bool IsMinimalHtmlResponse(HtmlDocument document)
    {
        try
        {
            var html = document.DocumentNode.OuterHtml ?? "";

            // Check for common patterns of minimal HTML responses
            var bodyNode = document.DocumentNode.SelectSingleNode("//body");
            var bodyContent = bodyNode?.InnerText?.Trim() ?? "";
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

    private decimal? ExtractPrice(HtmlDocument document, string selector)
    {
        try
        {
            var element = document.DocumentNode.SelectSingleNode(selector);
            if (element == null)
                return null;

            var priceText = element.InnerText?.Trim();
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

    private string? ExtractStockStatus(HtmlDocument document, string selector)
    {
        try
        {
            var element = document.DocumentNode.SelectSingleNode(selector);
            var stockText = element?.InnerText?.Trim();

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

    private string? ExtractSellerName(HtmlDocument document, string? selector)
    {
        if (string.IsNullOrEmpty(selector))
            return null;

        try
        {
            var element = document.DocumentNode.SelectSingleNode(selector);
            return element?.InnerText?.Trim();
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
