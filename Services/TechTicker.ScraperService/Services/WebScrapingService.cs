using Microsoft.Extensions.Options;
using TechTicker.ScraperService.Messages;
using TechTicker.ScraperService.Models;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using System.Net;

namespace TechTicker.ScraperService.Services
{
    /// <summary>
    /// Service for performing web scraping operations
    /// </summary>
    public class WebScrapingService : IWebScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly IHtmlParsingService _htmlParsingService;
        private readonly ILogger<WebScrapingService> _logger;
        private readonly ScrapingSettings _settings;

        public WebScrapingService(
            HttpClient httpClient,
            IHtmlParsingService htmlParsingService,
            ILogger<WebScrapingService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _htmlParsingService = htmlParsingService;
            _logger = logger;
            _settings = configuration.GetSection("ScrapingSettings").Get<ScrapingSettings>() ?? new ScrapingSettings();
        }

        public async Task<Result<ScrapingResult>> ScrapeProductPageAsync(ScrapeProductPageCommand command)
        {
            var result = new ScrapingResult();
            
            try
            {
                _logger.LogInformation("Starting scrape for mapping {MappingId} at URL: {Url}", 
                    command.MappingId, command.ExactProductUrl);

                // Configure HTTP client for this request
                ConfigureHttpClient(command.ScrapingProfile);

                // Add delay to appear more human-like
                await Task.Delay(Random.Shared.Next(1000, 3000));                // Perform HTTP request with retries
                var httpResult = await PerformHttpRequestWithRetries(command.ExactProductUrl);
                if (httpResult.IsFailure)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = httpResult.ErrorMessage;
                    result.ErrorCode = httpResult.ErrorCode;
                    return Result<ScrapingResult>.Success(result);
                }

                var html = httpResult.Data!;
                result.RawHtml = html;

                // Parse HTML to extract product data
                var parseResult = _htmlParsingService.ExtractProductData(html, command.Selectors);
                if (parseResult.IsFailure)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = parseResult.ErrorMessage;
                    result.ErrorCode = parseResult.ErrorCode;
                    return Result<ScrapingResult>.Success(result);
                }

                (string? productName, decimal? price, string? stockStatus) = parseResult.Data;

                // Validate that we got the essential data
                if (!price.HasValue)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Could not extract price from the page";
                    result.ErrorCode = ScrapingErrorCodes.PRICE_NOT_FOUND;
                    return Result<ScrapingResult>.Success(result);
                }

                result.IsSuccess = true;
                result.ProductName = productName;
                result.Price = price;
                result.StockStatus = stockStatus ?? "UNKNOWN";

                _logger.LogInformation("Successfully scraped product - Name: {Name}, Price: {Price}, Stock: {Stock}",
                    productName, price, stockStatus);

                return Result<ScrapingResult>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during scraping for mapping {MappingId}", command.MappingId);
                
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ScrapingErrorCodes.UNKNOWN_ERROR;
                
                return Result<ScrapingResult>.Success(result);
            }
        }

        private void ConfigureHttpClient(ScrapingProfile profile)
        {
            // Clear existing headers
            _httpClient.DefaultRequestHeaders.Clear();

            // Set User-Agent
            _httpClient.DefaultRequestHeaders.Add("User-Agent", profile.UserAgent);

            // Set additional headers from profile
            foreach (var header in profile.Headers)
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add header {Key}: {Value}", header.Key, header.Value);
                }
            }

            // Set common browser headers to appear more legitimate
            if (!profile.Headers.ContainsKey("Accept"))
            {
                _httpClient.DefaultRequestHeaders.Add("Accept", 
                    "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            }

            if (!profile.Headers.ContainsKey("Accept-Language"))
            {
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            }

            if (!profile.Headers.ContainsKey("Accept-Encoding"))
            {
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            }

            if (!profile.Headers.ContainsKey("Connection"))
            {
                _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            }

            // Set timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds);
        }

        private async Task<Result<string>> PerformHttpRequestWithRetries(string url)
        {
            var lastException = new Exception("Unknown error");
            
            for (int attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogDebug("HTTP request attempt {Attempt} for URL: {Url}", attempt, url);

                    var response = await _httpClient.GetAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug("Successfully retrieved content, length: {Length}", content.Length);
                        return Result<string>.Success(content);
                    }

                    var errorCode = DetermineErrorCode(response.StatusCode);
                    var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}";
                    
                    _logger.LogWarning("HTTP request failed with status {StatusCode} for URL: {Url}", 
                        response.StatusCode, url);

                    return Result<string>.Failure(errorMessage, errorCode);
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Request timeout on attempt {Attempt} for URL: {Url}", attempt, url);
                    lastException = ex;
                    
                    if (attempt == _settings.MaxRetryAttempts)
                    {
                        return Result<string>.Failure("Request timeout", ScrapingErrorCodes.TIMEOUT_ERROR);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Network error on attempt {Attempt} for URL: {Url}", attempt, url);
                    lastException = ex;
                    
                    if (attempt == _settings.MaxRetryAttempts)
                    {
                        return Result<string>.Failure("Network error: " + ex.Message, ScrapingErrorCodes.NETWORK_ERROR);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unexpected error on attempt {Attempt} for URL: {Url}", attempt, url);
                    lastException = ex;
                    
                    if (attempt == _settings.MaxRetryAttempts)
                    {
                        return Result<string>.Failure("Unexpected error: " + ex.Message, ScrapingErrorCodes.UNKNOWN_ERROR);
                    }
                }

                if (attempt < _settings.MaxRetryAttempts)
                {
                    var delay = TimeSpan.FromSeconds(_settings.RetryDelaySeconds * attempt);
                    _logger.LogDebug("Waiting {Delay} before retry attempt {NextAttempt}", delay, attempt + 1);
                    await Task.Delay(delay);
                }
            }

            return Result<string>.Failure($"All retry attempts failed: {lastException.Message}", 
                ScrapingErrorCodes.UNKNOWN_ERROR);
        }

        private string DetermineErrorCode(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.TooManyRequests => ScrapingErrorCodes.RATE_LIMITED,
                HttpStatusCode.Forbidden => ScrapingErrorCodes.BLOCKED_BY_CAPTCHA,
                HttpStatusCode.Unauthorized => ScrapingErrorCodes.BLOCKED_BY_CAPTCHA,
                HttpStatusCode.NotFound => ScrapingErrorCodes.HTTP_ERROR,
                HttpStatusCode.InternalServerError => ScrapingErrorCodes.HTTP_ERROR,
                HttpStatusCode.BadGateway => ScrapingErrorCodes.HTTP_ERROR,
                HttpStatusCode.ServiceUnavailable => ScrapingErrorCodes.HTTP_ERROR,
                HttpStatusCode.GatewayTimeout => ScrapingErrorCodes.TIMEOUT_ERROR,
                _ => ScrapingErrorCodes.HTTP_ERROR
            };
        }
    }
}
