using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for testing and simulating alert rules
/// </summary>
public interface IAlertTestingService
{
    /// <summary>
    /// Test an existing alert rule against a specific price point
    /// </summary>
    Task<Result<AlertTestResultDto>> TestAlertRuleAsync(Guid alertRuleId, TestPricePointDto testPricePoint);

    /// <summary>
    /// Test an existing alert rule against historical price data
    /// </summary>
    Task<Result<AlertTestResultDto>> TestAlertRuleAgainstHistoryAsync(AlertTestRequestDto request);

    /// <summary>
    /// Simulate a new alert rule definition against price data without saving it
    /// </summary>
    Task<Result<AlertTestResultDto>> SimulateAlertRuleAsync(AlertRuleSimulationRequestDto request);

    /// <summary>
    /// Get alert rule performance metrics (how often it would have triggered)
    /// </summary>
    Task<Result<AlertPerformanceMetricsDto>> GetAlertRulePerformanceAsync(
        Guid alertRuleId, 
        DateTimeOffset? startDate = null, 
        DateTimeOffset? endDate = null);

    /// <summary>
    /// Validate an alert rule configuration
    /// </summary>
    Task<Result<AlertRuleValidationResultDto>> ValidateAlertRuleAsync(TestAlertRuleDto alertRule);
}

/// <summary>
/// DTO for alert rule performance metrics
/// </summary>
public class AlertPerformanceMetricsDto
{
    public Guid AlertRuleId { get; set; }
    public string RuleDescription { get; set; } = null!;
    public DateTimeOffset? AnalysisPeriodStart { get; set; }
    public DateTimeOffset? AnalysisPeriodEnd { get; set; }
    public int TotalPricePointsAnalyzed { get; set; }
    public int TimesWouldHaveTriggered { get; set; }
    public double TriggerRate { get; set; }
    public decimal? LowestTriggeringPrice { get; set; }
    public decimal? HighestTriggeringPrice { get; set; }
    public decimal? AverageTriggeringPrice { get; set; }
    public List<AlertTestMatchDto> RecentTriggers { get; set; } = new();
    public Dictionary<string, int> TriggersByMonth { get; set; } = new();
    public Dictionary<string, int> TriggersBySeller { get; set; } = new();
}

/// <summary>
/// DTO for alert rule validation results
/// </summary>
public class AlertRuleValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public bool ProductExists { get; set; }
    public string? ProductName { get; set; }
    public decimal? CurrentLowestPrice { get; set; }
    public decimal? CurrentHighestPrice { get; set; }
    public string? MostCommonStockStatus { get; set; }
}
