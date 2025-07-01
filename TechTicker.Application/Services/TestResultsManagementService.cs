using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;
using TechTicker.Shared.Common;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing saved test results, history, and comparison
/// Phase 4: Database Integration - Uses repositories instead of in-memory storage
/// </summary>
public class TestResultsManagementService : ITestResultsManagementService
{
    private readonly IBrowserAutomationTestService _testService;
    private readonly ISavedTestResultRepository _savedTestResultRepository;
    private readonly ITestExecutionHistoryRepository _testExecutionHistoryRepository;
    private readonly ILogger<TestResultsManagementService> _logger;

    public TestResultsManagementService(
        IBrowserAutomationTestService testService,
        ISavedTestResultRepository savedTestResultRepository,
        ITestExecutionHistoryRepository testExecutionHistoryRepository,
        ILogger<TestResultsManagementService> logger)
    {
        _testService = testService;
        _savedTestResultRepository = savedTestResultRepository;
        _testExecutionHistoryRepository = testExecutionHistoryRepository;
        _logger = logger;
    }

    public async Task<Result<string>> SaveTestResultsAsync(
        string sessionId,
        string name,
        string? description = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving test results for session {SessionId} with name {Name}", sessionId, name);

            // Get the test results from the session
            var testResultsResult = await _testService.GetTestSessionResultsAsync(sessionId, cancellationToken);
            if (!testResultsResult.IsSuccess || testResultsResult.Data == null)
            {
                return Result<string>.Failure("Test results not found for session", "TEST_RESULTS_NOT_FOUND");
            }

            var testResults = testResultsResult.Data;

            // Get session details to extract test URL and other information
            var sessionDetailsResult = await _testService.GetTestSessionDetailsAsync(sessionId, cancellationToken);
            var sessionDetails = sessionDetailsResult.IsSuccess ? sessionDetailsResult.Data : null;

            // Get additional session details
            var sessionStatusResult = await _testService.GetTestSessionStatusAsync(sessionId, cancellationToken);
            var sessionStatus = sessionStatusResult.Data;

            // Calculate profile hash for grouping
            var profileHash = CalculateProfileHash(testResults.SessionId ?? "unknown");

            // Create the SavedTestResult entity
            var savedTestResult = new SavedTestResult
            {
                Name = name,
                Description = description,
                TestUrl = sessionDetails?.TestUrl ?? "Unknown",
                Success = testResults.Success,
                ExecutedAt = testResults.StartedAt.DateTime,
                Duration = testResults.Duration,
                ActionsExecuted = testResults.ActionsExecuted,
                ErrorCount = testResults.Errors?.Count ?? 0,
                ProfileHash = profileHash,
                CreatedBy = Guid.NewGuid(), // TODO: Get current user ID
                TestResultJson = JsonSerializer.Serialize(testResults),
                ProfileJson = JsonSerializer.Serialize(sessionDetails?.Profile ?? new BrowserAutomationProfileDto()),
                OptionsJson = JsonSerializer.Serialize(sessionDetails?.Options ?? new BrowserTestOptionsDto()),
                MetadataJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    { "sessionId", sessionId },
                    { "browserEngine", sessionDetails?.Profile?.PreferredBrowser ?? "chromium" },
                    { "deviceType", sessionDetails?.Options?.DeviceEmulation ?? "desktop" },
                    { "savedBy", "System" }
                }),
                ScreenshotsData = testResults.Screenshots != null ? 
                    string.Join("|", testResults.Screenshots.Select(s => s.Base64Data ?? "")) : null,
                VideoRecording = testResults.VideoRecording
            };

            // Add tags
            if (tags != null && tags.Any())
            {
                savedTestResult.Tags = tags.Select(tag => new SavedTestResultTag
                {
                    Tag = tag,
                    SavedTestResult = savedTestResult
                }).ToList();
            }

            // Save to database
            var createdResult = await _savedTestResultRepository.CreateAsync(savedTestResult);

            // Create test execution history entry
            var historyEntry = new TestExecutionHistory
            {
                SessionId = sessionId,
                SavedTestResultId = createdResult.Id,
                TestUrl = savedTestResult.TestUrl,
                ProfileHash = profileHash,
                Success = savedTestResult.Success,
                ExecutedAt = savedTestResult.ExecutedAt,
                Duration = savedTestResult.Duration,
                ActionsExecuted = savedTestResult.ActionsExecuted,
                ErrorCount = savedTestResult.ErrorCount,
                ExecutedBy = savedTestResult.CreatedBy,
                SessionName = name,
                BrowserEngine = "chromium", // TODO: Get from options
                DeviceType = "desktop" // TODO: Get from options
            };

            await _testExecutionHistoryRepository.CreateAsync(historyEntry);

            _logger.LogInformation("Successfully saved test results with ID {SavedResultId}", createdResult.Id);
            return Result<string>.Success(createdResult.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving test results for session {SessionId}", sessionId);
            return Result<string>.Failure("Failed to save test results", "SAVE_ERROR");
        }
    }

    public async Task<Result<PagedResponse<SavedTestResultDto>>> GetSavedTestResultsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        List<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting saved test results - Page: {Page}, Size: {Size}, Search: {Search}", 
                pageNumber, pageSize, searchTerm);

            // Get paged results from repository
            var (results, totalCount) = await _savedTestResultRepository.GetPagedAsync(
                pageNumber, pageSize, searchTerm, tags);

            // Convert to DTOs
            var resultDtos = results.Select(r => new SavedTestResultDto
            {
                Id = r.Id.ToString(),
                Name = r.Name,
                Description = r.Description,
                Tags = r.Tags?.Select(t => t.Tag).ToList(),
                TestUrl = r.TestUrl,
                Success = r.Success,
                SavedAt = r.SavedAt,
                ExecutedAt = r.ExecutedAt,
                Duration = r.Duration,
                ActionsExecuted = r.ActionsExecuted,
                ErrorCount = r.ErrorCount,
                ProfileHash = r.ProfileHash,
                CreatedBy = r.CreatedByUser?.UserName ?? "Unknown"
            }).ToList();

            // Get available tags
            var availableTags = await _savedTestResultRepository.GetAllTagsAsync();

            // Get statistics
            var (totalTests, successfulTests, failedTests, averageExecutionTime) = 
                await _savedTestResultRepository.GetStatisticsAsync();

            var statistics = new TestStatistics
            {
                TotalTests = totalTests,
                SuccessfulTests = successfulTests,
                FailedTests = failedTests,
                SuccessRate = totalTests > 0 ? (double)successfulTests / totalTests * 100 : 0,
                AverageExecutionTime = averageExecutionTime,
                MedianExecutionTime = CalculateMedian(resultDtos.Select(r => (double)r.Duration).ToList()),
                TotalActionsExecuted = resultDtos.Sum(r => r.ActionsExecuted),
                FirstTestDate = resultDtos.Count > 0 ? resultDtos.Min(r => r.ExecutedAt) : null,
                LastTestDate = resultDtos.Count > 0 ? resultDtos.Max(r => r.ExecutedAt) : null,
                UniqueUrls = resultDtos.Select(r => r.TestUrl).Distinct().Count(),
                UniqueProfiles = resultDtos.Select(r => r.ProfileHash).Distinct().Count()
            };

            var response = PagedResponse<SavedTestResultDto>.SuccessResult(
                resultDtos,
                pageNumber,
                pageSize,
                totalCount);

            return Result<PagedResponse<SavedTestResultDto>>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved test results");
            return Result<PagedResponse<SavedTestResultDto>>.Failure("Failed to get saved test results", "GET_SAVED_RESULTS_ERROR");
        }
    }

    public async Task<Result<SavedTestResultDetailDto>> GetSavedTestResultAsync(
        string savedResultId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting saved test result {SavedResultId}", savedResultId);

            if (!Guid.TryParse(savedResultId, out var id))
            {
                return Result<SavedTestResultDetailDto>.Failure("Invalid result ID format", "INVALID_ID");
            }

            var result = await _savedTestResultRepository.GetByIdAsync(id);
            if (result == null)
            {
                return Result<SavedTestResultDetailDto>.Failure("Saved test result not found", "SAVED_RESULT_NOT_FOUND");
            }

            // Convert to detailed DTO
            var detailDto = new SavedTestResultDetailDto
            {
                Id = result.Id.ToString(),
                Name = result.Name,
                Description = result.Description,
                Tags = result.Tags?.Select(t => t.Tag).ToList(),
                TestUrl = result.TestUrl,
                Success = result.Success,
                SavedAt = result.SavedAt,
                ExecutedAt = result.ExecutedAt,
                Duration = result.Duration,
                ActionsExecuted = result.ActionsExecuted,
                ErrorCount = result.ErrorCount,
                ProfileHash = result.ProfileHash,
                CreatedBy = result.CreatedByUser?.UserName ?? "Unknown",
                            TestResult = !string.IsNullOrEmpty(result.TestResultJson) ? 
                JsonSerializer.Deserialize<BrowserAutomationTestResultDto>(result.TestResultJson) ?? new BrowserAutomationTestResultDto() : 
                new BrowserAutomationTestResultDto(),
            Profile = !string.IsNullOrEmpty(result.ProfileJson) ? 
                JsonSerializer.Deserialize<BrowserAutomationProfileDto>(result.ProfileJson) ?? new BrowserAutomationProfileDto() : 
                new BrowserAutomationProfileDto(),
            Options = !string.IsNullOrEmpty(result.OptionsJson) ? 
                JsonSerializer.Deserialize<BrowserTestOptionsDto>(result.OptionsJson) ?? new BrowserTestOptionsDto() : 
                new BrowserTestOptionsDto(),
                Screenshots = !string.IsNullOrEmpty(result.ScreenshotsData) ? 
                    result.ScreenshotsData.Split('|').ToList() : null,
                VideoRecording = result.VideoRecording,
                Metadata = !string.IsNullOrEmpty(result.MetadataJson) ? 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(result.MetadataJson) ?? new Dictionary<string, object>() : 
                    new Dictionary<string, object>()
            };

            return Result<SavedTestResultDetailDto>.Success(detailDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved test result {SavedResultId}", savedResultId);
            return Result<SavedTestResultDetailDto>.Failure("Failed to get saved test result", "GET_SAVED_RESULT_ERROR");
        }
    }

    public async Task<Result<bool>> DeleteSavedTestResultAsync(
        string savedResultId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting saved test result {SavedResultId}", savedResultId);

            if (!Guid.TryParse(savedResultId, out var id))
            {
                return Result<bool>.Failure("Invalid result ID format", "INVALID_ID");
            }

            var deleted = await _savedTestResultRepository.DeleteAsync(id);
            return Result<bool>.Success(deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved test result {SavedResultId}", savedResultId);
            return Result<bool>.Failure("Failed to delete saved test result", "DELETE_SAVED_RESULT_ERROR");
        }
    }

    public async Task<Result<TestResultComparisonDto>> CompareTestResultsAsync(
        string firstResultId,
        string secondResultId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Comparing test results {FirstResultId} and {SecondResultId}", 
                firstResultId, secondResultId);

            if (!Guid.TryParse(firstResultId, out var firstId) || !Guid.TryParse(secondResultId, out var secondId))
            {
                return Result<TestResultComparisonDto>.Failure("Invalid result ID format", "INVALID_ID");
            }

            var results = await _savedTestResultRepository.GetForComparisonAsync(new List<Guid> { firstId, secondId });
            var resultsList = results.ToList();

            if (resultsList.Count != 2)
            {
                return Result<TestResultComparisonDto>.Failure("One or both test results not found", "RESULTS_NOT_FOUND");
            }

            var firstResult = resultsList.First(r => r.Id == firstId);
            var secondResult = resultsList.First(r => r.Id == secondId);

            // Convert to DTOs for comparison
            var firstDto = ConvertToDetailDto(firstResult);
            var secondDto = ConvertToDetailDto(secondResult);

            var comparison = await PerformDetailedComparison(firstDto, secondDto);
            return Result<TestResultComparisonDto>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing test results {FirstResultId} and {SecondResultId}", 
                firstResultId, secondResultId);
            return Result<TestResultComparisonDto>.Failure("Failed to compare test results", "COMPARISON_ERROR");
        }
    }

    public async Task<Result<TestExecutionTrendsDto>> GetTestExecutionTrendsAsync(
        string? profileId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting test execution trends for profile {ProfileId} from {FromDate} to {ToDate}", 
                profileId, fromDate, toDate);

            // Get execution statistics
            var (totalExecutions, successfulExecutions, successRate, averageExecutionTime, 
                 firstExecution, lastExecution, uniqueUrls, uniqueProfiles) = 
                await _testExecutionHistoryRepository.GetExecutionStatisticsAsync(fromDate, toDate, profileId);

            // Get trends data
            var trendsHistory = await _testExecutionHistoryRepository.GetTrendsDataAsync(profileId, fromDate, toDate);

            // Group by day for trend data
            var trendData = trendsHistory
                .GroupBy(h => h.ExecutedAt.Date)
                .Select(g => new TestTrendDataPoint
                {
                    Date = g.Key,
                    TestCount = g.Count(),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(h => h.Success) / g.Count() * 100 : 0,
                    AverageExecutionTime = g.Count() > 0 ? g.Average(h => h.Duration) : 0,
                    ErrorCount = g.Sum(h => h.ErrorCount)
                })
                .OrderBy(t => t.Date)
                .ToList();

            // Get popular URLs
            var popularUrls = (await _testExecutionHistoryRepository.GetPopularTestUrlsAsync(10))
                .Select(u => new PopularTestUrlDto
                {
                    Url = u.TestUrl,
                    TestCount = u.TestCount,
                    SuccessRate = u.SuccessRate,
                    AverageExecutionTime = u.AverageExecutionTime,
                    LastTested = u.LastTested
                }).ToList();

            // Get reliability metrics
            var reliabilityByBrowser = await _testExecutionHistoryRepository.GetReliabilityByBrowserAsync();
            var flakyTests = await _testExecutionHistoryRepository.GetFlakyTestsAsync();

            var reliability = new TestReliabilityMetrics
            {
                OverallReliability = successRate,
                ConsistencyScore = await _testExecutionHistoryRepository.GetConsistencyScoreAsync(),
                FlakeyTests = flakyTests.Count(),
                MostReliableUrls = popularUrls.Where(u => u.SuccessRate >= 95).Select(u => u.Url).Take(5).ToList(),
                LeastReliableUrls = popularUrls.Where(u => u.SuccessRate < 80).Select(u => u.Url).Take(5).ToList(),
                ReliabilityByBrowser = reliabilityByBrowser
            };

            var overallStats = new TestStatistics
            {
                TotalTests = totalExecutions,
                SuccessfulTests = successfulExecutions,
                FailedTests = totalExecutions - successfulExecutions,
                SuccessRate = successRate,
                AverageExecutionTime = averageExecutionTime,
                MedianExecutionTime = CalculateMedian(trendsHistory.Select(h => (double)h.Duration).ToList()),
                TotalActionsExecuted = trendsHistory.Sum(h => h.ActionsExecuted),
                FirstTestDate = firstExecution,
                LastTestDate = lastExecution,
                UniqueUrls = uniqueUrls,
                UniqueProfiles = uniqueProfiles
            };

            var trendsDto = new TestExecutionTrendsDto
            {
                OverallStatistics = overallStats,
                TrendData = trendData,
                TopErrors = new List<TopErrorDto>(), // TODO: Implement error analysis
                PerformanceTrends = new List<PerformanceTrendDto>(), // TODO: Implement performance trends
                PopularTestUrls = popularUrls,
                Reliability = reliability
            };

            return Result<TestExecutionTrendsDto>.Success(trendsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test execution trends");
            return Result<TestExecutionTrendsDto>.Failure("Failed to get test execution trends", "TRENDS_ERROR");
        }
    }

    public async Task<Result<ExportedTestResultDto>> ExportTestResultAsync(
        string savedResultId,
        TestResultExportFormat format,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting test result {SavedResultId} in format {Format}", savedResultId, format);

            var resultResult = await GetSavedTestResultAsync(savedResultId, cancellationToken);
            if (!resultResult.IsSuccess || resultResult.Data == null)
            {
                return Result<ExportedTestResultDto>.Failure("Test result not found", "RESULT_NOT_FOUND");
            }

            var result = resultResult.Data;
            byte[] exportData;
            string contentType;
            string fileName;

            switch (format)
            {
                case TestResultExportFormat.Json:
                    var jsonData = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                    exportData = Encoding.UTF8.GetBytes(jsonData);
                    contentType = "application/json";
                    fileName = $"test-result-{result.Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                    break;

                case TestResultExportFormat.Csv:
                    var csvData = ConvertToCsv(result);
                    exportData = Encoding.UTF8.GetBytes(csvData);
                    contentType = "text/csv";
                    fileName = $"test-result-{result.Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                    break;

                case TestResultExportFormat.Pdf:
                    // For MVP, we'll return a JSON representation
                    // In production, this would generate a proper PDF
                    var pdfData = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                    exportData = Encoding.UTF8.GetBytes(pdfData);
                    contentType = "application/pdf";
                    fileName = $"test-result-{result.Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
                    break;

                default:
                    return Result<ExportedTestResultDto>.Failure("Unsupported export format", "UNSUPPORTED_FORMAT");
            }

            var exportedResult = new ExportedTestResultDto
            {
                FileName = fileName,
                ContentType = contentType,
                Data = exportData,
                FileSizeBytes = exportData.Length,
                Format = format,
                ExportedAt = DateTime.UtcNow
            };

            return Result<ExportedTestResultDto>.Success(exportedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting test result {SavedResultId}", savedResultId);
            return Result<ExportedTestResultDto>.Failure("Failed to export test result", "EXPORT_ERROR");
        }
    }

    public async Task<Result<List<TestHistoryEntryDto>>> GetTestHistoryAsync(
        string? testUrl = null,
        string? profileHash = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting test history for URL {TestUrl}, Profile {ProfileHash}, Limit {Limit}", 
                testUrl, profileHash, limit);

            var (history, _) = await _testExecutionHistoryRepository.GetPagedAsync(
                1, limit, testUrl, profileHash);

            var historyDtos = history.Select(h => new TestHistoryEntryDto
            {
                SessionId = h.SessionId,
                SavedResultId = h.SavedTestResultId?.ToString(),
                TestUrl = h.TestUrl,
                ProfileHash = h.ProfileHash,
                Success = h.Success,
                ExecutedAt = h.ExecutedAt,
                Duration = h.Duration,
                ActionsExecuted = h.ActionsExecuted,
                ErrorCount = h.ErrorCount,
                ExecutedBy = h.ExecutedByUser?.UserName ?? "Unknown",
                SessionName = h.SessionName,
                BrowserEngine = h.BrowserEngine,
                DeviceType = h.DeviceType
            }).ToList();

            return Result<List<TestHistoryEntryDto>>.Success(historyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test history");
            return Result<List<TestHistoryEntryDto>>.Failure("Failed to get test history", "HISTORY_ERROR");
        }
    }

    #region Private Methods

    private SavedTestResultDetailDto ConvertToDetailDto(SavedTestResult entity)
    {
        return new SavedTestResultDetailDto
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Tags = entity.Tags?.Select(t => t.Tag).ToList(),
            TestUrl = entity.TestUrl,
            Success = entity.Success,
            SavedAt = entity.SavedAt,
            ExecutedAt = entity.ExecutedAt,
            Duration = entity.Duration,
            ActionsExecuted = entity.ActionsExecuted,
            ErrorCount = entity.ErrorCount,
            ProfileHash = entity.ProfileHash,
            CreatedBy = entity.CreatedByUser?.UserName ?? "Unknown",
            TestResult = !string.IsNullOrEmpty(entity.TestResultJson) ?
                JsonSerializer.Deserialize<BrowserAutomationTestResultDto>(entity.TestResultJson) ?? new BrowserAutomationTestResultDto() :
                new BrowserAutomationTestResultDto(),
            Profile = !string.IsNullOrEmpty(entity.ProfileJson) ?
                JsonSerializer.Deserialize<BrowserAutomationProfileDto>(entity.ProfileJson) ?? new BrowserAutomationProfileDto() :
                new BrowserAutomationProfileDto(),
            Options = !string.IsNullOrEmpty(entity.OptionsJson) ?
                JsonSerializer.Deserialize<BrowserTestOptionsDto>(entity.OptionsJson) ?? new BrowserTestOptionsDto() :
                new BrowserTestOptionsDto(),
            Screenshots = !string.IsNullOrEmpty(entity.ScreenshotsData) ?
                entity.ScreenshotsData.Split('|').ToList() : null,
            VideoRecording = entity.VideoRecording,
            Metadata = !string.IsNullOrEmpty(entity.MetadataJson) ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetadataJson) ?? new Dictionary<string, object>() :
                new Dictionary<string, object>()
        };
    }

    private string CalculateProfileHash(string profileData)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(profileData));
        return Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
    }

    private double CalculateMedian(List<double> values)
    {
        if (values.Count == 0) return 0;
        
        values.Sort();
        var mid = values.Count / 2;
        
        if (values.Count % 2 == 0)
        {
            return (values[mid - 1] + values[mid]) / 2.0;
        }
        
        return values[mid];
    }

    private Task<TestResultComparisonDto> PerformDetailedComparison(
        SavedTestResultDetailDto firstResult,
        SavedTestResultDetailDto secondResult)
    {
        var metrics = new TestComparisonMetrics
        {
            Duration = CompareMetric(firstResult.Duration, secondResult.Duration),
            ActionsExecuted = CompareMetric(firstResult.ActionsExecuted, secondResult.ActionsExecuted),
            ErrorCount = CompareMetric(firstResult.ErrorCount, secondResult.ErrorCount),
            MemoryUsage = CompareMetric(0, 0), // TODO: Implement memory usage comparison
            NetworkRequests = CompareMetric(0, 0), // TODO: Implement network requests comparison
            PageLoadTime = CompareMetric(0, 0) // TODO: Implement page load time comparison
        };

        var differences = new List<TestComparisonDifference>();
        
        // Compare basic properties
        if (firstResult.Success != secondResult.Success)
        {
            differences.Add(new TestComparisonDifference
            {
                Category = "result",
                Field = "success",
                FirstValue = firstResult.Success.ToString(),
                SecondValue = secondResult.Success.ToString(),
                Type = DifferenceType.Modified,
                Severity = DifferenceSeverity.Critical,
                Description = "Test success status differs"
            });
        }

        // Calculate similarity score
        var similarityScore = CalculateSimilarityScore(firstResult, secondResult, differences);

        var summary = new TestComparisonSummary
        {
            AreEqual = differences.Count == 0,
            TotalDifferences = differences.Count,
            CriticalDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Critical),
            WarningDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Warning),
            InfoDifferences = differences.Count(d => d.Severity == DifferenceSeverity.Info),
            SimilarityScore = similarityScore,
            OverallAssessment = GetOverallAssessment(similarityScore, differences),
            Recommendations = GenerateRecommendations(differences, metrics)
        };

        return Task.FromResult(new TestResultComparisonDto
        {
            FirstResult = new SavedTestResultDto
            {
                Id = firstResult.Id,
                Name = firstResult.Name,
                TestUrl = firstResult.TestUrl,
                Success = firstResult.Success,
                ExecutedAt = firstResult.ExecutedAt,
                Duration = firstResult.Duration,
                ActionsExecuted = firstResult.ActionsExecuted,
                ErrorCount = firstResult.ErrorCount,
                ProfileHash = firstResult.ProfileHash,
                CreatedBy = firstResult.CreatedBy
            },
            SecondResult = new SavedTestResultDto
            {
                Id = secondResult.Id,
                Name = secondResult.Name,
                TestUrl = secondResult.TestUrl,
                Success = secondResult.Success,
                ExecutedAt = secondResult.ExecutedAt,
                Duration = secondResult.Duration,
                ActionsExecuted = secondResult.ActionsExecuted,
                ErrorCount = secondResult.ErrorCount,
                ProfileHash = secondResult.ProfileHash,
                CreatedBy = secondResult.CreatedBy
            },
            Metrics = metrics,
            Differences = differences,
            Summary = summary
        });
    }

    private MetricComparison CompareMetric(double firstValue, double secondValue)
    {
        var difference = secondValue - firstValue;
        var percentageChange = firstValue != 0 ? (difference / firstValue) * 100 : 0;

        // Determine trend based on difference
        var trend = Math.Abs(difference) < 0.01 ? ComparisonTrend.NoChange : 
                   difference < 0 ? ComparisonTrend.Improved : ComparisonTrend.Degraded;

        return new MetricComparison
        {
            FirstValue = firstValue,
            SecondValue = secondValue,
            Difference = difference,
            PercentageChange = percentageChange,
            Trend = trend
        };
    }

    private double CalculateSimilarityScore(SavedTestResultDetailDto first, SavedTestResultDetailDto second, List<TestComparisonDifference> differences)
    {
        // Simple similarity calculation based on various factors
        double score = 100;

        // Deduct points for each difference based on severity
        foreach (var difference in differences)
        {
            score -= difference.Severity switch
            {
                DifferenceSeverity.Critical => 25,
                DifferenceSeverity.Warning => 10,
                DifferenceSeverity.Info => 2,
                _ => 0
            };
        }

        // Compare durations (deduct points for large differences)
        if (first.Duration > 0 && second.Duration > 0)
        {
            var durationDifference = Math.Abs(first.Duration - second.Duration) / (double)Math.Max(first.Duration, second.Duration);
            score -= durationDifference * 20; // Up to 20 points deduction
        }

        return Math.Max(0, score);
    }

    private string GetOverallAssessment(double similarityScore, List<TestComparisonDifference> differences)
    {
        if (similarityScore >= 90) return "Very Similar";
        if (similarityScore >= 75) return "Similar";
        if (similarityScore >= 60) return "Somewhat Similar";
        if (similarityScore >= 40) return "Different";
        return "Very Different";
    }

    private List<string> GenerateRecommendations(List<TestComparisonDifference> differences, TestComparisonMetrics metrics)
    {
        var recommendations = new List<string>();

        if (differences.Any(d => d.Severity == DifferenceSeverity.Critical))
        {
            recommendations.Add("Review critical differences to understand test behavior changes");
        }

        if (metrics.Duration.PercentageChange > 20)
        {
            recommendations.Add("Investigate performance degradation - execution time increased significantly");
        }
        else if (metrics.Duration.PercentageChange < -20)
        {
            recommendations.Add("Good performance improvement - execution time decreased significantly");
        }

        if (metrics.ErrorCount.SecondValue > metrics.ErrorCount.FirstValue)
        {
            recommendations.Add("New errors detected - review test environment or implementation");
        }

        return recommendations;
    }

    private string ConvertToCsv(SavedTestResultDetailDto result)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Field,Value");
        csv.AppendLine($"Name,\"{result.Name}\"");
        csv.AppendLine($"Description,\"{result.Description}\"");
        csv.AppendLine($"Test URL,\"{result.TestUrl}\"");
        csv.AppendLine($"Success,{result.Success}");
        csv.AppendLine($"Executed At,{result.ExecutedAt:yyyy-MM-dd HH:mm:ss}");
        csv.AppendLine($"Duration (ms),{result.Duration}");
        csv.AppendLine($"Actions Executed,{result.ActionsExecuted}");
        csv.AppendLine($"Error Count,{result.ErrorCount}");
        csv.AppendLine($"Created By,\"{result.CreatedBy}\"");
        return csv.ToString();
    }

    #endregion
} 