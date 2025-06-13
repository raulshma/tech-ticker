using AngleSharp;
using AngleSharp.Html.Dom;
using TechTicker.ScraperService.Messages;
using TechTicker.ScraperService.Models;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace TechTicker.ScraperService.Services
{
    /// <summary>
    /// Service for parsing HTML content and extracting product data
    /// </summary>
    public class HtmlParsingService : IHtmlParsingService
    {
        private readonly ILogger<HtmlParsingService> _logger;        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public HtmlParsingService(ILogger<HtmlParsingService> logger, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public Result<(string? productName, decimal? price, string? stockStatus)> ExtractProductData(
            string html, ScrapingSelectors selectors)
        {
            try
            {
                if (string.IsNullOrEmpty(html))
                {
                    return Result<(string?, decimal?, string?)>.Failure(
                        "HTML content is empty", ScrapingErrorCodes.PARSING_ERROR);
                }                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = (IHtmlDocument)context.OpenAsync(req => req.Content(html)).Result;

                var productName = ExtractProductName(document, selectors.ProductNameSelector);
                var price = ExtractPrice(document, selectors.PriceSelector);
                var stockStatus = ExtractStockStatus(document, selectors.StockSelector);

                _logger.LogDebug("Extracted data - Name: {ProductName}, Price: {Price}, Stock: {StockStatus}",
                    productName, price, stockStatus);

                return Result<(string?, decimal?, string?)>.Success((productName, price, stockStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing HTML content");
                return Result<(string?, decimal?, string?)>.Failure(ex.Message, ScrapingErrorCodes.PARSING_ERROR);
            }
        }

        private string? ExtractProductName(IHtmlDocument document, string selector)
        {
            try
            {
                var element = document.QuerySelector(selector);
                if (element == null)
                {
                    _logger.LogWarning("Product name element not found with selector: {Selector}", selector);
                    return null;
                }

                var name = element.TextContent?.Trim();
                _logger.LogDebug("Extracted product name: {ProductName}", name);
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product name with selector: {Selector}", selector);
                return null;
            }
        }

        private decimal? ExtractPrice(IHtmlDocument document, string selector)
        {
            try
            {
                var element = document.QuerySelector(selector);
                if (element == null)
                {
                    _logger.LogWarning("Price element not found with selector: {Selector}", selector);
                    return null;
                }

                var priceText = element.TextContent?.Trim();
                if (string.IsNullOrWhiteSpace(priceText))
                {
                    _logger.LogWarning("Price text is empty");
                    return null;
                }

                var price = ParsePrice(priceText);
                _logger.LogDebug("Extracted price: {PriceText} -> {Price}", priceText, price);
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting price with selector: {Selector}", selector);
                return null;
            }
        }

        private string? ExtractStockStatus(IHtmlDocument document, string selector)
        {
            try
            {
                var element = document.QuerySelector(selector);
                if (element == null)
                {
                    _logger.LogWarning("Stock status element not found with selector: {Selector}", selector);
                    return null;
                }

                var stockText = element.TextContent?.Trim();
                if (string.IsNullOrWhiteSpace(stockText))
                {
                    _logger.LogWarning("Stock status text is empty");
                    return null;
                }

                var normalizedStock = NormalizeStockStatus(stockText);
                _logger.LogDebug("Extracted stock status: {StockText} -> {NormalizedStock}", stockText, normalizedStock);
                return normalizedStock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting stock status with selector: {Selector}", selector);
                return null;
            }
        }

        private decimal? ParsePrice(string priceText)
        {
            try
            {
                // Remove common currency symbols and whitespace
                var cleanPrice = Regex.Replace(priceText, @"[^\d.,]", "");
                
                // Handle different decimal separators
                if (cleanPrice.Contains(',') && cleanPrice.Contains('.'))
                {
                    // Assume comma is thousands separator if both are present
                    cleanPrice = cleanPrice.Replace(",", "");
                }
                else if (cleanPrice.Contains(','))
                {
                    // Could be decimal separator in some locales
                    var lastCommaIndex = cleanPrice.LastIndexOf(',');
                    var afterComma = cleanPrice.Substring(lastCommaIndex + 1);
                    
                    if (afterComma.Length <= 2)
                    {
                        // Likely decimal separator
                        cleanPrice = cleanPrice.Replace(',', '.');
                    }
                    else
                    {
                        // Likely thousands separator
                        cleanPrice = cleanPrice.Replace(",", "");
                    }
                }

                if (decimal.TryParse(cleanPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                {
                    return price;
                }

                _logger.LogWarning("Could not parse price: {PriceText} -> {CleanPrice}", priceText, cleanPrice);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing price: {PriceText}", priceText);
                return null;
            }
        }

        private string NormalizeStockStatus(string stockText)
        {
            var lowerStock = stockText.ToLowerInvariant();

            if (lowerStock.Contains("in stock") || lowerStock.Contains("available") || 
                lowerStock.Contains("in-stock") || lowerStock.Contains("ready"))
            {
                return "IN_STOCK";
            }

            if (lowerStock.Contains("out of stock") || lowerStock.Contains("unavailable") ||
                lowerStock.Contains("out-of-stock") || lowerStock.Contains("sold out"))
            {
                return "OUT_OF_STOCK";
            }

            if (lowerStock.Contains("limited") || lowerStock.Contains("few left"))
            {
                return "LIMITED_STOCK";
            }

            // Return original text if can't normalize
            return stockText;
        }
    }
}
