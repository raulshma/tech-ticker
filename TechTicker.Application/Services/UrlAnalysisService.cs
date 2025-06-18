using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for analyzing URLs and extracting product data
/// </summary>
public class UrlAnalysisService : IUrlAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UrlAnalysisService> _logger;
    private readonly IBrowsingContext _browsingContext;
    private readonly ProductDiscoveryOptions _options;

    // Common product page patterns
    private static readonly string[] ProductUrlPatterns = new[]
    {
        @"/product/",
        @"/item/",
        @"/dp/",
        @"/p/",
        @"-p-\d+",
        @"/products/",
        @"/shop/"
    };

    // Supported domains for product extraction
    private static readonly Dictionary<string, SiteConfig> SupportedSites = new()
    {
        {
            "amazon.com", new SiteConfig
            {
                ProductNameSelectors = new[] { "#productTitle", "h1.a-size-large" },
                PriceSelectors = new[] { ".a-price .a-offscreen", ".a-price-whole" },
                ImageSelectors = new[] { "#landingImage", ".a-dynamic-image" },
                DescriptionSelectors = new[] { "#feature-bullets ul", "#productDescription" },
                ManufacturerSelectors = new[] { "#bylineInfo", ".a-color-secondary" }
            }
        },
        {
            "newegg.com", new SiteConfig
            {
                ProductNameSelectors = new[] { "h1.product-title", ".product-title" },
                PriceSelectors = new[] { ".price-current .price-current-num", ".price-current" },
                ImageSelectors = new[] { ".product-view-img-original img", ".mainSlide img" },
                DescriptionSelectors = new[] { ".product-overview", ".product-bullets" },
                ManufacturerSelectors = new[] { ".product-brand", ".grpBrand" }
            }
        },
        {
            "bestbuy.com", new SiteConfig
            {
                ProductNameSelectors = new[] { "h1.heading-5", ".sku-title h1" },
                PriceSelectors = new[] { ".pricing-current-price .sr-only", ".pricing-current-price" },
                ImageSelectors = new[] { ".primary-image img", ".carousel-image img" },
                DescriptionSelectors = new[] { ".product-data-value", ".key-specs" },
                ManufacturerSelectors = new[] { ".product-data-value.body-copy-lg" }
            }
        },
        {
            "vishalperipherals.com", new SiteConfig
            {
                ProductNameSelectors = new[] { "#product-single > div > div.details-info.col-xs-12.col-sm-12.col-md-6.col-lg-6 > div > form > h1" },
                PriceSelectors = new[] { "#js-product-price" },
                ImageSelectors = new[] { ".primary-image img", ".carousel-image img" },
                DescriptionSelectors = new[] { ".product-data-value", ".key-specs" },
                ManufacturerSelectors = new[] { "#product-single > div > div.details-info.col-xs-12.col-sm-12.col-md-6.col-lg-6 > div > div.product_infor > div:nth-child(4) > p > span" }
            }
        }
    };

    public UrlAnalysisService(HttpClient httpClient, IOptions<ProductDiscoveryOptions> options, ILogger<UrlAnalysisService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Configure AngleSharp
        var config = AngleSharp.Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);

        // Configure HttpClient with configurable User-Agent
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UrlAnalysis.UserAgent);
        _httpClient.Timeout = _options.UrlAnalysis.RequestTimeout;
    }

    public async Task<Result<ProductExtractionResult>> ExtractProductDataAsync(string url)
    {
        try
        {
            _logger.LogInformation("Starting product data extraction for URL: {Url}", url);

            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Result<ProductExtractionResult>.Failure("Invalid URL format", "INVALID_URL");
            }

            // Check if site is supported
            var domain = uri.Host.ToLowerInvariant();
            var siteConfig = GetSiteConfig(domain);
            if (siteConfig == null)
            {
                _logger.LogWarning("Unsupported domain: {Domain}", domain);
                return await ExtractGenericProductDataAsync(url);
            }

            // Fetch page content
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return Result<ProductExtractionResult>.Failure(
                    $"Failed to fetch page: HTTP {response.StatusCode}", "HTTP_ERROR");
            }

            var html = await response.Content.ReadAsStringAsync();
            var document = await _browsingContext.OpenAsync(req => req.Content(html));
            var htmlDocument = document as IHtmlDocument;

            // Extract product data using site-specific selectors
            var result = new ProductExtractionResult
            {
                IsSuccess = true,
                SourceUrl = url,
                ExtractedProductName = ExtractText(htmlDocument, siteConfig.ProductNameSelectors),
                ExtractedPrice = ExtractPrice(htmlDocument, siteConfig.PriceSelectors),
                ExtractedImageUrl = ExtractImageUrl(htmlDocument, siteConfig.ImageSelectors, uri),
                ExtractedDescription = ExtractText(htmlDocument, siteConfig.DescriptionSelectors),
                ExtractedManufacturer = ExtractText(htmlDocument, siteConfig.ManufacturerSelectors),
                ExtractedSpecifications = ExtractSpecifications(htmlDocument),
                RawMetadata = ExtractMetadata(htmlDocument)
            };

            // Validate that we extracted meaningful data
            if (string.IsNullOrWhiteSpace(result.ExtractedProductName))
            {
                return Result<ProductExtractionResult>.Failure(
                    "Could not extract product name from page", "EXTRACTION_FAILED");
            }

            _logger.LogInformation("Successfully extracted product data: {ProductName}", result.ExtractedProductName);
            return Result<ProductExtractionResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting product data from URL: {Url}", url);
            return Result<ProductExtractionResult>.Failure(ex);
        }
    }

    public async Task<Result<SiteCompatibilityResult>> AnalyzeSiteCompatibilityAsync(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Result<SiteCompatibilityResult>.Failure("Invalid URL format");
            }

            var domain = uri.Host.ToLowerInvariant();
            var siteConfig = GetSiteConfig(domain);

            var result = new SiteCompatibilityResult
            {
                Domain = domain,
                IsCompatible = siteConfig != null,
                Reason = siteConfig != null ? "Site has configured selectors" : "Site not in supported list",
                SupportedSelectors = siteConfig?.ProductNameSelectors?.ToList() ?? new List<string>()
            };

            // Test basic connectivity
            try
            {
                var response = await _httpClient.GetAsync(url);
                result.SiteMetadata = new Dictionary<string, object>
                {
                    ["StatusCode"] = (int)response.StatusCode,
                    ["ContentType"] = response.Content.Headers.ContentType?.ToString() ?? "unknown",
                    ["IsSuccessful"] = response.IsSuccessStatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    result.IsCompatible = false;
                    result.Reason = $"Site returned HTTP {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.IsCompatible = false;
                result.Reason = $"Connection failed: {ex.Message}";
            }

            return Result<SiteCompatibilityResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing site compatibility for URL: {Url}", url);
            return Result<SiteCompatibilityResult>.Failure(ex);
        }
    }

    public async Task<Result<List<string>>> ExtractProductUrlsFromPageAsync(string catalogUrl)
    {
        try
        {
            _logger.LogInformation("Extracting product URLs from catalog: {Url}", catalogUrl);

            var response = await _httpClient.GetAsync(catalogUrl);
            if (!response.IsSuccessStatusCode)
            {
                return Result<List<string>>.Failure($"Failed to fetch catalog page: HTTP {response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync();
            var document = await _browsingContext.OpenAsync(req => req.Content(html));

            var productUrls = new List<string>();
            var links = document.QuerySelectorAll("a[href]");

            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(href)) continue;

                // Convert relative URLs to absolute
                if (!Uri.TryCreate(new Uri(catalogUrl), href, out var absoluteUrl)) continue;

                var urlString = absoluteUrl.ToString();

                // Check if URL matches product patterns
                if (IsProductUrl(urlString))
                {
                    productUrls.Add(urlString);
                }
            }

            var uniqueUrls = productUrls.Distinct().ToList();
            _logger.LogInformation("Extracted {Count} unique product URLs from catalog", uniqueUrls.Count);

            return Result<List<string>>.Success(uniqueUrls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting product URLs from catalog: {Url}", catalogUrl);
            return Result<List<string>>.Failure(ex);
        }
    }

    public async Task<Result<bool>> IsValidProductUrlAsync(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Result<bool>.Success(false);
            }

            // Check URL patterns
            if (!IsProductUrl(url))
            {
                return Result<bool>.Success(false);
            }

            // Check if domain is supported or at least accessible
            var compatibilityResult = await AnalyzeSiteCompatibilityAsync(url);
            if (compatibilityResult.IsFailure)
            {
                return Result<bool>.Success(false);
            }

            return Result<bool>.Success(compatibilityResult.Data!.IsCompatible);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product URL: {Url}", url);
            return Result<bool>.Failure(ex);
        }
    }

    public Result<string> GetDomainFromUrl(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Result<string>.Failure("Invalid URL format");
            }

            return Result<string>.Success(uri.Host.ToLowerInvariant());
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex);
        }
    }

    private async Task<Result<ProductExtractionResult>> ExtractGenericProductDataAsync(string url)
    {
        try
        {
            _logger.LogInformation("Attempting generic product data extraction for: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return Result<ProductExtractionResult>.Failure($"Failed to fetch page: HTTP {response.StatusCode}");
            }

            var html = await response.Content.ReadAsStringAsync();
            var document = await _browsingContext.OpenAsync(req => req.Content(html));
            var htmlDocument = document as IHtmlDocument;

            // Generic extraction using common patterns
            var result = new ProductExtractionResult
            {
                IsSuccess = true,
                SourceUrl = url,
                ExtractedProductName = ExtractGenericProductName(htmlDocument),
                ExtractedPrice = ExtractGenericPrice(htmlDocument),
                ExtractedImageUrl = ExtractGenericImage(htmlDocument, new Uri(url)),
                ExtractedDescription = ExtractGenericDescription(htmlDocument),
                ExtractedSpecifications = ExtractSpecifications(htmlDocument),
                RawMetadata = ExtractMetadata(htmlDocument)
            };

            if (string.IsNullOrWhiteSpace(result.ExtractedProductName))
            {
                return Result<ProductExtractionResult>.Failure("Could not extract product name");
            }

            return Result<ProductExtractionResult>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ProductExtractionResult>.Failure(ex);
        }
    }

    private static SiteConfig? GetSiteConfig(string domain)
    {
        return SupportedSites.FirstOrDefault(kvp => domain.Contains(kvp.Key)).Value;
    }

    private static bool IsProductUrl(string url)
    {
        return ProductUrlPatterns.Any(pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase));
    }

    private static string? ExtractText(IHtmlDocument document, string[]? selectors)
    {
        if (selectors == null) return null;

        foreach (var selector in selectors)
        {
            var element = document.QuerySelector(selector);
            if (element != null && !string.IsNullOrWhiteSpace(element.TextContent))
            {
                return element.TextContent.Trim();
            }
        }

        return null;
    }

    private static decimal? ExtractPrice(IHtmlDocument document, string[]? selectors)
    {
        if (selectors == null) return null;

        foreach (var selector in selectors)
        {
            var element = document.QuerySelector(selector);
            if (element != null)
            {
                var priceText = element.TextContent.Trim();
                var match = Regex.Match(priceText, @"[\d,]+\.?\d*");
                if (match.Success && decimal.TryParse(match.Value.Replace(",", ""), out var price))
                {
                    return price;
                }
            }
        }

        return null;
    }

    private static string? ExtractImageUrl(IHtmlDocument document, string[]? selectors, Uri baseUri)
    {
        if (selectors == null) return null;

        foreach (var selector in selectors)
        {
            var img = document.QuerySelector(selector) as IHtmlImageElement;
            if (img != null && !string.IsNullOrWhiteSpace(img.Source))
            {
                if (Uri.TryCreate(baseUri, img.Source, out var absoluteUri))
                {
                    return absoluteUri.ToString();
                }
            }
        }

        return null;
    }

    private static Dictionary<string, object>? ExtractSpecifications(IHtmlDocument document)
    {
        var specs = new Dictionary<string, object>();

        // Look for common specification patterns
        var specElements = document.QuerySelectorAll(".spec, .specification, .feature, .detail");
        foreach (var element in specElements)
        {
            var text = element.TextContent?.Trim();
            if (!string.IsNullOrWhiteSpace(text) && text.Contains(':'))
            {
                var parts = text.Split(':', 2);
                if (parts.Length == 2)
                {
                    specs[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        return specs.Any() ? specs : null;
    }

    private static Dictionary<string, string> ExtractMetadata(IHtmlDocument document)
    {
        var metadata = new Dictionary<string, string>();

        // Extract meta tags
        var metaTags = document.QuerySelectorAll("meta[property], meta[name]");
        foreach (var meta in metaTags)
        {
            var property = meta.GetAttribute("property") ?? meta.GetAttribute("name");
            var content = meta.GetAttribute("content");

            if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(content))
            {
                metadata[property] = content;
            }
        }

        // Extract title
        var title = document.QuerySelector("title")?.TextContent;
        if (!string.IsNullOrWhiteSpace(title))
        {
            metadata["title"] = title;
        }

        return metadata;
    }

    private static string? ExtractGenericProductName(IHtmlDocument document)
    {
        // Try common selectors for product names
        var selectors = new[] { "h1", ".product-name", ".product-title", "#product-name", ".title" };
        return ExtractText(document, selectors);
    }

    private static decimal? ExtractGenericPrice(IHtmlDocument document)
    {
        var selectors = new[] { ".price", ".cost", ".amount", "[class*='price']", "[id*='price']" };
        return ExtractPrice(document, selectors);
    }

    private static string? ExtractGenericImage(IHtmlDocument document, Uri baseUri)
    {
        var selectors = new[] { ".product-image img", ".main-image img", "#product-image", ".hero-image img" };
        return ExtractImageUrl(document, selectors, baseUri);
    }

    private static string? ExtractGenericDescription(IHtmlDocument document)
    {
        var selectors = new[] { ".description", ".product-description", "#description", ".overview" };
        return ExtractText(document, selectors);
    }

    private class SiteConfig
    {
        public string[]? ProductNameSelectors { get; set; }
        public string[]? PriceSelectors { get; set; }
        public string[]? ImageSelectors { get; set; }
        public string[]? DescriptionSelectors { get; set; }
        public string[]? ManufacturerSelectors { get; set; }
    }
}