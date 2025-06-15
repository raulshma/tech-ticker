using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;
using TechTicker.Application.Messages;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping product data from web pages
/// </summary>
public class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly IBrowsingContext _browsingContext;

    public WebScrapingService(ILogger<WebScrapingService> logger)
    {
        _logger = logger;
        
        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
    }

    public async Task<ScrapingResult> ScrapeProductPageAsync(ScrapeProductPageCommand command)
    {
        try
        {
            _logger.LogInformation("Starting scraping for mapping {MappingId} at URL {Url}", 
                command.MappingId, command.ExactProductUrl);

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

            // Load the page
            var document = await _browsingContext.OpenAsync(req =>
            {
                req.Address(command.ExactProductUrl);
                foreach (var header in requestOptions)
                {
                    req.Header(header.Key, header.Value);
                }
            });

            if (document == null)
            {
                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to load the page",
                    ErrorCode = "PAGE_LOAD_FAILED"
                };
            }

            // Extract product data
            var productName = ExtractProductName(document, command.Selectors.ProductNameSelector);
            var price = ExtractPrice(document, command.Selectors.PriceSelector);
            var stockStatus = ExtractStockStatus(document, command.Selectors.StockSelector);
            var sellerNameOnPage = ExtractSellerName(document, command.Selectors.SellerNameOnPageSelector);

            if (price == null)
            {
                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Could not extract price from the page",
                    ErrorCode = "PRICE_EXTRACTION_FAILED"
                };
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
            _logger.LogError(ex, "Error scraping product page for mapping {MappingId}", command.MappingId);
            
            return new ScrapingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorCode = "SCRAPING_EXCEPTION"
            };
        }
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
