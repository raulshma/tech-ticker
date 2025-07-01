namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for alert testing statistics
/// </summary>
public class AlertTestingStatsDto
{
    /// <summary>
    /// Total number of alert tests run in the period
    /// </summary>
    public int TotalTestsRun { get; set; }

    /// <summary>
    /// Total number of alert simulations run in the period
    /// </summary>
    public int TotalSimulationsRun { get; set; }

    /// <summary>
    /// Total number of alert validations run in the period
    /// </summary>
    public int TotalValidationsRun { get; set; }

    /// <summary>
    /// Number of tests by condition type (e.g., "PriceBelow", "PriceAbove", etc.)
    /// </summary>
    public Dictionary<string, int> TestsByConditionType { get; set; } = new();

    /// <summary>
    /// Number of tests by user
    /// </summary>
    public Dictionary<string, int> TestsByUser { get; set; } = new();

    /// <summary>
    /// Most frequently tested products
    /// </summary>
    public Dictionary<string, int> TestsByProduct { get; set; } = new();

    /// <summary>
    /// Tests by result (success, failure, warning)
    /// </summary>
    public Dictionary<string, int> TestsByResult { get; set; } = new();

    /// <summary>
    /// When the last test was run
    /// </summary>
    public DateTimeOffset? LastTestRun { get; set; }

    /// <summary>
    /// When the first test was run in the period
    /// </summary>
    public DateTimeOffset? FirstTestRun { get; set; }

    /// <summary>
    /// Average test execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Total unique alert rules tested
    /// </summary>
    public int UniqueAlertRulesTested { get; set; }

    /// <summary>
    /// Total unique products tested
    /// </summary>
    public int UniqueProductsTested { get; set; }

    /// <summary>
    /// Daily test counts for trending
    /// </summary>
    public Dictionary<string, int> DailyTestCounts { get; set; } = new();

    /// <summary>
    /// Analysis period start date
    /// </summary>
    public DateTimeOffset? PeriodStart { get; set; }

    /// <summary>
    /// Analysis period end date
    /// </summary>
    public DateTimeOffset? PeriodEnd { get; set; }
} 