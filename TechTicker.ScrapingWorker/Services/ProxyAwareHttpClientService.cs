using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// HTTP client service that intelligently uses proxies for web scraping
/// </summary>
public class ProxyAwareHttpClientService
{
    private readonly IProxyPoolService _proxyPoolService;
    private readonly ILogger<ProxyAwareHttpClientService> _logger;
    private readonly ProxyPoolConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyAwareHttpClientService(
        IProxyPoolService proxyPoolService,
        ILogger<ProxyAwareHttpClientService> logger,
        IOptions<ProxyPoolConfiguration> config,
        IHttpClientFactory httpClientFactory)
    {
        _proxyPoolService = proxyPoolService;
        _logger = logger;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Perform HTTP GET request with intelligent proxy usage
    /// </summary>
    public async Task<ProxyAwareHttpResponse> GetAsync(string url, Dictionary<string, string>? headers = null, string? userAgent = null)
    {
        return await GetInternalAsync(url, headers, userAgent, false);
    }

    /// <summary>
    /// Perform HTTP GET request for binary content with intelligent proxy usage
    /// </summary>
    public async Task<ProxyAwareHttpResponse> GetBinaryAsync(string url, Dictionary<string, string>? headers = null, string? userAgent = null)
    {
        return await GetInternalAsync(url, headers, userAgent, true);
    }

    private async Task<ProxyAwareHttpResponse> GetInternalAsync(string url, Dictionary<string, string>? headers, string? userAgent, bool isBinary)
    {
        var response = new ProxyAwareHttpResponse
        {
            Url = url,
            RequestStartTime = DateTimeOffset.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();
        var retryCount = 0;
        var maxRetries = _config.MaxRetries;

        while (retryCount <= maxRetries)
        {
            ProxyConfiguration? proxy = null;
            HttpClient? httpClient = null;

            try
            {
                // Get proxy if pool is enabled
                if (_proxyPoolService.IsProxyPoolEnabled)
                {
                    proxy = await _proxyPoolService.GetNextProxyAsync();
                    if (proxy != null)
                    {
                        response.ProxyUsed = $"{proxy.Host}:{proxy.Port}";
                        response.ProxyId = proxy.ProxyConfigurationId;
                        _logger.LogDebug("Using proxy {ProxyHost}:{ProxyPort} for request to {Url}", 
                            proxy.Host, proxy.Port, url);
                    }
                    else
                    {
                        _logger.LogWarning("No available proxies, making direct request to {Url}", url);
                    }
                }

                // Create HTTP client with or without proxy
                httpClient = CreateHttpClient(proxy, userAgent);

                // Add custom headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.Remove(header.Key);
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                // Make the request
                var httpResponse = await httpClient.GetAsync(url);
                stopwatch.Stop();

                response.IsSuccess = httpResponse.IsSuccessStatusCode;
                response.StatusCode = (int)httpResponse.StatusCode;

                if (isBinary)
                {
                    response.BinaryContent = await httpResponse.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    response.Content = await httpResponse.Content.ReadAsStringAsync();
                }

                response.ResponseTime = stopwatch.Elapsed;
                response.Headers = httpResponse.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

                // Add content headers
                foreach (var header in httpResponse.Content.Headers)
                {
                    response.Headers[header.Key] = string.Join(", ", header.Value);
                }

                // Record proxy success if used
                if (proxy != null)
                {
                    await _proxyPoolService.RecordProxySuccessAsync(proxy.ProxyConfigurationId, (int)stopwatch.ElapsedMilliseconds);
                }

                if (response.IsSuccess)
                {
                    _logger.LogDebug("Successfully fetched {Url} in {ElapsedMs}ms using {ProxyInfo}", 
                        url, stopwatch.ElapsedMilliseconds, proxy != null ? $"proxy {proxy.Host}:{proxy.Port}" : "direct connection");
                    return response;
                }
                else
                {
                    _logger.LogWarning("HTTP request failed with status {StatusCode} for {Url}", 
                        httpResponse.StatusCode, url);
                    
                    // Record proxy failure if used
                    if (proxy != null)
                    {
                        await _proxyPoolService.RecordProxyFailureAsync(
                            proxy.ProxyConfigurationId, 
                            $"HTTP {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}", 
                            "HTTP_ERROR");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.ErrorMessage = ex.Message;
                response.ResponseTime = stopwatch.Elapsed;

                _logger.LogError(ex, "Error making HTTP request to {Url} (attempt {Attempt}/{MaxRetries})", 
                    url, retryCount + 1, maxRetries + 1);

                // Record proxy failure if used
                if (proxy != null)
                {
                    await _proxyPoolService.RecordProxyFailureAsync(
                        proxy.ProxyConfigurationId, 
                        ex.Message, 
                        ex.GetType().Name);
                }
            }
            finally
            {
                httpClient?.Dispose();
            }

            retryCount++;
            if (retryCount <= maxRetries)
            {
                var delay = _config.RetryDelayMs * retryCount; // Exponential backoff
                _logger.LogDebug("Retrying request to {Url} in {Delay}ms (attempt {Attempt}/{MaxRetries})", 
                    url, delay, retryCount + 1, maxRetries + 1);
                await Task.Delay(delay);
                stopwatch.Restart();
            }
        }

        response.IsSuccess = false;
        response.ErrorMessage = response.ErrorMessage ?? "Max retries exceeded";
        return response;
    }

    private HttpClient CreateHttpClient(ProxyConfiguration? proxy, string? userAgent)
    {
        if (proxy == null)
        {
            // Create direct connection client
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
            
            return client;
        }

        // Create proxy-enabled client
        var handler = new HttpClientHandler();
        
        try
        {
            var proxyUri = new Uri($"{proxy.ProxyType.ToLower()}://{proxy.Host}:{proxy.Port}");
            handler.Proxy = new WebProxy(proxyUri);
            
            if (!string.IsNullOrEmpty(proxy.Username))
            {
                handler.Proxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
            }

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(proxy.TimeoutSeconds);
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
            
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating proxy client for {ProxyHost}:{ProxyPort}", proxy.Host, proxy.Port);
            handler.Dispose();
            
            // Fallback to direct connection
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
            
            return client;
        }
    }
}

/// <summary>
/// Response from proxy-aware HTTP request
/// </summary>
public class ProxyAwareHttpResponse
{
    public string Url { get; set; } = null!;
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Content { get; set; }
    public byte[]? BinaryContent { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTimeOffset RequestStartTime { get; set; }
    public string? ProxyUsed { get; set; }
    public Guid? ProxyId { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}
