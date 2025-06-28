using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for testing alert rules against price data
/// </summary>
public class AlertTestRequestDto
{
    [Required]
    public Guid AlertRuleId { get; set; }

    /// <summary>
    /// Test against a specific price point
    /// </summary>
    public TestPricePointDto? TestPricePoint { get; set; }

    /// <summary>
    /// Test against historical price data within a date range
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Limit the number of historical records to test against
    /// </summary>
    public int? MaxRecords { get; set; } = 100;
}

/// <summary>
/// DTO for simulating a price point for testing
/// </summary>
public class TestPricePointDto
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(50)]
    public string StockStatus { get; set; } = null!;

    [MaxLength(100)]
    public string? SellerName { get; set; }

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
}

/// <summary>
/// DTO for alert test results
/// </summary>
public class AlertTestResultDto
{
    public Guid AlertRuleId { get; set; }
    public string AlertRuleDescription { get; set; } = null!;
    public bool WouldTrigger { get; set; }
    public string TestType { get; set; } = null!; // SINGLE_POINT, HISTORICAL_RANGE
    public int TotalPointsTested { get; set; }
    public int TriggeredCount { get; set; }
    public List<AlertTestMatchDto> Matches { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTimeOffset TestedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DTO for individual alert test matches
/// </summary>
public class AlertTestMatchDto
{
    public decimal Price { get; set; }
    public string StockStatus { get; set; } = null!;
    public string? SellerName { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? SourceUrl { get; set; }
    public string TriggerReason { get; set; } = null!;
    public bool WouldTrigger { get; set; }
}

/// <summary>
/// DTO for creating a test alert rule (without saving to database)
/// </summary>
public class TestAlertRuleDto
{
    [Required]
    public Guid CanonicalProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ConditionType { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string AlertType { get; set; } = "RECURRING";

    [Range(0.01, double.MaxValue)]
    public decimal? ThresholdValue { get; set; }

    [Range(0.1, 100.0)]
    public decimal? PercentageValue { get; set; }

    [MaxLength(100)]
    public string? SpecificSellerName { get; set; }

    public int NotificationFrequencyMinutes { get; set; } = 1440;
}

/// <summary>
/// DTO for testing a rule definition against price data
/// </summary>
public class AlertRuleSimulationRequestDto
{
    [Required]
    public TestAlertRuleDto AlertRule { get; set; } = null!;

    /// <summary>
    /// Test against a specific price point
    /// </summary>
    public TestPricePointDto? TestPricePoint { get; set; }

    /// <summary>
    /// Test against historical price data within a date range
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Limit the number of historical records to test against
    /// </summary>
    public int? MaxRecords { get; set; } = 100;
}
