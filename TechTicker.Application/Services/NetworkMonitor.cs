using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Diagnostics;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Implementation for monitoring network activity during browser automation
/// </summary>
public class NetworkMonitor : INetworkMonitor
{
    private readonly ILogger<NetworkMonitor> _logger;
    private readonly NetworkMetricsDto _metrics;
    private readonly ConcurrentBag<NetworkRequestDto> _requestDetails;
    private readonly ConcurrentDictionary<string, RequestTrackingInfo> _requestTimings;
    
    // Thread-safe counters
    private long _requestCount;
    private long _successfulRequestCount;
    private long _failedRequestCount;
    private long _bytesReceived;
    private long _bytesSent;
    
    private IPage? _monitoredPage;
    private bool _isDisposed;

    public NetworkMonitor(ILogger<NetworkMonitor> logger)
    {
        _logger = logger;
        _metrics = new NetworkMetricsDto();
        _requestDetails = new ConcurrentBag<NetworkRequestDto>();
        _requestTimings = new ConcurrentDictionary<string, RequestTrackingInfo>();
    }

    public bool IsMonitoring { get; private set; }

    public void StartMonitoring(IPage page)
    {
        if (IsMonitoring)
        {
            _logger.LogWarning("Network monitoring is already active. Stopping previous monitoring.");
            StopMonitoring();
        }

        _monitoredPage = page;
        _metrics.MonitoringStartTime = DateTimeOffset.UtcNow;
        
        // Attach event handlers
        _monitoredPage.Request += OnRequest;
        _monitoredPage.Response += OnResponse;
        _monitoredPage.RequestFailed += OnRequestFailed;

        IsMonitoring = true;
        
        _logger.LogDebug("Started network monitoring for page: {Url}", page.Url);
    }

    public void StopMonitoring()
    {
        if (!IsMonitoring || _monitoredPage == null)
        {
            return;
        }

        try
        {
            // Detach event handlers
            _monitoredPage.Request -= OnRequest;
            _monitoredPage.Response -= OnResponse;
            _monitoredPage.RequestFailed -= OnRequestFailed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detaching network event handlers");
        }

        _metrics.MonitoringEndTime = DateTimeOffset.UtcNow;
        IsMonitoring = false;

        // Calculate final metrics
        CalculateFinalMetrics();

        _logger.LogDebug("Stopped network monitoring. Total requests: {RequestCount}, Duration: {Duration}ms", 
            Interlocked.Read(ref _requestCount), _metrics.TotalMonitoringTimeMs);
    }

    public NetworkMetricsDto GetNetworkMetrics()
    {
        // Ensure we have up-to-date metrics
        if (IsMonitoring)
        {
            CalculateFinalMetrics();
        }

        // Return a copy to prevent external modification
        return new NetworkMetricsDto
        {
            RequestCount = (int)Interlocked.Read(ref _requestCount),
            SuccessfulRequestCount = (int)Interlocked.Read(ref _successfulRequestCount),
            FailedRequestCount = (int)Interlocked.Read(ref _failedRequestCount),
            BytesReceived = Interlocked.Read(ref _bytesReceived),
            BytesSent = Interlocked.Read(ref _bytesSent),
            AverageResponseTimeMs = _metrics.AverageResponseTimeMs,
            RequestDetails = new List<NetworkRequestDto>(_requestDetails),
            MonitoringStartTime = _metrics.MonitoringStartTime,
            MonitoringEndTime = _metrics.MonitoringEndTime
        };
    }

    public void Reset()
    {
        if (IsMonitoring)
        {
            StopMonitoring();
        }

        Interlocked.Exchange(ref _requestCount, 0);
        Interlocked.Exchange(ref _successfulRequestCount, 0);
        Interlocked.Exchange(ref _failedRequestCount, 0);
        Interlocked.Exchange(ref _bytesReceived, 0);
        Interlocked.Exchange(ref _bytesSent, 0);
        
        _metrics.AverageResponseTimeMs = 0;
        _metrics.MonitoringStartTime = null;
        _metrics.MonitoringEndTime = null;
        
        _requestDetails.Clear();
        _requestTimings.Clear();

        _logger.LogDebug("Reset network monitoring metrics");
    }

    private void OnRequest(object? sender, IRequest request)
    {
        if (_isDisposed) return;

        try
        {
            var requestInfo = new RequestTrackingInfo
            {
                StartTime = DateTimeOffset.UtcNow,
                Method = request.Method,
                Url = request.Url
            };

            _requestTimings.TryAdd(request.Url, requestInfo);

            // Estimate request size (headers + body)
            var requestSize = EstimateRequestSize(request);
            Interlocked.Add(ref _bytesSent, requestSize);

            _logger.LogTrace("Network request started: {Method} {Url}", request.Method, request.Url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling request event for {Url}", request.Url);
        }
    }

    private void OnResponse(object? sender, IResponse response)
    {
        if (_isDisposed) return;

        try
        {
            var url = response.Url;
            var statusCode = response.Status;
            var isSuccess = statusCode >= 200 && statusCode < 400;

            // Update counters
            Interlocked.Increment(ref _requestCount);
            if (isSuccess)
            {
                Interlocked.Increment(ref _successfulRequestCount);
            }
            else
            {
                Interlocked.Increment(ref _failedRequestCount);
            }

            // Calculate response time if we tracked the request
            var duration = 0;
            if (_requestTimings.TryGetValue(url, out var requestInfo))
            {
                duration = (int)(DateTimeOffset.UtcNow - requestInfo.StartTime).TotalMilliseconds;
            }

            // Estimate response size
            var responseSize = EstimateResponseSize(response);
            Interlocked.Add(ref _bytesReceived, responseSize);

            // Create detailed request info
            var requestDetail = new NetworkRequestDto
            {
                Url = url,
                Method = response.Request.Method,
                StatusCode = statusCode,
                StatusText = response.StatusText,
                Headers = GetResponseHeaders(response),
                Duration = duration,
                Size = responseSize,
                Timestamp = DateTimeOffset.UtcNow
            };

            _requestDetails.Add(requestDetail);

            _logger.LogTrace("Network response received: {Method} {Url} -> {StatusCode} ({Duration}ms, {Size} bytes)", 
                response.Request.Method, url, statusCode, duration, responseSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling response event for {Url}", response.Url);
        }
    }

    private void OnRequestFailed(object? sender, IRequest request)
    {
        if (_isDisposed) return;

        try
        {
            // Update counters
            Interlocked.Increment(ref _requestCount);
            Interlocked.Increment(ref _failedRequestCount);

            // Calculate duration if we tracked the request
            var duration = 0;
            if (_requestTimings.TryGetValue(request.Url, out var requestInfo))
            {
                duration = (int)(DateTimeOffset.UtcNow - requestInfo.StartTime).TotalMilliseconds;
            }

            // Create detailed request info for failed request
            var requestDetail = new NetworkRequestDto
            {
                Url = request.Url,
                Method = request.Method,
                StatusCode = 0, // Failed request
                StatusText = "Request Failed",
                Headers = new Dictionary<string, string>(),
                Duration = duration,
                Size = 0,
                Timestamp = DateTimeOffset.UtcNow
            };

            _requestDetails.Add(requestDetail);

            _logger.LogTrace("Network request failed: {Method} {Url} ({Duration}ms)", 
                request.Method, request.Url, duration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling request failed event for {Url}", request.Url);
        }
    }

    private void CalculateFinalMetrics()
    {
        var allRequests = _requestDetails.ToList();
        if (allRequests.Count > 0)
        {
            _metrics.AverageResponseTimeMs = allRequests.Average(r => r.Duration);
        }

        _metrics.RequestDetails = allRequests;
    }

    private static long EstimateRequestSize(IRequest request)
    {
        try
        {
            // Estimate size: URL + headers + post data (if any)
            var urlSize = System.Text.Encoding.UTF8.GetByteCount(request.Url);
            var headersSize = request.Headers.Sum(h => 
                System.Text.Encoding.UTF8.GetByteCount($"{h.Key}: {h.Value}\r\n"));
            
            // Post data size (if available)
            var postDataSize = 0;
            try
            {
                var postData = request.PostData;
                if (!string.IsNullOrEmpty(postData))
                {
                    postDataSize = System.Text.Encoding.UTF8.GetByteCount(postData);
                }
            }
            catch
            {
                // Post data might not be available
            }

            return urlSize + headersSize + postDataSize;
        }
        catch
        {
            return 0; // Fallback
        }
    }

    private static long EstimateResponseSize(IResponse response)
    {
        try
        {
            // Try to get actual content length from headers
            if (response.Headers.TryGetValue("content-length", out var contentLengthStr) &&
                long.TryParse(contentLengthStr, out var contentLength))
            {
                return contentLength;
            }

            // Estimate headers size
            var headersSize = response.Headers.Sum(h => 
                System.Text.Encoding.UTF8.GetByteCount($"{h.Key}: {h.Value}\r\n"));

            // For responses without content-length, we can't easily get the body size
            // Return at least the headers size
            return headersSize;
        }
        catch
        {
            return 0; // Fallback
        }
    }

    private static Dictionary<string, string> GetResponseHeaders(IResponse response)
    {
        try
        {
            return response.Headers.ToDictionary(h => h.Key, h => h.Value);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        StopMonitoring();
    }

    private class RequestTrackingInfo
    {
        public DateTimeOffset StartTime { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
