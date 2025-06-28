using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for testing and simulating alert rules
/// </summary>
public class AlertTestingService : IAlertTestingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertTestingService> _logger;

    public AlertTestingService(
        IUnitOfWork unitOfWork,
        ILogger<AlertTestingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AlertTestResultDto>> TestAlertRuleAsync(Guid alertRuleId, TestPricePointDto testPricePoint)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result<AlertTestResultDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            var pricePoint = new PricePointRecordedEvent
            {
                CanonicalProductId = alertRule.CanonicalProductId,
                SellerName = testPricePoint.SellerName ?? "Test Seller",
                Price = testPricePoint.Price,
                StockStatus = StockStatus.Normalize(testPricePoint.StockStatus),
                SourceUrl = testPricePoint.SourceUrl ?? "test://example.com",
                Timestamp = testPricePoint.Timestamp ?? DateTimeOffset.UtcNow
            };

            var wouldTrigger = await EvaluateAlertCondition(alertRule, pricePoint);
            var triggerReason = await GetTriggerReason(alertRule, pricePoint, wouldTrigger);

            var result = new AlertTestResultDto
            {
                AlertRuleId = alertRuleId,
                AlertRuleDescription = alertRule.RuleDescription,
                WouldTrigger = wouldTrigger,
                TestType = "SINGLE_POINT",
                TotalPointsTested = 1,
                TriggeredCount = wouldTrigger ? 1 : 0,
                Matches = new List<AlertTestMatchDto>
                {
                    new AlertTestMatchDto
                    {
                        Price = pricePoint.Price,
                        StockStatus = pricePoint.StockStatus,
                        SellerName = pricePoint.SellerName,
                        Timestamp = pricePoint.Timestamp,
                        SourceUrl = pricePoint.SourceUrl,
                        TriggerReason = triggerReason,
                        WouldTrigger = wouldTrigger
                    }
                }
            };

            return Result<AlertTestResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing alert rule {AlertRuleId}", alertRuleId);
            return Result<AlertTestResultDto>.Failure("Failed to test alert rule", "TEST_FAILED");
        }
    }

    public async Task<Result<AlertTestResultDto>> TestAlertRuleAgainstHistoryAsync(AlertTestRequestDto request)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(request.AlertRuleId);
            if (alertRule == null)
            {
                return Result<AlertTestResultDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            // Get historical price data
            var priceHistory = await _unitOfWork.PriceHistory.GetPriceHistoryAsync(
                alertRule.CanonicalProductId,
                alertRule.SpecificSellerName,
                request.StartDate,
                request.EndDate,
                request.MaxRecords);

            var matches = new List<AlertTestMatchDto>();
            int triggeredCount = 0;

            foreach (var priceRecord in priceHistory)
            {
                var pricePoint = new PricePointRecordedEvent
                {
                    CanonicalProductId = priceRecord.CanonicalProductId,
                    SellerName = priceRecord.SellerName,
                    Price = priceRecord.Price,
                    StockStatus = priceRecord.StockStatus,
                    SourceUrl = priceRecord.SourceUrl,
                    Timestamp = priceRecord.Timestamp
                };

                var wouldTrigger = await EvaluateAlertCondition(alertRule, pricePoint);
                var triggerReason = await GetTriggerReason(alertRule, pricePoint, wouldTrigger);

                if (wouldTrigger)
                {
                    triggeredCount++;
                }

                matches.Add(new AlertTestMatchDto
                {
                    Price = priceRecord.Price,
                    StockStatus = priceRecord.StockStatus,
                    SellerName = priceRecord.SellerName,
                    Timestamp = priceRecord.Timestamp,
                    SourceUrl = priceRecord.SourceUrl,
                    TriggerReason = triggerReason,
                    WouldTrigger = wouldTrigger
                });
            }

            var result = new AlertTestResultDto
            {
                AlertRuleId = request.AlertRuleId,
                AlertRuleDescription = alertRule.RuleDescription,
                WouldTrigger = triggeredCount > 0,
                TestType = "HISTORICAL_RANGE",
                TotalPointsTested = matches.Count,
                TriggeredCount = triggeredCount,
                Matches = matches.OrderByDescending(m => m.Timestamp).ToList()
            };

            return Result<AlertTestResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing alert rule {AlertRuleId} against history", request.AlertRuleId);
            return Result<AlertTestResultDto>.Failure("Failed to test alert rule against history", "TEST_FAILED");
        }
    }

    public async Task<Result<AlertTestResultDto>> SimulateAlertRuleAsync(AlertRuleSimulationRequestDto request)
    {
        try
        {
            // Create a temporary alert rule for testing
            var tempAlertRule = new AlertRule
            {
                AlertRuleId = Guid.NewGuid(),
                UserId = Guid.Empty, // Temporary
                CanonicalProductId = request.AlertRule.CanonicalProductId,
                ConditionType = request.AlertRule.ConditionType,
                AlertType = request.AlertRule.AlertType,
                ThresholdValue = request.AlertRule.ThresholdValue,
                PercentageValue = request.AlertRule.PercentageValue,
                SpecificSellerName = request.AlertRule.SpecificSellerName,
                NotificationFrequencyMinutes = request.AlertRule.NotificationFrequencyMinutes,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            if (request.TestPricePoint != null)
            {
                // Test against single point
                var testRequest = new AlertTestRequestDto
                {
                    AlertRuleId = tempAlertRule.AlertRuleId
                };

                return await TestSinglePricePoint(tempAlertRule, request.TestPricePoint);
            }
            else
            {
                // Test against historical data
                return await TestAgainstHistoricalData(tempAlertRule, request.StartDate, request.EndDate, request.MaxRecords);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating alert rule");
            return Result<AlertTestResultDto>.Failure("Failed to simulate alert rule", "SIMULATION_FAILED");
        }
    }

    private async Task<Result<AlertTestResultDto>> TestSinglePricePoint(AlertRule alertRule, TestPricePointDto testPricePoint)
    {
        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = alertRule.CanonicalProductId,
            SellerName = testPricePoint.SellerName ?? "Test Seller",
            Price = testPricePoint.Price,
            StockStatus = StockStatus.Normalize(testPricePoint.StockStatus),
            SourceUrl = testPricePoint.SourceUrl ?? "test://example.com",
            Timestamp = testPricePoint.Timestamp ?? DateTimeOffset.UtcNow
        };

        var wouldTrigger = await EvaluateAlertCondition(alertRule, pricePoint);
        var triggerReason = await GetTriggerReason(alertRule, pricePoint, wouldTrigger);

        var result = new AlertTestResultDto
        {
            AlertRuleId = alertRule.AlertRuleId,
            AlertRuleDescription = alertRule.RuleDescription,
            WouldTrigger = wouldTrigger,
            TestType = "SINGLE_POINT_SIMULATION",
            TotalPointsTested = 1,
            TriggeredCount = wouldTrigger ? 1 : 0,
            Matches = new List<AlertTestMatchDto>
            {
                new AlertTestMatchDto
                {
                    Price = pricePoint.Price,
                    StockStatus = pricePoint.StockStatus,
                    SellerName = pricePoint.SellerName,
                    Timestamp = pricePoint.Timestamp,
                    SourceUrl = pricePoint.SourceUrl,
                    TriggerReason = triggerReason,
                    WouldTrigger = wouldTrigger
                }
            }
        };

        return Result<AlertTestResultDto>.Success(result);
    }

    private async Task<Result<AlertTestResultDto>> TestAgainstHistoricalData(
        AlertRule alertRule, 
        DateTimeOffset? startDate, 
        DateTimeOffset? endDate, 
        int? maxRecords)
    {
        // Get historical price data
        var priceHistory = await _unitOfWork.PriceHistory.GetPriceHistoryAsync(
            alertRule.CanonicalProductId,
            alertRule.SpecificSellerName,
            startDate,
            endDate,
            maxRecords);

        var matches = new List<AlertTestMatchDto>();
        int triggeredCount = 0;

        foreach (var priceRecord in priceHistory)
        {
            var pricePoint = new PricePointRecordedEvent
            {
                CanonicalProductId = priceRecord.CanonicalProductId,
                SellerName = priceRecord.SellerName,
                Price = priceRecord.Price,
                StockStatus = priceRecord.StockStatus,
                SourceUrl = priceRecord.SourceUrl,
                Timestamp = priceRecord.Timestamp
            };

            var wouldTrigger = await EvaluateAlertCondition(alertRule, pricePoint);
            var triggerReason = await GetTriggerReason(alertRule, pricePoint, wouldTrigger);

            if (wouldTrigger)
            {
                triggeredCount++;
            }

            matches.Add(new AlertTestMatchDto
            {
                Price = priceRecord.Price,
                StockStatus = priceRecord.StockStatus,
                SellerName = priceRecord.SellerName,
                Timestamp = priceRecord.Timestamp,
                SourceUrl = priceRecord.SourceUrl,
                TriggerReason = triggerReason,
                WouldTrigger = wouldTrigger
            });
        }

        var result = new AlertTestResultDto
        {
            AlertRuleId = alertRule.AlertRuleId,
            AlertRuleDescription = alertRule.RuleDescription,
            WouldTrigger = triggeredCount > 0,
            TestType = "HISTORICAL_SIMULATION",
            TotalPointsTested = matches.Count,
            TriggeredCount = triggeredCount,
            Matches = matches.OrderByDescending(m => m.Timestamp).ToList()
        };

        return Result<AlertTestResultDto>.Success(result);
    }

    public async Task<Result<AlertPerformanceMetricsDto>> GetAlertRulePerformanceAsync(
        Guid alertRuleId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result<AlertPerformanceMetricsDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            // Default to last 30 days if no dates provided
            var analysisStart = startDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var analysisEnd = endDate ?? DateTimeOffset.UtcNow;

            var priceHistory = await _unitOfWork.PriceHistory.GetPriceHistoryAsync(
                alertRule.CanonicalProductId,
                alertRule.SpecificSellerName,
                analysisStart,
                analysisEnd);

            var triggers = new List<AlertTestMatchDto>();
            var triggersByMonth = new Dictionary<string, int>();
            var triggersBySeller = new Dictionary<string, int>();
            var triggeringPrices = new List<decimal>();

            foreach (var priceRecord in priceHistory)
            {
                var pricePoint = new PricePointRecordedEvent
                {
                    CanonicalProductId = priceRecord.CanonicalProductId,
                    SellerName = priceRecord.SellerName,
                    Price = priceRecord.Price,
                    StockStatus = priceRecord.StockStatus,
                    SourceUrl = priceRecord.SourceUrl,
                    Timestamp = priceRecord.Timestamp
                };

                var wouldTrigger = await EvaluateAlertCondition(alertRule, pricePoint);

                if (wouldTrigger)
                {
                    var triggerReason = await GetTriggerReason(alertRule, pricePoint, true);

                    triggers.Add(new AlertTestMatchDto
                    {
                        Price = priceRecord.Price,
                        StockStatus = priceRecord.StockStatus,
                        SellerName = priceRecord.SellerName,
                        Timestamp = priceRecord.Timestamp,
                        SourceUrl = priceRecord.SourceUrl,
                        TriggerReason = triggerReason,
                        WouldTrigger = true
                    });

                    triggeringPrices.Add(priceRecord.Price);

                    // Group by month
                    var monthKey = priceRecord.Timestamp.ToString("yyyy-MM");
                    triggersByMonth[monthKey] = triggersByMonth.GetValueOrDefault(monthKey, 0) + 1;

                    // Group by seller
                    triggersBySeller[priceRecord.SellerName] = triggersBySeller.GetValueOrDefault(priceRecord.SellerName, 0) + 1;
                }
            }

            var metrics = new AlertPerformanceMetricsDto
            {
                AlertRuleId = alertRuleId,
                RuleDescription = alertRule.RuleDescription,
                AnalysisPeriodStart = analysisStart,
                AnalysisPeriodEnd = analysisEnd,
                TotalPricePointsAnalyzed = priceHistory.Count(),
                TimesWouldHaveTriggered = triggers.Count,
                TriggerRate = priceHistory.Any() ? (double)triggers.Count / priceHistory.Count() * 100 : 0,
                LowestTriggeringPrice = triggeringPrices.Any() ? triggeringPrices.Min() : null,
                HighestTriggeringPrice = triggeringPrices.Any() ? triggeringPrices.Max() : null,
                AverageTriggeringPrice = triggeringPrices.Any() ? triggeringPrices.Average() : null,
                RecentTriggers = triggers.OrderByDescending(t => t.Timestamp).Take(10).ToList(),
                TriggersByMonth = triggersByMonth,
                TriggersBySeller = triggersBySeller
            };

            return Result<AlertPerformanceMetricsDto>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert rule performance for {AlertRuleId}", alertRuleId);
            return Result<AlertPerformanceMetricsDto>.Failure("Failed to get alert rule performance", "PERFORMANCE_FAILED");
        }
    }

    public async Task<Result<AlertRuleValidationResultDto>> ValidateAlertRuleAsync(TestAlertRuleDto alertRule)
    {
        try
        {
            var result = new AlertRuleValidationResultDto { IsValid = true };

            // Check if product exists
            var product = await _unitOfWork.Products.GetByIdAsync(alertRule.CanonicalProductId);
            result.ProductExists = product != null;
            result.ProductName = product?.Name;

            if (!result.ProductExists)
            {
                result.ValidationErrors.Add("Product does not exist");
                result.IsValid = false;
            }

            // Validate condition type
            var validConditionTypes = new[] { "PRICE_BELOW", "PERCENT_DROP_FROM_LAST", "BACK_IN_STOCK" };
            if (!validConditionTypes.Contains(alertRule.ConditionType))
            {
                result.ValidationErrors.Add($"Invalid condition type. Must be one of: {string.Join(", ", validConditionTypes)}");
                result.IsValid = false;
            }

            // Validate condition-specific requirements
            switch (alertRule.ConditionType)
            {
                case "PRICE_BELOW":
                    if (!alertRule.ThresholdValue.HasValue)
                    {
                        result.ValidationErrors.Add("ThresholdValue is required for PRICE_BELOW condition");
                        result.IsValid = false;
                    }
                    break;

                case "PERCENT_DROP_FROM_LAST":
                    if (!alertRule.PercentageValue.HasValue)
                    {
                        result.ValidationErrors.Add("PercentageValue is required for PERCENT_DROP_FROM_LAST condition");
                        result.IsValid = false;
                    }
                    break;
            }

            // Get current price data for suggestions
            if (result.ProductExists)
            {
                var currentPrices = await _unitOfWork.PriceHistory.GetCurrentPricesAsync(alertRule.CanonicalProductId);
                if (currentPrices.Any())
                {
                    result.CurrentLowestPrice = currentPrices.Min(p => p.Price);
                    result.CurrentHighestPrice = currentPrices.Max(p => p.Price);
                    result.MostCommonStockStatus = currentPrices
                        .GroupBy(p => p.StockStatus)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key;

                    // Add suggestions based on current prices
                    if (alertRule.ConditionType == "PRICE_BELOW" && alertRule.ThresholdValue.HasValue)
                    {
                        if (alertRule.ThresholdValue.Value < result.CurrentLowestPrice)
                        {
                            result.Warnings.Add($"Threshold price ${alertRule.ThresholdValue:F2} is below current lowest price ${result.CurrentLowestPrice:F2}");
                        }
                        else if (alertRule.ThresholdValue.Value > result.CurrentHighestPrice)
                        {
                            result.Suggestions.Add($"Consider lowering threshold below ${result.CurrentHighestPrice:F2} for more frequent alerts");
                        }
                    }
                }
                else
                {
                    result.Warnings.Add("No price history found for this product");
                }
            }

            return Result<AlertRuleValidationResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating alert rule");
            return Result<AlertRuleValidationResultDto>.Failure("Failed to validate alert rule", "VALIDATION_FAILED");
        }
    }

    private async Task<bool> EvaluateAlertCondition(AlertRule alertRule, PricePointRecordedEvent pricePoint)
    {
        switch (alertRule.ConditionType)
        {
            case "PRICE_BELOW":
                return alertRule.ThresholdValue.HasValue && pricePoint.Price <= alertRule.ThresholdValue.Value;

            case "PERCENT_DROP_FROM_LAST":
                if (!alertRule.PercentageValue.HasValue)
                    return false;

                var lastPrice = await _unitOfWork.PriceHistory.GetLastPriceAsync(
                    pricePoint.CanonicalProductId, pricePoint.SellerName);

                if (lastPrice == null)
                    return false;

                var percentageChange = ((lastPrice.Price - pricePoint.Price) / lastPrice.Price) * 100;
                return percentageChange >= alertRule.PercentageValue.Value;

            case "BACK_IN_STOCK":
                var lastStockStatus = await _unitOfWork.PriceHistory.GetLastStockStatusAsync(
                    pricePoint.CanonicalProductId, pricePoint.SellerName);

                var normalizedCurrentStatus = StockStatus.Normalize(pricePoint.StockStatus);
                var normalizedLastStatus = StockStatus.Normalize(lastStockStatus);

                return StockStatus.IsBackInStock(normalizedLastStatus, normalizedCurrentStatus);

            default:
                return false;
        }
    }

    private async Task<string> GetTriggerReason(AlertRule alertRule, PricePointRecordedEvent pricePoint, bool wouldTrigger)
    {
        if (!wouldTrigger)
            return "Condition not met";

        switch (alertRule.ConditionType)
        {
            case "PRICE_BELOW":
                return $"Price ${pricePoint.Price:F2} is below threshold ${alertRule.ThresholdValue:F2}";

            case "PERCENT_DROP_FROM_LAST":
                var lastPrice = await _unitOfWork.PriceHistory.GetLastPriceAsync(
                    pricePoint.CanonicalProductId, pricePoint.SellerName);

                if (lastPrice != null)
                {
                    var percentageChange = ((lastPrice.Price - pricePoint.Price) / lastPrice.Price) * 100;
                    return $"Price dropped {percentageChange:F1}% from ${lastPrice.Price:F2} to ${pricePoint.Price:F2}";
                }
                return "Price drop detected";

            case "BACK_IN_STOCK":
                return $"Product is back in stock (status: {pricePoint.StockStatus})";

            default:
                return "Unknown condition";
        }
    }
}
