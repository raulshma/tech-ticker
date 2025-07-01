using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;

namespace TechTicker.ScrapingWorker.Services;

/// <summary>
/// HTTP client service that intelligently uses proxies for web scraping
/// If no active proxies are available in the pool, requests will be made via direct connection
/// </summary>
public class ProxyAwareHttpClientService
{
    private readonly IProxyPoolService _proxyPoolService;
    private readonly ILogger<ProxyAwareHttpClientService> _logger;
    private readonly ProxyPoolConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _encryptionKey;

    // Supported proxy types
    private static readonly string[] SupportedProxyTypes = { "HTTP", "HTTPS", "SOCKS4", "SOCKS5" };

    public ProxyAwareHttpClientService(
        IProxyPoolService proxyPoolService,
        ILogger<ProxyAwareHttpClientService> logger,
        IOptions<ProxyPoolConfiguration> config,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _proxyPoolService = proxyPoolService;
        _logger = logger;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _encryptionKey = configuration["AiConfiguration:EncryptionKey"] ?? throw new InvalidOperationException("Encryption key not found in configuration");
    }

    /// <summary>
    /// Perform HTTP GET request with intelligent proxy usage
    /// If no active proxies are available in the pool, the request will be made via direct connection
    /// </summary>
    public async Task<ProxyAwareHttpResponse> GetAsync(string url, Dictionary<string, string>? headers = null, string? userAgent = null)
    {
        return await GetInternalAsync(url, headers, userAgent, false);
    }

    /// <summary>
    /// Perform HTTP GET request for binary content with intelligent proxy usage
    /// If no active proxies are available in the pool, the request will be made via direct connection
    /// </summary>
    public async Task<ProxyAwareHttpResponse> GetBinaryAsync(string url, Dictionary<string, string>? headers = null, string? userAgent = null)
    {
        return await GetInternalAsync(url, headers, userAgent, true);
    }

    /// <summary>
    /// Check if there are any active proxies available in the pool
    /// </summary>
    public async Task<bool> HasActiveProxiesAsync()
    {
        if (!_proxyPoolService.IsProxyPoolEnabled)
        {
            return false;
        }

        try
        {
            var proxy = await _proxyPoolService.GetNextProxyAsync();
            return proxy != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking for active proxies");
            return false;
        }
    }

    /// <summary>
    /// Get proxy pool statistics
    /// </summary>
    public async Task<ProxyPoolStatsDto> GetProxyPoolStatsAsync()
    {
        return await _proxyPoolService.GetPoolStatsAsync();
    }

    /// <summary>
    /// Check if a proxy type is supported
    /// </summary>
    public static bool IsProxyTypeSupported(string? proxyType)
    {
        if (string.IsNullOrWhiteSpace(proxyType))
            return false;
            
        return SupportedProxyTypes.Contains(proxyType.ToUpperInvariant());
    }

    /// <summary>
    /// Get list of supported proxy types
    /// </summary>
    public static string[] GetSupportedProxyTypes()
    {
        return SupportedProxyTypes.ToArray();
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
                        _logger.LogInformation("No active proxies available in pool, making direct request to {Url}", url);
                    }
                }
                else
                {
                    _logger.LogDebug("Proxy pool is disabled, making direct request to {Url}", url);
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
                    var connectionType = proxy != null ? $"proxy {proxy.Host}:{proxy.Port}" : "direct connection";
                    _logger.LogDebug("Successfully fetched {Url} in {ElapsedMs}ms using {ConnectionType}", 
                        url, stopwatch.ElapsedMilliseconds, connectionType);
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

                // Enhanced error logging for proxy issues
                if (proxy != null)
                {
                    _logger.LogError(ex, "Error making HTTP request to {Url} via proxy {ProxyHost}:{ProxyPort} (attempt {Attempt}/{MaxRetries}). Error: {ErrorMessage}", 
                        url, proxy.Host, proxy.Port, retryCount + 1, maxRetries + 1, ex.Message);
                    
                    // Log specific proxy tunnel errors
                    if (ex.Message.Contains("proxy tunnel") || ex.Message.Contains("400"))
                    {
                        _logger.LogWarning("Proxy tunnel error detected for {ProxyHost}:{ProxyPort}. This may indicate proxy configuration issues or proxy server problems.", 
                            proxy.Host, proxy.Port);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error making HTTP request to {Url} via direct connection (attempt {Attempt}/{MaxRetries})", 
                        url, retryCount + 1, maxRetries + 1);
                }

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

            _logger.LogDebug("Created HTTP client for direct connection with timeout {Timeout}s", _config.RequestTimeoutSeconds);
            return client;
        }

        // Validate proxy configuration
        if (string.IsNullOrEmpty(proxy.Host) || proxy.Port <= 0 || proxy.Port > 65535)
        {
            _logger.LogWarning("Invalid proxy configuration: Host={Host}, Port={Port}. Falling back to direct connection.", 
                proxy.Host, proxy.Port);
            
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);

            if (!string.IsNullOrEmpty(userAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }

            return client;
        }

        // Validate proxy type
        if (!IsProxyTypeSupported(proxy.ProxyType))
        {
            _logger.LogWarning("Unsupported proxy type '{ProxyType}' for {ProxyHost}:{ProxyPort}. Supported types: {SupportedTypes}. Falling back to direct connection.", 
                proxy.ProxyType ?? "NULL", proxy.Host, proxy.Port, string.Join(", ", SupportedProxyTypes));
            
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
            // Configure proxy based on type
            if (proxy.ProxyType.Equals("SOCKS5", StringComparison.OrdinalIgnoreCase))
            {
                // .NET 6+ native SOCKS5 support
                var socksProxy = new WebProxy($"socks5://{proxy.Host}:{proxy.Port}");

                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    var decryptedPassword = EncryptionUtilities.DecryptString(proxy.Password, _encryptionKey);
                    socksProxy.Credentials = new NetworkCredential(proxy.Username, decryptedPassword);
                }

                handler.Proxy = socksProxy;
                handler.UseProxy = true;

                _logger.LogDebug("Configured SOCKS5 proxy for {ProxyHost}:{ProxyPort}", proxy.Host, proxy.Port);
            }
            else if (proxy.ProxyType.Equals("SOCKS4", StringComparison.OrdinalIgnoreCase))
            {
                // .NET 6+ native SOCKS4 support
                var socksProxy = new WebProxy($"socks4://{proxy.Host}:{proxy.Port}");

                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    var decryptedPassword = EncryptionUtilities.DecryptString(proxy.Password, _encryptionKey);
                    socksProxy.Credentials = new NetworkCredential(proxy.Username, decryptedPassword);
                }

                handler.Proxy = socksProxy;
                handler.UseProxy = true;

                _logger.LogDebug("Configured SOCKS4 proxy for {ProxyHost}:{ProxyPort}", proxy.Host, proxy.Port);
            }
            else if (proxy.ProxyType.Equals("HTTPS", StringComparison.OrdinalIgnoreCase))
            {
                // For HTTPS proxies, use the https:// scheme
                var proxyUri = new Uri($"https://{proxy.Host}:{proxy.Port}");
                var webProxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    var decryptedPassword = EncryptionUtilities.DecryptString(proxy.Password, _encryptionKey);
                    webProxy.Credentials = new NetworkCredential(proxy.Username, decryptedPassword);
                }

                handler.Proxy = webProxy;
                handler.UseProxy = true;

                // Configure SSL/TLS for HTTPS proxies
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    // For HTTPS proxies, we may need to be more lenient with certificate validation
                    // This allows self-signed certificates and other common proxy certificate issues
                    _logger.LogDebug("SSL certificate validation for HTTPS proxy {ProxyHost}:{ProxyPort} - Policy errors: {SslPolicyErrors}", 
                        proxy.Host, proxy.Port, sslPolicyErrors);
                    return true; // Accept all certificates for proxy connections
                };

                _logger.LogDebug("Configured HTTPS proxy for {ProxyHost}:{ProxyPort} using URI {ProxyUri}", 
                    proxy.Host, proxy.Port, proxyUri);
            }
            else if (proxy.ProxyType.Equals("HTTP", StringComparison.OrdinalIgnoreCase))
            {
                // For HTTP proxies, use the http:// scheme
                var proxyUri = new Uri($"http://{proxy.Host}:{proxy.Port}");
                var webProxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    var decryptedPassword = EncryptionUtilities.DecryptString(proxy.Password, _encryptionKey);
                    webProxy.Credentials = new NetworkCredential(proxy.Username, decryptedPassword);
                }

                handler.Proxy = webProxy;
                handler.UseProxy = true;

                _logger.LogDebug("Configured HTTP proxy for {ProxyHost}:{ProxyPort} using URI {ProxyUri}", 
                    proxy.Host, proxy.Port, proxyUri);
            }
            else
            {
                // Unknown proxy type - log warning and fall back to HTTP
                _logger.LogWarning("Unknown proxy type '{ProxyType}' for {ProxyHost}:{ProxyPort}. Falling back to HTTP proxy.", 
                    proxy.ProxyType, proxy.Host, proxy.Port);
                
                var proxyUri = new Uri($"http://{proxy.Host}:{proxy.Port}");
                var webProxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxy.Username) && !string.IsNullOrEmpty(proxy.Password))
                {
                    var decryptedPassword = EncryptionUtilities.DecryptString(proxy.Password, _encryptionKey);
                    webProxy.Credentials = new NetworkCredential(proxy.Username, decryptedPassword);
                }

                handler.Proxy = webProxy;
                handler.UseProxy = true;

                _logger.LogDebug("Configured fallback HTTP proxy for {ProxyHost}:{ProxyPort} using URI {ProxyUri}", 
                    proxy.Host, proxy.Port, proxyUri);
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
            _logger.LogError(ex, "Error creating proxy client for {ProxyHost}:{ProxyPort}. Error: {ErrorMessage}", 
                proxy.Host, proxy.Port, ex.Message);
            handler.Dispose();

            // Fallback to direct connection
            _logger.LogInformation("Falling back to direct connection due to proxy configuration error");
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
