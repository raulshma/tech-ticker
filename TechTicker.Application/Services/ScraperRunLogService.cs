using System.Text.Json;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing scraper run logs
/// </summary>
public class ScraperRunLogService : IScraperRunLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScraperRunLogService> _logger;

    public ScraperRunLogService(IUnitOfWork unitOfWork, ILogger<ScraperRunLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<ScraperRunLogSummaryDto>>> GetPagedLogsAsync(ScraperRunLogFilterDto filter)
    {
        try
        {
            var (logs, totalCount) = await _unitOfWork.ScraperRunLogs.GetPagedAsync(
                filter.Page,
                filter.PageSize,
                filter.MappingId,
                filter.Status,
                filter.ErrorCategory,
                filter.DateFrom,
                filter.DateTo,
                filter.SellerName);

            var summaryDtos = logs.Select(MapToSummaryDto).ToList();

            var result = new PagedResultDto<ScraperRunLogSummaryDto>
            {
                Items = summaryDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Result<PagedResultDto<ScraperRunLogSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged scraper run logs");
            return Result<PagedResultDto<ScraperRunLogSummaryDto>>.Failure("Failed to retrieve scraper run logs");
        }
    }

    public async Task<Result<ScraperRunLogDto>> GetRunByIdAsync(Guid runId)
    {
        try
        {
            var runLog = await _unitOfWork.ScraperRunLogs.GetByIdAsync(runId);
            if (runLog == null)
            {
                return Result<ScraperRunLogDto>.Failure("Scraper run log not found");
            }

            var dto = MapToDetailDto(runLog);
            
            // Get retry attempts if this is a parent run
            if (!runLog.IsRetry)
            {
                var retryChain = await _unitOfWork.ScraperRunLogs.GetRetryChainAsync(runId);
                dto.RetryAttempts = retryChain.Where(r => r.IsRetry).Select(MapToSummaryDto).ToList();
            }

            return Result<ScraperRunLogDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scraper run log {RunId}", runId);
            return Result<ScraperRunLogDto>.Failure("Failed to retrieve scraper run log");
        }
    }

    public async Task<Result<PagedResultDto<ScraperRunLogSummaryDto>>> GetLogsByMappingIdAsync(Guid mappingId, int page = 1, int pageSize = 20)
    {
        try
        {
            var (logs, totalCount) = await _unitOfWork.ScraperRunLogs.GetByMappingIdAsync(mappingId, page, pageSize);
            var summaryDtos = logs.Select(MapToSummaryDto).ToList();

            var result = new PagedResultDto<ScraperRunLogSummaryDto>
            {
                Items = summaryDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Result<PagedResultDto<ScraperRunLogSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scraper run logs for mapping {MappingId}", mappingId);
            return Result<PagedResultDto<ScraperRunLogSummaryDto>>.Failure("Failed to retrieve scraper run logs for mapping");
        }
    }

    public async Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetRecentFailedRunsAsync(int count = 10)
    {
        try
        {
            var logs = await _unitOfWork.ScraperRunLogs.GetRecentFailedRunsAsync(count);
            var summaryDtos = logs.Select(MapToSummaryDto);
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Success(summaryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent failed runs");
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Failure("Failed to retrieve recent failed runs");
        }
    }

    public async Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetRecentRunsForMappingAsync(Guid mappingId, int count = 5)
    {
        try
        {
            var logs = await _unitOfWork.ScraperRunLogs.GetRecentRunsForMappingAsync(mappingId, count);
            var summaryDtos = logs.Select(MapToSummaryDto);
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Success(summaryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent runs for mapping {MappingId}", mappingId);
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Failure("Failed to retrieve recent runs for mapping");
        }
    }

    public async Task<Result<ScraperRunStatisticsDto>> GetStatisticsAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid? mappingId = null)
    {
        try
        {
            var stats = await _unitOfWork.ScraperRunLogs.GetStatisticsAsync(dateFrom, dateTo, mappingId);
            
            var dto = new ScraperRunStatisticsDto
            {
                TotalRuns = stats.TotalRuns,
                SuccessfulRuns = stats.SuccessfulRuns,
                FailedRuns = stats.FailedRuns,
                InProgressRuns = stats.InProgressRuns,
                SuccessRate = stats.SuccessRate,
                AverageResponseTime = stats.AverageResponseTime,
                AverageDuration = stats.AverageDuration,
                ErrorCategoryCounts = stats.ErrorCategoryCounts,
                StatusCounts = stats.StatusCounts,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            return Result<ScraperRunStatisticsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scraper run statistics");
            return Result<ScraperRunStatisticsDto>.Failure("Failed to retrieve scraper run statistics");
        }
    }

    public async Task<Result<IEnumerable<ScraperRunLogDto>>> GetRetryChainAsync(Guid runId)
    {
        try
        {
            var retryChain = await _unitOfWork.ScraperRunLogs.GetRetryChainAsync(runId);
            var dtos = retryChain.Select(MapToDetailDto);
            return Result<IEnumerable<ScraperRunLogDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting retry chain for run {RunId}", runId);
            return Result<IEnumerable<ScraperRunLogDto>>.Failure("Failed to retrieve retry chain");
        }
    }

    public async Task<Result<IEnumerable<SellerPerformanceMetricDto>>> GetPerformanceMetricsBySellerAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null)
    {
        try
        {
            var metrics = await _unitOfWork.ScraperRunLogs.GetPerformanceMetricsBySellerAsync(dateFrom, dateTo);
            var dtos = metrics.Select(m => new SellerPerformanceMetricDto
            {
                SellerName = m.SellerName,
                TotalRuns = m.TotalRuns,
                SuccessfulRuns = m.SuccessfulRuns,
                SuccessRate = m.SuccessRate,
                AverageResponseTime = m.AverageResponseTime,
                AverageDuration = m.AverageDuration
            });

            return Result<IEnumerable<SellerPerformanceMetricDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics by seller");
            return Result<IEnumerable<SellerPerformanceMetricDto>>.Failure("Failed to retrieve performance metrics");
        }
    }

    public async Task<Result<Guid>> CreateRunLogAsync(CreateScraperRunLogDto createDto)
    {
        try
        {
            var runLog = new ScraperRunLog
            {
                RunId = Guid.NewGuid(),
                MappingId = createDto.MappingId,
                StartedAt = DateTimeOffset.UtcNow,
                Status = "STARTED",
                TargetUrl = createDto.TargetUrl,
                UserAgent = createDto.UserAgent,
                AdditionalHeaders = createDto.AdditionalHeaders != null ? JsonSerializer.Serialize(createDto.AdditionalHeaders) : null,
                Selectors = createDto.Selectors != null ? JsonSerializer.Serialize(createDto.Selectors) : null,
                AttemptNumber = createDto.AttemptNumber,
                ParentRunId = createDto.ParentRunId,
                DebugNotes = createDto.DebugNotes
            };

            await _unitOfWork.ScraperRunLogs.AddAsync(runLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created scraper run log {RunId} for mapping {MappingId}", runLog.RunId, createDto.MappingId);
            return Result<Guid>.Success(runLog.RunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scraper run log for mapping {MappingId}", createDto.MappingId);
            return Result<Guid>.Failure("Failed to create scraper run log");
        }
    }

    public async Task<Result<bool>> UpdateRunLogAsync(Guid runId, UpdateScraperRunLogDto updateDto)
    {
        try
        {
            var runLog = await _unitOfWork.ScraperRunLogs.GetByIdAsync(runId);
            if (runLog == null)
            {
                return Result<bool>.Failure("Scraper run log not found");
            }

            // Update fields if provided
            if (updateDto.HttpStatusCode.HasValue)
                runLog.HttpStatusCode = updateDto.HttpStatusCode.Value;

            if (updateDto.ResponseTime.HasValue)
                runLog.ResponseTime = updateDto.ResponseTime.Value;

            if (updateDto.ResponseSizeBytes.HasValue)
                runLog.ResponseSizeBytes = updateDto.ResponseSizeBytes.Value;

            if (updateDto.PageLoadTime.HasValue)
                runLog.PageLoadTime = updateDto.PageLoadTime.Value;

            if (updateDto.ParsingTime.HasValue)
                runLog.ParsingTime = updateDto.ParsingTime.Value;

            if (!string.IsNullOrEmpty(updateDto.RawHtmlSnippet))
                runLog.RawHtmlSnippet = updateDto.RawHtmlSnippet;

            if (!string.IsNullOrEmpty(updateDto.DebugNotes))
                runLog.DebugNotes = updateDto.DebugNotes;

            if (!string.IsNullOrEmpty(updateDto.ProxyUsed))
                runLog.ProxyUsed = updateDto.ProxyUsed;

            if (updateDto.ProxyId.HasValue)
                runLog.ProxyId = updateDto.ProxyId.Value;

            _unitOfWork.ScraperRunLogs.Update(runLog);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scraper run log {RunId}", runId);
            return Result<bool>.Failure("Failed to update scraper run log");
        }
    }

    public async Task<Result<bool>> CompleteRunAsync(Guid runId, CompleteScraperRunDto completeDto)
    {
        try
        {
            var runLog = await _unitOfWork.ScraperRunLogs.GetByIdAsync(runId);
            if (runLog == null)
            {
                return Result<bool>.Failure("Scraper run log not found");
            }

            runLog.Status = "SUCCESS";
            runLog.CompletedAt = DateTimeOffset.UtcNow;
            runLog.Duration = runLog.CompletedAt - runLog.StartedAt;
            runLog.ExtractedProductName = completeDto.ExtractedProductName;
            runLog.ExtractedPrice = completeDto.ExtractedPrice;
            runLog.ExtractedStockStatus = completeDto.ExtractedStockStatus;
            runLog.ExtractedSellerName = completeDto.ExtractedSellerName;
            runLog.ExtractedPrimaryImageUrl = completeDto.ExtractedPrimaryImageUrl;
            runLog.ExtractedAdditionalImageUrls = completeDto.ExtractedAdditionalImageUrls != null
                ? JsonSerializer.Serialize(completeDto.ExtractedAdditionalImageUrls)
                : null;
            runLog.ExtractedOriginalImageUrls = completeDto.ExtractedOriginalImageUrls != null
                ? JsonSerializer.Serialize(completeDto.ExtractedOriginalImageUrls)
                : null;
            runLog.ImageProcessingCount = completeDto.ImageProcessingCount;
            runLog.ImageUploadCount = completeDto.ImageUploadCount;
            runLog.ImageScrapingError = completeDto.ImageScrapingError;

            if (completeDto.ResponseTime.HasValue)
                runLog.ResponseTime = completeDto.ResponseTime.Value;

            if (completeDto.PageLoadTime.HasValue)
                runLog.PageLoadTime = completeDto.PageLoadTime.Value;

            if (completeDto.ParsingTime.HasValue)
                runLog.ParsingTime = completeDto.ParsingTime.Value;

            if (!string.IsNullOrEmpty(completeDto.RawHtmlSnippet))
                runLog.RawHtmlSnippet = completeDto.RawHtmlSnippet;

            if (!string.IsNullOrEmpty(completeDto.DebugNotes))
                runLog.DebugNotes = completeDto.DebugNotes;

            if (!string.IsNullOrEmpty(completeDto.ProxyUsed))
                runLog.ProxyUsed = completeDto.ProxyUsed;

            if (completeDto.ProxyId.HasValue)
                runLog.ProxyId = completeDto.ProxyId.Value;

            // Handle specification data
            if (!string.IsNullOrEmpty(completeDto.SpecificationData))
                runLog.SpecificationData = completeDto.SpecificationData;

            if (!string.IsNullOrEmpty(completeDto.SpecificationMetadata))
                runLog.SpecificationMetadata = completeDto.SpecificationMetadata;

            if (completeDto.SpecificationCount.HasValue)
                runLog.SpecificationCount = completeDto.SpecificationCount.Value;

            if (!string.IsNullOrEmpty(completeDto.SpecificationParsingStrategy))
                runLog.SpecificationParsingStrategy = completeDto.SpecificationParsingStrategy;

            if (completeDto.SpecificationQualityScore.HasValue)
                runLog.SpecificationQualityScore = completeDto.SpecificationQualityScore.Value;

            if (completeDto.SpecificationParsingTime.HasValue)
                runLog.SpecificationParsingTime = completeDto.SpecificationParsingTime.Value;

            if (!string.IsNullOrEmpty(completeDto.SpecificationError))
                runLog.SpecificationError = completeDto.SpecificationError;

            _unitOfWork.ScraperRunLogs.Update(runLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Completed scraper run log {RunId} successfully", runId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing scraper run log {RunId}", runId);
            return Result<bool>.Failure("Failed to complete scraper run log");
        }
    }

    public async Task<Result<bool>> FailRunAsync(Guid runId, FailScraperRunDto failDto)
    {
        try
        {
            var runLog = await _unitOfWork.ScraperRunLogs.GetByIdAsync(runId);
            if (runLog == null)
            {
                return Result<bool>.Failure("Scraper run log not found");
            }

            runLog.Status = "FAILED";
            runLog.CompletedAt = DateTimeOffset.UtcNow;
            runLog.Duration = runLog.CompletedAt - runLog.StartedAt;
            runLog.ErrorMessage = failDto.ErrorMessage;
            runLog.ErrorCode = failDto.ErrorCode;
            runLog.ErrorStackTrace = failDto.ErrorStackTrace;
            runLog.ErrorCategory = failDto.ErrorCategory;

            if (failDto.HttpStatusCode.HasValue)
                runLog.HttpStatusCode = failDto.HttpStatusCode.Value;

            if (failDto.ResponseTime.HasValue)
                runLog.ResponseTime = failDto.ResponseTime.Value;

            if (!string.IsNullOrEmpty(failDto.RawHtmlSnippet))
                runLog.RawHtmlSnippet = failDto.RawHtmlSnippet;

            if (!string.IsNullOrEmpty(failDto.DebugNotes))
                runLog.DebugNotes = failDto.DebugNotes;

            if (!string.IsNullOrEmpty(failDto.ProxyUsed))
                runLog.ProxyUsed = failDto.ProxyUsed;

            if (failDto.ProxyId.HasValue)
                runLog.ProxyId = failDto.ProxyId.Value;

            _unitOfWork.ScraperRunLogs.Update(runLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogWarning("Failed scraper run log {RunId} with error: {ErrorMessage}", runId, failDto.ErrorMessage);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error failing scraper run log {RunId}", runId);
            return Result<bool>.Failure("Failed to update scraper run log");
        }
    }

    public async Task<Result<int>> CleanupOldLogsAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
            var deletedCount = await _unitOfWork.ScraperRunLogs.CleanupOldLogsAsync(cutoffDate);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {DeletedCount} old scraper run logs older than {CutoffDate}", deletedCount, cutoffDate);
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old scraper run logs");
            return Result<int>.Failure("Failed to cleanup old logs");
        }
    }

    public async Task<Result<IEnumerable<ScraperRunLogSummaryDto>>> GetInProgressRunsAsync()
    {
        try
        {
            var logs = await _unitOfWork.ScraperRunLogs.GetInProgressRunsAsync();
            var summaryDtos = logs.Select(MapToSummaryDto);
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Success(summaryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting in-progress runs");
            return Result<IEnumerable<ScraperRunLogSummaryDto>>.Failure("Failed to retrieve in-progress runs");
        }
    }

    private static ScraperRunLogSummaryDto MapToSummaryDto(ScraperRunLog runLog)
    {
        return new ScraperRunLogSummaryDto
        {
            RunId = runLog.RunId,
            MappingId = runLog.MappingId,
            StartedAt = runLog.StartedAt,
            CompletedAt = runLog.CompletedAt,
            Duration = runLog.Duration,
            Status = runLog.Status,
            StatusDisplayName = runLog.StatusDisplayName,
            TargetUrl = runLog.TargetUrl,
            ExtractedPrice = runLog.ExtractedPrice,
            ErrorMessage = runLog.ErrorMessage,
            ErrorCategory = runLog.ErrorCategory,
            ErrorCategoryDisplayName = runLog.ErrorCategoryDisplayName,
            AttemptNumber = runLog.AttemptNumber,
            IsRetry = runLog.IsRetry,
            ResponseTime = runLog.ResponseTime,
            SellerName = runLog.Mapping?.SellerName ?? "Unknown",
            ProductName = runLog.Mapping?.Product?.Name ?? "Unknown"
        };
    }

    private static ScraperRunLogDto MapToDetailDto(ScraperRunLog runLog)
    {
        Dictionary<string, string>? additionalHeaders = null;
        ScrapingSelectorsDto? selectors = null;

        try
        {
            if (!string.IsNullOrEmpty(runLog.AdditionalHeaders))
                additionalHeaders = JsonSerializer.Deserialize<Dictionary<string, string>>(runLog.AdditionalHeaders);

            if (!string.IsNullOrEmpty(runLog.Selectors))
                selectors = JsonSerializer.Deserialize<ScrapingSelectorsDto>(runLog.Selectors);
        }
        catch (JsonException)
        {
            // Ignore JSON parsing errors for display purposes
        }

        return new ScraperRunLogDto
        {
            RunId = runLog.RunId,
            MappingId = runLog.MappingId,
            StartedAt = runLog.StartedAt,
            CompletedAt = runLog.CompletedAt,
            Duration = runLog.Duration,
            Status = runLog.Status,
            StatusDisplayName = runLog.StatusDisplayName,
            TargetUrl = runLog.TargetUrl,
            UserAgent = runLog.UserAgent,
            AdditionalHeaders = additionalHeaders,
            Selectors = selectors,
            HttpStatusCode = runLog.HttpStatusCode,
            ResponseTime = runLog.ResponseTime,
            ResponseSizeBytes = runLog.ResponseSizeBytes,
            ProxyUsed = runLog.ProxyUsed,
            ProxyId = runLog.ProxyId,
            ExtractedProductName = runLog.ExtractedProductName,
            ExtractedPrice = runLog.ExtractedPrice,
            ExtractedStockStatus = runLog.ExtractedStockStatus,
            ExtractedSellerName = runLog.ExtractedSellerName,
            ExtractedPrimaryImageUrl = runLog.ExtractedPrimaryImageUrl,
            ExtractedAdditionalImageUrls = ParseJsonStringToList(runLog.ExtractedAdditionalImageUrls),
            ExtractedOriginalImageUrls = ParseJsonStringToList(runLog.ExtractedOriginalImageUrls),
            ImageProcessingCount = runLog.ImageProcessingCount,
            ImageUploadCount = runLog.ImageUploadCount,
            ImageScrapingError = runLog.ImageScrapingError,
            SpecificationData = runLog.SpecificationData,
            SpecificationMetadata = runLog.SpecificationMetadata,
            SpecificationCount = runLog.SpecificationCount,
            SpecificationParsingStrategy = runLog.SpecificationParsingStrategy,
            SpecificationQualityScore = runLog.SpecificationQualityScore,
            SpecificationParsingTime = runLog.SpecificationParsingTime,
            SpecificationError = runLog.SpecificationError,
            ErrorMessage = runLog.ErrorMessage,
            ErrorCode = runLog.ErrorCode,
            ErrorStackTrace = runLog.ErrorStackTrace,
            ErrorCategory = runLog.ErrorCategory,
            ErrorCategoryDisplayName = runLog.ErrorCategoryDisplayName,
            AttemptNumber = runLog.AttemptNumber,
            ParentRunId = runLog.ParentRunId,
            IsRetry = runLog.IsRetry,
            PageLoadTime = runLog.PageLoadTime,
            ParsingTime = runLog.ParsingTime,
            RawHtmlSnippet = runLog.RawHtmlSnippet,
            DebugNotes = runLog.DebugNotes,
            CreatedAt = runLog.CreatedAt,
            Mapping = runLog.Mapping != null ? new ProductSellerMappingDto
            {
                MappingId = runLog.Mapping.MappingId,
                CanonicalProductId = runLog.Mapping.CanonicalProductId,
                SellerName = runLog.Mapping.SellerName,
                ExactProductUrl = runLog.Mapping.ExactProductUrl,
                IsActiveForScraping = runLog.Mapping.IsActiveForScraping,
                LastScrapedAt = runLog.Mapping.LastScrapedAt,
                LastScrapeStatus = runLog.Mapping.LastScrapeStatus,
                ConsecutiveFailureCount = runLog.Mapping.ConsecutiveFailureCount,
                CreatedAt = runLog.Mapping.CreatedAt,
                UpdatedAt = runLog.Mapping.UpdatedAt
            } : null
        };
    }

    private static List<string>? ParseJsonStringToList(string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<string>>(jsonString);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
