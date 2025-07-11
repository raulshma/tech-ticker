using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for monitoring alert system performance and metrics
/// </summary>
public class AlertPerformanceMonitoringService : IAlertPerformanceMonitoringService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertPerformanceMonitoringService> _logger;
    private readonly IDistributedCache _distributedCache;
    
    // Redis cache configuration
    private const string CacheKeyPrefix = "alert_performance_";
    private const string BatchPerformanceCacheKey = CacheKeyPrefix + "batch_analysis";
    private const string SystemEventsKey = CacheKeyPrefix + "system_events";
    private const string PerformanceMetricsKeyPrefix = CacheKeyPrefix + "metrics_";
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _metricsTimeout = TimeSpan.FromMinutes(30);

    public AlertPerformanceMonitoringService(
        IUnitOfWork unitOfWork,
        ILogger<AlertPerformanceMonitoringService> logger,
        IDistributedCache distributedCache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<Result<AlertSystemPerformanceDto>> GetSystemPerformanceAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-7);
            var end = endDate ?? DateTimeOffset.UtcNow;

            // Get alert history data
            var (alertHistory, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                startDate: start,
                endDate: end,
                pageSize: int.MaxValue);

            // Calculate evaluation metrics
            var evaluationMetrics = CalculateEvaluationMetrics(alertHistory, start, end);

            // Calculate notification metrics
            var notificationMetrics = CalculateNotificationMetrics(alertHistory);

            // Get system health
            var systemHealth = await GetSystemHealthInternalAsync();

            // Get performance trends
            var trends = await GetPerformanceTrendsInternalAsync(start, end, TimeSpan.FromHours(1));

            var performance = new AlertSystemPerformanceDto
            {
                PeriodStart = start,
                PeriodEnd = end,
                EvaluationMetrics = evaluationMetrics,
                NotificationMetrics = notificationMetrics,
                SystemHealth = systemHealth,
                PerformanceTrends = trends
            };

            return Result<AlertSystemPerformanceDto>.Success(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system performance metrics");
            return Result<AlertSystemPerformanceDto>.Failure("Failed to get system performance", "PERFORMANCE_ERROR");
        }
    }

    public async Task<Result<AlertRulePerformanceAnalysisDto>> GetAlertRulePerformanceAsync(Guid alertRuleId)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result<AlertRulePerformanceAnalysisDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            var alertHistory = await _unitOfWork.AlertHistories.GetByAlertRuleIdAsync(alertRuleId);
            var recentHistory = alertHistory.Where(ah => ah.TriggeredAt >= DateTimeOffset.UtcNow.AddDays(-30));

            var analysis = new AlertRulePerformanceAnalysisDto
            {
                AlertRuleId = alertRuleId,
                RuleDescription = alertRule.RuleDescription,
                ConditionType = alertRule.ConditionType,
                IsActive = alertRule.IsActive,
                TimesTriggered = recentHistory.Count(),
                NotificationsSent = recentHistory.Count(ah => ah.NotificationStatus == "SENT"),
                NotificationSuccessRate = recentHistory.Any() 
                    ? (double)recentHistory.Count(ah => ah.NotificationStatus == "SENT") / recentHistory.Count() * 100 
                    : 0,
                LastTriggered = recentHistory.OrderByDescending(ah => ah.TriggeredAt).FirstOrDefault()?.TriggeredAt,
                LastSuccessfulNotification = recentHistory
                    .Where(ah => ah.NotificationStatus == "SENT")
                    .OrderByDescending(ah => ah.NotificationSentAt)
                    .FirstOrDefault()?.NotificationSentAt,
                RecentErrors = recentHistory
                    .Where(ah => !string.IsNullOrEmpty(ah.NotificationError))
                    .Select(ah => ah.NotificationError!)
                    .Distinct()
                    .Take(5)
                    .ToList()
            };

            // Calculate performance metrics from Redis cache
            var metrics = await GetFromCacheAsync<List<PerformanceMetric>>(PerformanceMetricsKeyPrefix + alertRuleId);
            if (metrics != null)
            {
                var recentMetrics = metrics.Where(m => m.Timestamp >= DateTimeOffset.UtcNow.AddDays(-30));
                if (recentMetrics.Any())
                {
                    var evaluationMetrics = recentMetrics.Where(m => m.Type == "EVALUATION").ToList();
                    var notificationMetrics = recentMetrics.Where(m => m.Type == "NOTIFICATION").ToList();
                    
                    if (evaluationMetrics.Any())
                    {
                        analysis.AverageEvaluationTime = TimeSpan.FromMilliseconds(
                            evaluationMetrics.Average(m => m.Duration.TotalMilliseconds));
                    }
                    
                    if (notificationMetrics.Any())
                    {
                        analysis.AverageNotificationTime = TimeSpan.FromMilliseconds(
                            notificationMetrics.Average(m => m.Duration.TotalMilliseconds));
                    }
                }
            }

            // Determine performance rating
            analysis.PerformanceRating = DeterminePerformanceRating(analysis);

            // Generate optimization suggestions
            analysis.OptimizationSuggestions = GenerateOptimizationSuggestions(analysis);

            return Result<AlertRulePerformanceAnalysisDto>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert rule performance for {AlertRuleId}", alertRuleId);
            return Result<AlertRulePerformanceAnalysisDto>.Failure("Failed to get alert rule performance", "PERFORMANCE_ERROR");
        }
    }

    public async Task<Result<RealTimeAlertMonitoringDto>> GetRealTimeMonitoringAsync()
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var oneHourAgo = now.AddHours(-1);

            // Get recent alert history
            var (recentAlerts, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                startDate: oneHourAgo,
                endDate: now,
                pageSize: 1000);

            var monitoring = new RealTimeAlertMonitoringDto
            {
                AlertsInLastMinute = recentAlerts.Count(ah => ah.TriggeredAt >= oneMinuteAgo),
                AlertsInLastHour = recentAlerts.Count(),
                NotificationsInLastMinute = recentAlerts.Count(ah => ah.NotificationSentAt >= oneMinuteAgo),
                NotificationsInLastHour = recentAlerts.Count(ah => ah.NotificationSentAt >= oneHourAgo),
                RecentActivity = recentAlerts
                    .OrderByDescending(ah => ah.TriggeredAt)
                    .Take(20)
                    .Select(ah => new RecentAlertActivityDto
                    {
                        Timestamp = ah.TriggeredAt,
                        AlertRuleId = ah.AlertRuleId,
                        ProductName = ah.Product?.Name ?? "Unknown",
                        ConditionType = ah.ConditionType,
                        TriggeringPrice = ah.TriggeringPrice,
                        Status = ah.NotificationStatus,
                        ProcessingTime = ah.NotificationDelay ?? TimeSpan.Zero
                    })
                    .ToList()
            };

            // Get recent system events from Redis
            var systemEvents = await GetFromCacheAsync<List<AlertSystemEventDto>>(SystemEventsKey);
            if (systemEvents != null)
            {
                monitoring.RecentEvents = systemEvents
                    .Where(e => e.Timestamp >= oneHourAgo)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(50)
                    .ToList();
            }
            else
            {
                monitoring.RecentEvents = new List<AlertSystemEventDto>();
            }

            // Calculate average processing time from Redis metrics
            // Note: For performance, we'll calculate this from recent alerts instead of all cached metrics
            var recentEvaluationTimes = recentAlerts
                .Where(ah => ah.TriggeredAt >= oneMinuteAgo)
                .Select(ah => ah.NotificationDelay ?? TimeSpan.Zero)
                .Where(delay => delay > TimeSpan.Zero);

            if (recentEvaluationTimes.Any())
            {
                monitoring.AverageProcessingTime = TimeSpan.FromMilliseconds(
                    recentEvaluationTimes.Average(t => t.TotalMilliseconds));
            }

            return Result<RealTimeAlertMonitoringDto>.Success(monitoring);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time monitoring data");
            return Result<RealTimeAlertMonitoringDto>.Failure("Failed to get real-time monitoring", "MONITORING_ERROR");
        }
    }

    public async Task<Result<List<AlertPerformanceTrendDto>>> GetPerformanceTrendsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        TimeSpan interval)
    {
        try
        {
            var trends = await GetPerformanceTrendsInternalAsync(startDate, endDate, interval);
            return Result<List<AlertPerformanceTrendDto>>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance trends");
            return Result<List<AlertPerformanceTrendDto>>.Failure("Failed to get performance trends", "TRENDS_ERROR");
        }
    }

    public async Task RecordAlertEvaluationMetricsAsync(
        Guid alertRuleId,
        TimeSpan evaluationTime,
        bool wasTriggered,
        bool hadError = false,
        string? errorMessage = null)
    {
        try
        {
            var metric = new PerformanceMetric
            {
                AlertRuleId = alertRuleId,
                Type = "EVALUATION",
                Timestamp = DateTimeOffset.UtcNow,
                Duration = evaluationTime,
                WasSuccessful = !hadError,
                WasTriggered = wasTriggered,
                ErrorMessage = errorMessage
            };

            // Store metrics in Redis
            var metricsKey = PerformanceMetricsKeyPrefix + alertRuleId;
            var existingMetrics = await GetFromCacheAsync<List<PerformanceMetric>>(metricsKey) ?? new List<PerformanceMetric>();
            
            existingMetrics.Add(metric);
            
            // Keep only recent metrics (last 24 hours) to prevent memory bloat
            var cutoff = DateTimeOffset.UtcNow.AddDays(-1);
            existingMetrics = existingMetrics
                .Where(m => m.Timestamp >= cutoff)
                .ToList();
            
            await SetCacheAsync(metricsKey, existingMetrics, _metricsTimeout);

            if (hadError)
            {
                await RecordSystemEventAsync("ERROR", $"Alert evaluation error: {errorMessage}", "AlertEvaluation", 
                    new Dictionary<string, object> { { "AlertRuleId", alertRuleId } });
            }

            // Invalidate cache when new metrics are recorded
            _ = Task.Run(InvalidatePerformanceCacheAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording alert evaluation metrics for {AlertRuleId}", alertRuleId);
        }
    }

    public async Task RecordNotificationDeliveryMetricsAsync(
        Guid alertRuleId,
        string notificationChannel,
        TimeSpan deliveryTime,
        bool wasSuccessful,
        string? errorMessage = null)
    {
        try
        {
            var metric = new PerformanceMetric
            {
                AlertRuleId = alertRuleId,
                Type = "NOTIFICATION",
                Timestamp = DateTimeOffset.UtcNow,
                Duration = deliveryTime,
                WasSuccessful = wasSuccessful,
                Channel = notificationChannel,
                ErrorMessage = errorMessage
            };

            // Store metrics in Redis
            var metricsKey = PerformanceMetricsKeyPrefix + alertRuleId;
            var existingMetrics = await GetFromCacheAsync<List<PerformanceMetric>>(metricsKey) ?? new List<PerformanceMetric>();
            
            existingMetrics.Add(metric);
            
            // Keep only recent metrics
            var cutoff = DateTimeOffset.UtcNow.AddDays(-1);
            existingMetrics = existingMetrics
                .Where(m => m.Timestamp >= cutoff)
                .ToList();
            
            await SetCacheAsync(metricsKey, existingMetrics, _metricsTimeout);

            if (!wasSuccessful)
            {
                await RecordSystemEventAsync("ERROR", $"Notification delivery failed: {errorMessage}", "NotificationDelivery",
                    new Dictionary<string, object>
                    {
                        { "AlertRuleId", alertRuleId },
                        { "Channel", notificationChannel }
                    });
            }

            // Invalidate cache when new metrics are recorded
            _ = Task.Run(InvalidatePerformanceCacheAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording notification delivery metrics for {AlertRuleId}", alertRuleId);
        }
    }

    public async Task<Result<AlertSystemHealthDto>> GetSystemHealthAsync()
    {
        try
        {
            var health = await GetSystemHealthInternalAsync();
            return Result<AlertSystemHealthDto>.Success(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return Result<AlertSystemHealthDto>.Failure("Failed to get system health", "HEALTH_ERROR");
        }
    }

    public async Task<Result<List<AlertRulePerformanceAnalysisDto>>> GetTopPerformingAlertRulesAsync(
        int count = 10,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            // Use optimized batch method instead of N+1 queries
            var performanceAnalyses = await GetBatchAlertRulePerformanceAsync(startDate, endDate);

            var topPerformers = performanceAnalyses
                .Where(a => a.PerformanceRating == PerformanceRating.Excellent || a.PerformanceRating == PerformanceRating.Good)
                .OrderByDescending(a => a.NotificationSuccessRate)
                .ThenByDescending(a => a.TimesTriggered)
                .Take(count)
                .ToList();

            return Result<List<AlertRulePerformanceAnalysisDto>>.Success(topPerformers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top performing alert rules");
            return Result<List<AlertRulePerformanceAnalysisDto>>.Failure("Failed to get top performers", "TOP_PERFORMERS_ERROR");
        }
    }

    public async Task<Result<List<AlertRulePerformanceAnalysisDto>>> GetPoorlyPerformingAlertRulesAsync(
        int count = 10,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            // Use optimized batch method instead of N+1 queries
            var performanceAnalyses = await GetBatchAlertRulePerformanceAsync(startDate, endDate);

            var poorPerformers = performanceAnalyses
                .Where(a => a.PerformanceRating == PerformanceRating.Poor || a.PerformanceRating == PerformanceRating.Critical)
                .OrderBy(a => a.NotificationSuccessRate)
                .ThenByDescending(a => a.RecentErrors.Count)
                .Take(count)
                .ToList();

            return Result<List<AlertRulePerformanceAnalysisDto>>.Success(poorPerformers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting poorly performing alert rules");
            return Result<List<AlertRulePerformanceAnalysisDto>>.Failure("Failed to get poor performers", "POOR_PERFORMERS_ERROR");
        }
    }

    /// <summary>
    /// Get performance analysis for multiple alert rules in a single batch operation (optimized)
    /// </summary>
    private async Task<List<AlertRulePerformanceAnalysisDto>> GetBatchAlertRulePerformanceAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            // Check Redis cache first (only for default date range requests)
            if (startDate == null && endDate == null)
            {
                var cachedData = await GetFromCacheAsync<List<AlertRulePerformanceAnalysisDto>>(BatchPerformanceCacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Returning cached batch performance analysis from Redis");
                    return cachedData;
                }
            }

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
            
            // Get all alert rules in one query
            var alertRules = await _unitOfWork.AlertRules.GetAllAsync();
            var alertRuleIds = alertRules.Select(ar => ar.AlertRuleId).ToList();
            
            // Get all alert history for all rules in one query with date filtering
            var (allAlertHistory, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                startDate: cutoffDate,
                endDate: DateTimeOffset.UtcNow,
                pageSize: int.MaxValue);
            
            // Group alert history by alert rule ID for efficient lookups
            var alertHistoryByRuleId = allAlertHistory
                .GroupBy(ah => ah.AlertRuleId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var performanceAnalyses = new List<AlertRulePerformanceAnalysisDto>();

            foreach (var alertRule in alertRules)
            {
                // Get recent history for this rule (already filtered by date)
                var recentHistory = alertHistoryByRuleId.GetValueOrDefault(alertRule.AlertRuleId, new List<AlertHistory>());

                var analysis = new AlertRulePerformanceAnalysisDto
                {
                    AlertRuleId = alertRule.AlertRuleId,
                    RuleDescription = alertRule.RuleDescription,
                    ConditionType = alertRule.ConditionType,
                    IsActive = alertRule.IsActive,
                    TimesTriggered = recentHistory.Count,
                    NotificationsSent = recentHistory.Count(ah => ah.NotificationStatus == "SENT"),
                    NotificationSuccessRate = recentHistory.Any() 
                        ? (double)recentHistory.Count(ah => ah.NotificationStatus == "SENT") / recentHistory.Count() * 100 
                        : 0,
                    LastTriggered = recentHistory.OrderByDescending(ah => ah.TriggeredAt).FirstOrDefault()?.TriggeredAt,
                    LastSuccessfulNotification = recentHistory
                        .Where(ah => ah.NotificationStatus == "SENT")
                        .OrderByDescending(ah => ah.NotificationSentAt)
                        .FirstOrDefault()?.NotificationSentAt,
                    RecentErrors = recentHistory
                        .Where(ah => !string.IsNullOrEmpty(ah.NotificationError))
                        .Select(ah => ah.NotificationError!)
                        .Distinct()
                        .Take(5)
                        .ToList()
                };

                // Calculate average times from notification delays (proxy for evaluation/notification times)
                var successfulNotifications = recentHistory.Where(ah => ah.NotificationDelay.HasValue);
                if (successfulNotifications.Any())
                {
                    var avgDelay = successfulNotifications.Average(ah => ah.NotificationDelay!.Value.TotalMilliseconds);
                    analysis.AverageEvaluationTime = TimeSpan.FromMilliseconds(avgDelay * 0.3); // Assume 30% of delay is evaluation
                    analysis.AverageNotificationTime = TimeSpan.FromMilliseconds(avgDelay * 0.7); // Assume 70% is notification
                }

                // Determine performance rating
                analysis.PerformanceRating = DeterminePerformanceRating(analysis);

                // Generate optimization suggestions
                analysis.OptimizationSuggestions = GenerateOptimizationSuggestions(analysis);

                performanceAnalyses.Add(analysis);
            }

            // Cache the result for default date range requests
            if (startDate == null && endDate == null)
            {
                _ = Task.Run(async () =>
                {
                    await SetCacheAsync(BatchPerformanceCacheKey, performanceAnalyses);
                    _logger.LogDebug("Cached batch performance analysis for {Count} alert rules", performanceAnalyses.Count);
                });
            }

            return performanceAnalyses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch alert rule performance");
            return new List<AlertRulePerformanceAnalysisDto>();
        }
    }

    public async Task<Result<AlertPerformanceReportDto>> GeneratePerformanceReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        try
        {
            var overallPerformance = await GetSystemPerformanceAsync(startDate, endDate);
            if (!overallPerformance.IsSuccess)
                return Result<AlertPerformanceReportDto>.Failure(overallPerformance.ErrorMessage ?? "Unknown error", overallPerformance.ErrorCode);

            var topPerformers = await GetTopPerformingAlertRulesAsync(5, startDate, endDate);
            var poorPerformers = await GetPoorlyPerformingAlertRulesAsync(5, startDate, endDate);

            var report = new AlertPerformanceReportDto
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                OverallPerformance = overallPerformance.Data ?? new AlertSystemPerformanceDto(),
                TopPerformers = topPerformers.IsSuccess && topPerformers.Data != null ? topPerformers.Data : new List<AlertRulePerformanceAnalysisDto>(),
                PoorPerformers = poorPerformers.IsSuccess && poorPerformers.Data != null ? poorPerformers.Data : new List<AlertRulePerformanceAnalysisDto>(),
                KeyInsights = GenerateKeyInsights(overallPerformance.Data ?? new AlertSystemPerformanceDto()),
                Recommendations = GenerateRecommendations(overallPerformance.Data ?? new AlertSystemPerformanceDto())
            };

            return Result<AlertPerformanceReportDto>.Success(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report");
            return Result<AlertPerformanceReportDto>.Failure("Failed to generate performance report", "REPORT_ERROR");
        }
    }

    public async Task<Result<Dictionary<string, NotificationChannelStatsDto>>> GetNotificationChannelStatsAsync(
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTimeOffset.UtcNow.AddDays(-7);
            var end = endDate ?? DateTimeOffset.UtcNow;

            var (alertHistory, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                startDate: start,
                endDate: end,
                pageSize: int.MaxValue);

            var channelStats = new Dictionary<string, NotificationChannelStatsDto>();

            // Group by notification channel (we'll assume Discord for now)
            var discordStats = new NotificationChannelStatsDto
            {
                ChannelName = "Discord",
                TotalNotifications = alertHistory.Count(),
                SuccessfulDeliveries = alertHistory.Count(ah => ah.NotificationStatus == "SENT"),
                FailedDeliveries = alertHistory.Count(ah => ah.NotificationStatus == "FAILED"),
                IsHealthy = true
            };

            if (discordStats.TotalNotifications > 0)
            {
                discordStats.SuccessRate = (double)discordStats.SuccessfulDeliveries / discordStats.TotalNotifications * 100;

                var successfulDeliveries = alertHistory.Where(ah => ah.NotificationDelay.HasValue);
                if (successfulDeliveries.Any())
                {
                    var delays = successfulDeliveries.Select(ah => ah.NotificationDelay!.Value);
                    discordStats.AverageDeliveryTime = TimeSpan.FromTicks((long)delays.Average(d => d.Ticks));
                    discordStats.MaxDeliveryTime = delays.Max();
                    discordStats.MinDeliveryTime = delays.Min();
                }

                discordStats.IsHealthy = discordStats.SuccessRate >= 95;
                if (!discordStats.IsHealthy)
                {
                    discordStats.HealthIssues.Add($"Success rate ({discordStats.SuccessRate:F1}%) is below 95%");
                }
            }

            channelStats["Discord"] = discordStats;

            return Result<Dictionary<string, NotificationChannelStatsDto>>.Success(channelStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification channel stats");
            return Result<Dictionary<string, NotificationChannelStatsDto>>.Failure("Failed to get channel stats", "CHANNEL_STATS_ERROR");
        }
    }

    public async Task RecordSystemEventAsync(string eventType, string message, string? component = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            var systemEvent = new AlertSystemEventDto
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = eventType,
                Message = message,
                Component = component,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            // Store system events in Redis
            var existingEvents = await GetFromCacheAsync<List<AlertSystemEventDto>>(SystemEventsKey) ?? new List<AlertSystemEventDto>();
            existingEvents.Add(systemEvent);

            // Keep only recent events (last 24 hours)
            var cutoff = DateTimeOffset.UtcNow.AddDays(-1);
            existingEvents = existingEvents.Where(e => e.Timestamp >= cutoff).ToList();
            
            await SetCacheAsync(SystemEventsKey, existingEvents, _metricsTimeout);

            _logger.LogInformation("System event recorded: {EventType} - {Message}", eventType, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording system event");
        }
    }

    /// <summary>
    /// Invalidate the performance cache to ensure fresh data on next request
    /// </summary>
    private async Task InvalidatePerformanceCacheAsync()
    {
        try
        {
            await _distributedCache.RemoveAsync(BatchPerformanceCacheKey);
            _logger.LogDebug("Invalidated performance cache in Redis");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate performance cache in Redis");
        }
    }

    /// <summary>
    /// Get data from Redis cache
    /// </summary>
    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cachedData = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<T>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get data from Redis cache for key: {Key}", key);
        }
        return null;
    }

    /// <summary>
    /// Set data in Redis cache
    /// </summary>
    private async Task SetCacheAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(data);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(_cacheTimeout);
            }

            await _distributedCache.SetStringAsync(key, serializedData, options);
            _logger.LogDebug("Cached data in Redis for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache data in Redis for key: {Key}", key);
        }
    }

    // Helper methods
    private AlertEvaluationMetricsDto CalculateEvaluationMetrics(IEnumerable<AlertHistory> alertHistory, DateTimeOffset start, DateTimeOffset end)
    {
        var metrics = new AlertEvaluationMetricsDto
        {
            TotalAlertsTriggered = alertHistory.Count(),
            TriggersByConditionType = alertHistory.GroupBy(ah => ah.ConditionType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate trigger rate and other metrics from database data
        // Note: For performance, we'll use basic calculations from alert history
        // In a production system, you might want to store these metrics separately
        var totalEvaluations = alertHistory.Count();
        if (totalEvaluations > 0)
        {
            metrics.TotalAlertsEvaluated = totalEvaluations;
            metrics.AlertTriggerRate = 100.0; // All entries in alertHistory represent triggered alerts
            metrics.EvaluationErrors = 0; // Errors would be tracked separately
            metrics.ErrorRate = 0;

            // Use notification delays as proxy for evaluation times
            var notificationDelays = alertHistory
                .Where(ah => ah.NotificationDelay.HasValue)
                .Select(ah => ah.NotificationDelay!.Value);
                
            if (notificationDelays.Any())
            {
                metrics.AverageEvaluationTime = TimeSpan.FromTicks((long)notificationDelays.Average(d => d.Ticks));
                metrics.MaxEvaluationTime = notificationDelays.Max();
                metrics.MinEvaluationTime = notificationDelays.Min();
            }
        }

        return metrics;
    }

    private NotificationDeliveryMetricsDto CalculateNotificationMetrics(IEnumerable<AlertHistory> alertHistory)
    {
        var metrics = new NotificationDeliveryMetricsDto
        {
            TotalNotificationsSent = alertHistory.Count(),
            SuccessfulDeliveries = alertHistory.Count(ah => ah.NotificationStatus == "SENT"),
            FailedDeliveries = alertHistory.Count(ah => ah.NotificationStatus == "FAILED"),
            PendingDeliveries = alertHistory.Count(ah => ah.NotificationStatus == "PENDING")
        };

        if (metrics.TotalNotificationsSent > 0)
        {
            metrics.DeliverySuccessRate = (double)metrics.SuccessfulDeliveries / metrics.TotalNotificationsSent * 100;

            var successfulDeliveries = alertHistory.Where(ah => ah.NotificationDelay.HasValue);
            if (successfulDeliveries.Any())
            {
                var delays = successfulDeliveries.Select(ah => ah.NotificationDelay!.Value);
                metrics.AverageDeliveryTime = TimeSpan.FromTicks((long)delays.Average(d => d.Ticks));
                metrics.MaxDeliveryTime = delays.Max();
                metrics.MinDeliveryTime = delays.Min();
            }
        }

        metrics.DeliveriesByChannel["Discord"] = metrics.TotalNotificationsSent;
        metrics.SuccessRateByChannel["Discord"] = metrics.DeliverySuccessRate;

        return metrics;
    }

    private async Task<AlertSystemHealthDto> GetSystemHealthInternalAsync()
    {
        var health = new AlertSystemHealthDto();

        try
        {
            var allAlertRules = await _unitOfWork.AlertRules.GetAllAsync();
            health.ActiveAlertRules = allAlertRules.Count(ar => ar.IsActive);
            health.InactiveAlertRules = allAlertRules.Count(ar => !ar.IsActive);

            // Calculate error count from Redis cache and recent alert history
            var recentCutoff = DateTimeOffset.UtcNow.AddHours(-1);
            
            // Get recent system events from Redis
            var systemEvents = await GetFromCacheAsync<List<AlertSystemEventDto>>(SystemEventsKey);
            if (systemEvents != null)
            {
                var recentErrors = systemEvents.Where(e => e.EventType == "ERROR" && e.Timestamp >= recentCutoff);
                if (recentErrors.Any())
                {
                    health.HealthIssues.AddRange(recentErrors.Select(e => e.Message).Distinct().Take(5));
                }
            }

            // Get recent alert history to check for errors
            var (recentAlerts, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                startDate: recentCutoff,
                endDate: DateTimeOffset.UtcNow,
                pageSize: 1000);

            health.AlertRulesWithErrors = recentAlerts.Count(ah => ah.NotificationStatus == "FAILED");

            // Simple health calculation
            health.IsHealthy = health.HealthIssues.Count == 0 && health.AlertRulesWithErrors < 5;

            // Mock system metrics (in a real implementation, these would come from system monitoring)
            health.SystemUptime = TimeSpan.FromHours(24); // Mock uptime
            health.MemoryUsageMB = 512; // Mock memory usage
            health.CpuUsagePercent = 15; // Mock CPU usage
            health.QueueBacklog = 0; // Mock queue backlog
            health.AverageProcessingDelay = TimeSpan.FromMilliseconds(100); // Mock processing delay
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating system health");
            health.IsHealthy = false;
            health.HealthIssues.Add($"Error calculating health metrics: {ex.Message}");
        }

        return health;
    }

    private async Task<List<AlertPerformanceTrendDto>> GetPerformanceTrendsInternalAsync(DateTimeOffset start, DateTimeOffset end, TimeSpan interval)
    {
        var trends = new List<AlertPerformanceTrendDto>();

        try
        {
            var current = start;
            while (current < end)
            {
                var periodEnd = current.Add(interval);
                if (periodEnd > end) periodEnd = end;

                var (periodAlerts, _) = await _unitOfWork.AlertHistories.GetAlertHistoryAsync(
                    startDate: current,
                    endDate: periodEnd,
                    pageSize: int.MaxValue);

                var trend = new AlertPerformanceTrendDto
                {
                    Timestamp = current,
                    AlertsTriggered = periodAlerts.Count(),
                    NotificationsSent = periodAlerts.Count(ah => ah.NotificationStatus == "SENT"),
                    ErrorCount = periodAlerts.Count(ah => ah.NotificationStatus == "FAILED")
                };

                if (trend.NotificationsSent > 0)
                {
                    trend.SuccessRate = (double)trend.NotificationsSent / periodAlerts.Count() * 100;

                    var successfulDeliveries = periodAlerts.Where(ah => ah.NotificationDelay.HasValue);
                    if (successfulDeliveries.Any())
                    {
                        trend.AverageResponseTime = TimeSpan.FromTicks(
                            (long)successfulDeliveries.Average(ah => ah.NotificationDelay!.Value.Ticks));
                    }
                }

                trends.Add(trend);
                current = periodEnd;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance trends");
        }

        return trends;
    }

    private PerformanceRating DeterminePerformanceRating(AlertRulePerformanceAnalysisDto analysis)
    {
        if (!analysis.IsActive)
            return PerformanceRating.Fair;

        if (analysis.RecentErrors.Count > 5)
            return PerformanceRating.Critical;

        if (analysis.NotificationSuccessRate < 50)
            return PerformanceRating.Poor;

        if (analysis.NotificationSuccessRate < 80)
            return PerformanceRating.Fair;

        if (analysis.NotificationSuccessRate < 95)
            return PerformanceRating.Good;

        return PerformanceRating.Excellent;
    }

    private List<string> GenerateOptimizationSuggestions(AlertRulePerformanceAnalysisDto analysis)
    {
        var suggestions = new List<string>();

        if (!analysis.IsActive)
        {
            suggestions.Add("Alert rule is inactive. Consider activating if still needed or removing if obsolete.");
        }

        if (analysis.NotificationSuccessRate < 95 && analysis.NotificationSuccessRate > 0)
        {
            suggestions.Add($"Notification success rate is {analysis.NotificationSuccessRate:F1}%. Review notification configuration.");
        }

        if (analysis.RecentErrors.Count > 0)
        {
            suggestions.Add($"Recent errors detected ({analysis.RecentErrors.Count}). Review error messages and fix underlying issues.");
        }

        if (analysis.TimesTriggered == 0 && analysis.IsActive)
        {
            suggestions.Add("Alert rule hasn't triggered recently. Review conditions to ensure they're appropriate.");
        }

        if (analysis.AverageEvaluationTime > TimeSpan.FromSeconds(5))
        {
            suggestions.Add("Alert evaluation is taking longer than expected. Consider optimizing conditions.");
        }

        return suggestions;
    }

    private List<string> GenerateKeyInsights(AlertSystemPerformanceDto performance)
    {
        var insights = new List<string>();

        if (performance.EvaluationMetrics.AlertTriggerRate > 50)
        {
            insights.Add($"High alert trigger rate ({performance.EvaluationMetrics.AlertTriggerRate:F1}%) may indicate overly sensitive conditions.");
        }

        if (performance.NotificationMetrics.DeliverySuccessRate < 95)
        {
            insights.Add($"Notification delivery success rate ({performance.NotificationMetrics.DeliverySuccessRate:F1}%) is below optimal threshold.");
        }

        if (performance.EvaluationMetrics.ErrorRate > 5)
        {
            insights.Add($"Alert evaluation error rate ({performance.EvaluationMetrics.ErrorRate:F1}%) indicates system issues.");
        }

        return insights;
    }

    private List<string> GenerateRecommendations(AlertSystemPerformanceDto performance)
    {
        var recommendations = new List<string>();

        if (performance.EvaluationMetrics.AlertTriggerRate > 50)
        {
            recommendations.Add("Review alert rule conditions to reduce false positives.");
        }

        if (performance.NotificationMetrics.DeliverySuccessRate < 95)
        {
            recommendations.Add("Investigate notification delivery issues and improve reliability.");
        }

        if (performance.SystemHealth.AlertRulesWithErrors > 0)
        {
            recommendations.Add("Address alert rules with errors to improve system reliability.");
        }

        return recommendations;
    }
}

/// <summary>
/// Internal class for tracking performance metrics
/// </summary>
internal class PerformanceMetric
{
    public Guid AlertRuleId { get; set; }
    public string Type { get; set; } = null!; // EVALUATION, NOTIFICATION
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public bool WasSuccessful { get; set; }
    public bool WasTriggered { get; set; }
    public string? Channel { get; set; }
    public string? ErrorMessage { get; set; }
}
