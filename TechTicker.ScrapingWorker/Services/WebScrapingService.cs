using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.ScrapingWorker.Services;
using Microsoft.Playwright;
using Microsoft.Extensions.Caching.Memory;
using TechTicker.Shared.Utilities.Html;
using System.Text.Json;
using TechTicker.DataAccess.Repositories.Interfaces;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// Service for scraping product data from web pages
/// </summary>
public partial class WebScrapingService
{
    private readonly ILogger<WebScrapingService> _logger;
    private readonly ProxyAwareHttpClientService _proxyHttpClient;
    private readonly IScraperRunLogService _scraperRunLogService;
    private readonly IImageScrapingService _imageScrapingService;
    private readonly ITableParser _tableParser;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpecificationNormalizer _specNormalizer;

    [GeneratedRegex(@"[\d,]+\.?\d*")]
    private static partial Regex PriceRegex();

    public WebScrapingService(
        ILogger<WebScrapingService> logger,
        ProxyAwareHttpClientService proxyHttpClient,
        IScraperRunLogService scraperRunLogService,
        IImageScrapingService imageScrapingService,
        ITableParser tableParser,
        IMemoryCache cache,
        IUnitOfWork unitOfWork,
        ISpecificationNormalizer specNormalizer)
    {
        _logger = logger;
        _proxyHttpClient = proxyHttpClient;
        _scraperRunLogService = scraperRunLogService;
        _imageScrapingService = imageScrapingService;
        _tableParser = tableParser;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _specNormalizer = specNormalizer;
    }

    public async Task<ScrapingResult> ScrapeProductPageAsync(TechTicker.Application.Messages.ScrapeProductPageCommand command)
    {
        if (command.RequiresBrowserAutomation)
        {
            return await ScrapeWithBrowserAutomationAsync(command);
        }

        var overallStopwatch = Stopwatch.StartNew();
        Guid? runId = null;
        ProxyAwareHttpResponse? proxyResponse = null;

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
                    SellerNameOnPageSelector = command.Selectors.SellerNameOnPageSelector,
                    ImageSelector = command.Selectors.ImageSelector
                },
                AttemptNumber = 1,
                DebugNotes = "Starting scraping operation"
            };

            var createResult = await _scraperRunLogService.CreateRunLogAsync(createLogDto);
            if (createResult.IsSuccess)
            {
                runId = createResult.Data;
            }

            // Load the page with timing using proxy-aware HTTP client
            var pageLoadStopwatch = Stopwatch.StartNew();
            proxyResponse = await _proxyHttpClient.GetAsync(
                command.ExactProductUrl,
                command.ScrapingProfile.Headers,
                command.ScrapingProfile.UserAgent);
            pageLoadStopwatch.Stop();

            if (!proxyResponse.IsSuccess)
            {
                var errorMessage = $"Failed to load page: {proxyResponse.ErrorMessage ?? $"HTTP {proxyResponse.StatusCode}"}";

                if (runId.HasValue)
                {
                    await _scraperRunLogService.FailRunAsync(runId.Value, new FailScraperRunDto
                    {
                        ErrorMessage = errorMessage,
                        ErrorCode = "HTTP_REQUEST_FAILED",
                        ErrorCategory = "NETWORK",
                        PageLoadTime = pageLoadStopwatch.Elapsed,
                        DebugNotes = $"Proxy used: {proxyResponse.ProxyUsed ?? "Direct connection"}. Status: {proxyResponse.StatusCode}",
                        ProxyUsed = proxyResponse.ProxyUsed,
                        ProxyId = proxyResponse.ProxyId
                    });
                }

                return new ScrapingResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ErrorCode = "HTTP_REQUEST_FAILED"
                };
            }

            var htmlContent = proxyResponse.Content!;

            // Parse HTML with AngleSharp
            var config = AngleSharp.Configuration.Default;
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(htmlContent));

            // Update log with page load timing
            if (runId.HasValue)
            {
                await _scraperRunLogService.UpdateRunLogAsync(runId.Value, new UpdateScraperRunLogDto
                {
                    PageLoadTime = pageLoadStopwatch.Elapsed,
                    DebugNotes = $"Page loaded in {pageLoadStopwatch.ElapsedMilliseconds}ms. Status: {proxyResponse.StatusCode}. Proxy: {proxyResponse.ProxyUsed ?? "Direct connection"}",
                    ProxyUsed = proxyResponse.ProxyUsed,
                    ProxyId = proxyResponse.ProxyId
                });
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
                        DebugNotes = "HTML content was null or whitespace",
                        ProxyUsed = proxyResponse.ProxyUsed,
                        ProxyId = proxyResponse.ProxyId
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

            // Scrape images if selector is provided
            ImageScrapingResult? imageResult = null;
            if (!string.IsNullOrEmpty(command.Selectors.ImageSelector))
            {
                try
                {
                    imageResult = await _imageScrapingService.ScrapeImagesAsync(
                        document,
                        command.Selectors.ImageSelector,
                        command.ExactProductUrl,
                        command.CanonicalProductId);

                    _logger.LogInformation("Image scraping completed: {SuccessCount} images uploaded",
                        imageResult.SuccessfulUploads);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Image scraping failed but continuing with product scraping");
                }
            }

            // Scrape specifications if enabled and selector is provided
            ProductSpecificationResult? specificationResult = null;
            if (command.ScrapeSpecifications && !string.IsNullOrEmpty(command.Selectors.SpecificationTableSelector))
            {
                var specStopwatch = Stopwatch.StartNew();
                try
                {
                    specificationResult = await ScrapeProductSpecificationsAsync(
                        document, 
                        command.Selectors.SpecificationTableSelector,
                        command.Selectors.SpecificationContainerSelector,
                        command.Selectors.SpecificationOptions,
                        command.MappingId);
                    
                    specStopwatch.Stop();
                    specificationResult.ParsingTimeMs = specStopwatch.ElapsedMilliseconds;

                    _logger.LogInformation("Specification scraping completed for mapping {MappingId}: {SpecCount} specifications parsed",
                        command.MappingId, specificationResult.Specifications?.Count ?? 0);

                    // Update ProductSellerMapping with specification data if successful
                    if (specificationResult.IsSuccess)
                    {
                        await UpdateProductSellerMappingWithSpecifications(command.MappingId, specificationResult);
                        if (runId.HasValue)
                        {
                            await UpdateRunLogWithSpecifications(runId.Value, specificationResult);
                        }
                    }

                    // Note: Normalization is now handled in UpdateProductSellerMappingWithSpecifications
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Specification scraping failed for mapping {MappingId}", command.MappingId);
                    specificationResult = new ProductSpecificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        ParsingTimeMs = specStopwatch.ElapsedMilliseconds
                    };
                }
            }

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
                            : $"Price selector '{command.Selectors.PriceSelector}' did not match any elements",
                        ProxyUsed = proxyResponse.ProxyUsed,
                        ProxyId = proxyResponse.ProxyId
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
                    ExtractedPrimaryImageUrl = imageResult?.PrimaryImageUrl,
                    ExtractedAdditionalImageUrls = imageResult?.AdditionalImageUrls,
                    ExtractedOriginalImageUrls = imageResult?.OriginalImageUrls,
                    ImageProcessingCount = imageResult?.ProcessedCount,
                    ImageUploadCount = imageResult?.SuccessfulUploads,
                    ImageScrapingError = imageResult?.ErrorMessage,
                    PageLoadTime = pageLoadStopwatch.Elapsed,
                    ParsingTime = parsingStopwatch.Elapsed,
                    RawHtmlSnippet = htmlSnippet,
                    DebugNotes = $"Successfully extracted all data in {overallStopwatch.ElapsedMilliseconds}ms",
                    ProxyUsed = proxyResponse.ProxyUsed,
                    ProxyId = proxyResponse.ProxyId,
                    // Specification data
                    SpecificationData = specificationResult?.IsSuccess == true ? 
                        JsonSerializer.Serialize(specificationResult.Specifications) : null,
                    SpecificationMetadata = specificationResult != null ? 
                        JsonSerializer.Serialize(specificationResult.Metadata) : null,
                    SpecificationCount = specificationResult?.Specifications?.Count,
                    SpecificationParsingStrategy = specificationResult?.Metadata?.Structure.ToString(),
                    SpecificationQualityScore = specificationResult?.Quality?.OverallScore,
                    SpecificationParsingTime = specificationResult?.ParsingTimeMs,
                    SpecificationError = specificationResult?.IsSuccess == true ? null : specificationResult?.ErrorMessage
                });
            }

            var result = new ScrapingResult
            {
                IsSuccess = true,
                ProductName = productName,
                Price = price.Value,
                StockStatus = stockStatus ?? "Unknown",
                SellerNameOnPage = sellerNameOnPage,
                ScrapedAt = DateTimeOffset.UtcNow,
                PrimaryImageUrl = imageResult?.PrimaryImageUrl,
                AdditionalImageUrls = imageResult?.AdditionalImageUrls ?? new List<string>(),
                OriginalImageUrls = imageResult?.OriginalImageUrls ?? new List<string>(),
                Specifications = specificationResult
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
                    DebugNotes = SanitizeString($"Exception occurred after {overallStopwatch.ElapsedMilliseconds}ms: {ex.GetType().Name}"),
                    ProxyUsed = proxyResponse?.ProxyUsed,
                    ProxyId = proxyResponse?.ProxyId
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
            if (element == null)
                return "In Stock"; // Assuming default to "In Stock" if element is not found

            var rawStockText = element.TextContent?.Trim();
            var stockText = SanitizeString(rawStockText);

            if (string.IsNullOrEmpty(stockText))
                return "Unknown"; // Return "Unknown" if stockText is null or whitespace

            // Normalize stock status to handle different cases and edge cases
            var lowerStock = stockText.ToLower();

            // Check for common stock status phrases
            if (lowerStock.Contains("in stock") || lowerStock.Contains("available") || lowerStock.Contains("in-stock"))
                return "In Stock";

            if (lowerStock.Contains("out of stock") || lowerStock.Contains("unavailable") || lowerStock.Contains("sold out") || lowerStock == "unavailable")
                return "Out of Stock";

            if (lowerStock.Contains("limited") || lowerStock.Contains("few left"))
                return "Limited Stock";

            // Return original text if no pattern matches to ensure all cases are handled
            return stockText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract stock status using selector {Selector}", selector);
            return "Unknown"; // Return "Unknown" in case of an exception
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

    private async Task<ProductSpecificationResult> ScrapeProductSpecificationsAsync(
        IDocument document,
        string tableSelector,
        string? containerSelector,
        TechTicker.Application.DTOs.SpecificationParsingOptions? options,
        Guid mappingId)
    {
        try
        {
            _logger.LogInformation("Starting specification parsing for mapping {MappingId}", mappingId);

            // Find specification tables
            var tables = string.IsNullOrEmpty(containerSelector)
                ? document.QuerySelectorAll(tableSelector)
                : document.QuerySelector(containerSelector)?.QuerySelectorAll(tableSelector);

            if (tables == null || !tables.Any())
            {
                return new ProductSpecificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No specification tables found with the provided selector"
                };
            }

            // Convert to HTML and parse with HtmlUtilities
            var htmlContent = string.Join("\n", tables.Select(t => t.OuterHtml));
            
            var parsingOptions = new ParsingOptions
            {
                EnableCaching = options?.EnableCaching ?? true,
                ThrowOnError = options?.ThrowOnError ?? false,
                MaxCacheEntries = options?.MaxCacheEntries ?? 1000,
                CacheExpiry = options?.CacheExpiry ?? TimeSpan.FromHours(24)
            };

            var parseResult = await _tableParser.ParseAsync(htmlContent, parsingOptions);

            if (!parseResult.Success || !parseResult.Data.Any())
            {
                return new ProductSpecificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to parse specification tables",
                    Metadata = new ParseMetadata
                    {
                        Warnings = parseResult.Warnings,
                        ProcessingTimeMs = (long)parseResult.ProcessingTime.TotalMilliseconds
                    }
                };
            }

            // Merge all parsed specifications
            var mergedSpec = MergeSpecifications(parseResult.Data);

            _logger.LogInformation("Successfully parsed {SpecCount} specifications for mapping {MappingId}", 
                mergedSpec.Specifications.Count, mappingId);

            // Normalize specifications using canonical templates
            Dictionary<string, Domain.Entities.Canonical.NormalizedSpecificationValue> normalized = new();
            Dictionary<string, string> uncategorized = new();
            string? categoryName = null;
            
            try
            {
                // Determine product category via mapping
                var mapping = await _unitOfWork.ProductSellerMappings.GetByIdAsync(mappingId);
                if (mapping != null && mapping.CanonicalProductId != Guid.Empty)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(mapping.CanonicalProductId);
                    if (product?.Category != null)
                    {
                        categoryName = product.Category.Name;
                    }
                }

                var rawStringSpecs = mergedSpec.Specifications.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.ToString() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

                (normalized, uncategorized) = _specNormalizer.Normalize(rawStringSpecs, categoryName ?? string.Empty);
                
                _logger.LogInformation("Normalized {RawCount} specifications into {NormalizedCount} canonical and {UncategorizedCount} uncategorized for mapping {MappingId}",
                    rawStringSpecs.Count, normalized.Count, uncategorized.Count, mappingId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Specification normalization failed for mapping {MappingId}; continuing without normalized data", mappingId);
            }

            return new ProductSpecificationResult
            {
                IsSuccess = true,
                Specifications = mergedSpec.Specifications,
                Metadata = mergedSpec.Metadata,
                Quality = mergedSpec.Quality,
                ParsingTimeMs = mergedSpec.Metadata.ProcessingTimeMs,
                NormalizedSpecifications = normalized,
                UncategorizedSpecifications = uncategorized
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing specifications for mapping {MappingId}", mappingId);
            return new ProductSpecificationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private ProductSpecification MergeSpecifications(List<ProductSpecification> specifications)
    {
        if (specifications.Count == 1)
            return specifications[0];

        var merged = new ProductSpecification
        {
            Id = Guid.NewGuid().ToString(),
            Metadata = specifications.First().Metadata,
            Source = specifications.First().Source
        };

        foreach (var spec in specifications)
        {
            // Merge specifications - this is the main data we care about
            foreach (var kvp in spec.Specifications)
            {
                if (!merged.Specifications.ContainsKey(kvp.Key))
                    merged.Specifications[kvp.Key] = kvp.Value;
            }
        }

        // Calculate merged quality metrics
        merged.Quality = new QualityMetrics
        {
            OverallScore = specifications.Average(s => s.Quality.OverallScore),
            StructureConfidence = specifications.Average(s => s.Quality.StructureConfidence),
            TypeDetectionAccuracy = specifications.Average(s => s.Quality.TypeDetectionAccuracy),
            CompletenessScore = specifications.Average(s => s.Quality.CompletenessScore)
        };

        return merged;
    }

    private async Task<ScrapingResult> ScrapeWithBrowserAutomationAsync(TechTicker.Application.Messages.ScrapeProductPageCommand command)
    {
        // Log start
        _logger.LogInformation("[BrowserAutomation] Starting Playwright scraping for mapping {MappingId} at URL {Url}",
            command.MappingId, command.ExactProductUrl);

        Guid? runId = null;
        try
        {
            // Create initial run log entry
            var createLogDto = new CreateScraperRunLogDto
            {
                MappingId = command.MappingId,
                TargetUrl = command.ExactProductUrl,
                UserAgent = command.BrowserAutomationProfile?.UserAgent,
                AdditionalHeaders = command.BrowserAutomationProfile?.Headers,
                Selectors = new ScrapingSelectorsDto
                {
                    ProductNameSelector = command.Selectors.ProductNameSelector,
                    PriceSelector = command.Selectors.PriceSelector,
                    StockSelector = command.Selectors.StockSelector,
                    SellerNameOnPageSelector = command.Selectors.SellerNameOnPageSelector,
                    ImageSelector = command.Selectors.ImageSelector,
                    SpecificationTableSelector = command.Selectors.SpecificationTableSelector,
                    SpecificationContainerSelector = command.Selectors.SpecificationContainerSelector
                },
                AttemptNumber = 1,
                DebugNotes = "Starting browser automation scraping operation"
            };

            var createResult = await _scraperRunLogService.CreateRunLogAsync(createLogDto);
            if (createResult.IsSuccess)
            {
                runId = createResult.Data;
            }

            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browserType = (command.BrowserAutomationProfile?.PreferredBrowser ?? "chromium").ToLower();
            IBrowser browser = browserType switch
            {
                "firefox" => await playwright.Firefox.LaunchAsync(new() { Headless = true }),
                "webkit" => await playwright.Webkit.LaunchAsync(new() { Headless = true }),
                _ => await playwright.Chromium.LaunchAsync(new() { Headless = true })
            };

            // Proxy support
            var contextOptions = new Microsoft.Playwright.BrowserNewContextOptions();
            if (!string.IsNullOrEmpty(command.BrowserAutomationProfile?.ProxyServer))
            {
                contextOptions.Proxy = new Microsoft.Playwright.Proxy
                {
                    Server = command.BrowserAutomationProfile.ProxyServer,
                    Username = command.BrowserAutomationProfile.ProxyUsername,
                    Password = command.BrowserAutomationProfile.ProxyPassword
                };
            }

            // User-Agent and headers
            if (!string.IsNullOrEmpty(command.BrowserAutomationProfile?.UserAgent))
                contextOptions.UserAgent = command.BrowserAutomationProfile.UserAgent;
            if (command.BrowserAutomationProfile?.Headers != null)
                contextOptions.ExtraHTTPHeaders = command.BrowserAutomationProfile.Headers;

            // Timeout
            int timeoutMs = (command.BrowserAutomationProfile?.TimeoutSeconds ?? 30) * 1000;

            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();

            // Go to page
            await page.GotoAsync(command.ExactProductUrl, new() { Timeout = timeoutMs, WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });

            // Perform actions (scroll, click, etc.)
            if (command.BrowserAutomationProfile?.Actions != null)
            {
                foreach (var action in command.BrowserAutomationProfile.Actions)
                {
                    var repeat = action.Repeat ?? 1;
                    for (int i = 0; i < repeat; i++)
                    {
                        switch (action.ActionType.ToLower())
                        {
                            case "scroll":
                                // Scroll down by one viewport
                                await page.EvaluateAsync("window.scrollBy(0, window.innerHeight);");
                                break;
                            case "click":
                                // Click an element by selector
                                if (!string.IsNullOrEmpty(action.Selector))
                                    await page.ClickAsync(action.Selector, new() { Timeout = timeoutMs });
                                break;
                            case "waitforselector":
                                // Wait for a selector to appear
                                if (!string.IsNullOrEmpty(action.Selector))
                                    await page.WaitForSelectorAsync(action.Selector, new() { Timeout = timeoutMs });
                                break;
                            case "type":
                                // Type text into an input (use Locator.FillAsync instead of deprecated TypeAsync)
                                if (!string.IsNullOrEmpty(action.Selector) && action is { Value: string value })
                                    await page.Locator(action.Selector).FillAsync(value, new() { Timeout = timeoutMs });
                                break;
                            case "wait":
                            case "waitfortimeout":
                                // Wait for a specified time (ms)
                                if (action.DelayMs.HasValue)
                                    await page.WaitForTimeoutAsync(action.DelayMs.Value);
                                break;
                            case "screenshot":
                                // Take a screenshot (optional: path)
                                var screenshotPath = !string.IsNullOrEmpty(action.Value) ? action.Value : $"screenshot_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
                                await page.ScreenshotAsync(new() { Path = screenshotPath });
                                break;
                            case "evaluate":
                                // Evaluate custom JS
                                if (!string.IsNullOrEmpty(action.Value))
                                    await page.EvaluateAsync(action.Value);
                                break;
                            case "hover":
                                // Hover over an element
                                if (!string.IsNullOrEmpty(action.Selector))
                                    await page.HoverAsync(action.Selector, new() { Timeout = timeoutMs });
                                break;
                            case "selectoption":
                                // Select an option in a <select>
                                if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                                {
                                    var optionValue = action.Value;
                                    await page.SelectOptionAsync(action.Selector, optionValue);
                                }
                                break;
                            case "setvalue":
                                // Set value of an input (JS)
                                if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                                {
                                    var setValue = action.Value.Replace("'", "\\'");
                                    await page.EvaluateAsync($"document.querySelector('{action.Selector}')?.value = '{setValue}'");
                                }
                                break;
                            default:
                                // Unknown action type
                                _logger.LogWarning("[BrowserAutomation] Unknown action type: {ActionType}", action.ActionType);
                                break;
                        }
                        // Delay after action if specified
                        if (action.DelayMs.HasValue && action.ActionType.ToLower() != "wait" && action.ActionType.ToLower() != "waitfortimeout")
                            await Task.Delay(action.DelayMs.Value);
                    }
                }
            }

            // Wait for selectors if specified
            if (!string.IsNullOrEmpty(command.Selectors.PriceSelector))
                await page.WaitForSelectorAsync(command.Selectors.PriceSelector, new() { Timeout = timeoutMs });

            // Extract data
            var productName = !string.IsNullOrEmpty(command.Selectors.ProductNameSelector)
                ? await page.TextContentAsync(command.Selectors.ProductNameSelector)
                : null;
            var priceText = !string.IsNullOrEmpty(command.Selectors.PriceSelector)
                ? await page.TextContentAsync(command.Selectors.PriceSelector)
                : null;
            var stockStatus = !string.IsNullOrEmpty(command.Selectors.StockSelector)
                ? await page.TextContentAsync(command.Selectors.StockSelector)
                : null;
            var sellerNameOnPage = !string.IsNullOrEmpty(command.Selectors.SellerNameOnPageSelector)
                ? await page.TextContentAsync(command.Selectors.SellerNameOnPageSelector)
                : null;

            // Parse price
            decimal? price = null;
            if (!string.IsNullOrEmpty(priceText))
            {
                var cleaned = new string(priceText.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    price = parsed;
            }

            // Scrape images if selector is provided
            List<string> imageUrls = new();
            if (!string.IsNullOrEmpty(command.Selectors.ImageSelector))
            {
                var imageElements = await page.QuerySelectorAllAsync(command.Selectors.ImageSelector);
                foreach (var img in imageElements)
                {
                    var src = await img.GetAttributeAsync("src");
                    if (!string.IsNullOrEmpty(src)) imageUrls.Add(src);
                }
            }

            // Scrape specifications if enabled and selector is provided
            ProductSpecificationResult? specificationResult = null;
            if (command.ScrapeSpecifications && !string.IsNullOrEmpty(command.Selectors.SpecificationTableSelector))
            {
                try
                {
                    // Get page content for specification parsing
                    var pageContent = await page.ContentAsync();
                    var config = AngleSharp.Configuration.Default;
                    var angleSharpContext = BrowsingContext.New(config);
                    var document = await angleSharpContext.OpenAsync(req => req.Content(pageContent));

                    specificationResult = await ScrapeProductSpecificationsAsync(
                        document,
                        command.Selectors.SpecificationTableSelector,
                        command.Selectors.SpecificationContainerSelector,
                        command.Selectors.SpecificationOptions,
                        command.MappingId);

                    _logger.LogInformation("[BrowserAutomation] Specification scraping completed for mapping {MappingId}: {SpecCount} specifications parsed",
                        command.MappingId, specificationResult.Specifications?.Count ?? 0);

                    // Update ProductSellerMapping with specification data if successful
                    if (specificationResult.IsSuccess)
                    {
                        await UpdateProductSellerMappingWithSpecifications(command.MappingId, specificationResult);
                        if (runId.HasValue)
                        {
                            await UpdateRunLogWithSpecifications(runId.Value, specificationResult);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[BrowserAutomation] Specification scraping failed for mapping {MappingId}", command.MappingId);
                    specificationResult = new ProductSpecificationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                }
            }

            await browser.CloseAsync();

            return new ScrapingResult
            {
                IsSuccess = price.HasValue,
                ProductName = productName,
                Price = price ?? 0,
                StockStatus = stockStatus,
                SellerNameOnPage = sellerNameOnPage,
                ScrapedAt = DateTimeOffset.UtcNow,
                ErrorMessage = price.HasValue ? null : "Could not extract price from the page (browser automation)",
                ErrorCode = price.HasValue ? null : "BROWSER_AUTOMATION_EXTRACTION_FAILED",
                PrimaryImageUrl = imageUrls.FirstOrDefault(),
                AdditionalImageUrls = imageUrls.Skip(1).ToList(),
                OriginalImageUrls = imageUrls,
                Specifications = specificationResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BrowserAutomation] Playwright scraping failed for mapping {MappingId}", command.MappingId);
            return new ScrapingResult
            {
                IsSuccess = false,
                ErrorMessage = $"Browser automation failed: {ex.Message}",
                ErrorCode = "BROWSER_AUTOMATION_ERROR"
            };
        }
    }

    private async Task UpdateRunLogWithSpecifications(Guid runId, ProductSpecificationResult specResult)
    {
        try
        {
            await _scraperRunLogService.UpdateRunLogAsync(runId, new UpdateScraperRunLogDto
            {
                SpecificationData = specResult.IsSuccess ? 
                    JsonSerializer.Serialize(specResult.Specifications) : null,
                SpecificationMetadata = JsonSerializer.Serialize(specResult.Metadata),
                SpecificationCount = specResult.Specifications?.Count,
                SpecificationParsingStrategy = specResult.Metadata?.Structure.ToString(),
                SpecificationQualityScore = specResult.Quality?.OverallScore,
                SpecificationParsingTime = specResult.ParsingTimeMs,
                SpecificationError = specResult.IsSuccess ? null : specResult.ErrorMessage,
                DebugNotes = $"Specification parsing: {(specResult.IsSuccess ? "Success" : "Failed")} - {specResult.Specifications?.Count ?? 0} specs parsed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update run log with specification data for run {RunId}", runId);
        }
    }

    private async Task UpdateProductSellerMappingWithSpecifications(Guid mappingId, ProductSpecificationResult specResult)
    {
        try
        {
            // Get the mapping from the database
            var mapping = await _unitOfWork.ProductSellerMappings.GetByIdAsync(mappingId);
            if (mapping == null)
            {
                _logger.LogWarning("ProductSellerMapping {MappingId} not found for specification update", mappingId);
                return;
            }

            // Update specification-related fields in the mapping
            mapping.LatestSpecifications = JsonSerializer.Serialize(specResult.Specifications);
            mapping.SpecificationsLastUpdated = DateTime.UtcNow;
            mapping.SpecificationsQualityScore = specResult.Quality?.OverallScore;
            mapping.UpdatedAt = DateTimeOffset.UtcNow;

            // Update both the mapping and the canonical product if quality score is good enough
            if (specResult.Quality?.OverallScore >= 0.7 && mapping.CanonicalProductId != Guid.Empty)
            {
                // Get the canonical product
                var product = await _unitOfWork.Products.GetByIdAsync(mapping.CanonicalProductId);
                if (product != null)
                {
                    // Update normalized specifications if available
                    if (specResult.NormalizedSpecifications?.Any() == true)
                    {
                        var existingNormalized = product.NormalizedSpecificationsDict ?? new Dictionary<string, Domain.Entities.Canonical.NormalizedSpecificationValue>();
                        var newNormalized = specResult.NormalizedSpecifications;
                        
                        // Merge normalized specifications, prioritizing newer ones
                        var mergedNormalized = new Dictionary<string, Domain.Entities.Canonical.NormalizedSpecificationValue>(existingNormalized);
                        foreach (var spec in newNormalized)
                        {
                            mergedNormalized[spec.Key] = spec.Value;
                        }

                        product.NormalizedSpecificationsDict = mergedNormalized;
                        
                        _logger.LogInformation("Updated Product {ProductId} normalized specifications with {NewNormalizedCount} new items from mapping {MappingId}",
                            product.ProductId, newNormalized.Count, mappingId);
                    }

                    // Update uncategorized specifications if available
                    if (specResult.UncategorizedSpecifications?.Any() == true)
                    {
                        var existingUncategorized = product.UncategorizedSpecificationsDict ?? new Dictionary<string, string>();
                        var newUncategorized = specResult.UncategorizedSpecifications;
                        
                        // Merge uncategorized specifications, prioritizing newer ones
                        var mergedUncategorized = new Dictionary<string, string>(existingUncategorized);
                        foreach (var spec in newUncategorized)
                        {
                            mergedUncategorized[spec.Key] = spec.Value;
                        }

                        product.UncategorizedSpecificationsDict = mergedUncategorized;
                        
                        _logger.LogInformation("Updated Product {ProductId} uncategorized specifications with {NewUncategorizedCount} new items from mapping {MappingId}",
                            product.ProductId, newUncategorized.Count, mappingId);
                    }

                    product.UpdatedAt = DateTimeOffset.UtcNow;
                    _unitOfWork.Products.Update(product);
                    
                    _logger.LogInformation("Updated Product {ProductId} specifications from mapping {MappingId}",
                        product.ProductId, mappingId);
                }
            }
            else if (specResult.Quality?.OverallScore < 0.7)
            {
                _logger.LogInformation("Skipping product specification update for mapping {MappingId} due to low quality score: {QualityScore}",
                    mappingId, specResult.Quality?.OverallScore ?? 0);
            }

            // Save changes
            _unitOfWork.ProductSellerMappings.Update(mapping);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated ProductSellerMapping {MappingId} with {SpecCount} specifications (quality: {Quality})",
                mappingId, specResult.Specifications?.Count ?? 0, specResult.Quality?.OverallScore ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update ProductSellerMapping with specification data for mapping {MappingId}", mappingId);
        }
    }
}
