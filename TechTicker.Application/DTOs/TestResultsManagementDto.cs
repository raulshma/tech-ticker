using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for saved test result summary
/// </summary>
public class SavedTestResultDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public string TestUrl { get; set; } = null!;
    public bool Success { get; set; }
    public DateTime SavedAt { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int Duration { get; set; } // milliseconds
    public int ActionsExecuted { get; set; }
    public int ErrorCount { get; set; }
    public string ProfileHash { get; set; } = null!; // For grouping related tests
    public string CreatedBy { get; set; } = null!; // User who saved the result
}

/// <summary>
/// DTO for detailed saved test result
/// </summary>
public class SavedTestResultDetailDto : SavedTestResultDto
{
    public BrowserAutomationTestResultDto TestResult { get; set; } = null!;
    public BrowserAutomationProfileDto Profile { get; set; } = null!;
    public BrowserTestOptionsDto Options { get; set; } = null!;
    public List<string>? Screenshots { get; set; } // Base64 encoded screenshots
    public string? VideoRecording { get; set; } // Base64 encoded video
    public Dictionary<string, object>? Metadata { get; set; } // Additional metadata
}

/// <summary>
/// DTO for test result comparison
/// </summary>
public class TestResultComparisonDto
{
    public SavedTestResultDto FirstResult { get; set; } = null!;
    public SavedTestResultDto SecondResult { get; set; } = null!;
    public TestComparisonMetrics Metrics { get; set; } = null!;
    public List<TestComparisonDifference> Differences { get; set; } = new();
    public TestComparisonSummary Summary { get; set; } = null!;
}

/// <summary>
/// Metrics comparison between two test results
/// </summary>
public class TestComparisonMetrics
{
    public MetricComparison Duration { get; set; } = null!;
    public MetricComparison ActionsExecuted { get; set; } = null!;
    public MetricComparison ErrorCount { get; set; } = null!;
    public MetricComparison MemoryUsage { get; set; } = null!;
    public MetricComparison NetworkRequests { get; set; } = null!;
    public MetricComparison PageLoadTime { get; set; } = null!;
}

/// <summary>
/// Individual metric comparison
/// </summary>
public class MetricComparison
{
    public double FirstValue { get; set; }
    public double SecondValue { get; set; }
    public double Difference { get; set; }
    public double PercentageChange { get; set; }
    public ComparisonTrend Trend { get; set; }
}

/// <summary>
/// Comparison trend indicator
/// </summary>
public enum ComparisonTrend
{
    Improved,
    Degraded,
    NoChange
}

/// <summary>
/// Individual difference between test results
/// </summary>
public class TestComparisonDifference
{
    public string Category { get; set; } = null!; // "action", "timing", "error", "screenshot"
    public string Field { get; set; } = null!;
    public string? FirstValue { get; set; }
    public string? SecondValue { get; set; }
    public DifferenceType Type { get; set; }
    public DifferenceSeverity Severity { get; set; }
    public string Description { get; set; } = null!;
}

/// <summary>
/// Type of difference
/// </summary>
public enum DifferenceType
{
    Added,
    Removed,
    Modified,
    Reordered
}

/// <summary>
/// Severity of difference
/// </summary>
public enum DifferenceSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Summary of test comparison
/// </summary>
public class TestComparisonSummary
{
    public bool AreEqual { get; set; }
    public int TotalDifferences { get; set; }
    public int CriticalDifferences { get; set; }
    public int WarningDifferences { get; set; }
    public int InfoDifferences { get; set; }
    public double SimilarityScore { get; set; } // 0-100
    public string OverallAssessment { get; set; } = null!;
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// DTO for test execution trends and statistics
/// </summary>
public class TestExecutionTrendsDto
{
    public TestStatistics OverallStatistics { get; set; } = null!;
    public List<TestTrendDataPoint> TrendData { get; set; } = new();
    public List<TopErrorDto> TopErrors { get; set; } = new();
    public List<PerformanceTrendDto> PerformanceTrends { get; set; } = new();
    public List<PopularTestUrlDto> PopularTestUrls { get; set; } = new();
    public TestReliabilityMetrics Reliability { get; set; } = null!;
}

/// <summary>
/// Overall test statistics
/// </summary>
public class TestStatistics
{
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public double MedianExecutionTime { get; set; }
    public int TotalActionsExecuted { get; set; }
    public DateTime? FirstTestDate { get; set; }
    public DateTime? LastTestDate { get; set; }
    public int UniqueUrls { get; set; }
    public int UniqueProfiles { get; set; }
}

/// <summary>
/// Data point for trend visualization
/// </summary>
public class TestTrendDataPoint
{
    public DateTime Date { get; set; }
    public int TestCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public int ErrorCount { get; set; }
}

/// <summary>
/// Top error information
/// </summary>
public class TopErrorDto
{
    public string ErrorMessage { get; set; } = null!;
    public int Occurrences { get; set; }
    public double Percentage { get; set; }
    public List<string> AffectedUrls { get; set; } = new();
    public DateTime? FirstOccurrence { get; set; }
    public DateTime? LastOccurrence { get; set; }
}

/// <summary>
/// Performance trend information
/// </summary>
public class PerformanceTrendDto
{
    public string MetricName { get; set; } = null!;
    public List<PerformanceDataPoint> DataPoints { get; set; } = new();
    public double CurrentAverage { get; set; }
    public double PreviousAverage { get; set; }
    public ComparisonTrend Trend { get; set; }
    public double PercentageChange { get; set; }
}

/// <summary>
/// Performance data point
/// </summary>
public class PerformanceDataPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public int SampleCount { get; set; }
}

/// <summary>
/// Popular test URL information
/// </summary>
public class PopularTestUrlDto
{
    public string Url { get; set; } = null!;
    public int TestCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public DateTime? LastTested { get; set; }
}

/// <summary>
/// Test reliability metrics
/// </summary>
public class TestReliabilityMetrics
{
    public double OverallReliability { get; set; } // 0-100
    public double ConsistencyScore { get; set; } // 0-100
    public int FlakeyTests { get; set; } // Tests with inconsistent results
    public List<string> MostReliableUrls { get; set; } = new();
    public List<string> LeastReliableUrls { get; set; } = new();
    public Dictionary<string, double> ReliabilityByBrowser { get; set; } = new();
}

/// <summary>
/// DTO for exported test result
/// </summary>
public class ExportedTestResultDto
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] Data { get; set; } = null!;
    public int FileSizeBytes { get; set; }
    public TestResultExportFormat Format { get; set; }
    public DateTime ExportedAt { get; set; }
}

/// <summary>
/// DTO for test history entry
/// </summary>
public class TestHistoryEntryDto
{
    public string SessionId { get; set; } = null!;
    public string? SavedResultId { get; set; } // If this test was saved
    public string TestUrl { get; set; } = null!;
    public string ProfileHash { get; set; } = null!;
    public bool Success { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int Duration { get; set; }
    public int ActionsExecuted { get; set; }
    public int ErrorCount { get; set; }
    public string ExecutedBy { get; set; } = null!;
    public string? SessionName { get; set; }
    public string BrowserEngine { get; set; } = null!;
    public string DeviceType { get; set; } = null!;
}

/// <summary>
/// Request DTO for saving test results
/// </summary>
public class SaveTestResultRequestDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request DTO for test result comparison
/// </summary>
public class CompareTestResultsRequestDto
{
    public string FirstResultId { get; set; } = null!;
    public string SecondResultId { get; set; } = null!;
    public bool IncludeScreenshots { get; set; } = false;
    public bool IncludeNetworkData { get; set; } = false;
    public bool IncludeDetailedDifferences { get; set; } = true;
}

/// <summary>
/// Request DTO for test trends
/// </summary>
public class GetTestTrendsRequestDto
{
    public string? ProfileId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int MaxDataPoints { get; set; } = 30;
    public string GroupBy { get; set; } = "day"; // day, week, month
    public List<string>? Metrics { get; set; } // Specific metrics to include
}

/// <summary>
/// Response DTO for saved test results with pagination
/// </summary>
public class SavedTestResultsResponseDto : PagedResponse<SavedTestResultDto>
{
    public List<string> AvailableTags { get; set; } = new();
    public TestStatistics Statistics { get; set; } = new();
} 