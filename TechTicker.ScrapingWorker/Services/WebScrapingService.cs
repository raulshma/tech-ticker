using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
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
public class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly IBrowsingContext _browsingContext;
    private readonly IScraperRunLogService _scraperRunLogService;

    public WebScrapingService(ILogger<WebScrapingService> logger, IScraperRunLogService scraperRunLogService)
    {
        _logger = logger;
        _scraperRunLogService = scraperRunLogService;

        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
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

            // Configure request with custom headers and user agent
            var requestOptions = new Dictionary<string, string>
            {
                ["User-Agent"] = command.ScrapingProfile.UserAgent
            };

            if (command.ScrapingProfile.Headers != null)
            {
                foreach (var header in command.ScrapingProfile.Headers)
                {
                    requestOptions[header.Key] = header.Value;
                }
            }

            // Load the page with timing
            var pageLoadStopwatch = Stopwatch.StartNew();
            var document = await _browsingContext.OpenAsync(req =>
            {
                req.Address(command.ExactProductUrl);
                foreach (var header in requestOptions)
                {
                    req.Header(header.Key, header.Value);
                }
            });
            pageLoadStopwatch.Stop();

            // Update log with page load timing
            if (runId.HasValue)
            {
                await _scraperRunLogService.UpdateRunLogAsync(runId.Value, new UpdateScraperRunLogDto
                {
                    PageLoadTime = pageLoadStopwatch.Elapsed,
                    DebugNotes = $"Page loaded in {pageLoadStopwatch.ElapsedMilliseconds}ms"
                });
            }

            if (document == null)
            {
                var errorMessage = "Failed to load the page";
                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "PAGE_LOAD_FAILED",
                        ErrorCategory = "NETWORK",
                        DebugNotes = "Document was null after page load attempt"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "PAGE_LOAD_FAILED"
                };
            }

            // Extract product data with timing
            var htmlDocument = document as IHtmlDocument;
            if (htmlDocument == null)
            {
                var errorMessage = "Failed to cast document to HTML document";
                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "DOCUMENT_CAST_FAILED",
                        ErrorCategory = "PARSING",
                        DebugNotes = "Document could not be cast to IHtmlDocument"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "DOCUMENT_CAST_FAILED"
                };
            }

            var parsingStopwatch = Stopwatch.StartNew();
            var productName = ExtractProductName(htmlDocument, command.Selectors.ProductNameSelector);
            var price = ExtractPrice(htmlDocument, command.Selectors.PriceSelector);
            var stockStatus = ExtractStockStatus(htmlDocument, command.Selectors.StockSelector);
            var sellerNameOnPage = ExtractSellerName(htmlDocument, command.Selectors.SellerNameOnPageSelector);
            parsingStopwatch.Stop();

            // Get HTML snippet for debugging (first 1000 characters)
            var htmlSnippet = htmlDocument.DocumentElement?.OuterHtml?.Substring(0, Math.Min(1000, htmlDocument.DocumentElement.OuterHtml.Length));

            if (price == null)
            {
                var errorMessage = "Could not extract price from the page";
                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "PRICE_EXTRACTION_FAILED",
                        ErrorCategory = "SELECTOR",
                        ParsingTime = parsingStopwatch.Elapsed,
                        RawHtmlSnippet = htmlSnippet,
                        DebugNotes = $"Price selector '{command.Selectors.PriceSelector}' did not match any elements"
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "PRICE_EXTRACTION_FAILED"
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

    private string? ExtractProductName(IHtmlDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            return element?.TextContent?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract product name using selector {Selector}", selector);
            return null;
        }
    }

    private decimal? ExtractPrice(IHtmlDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            if (element == null)
                return null;

            var priceText = element.TextContent?.Trim();
            if (string.IsNullOrEmpty(priceText))
                return null;

            // Remove currency symbols and extract numeric value
            var priceMatch = Regex.Match(priceText, @"[\d,]+\.?\d*");
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

    private string? ExtractStockStatus(IHtmlDocument document, string selector)
    {
        try
        {
            var element = document.QuerySelector(selector);
            var stockText = element?.TextContent?.Trim();

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

    private string? ExtractSellerName(IHtmlDocument document, string? selector)
    {
        if (string.IsNullOrEmpty(selector))
            return null;

        try
        {
            var element = document.QuerySelector(selector);
            return element?.TextContent?.Trim();
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
