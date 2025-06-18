using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Main service for product discovery operations
/// </summary>
public class ProductDiscoveryService : IProductDiscoveryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductDiscoveryCandidateRepository _candidateRepository;
    private readonly IDiscoveryApprovalWorkflowRepository _workflowRepository;
    private readonly IUrlAnalysisService _urlAnalysisService;
    private readonly ICategoryPredictionService _categoryPredictionService;
    private readonly IProductSimilarityService _similarityService;
    private readonly IDiscoveryWorkflowService _workflowService;
    private readonly IMappingService _mappingService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<ProductDiscoveryService> _logger;
    private readonly ProductDiscoveryOptions _options;

    public ProductDiscoveryService(
        IUnitOfWork unitOfWork,
        IProductDiscoveryCandidateRepository candidateRepository,
        IDiscoveryApprovalWorkflowRepository workflowRepository,
        IUrlAnalysisService urlAnalysisService,
        ICategoryPredictionService categoryPredictionService,
        IProductSimilarityService similarityService,
        IDiscoveryWorkflowService workflowService,
        IMappingService mappingService,
        IMessagePublisher messagePublisher,
        IOptions<ProductDiscoveryOptions> options,
        ILogger<ProductDiscoveryService> logger)
    {
        _unitOfWork = unitOfWork;
        _candidateRepository = candidateRepository;
        _workflowRepository = workflowRepository;
        _urlAnalysisService = urlAnalysisService;
        _categoryPredictionService = categoryPredictionService;
        _similarityService = similarityService;
        _workflowService = workflowService;
        _mappingService = mappingService;
        _messagePublisher = messagePublisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<DiscoveryResult>> AnalyzeUrlAsync(string url, Guid? userId = null)
    {
        var startTime = DateTimeOffset.UtcNow;
        var commandId = Guid.NewGuid();
        
        try
        {
            _logger.LogInformation("Starting URL analysis for {Url} with CommandId {CommandId}", url, commandId);

            // Step 1: Validate and analyze URL
            var urlValidationResult = await _urlAnalysisService.IsValidProductUrlAsync(url);
            if (urlValidationResult.IsFailure)
            {
                return Result<DiscoveryResult>.Failure($"URL validation failed: {urlValidationResult.ErrorMessage}");
            }

            if (!urlValidationResult.Data)
            {
                return Result<DiscoveryResult>.Failure("URL does not appear to be a valid product page");
            }

            // Step 2: Extract product data
            var extractionResult = await _urlAnalysisService.ExtractProductDataAsync(url);
            if (extractionResult.IsFailure)
            {
                await PublishFailureEventAsync(commandId, url, extractionResult.ErrorMessage!, userId);
                return Result<DiscoveryResult>.Failure($"Product extraction failed: {extractionResult.ErrorMessage}");
            }

            var extractedData = extractionResult.Data!;

            // Step 3: Predict category
            CategoryPrediction? categoryPrediction = null;
            var categoryResult = await _categoryPredictionService.PredictCategoryAsync(extractedData);
            if (categoryResult.IsSuccess)
            {
                categoryPrediction = categoryResult.Data!;
            }
            else
            {
                _logger.LogWarning("Category prediction failed for {Url}: {Error}", url, categoryResult.ErrorMessage);
            }

            // Step 4: Find similar products
            var similarProductsResult = await _similarityService.FindSimilarProductsAsync(extractedData);
            var similarProducts = similarProductsResult.IsSuccess ? similarProductsResult.Data! : new List<SimilarProductResult>();

            // Step 5: Create discovery candidate
            var candidate = new ProductDiscoveryCandidate
            {
                CandidateId = Guid.NewGuid(),
                SourceUrl = url,
                ExtractedProductName = extractedData.ExtractedProductName ?? "Unknown Product",
                ExtractedManufacturer = extractedData.ExtractedManufacturer,
                ExtractedModelNumber = extractedData.ExtractedModelNumber,
                ExtractedPrice = extractedData.ExtractedPrice,
                ExtractedImageUrl = extractedData.ExtractedImageUrl,
                ExtractedDescription = extractedData.ExtractedDescription,
                ExtractedSpecifications = extractedData.ExtractedSpecifications != null ? 
                    System.Text.Json.JsonSerializer.Serialize(extractedData.ExtractedSpecifications) : null,
                SuggestedCategoryId = categoryPrediction?.CategoryId,
                CategoryConfidenceScore = categoryPrediction?.ConfidenceScore ?? 0,
                SimilarProductId = similarProducts.FirstOrDefault()?.ProductId,
                SimilarityScore = similarProducts.FirstOrDefault()?.SimilarityScore ?? 0,
                DiscoveryMethod = "URL_ANALYSIS",
                DiscoveredByUserId = userId,
                DiscoveredAt = DateTimeOffset.UtcNow,
                Status = DiscoveryStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Step 6: Save candidate
            await _candidateRepository.AddAsync(candidate);
            await _unitOfWork.SaveChangesAsync();

            // Step 7: Map to DTO
            var candidateDto = MapCandidateToDto(candidate);

            var processingTime = DateTimeOffset.UtcNow - startTime;
            var discoveryResult = new DiscoveryResult
            {
                IsSuccess = true,
                Candidates = new List<ProductDiscoveryCandidateDto> { candidateDto },
                Metadata = new DiscoveryMetadata
                {
                    ProcessedUrls = 1,
                    SuccessfulExtractions = 1,
                    FailedExtractions = 0,
                    ProcessingTime = processingTime
                }
            };

            // Step 8: Publish success event
            await PublishSuccessEventAsync(commandId, new List<Guid> { candidate.CandidateId }, processingTime, userId);

            _logger.LogInformation("Successfully analyzed URL {Url} and created candidate {CandidateId}", url, candidate.CandidateId);
            
            return Result<DiscoveryResult>.Success(discoveryResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing URL {Url} with CommandId {CommandId}", url, commandId);
            await PublishFailureEventAsync(commandId, url, ex.Message, userId);
            return Result<DiscoveryResult>.Failure(ex);
        }
    }

    public async Task<Result<DiscoveryResult>> BulkAnalyzeUrlsAsync(IEnumerable<string> urls, Guid? userId = null)
    {
        var startTime = DateTimeOffset.UtcNow;
        var commandId = Guid.NewGuid();
        var urlList = urls.ToList();
        
        try
        {
            _logger.LogInformation("Starting bulk URL analysis for {Count} URLs with CommandId {CommandId}", urlList.Count, commandId);

            var candidates = new List<ProductDiscoveryCandidateDto>();
            var successCount = 0;
            var failureCount = 0;
            var warnings = new List<string>();
            var errors = new List<string>();

            // Process URLs in parallel with limited concurrency
            var semaphore = new SemaphoreSlim(_options.MaxConcurrentAnalysis, _options.MaxConcurrentAnalysis);
            var tasks = urlList.Select(async url =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var result = await AnalyzeUrlAsync(url, userId);
                    if (result.IsSuccess && result.Data!.Candidates.Any())
                    {
                        return new { Success = true, Candidate = (ProductDiscoveryCandidateDto?)result.Data.Candidates.First(), Error = (string?)null };
                    }
                    else
                    {
                        return new { Success = false, Candidate = (ProductDiscoveryCandidateDto?)null, Error = (string?)(result.ErrorMessage ?? "Unknown error") };
                    }
                }
                catch (Exception ex)
                {
                    return new { Success = false, Candidate = (ProductDiscoveryCandidateDto?)null, Error = (string?)ex.Message };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                if (result.Success && result.Candidate != null)
                {
                    candidates.Add(result.Candidate);
                    successCount++;
                }
                else
                {
                    failureCount++;
                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        errors.Add(result.Error);
                    }
                }
            }

            var processingTime = DateTimeOffset.UtcNow - startTime;
            var discoveryResult = new DiscoveryResult
            {
                IsSuccess = successCount > 0,
                Candidates = candidates,
                Metadata = new DiscoveryMetadata
                {
                    ProcessedUrls = urlList.Count,
                    SuccessfulExtractions = successCount,
                    FailedExtractions = failureCount,
                    ProcessingTime = processingTime,
                    Warnings = warnings,
                    Errors = errors
                }
            };

            // Publish bulk analysis event
            await PublishBulkSuccessEventAsync(commandId, candidates.Select(c => c.CandidateId).ToList(), 
                urlList.Count, successCount, failureCount, processingTime, userId);

            _logger.LogInformation("Completed bulk URL analysis: {Success}/{Total} successful", successCount, urlList.Count);
            
            return Result<DiscoveryResult>.Success(discoveryResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk URL analysis with CommandId {CommandId}", commandId);
            return Result<DiscoveryResult>.Failure(ex);
        }
    }

    public async Task<Result<ProductDiscoveryCandidateDto>> GetCandidateAsync(Guid candidateId)
    {
        try
        {
            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<ProductDiscoveryCandidateDto>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            var dto = MapCandidateToDto(candidate);
            return Result<ProductDiscoveryCandidateDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidate {CandidateId}", candidateId);
            return Result<ProductDiscoveryCandidateDto>.Failure(ex);
        }
    }

    public async Task<Result<PagedResponse<ProductDiscoveryCandidateDto>>> GetPendingCandidatesAsync(int page, int pageSize)
    {
        try
        {
            var result = await _candidateRepository.GetPendingCandidatesAsync(page, pageSize);
            var candidates = result.Items;
            var totalCount = result.TotalCount;

            var dtos = new List<ProductDiscoveryCandidateDto>();
            foreach (var candidate in candidates)
            {
                var dto = _mappingService.MapToDto(candidate);
                dtos.Add(dto);
            }

            var pagedResponse = PagedResponse<ProductDiscoveryCandidateDto>.SuccessResult(
                dtos, page, pageSize, totalCount);

            return Result<PagedResponse<ProductDiscoveryCandidateDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending candidates for page {Page}", page);
            return Result<PagedResponse<ProductDiscoveryCandidateDto>>.Failure(ex);
        }
    }

    public async Task<Result<ApprovalResult>> ApproveCandidateAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision)
    {
        try
        {
            return await _workflowService.ProcessApprovalAsync(candidateId, reviewerId, decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving candidate {CandidateId} by reviewer {ReviewerId}", candidateId, reviewerId);
            return Result<ApprovalResult>.Failure(ex);
        }
    }

    private ProductDiscoveryCandidateDto MapCandidateToDto(ProductDiscoveryCandidate candidate)
    {
        // Use the mapping service to convert entity to DTO
        return _mappingService.MapToDto(candidate);
    }

    private async Task PublishSuccessEventAsync(Guid commandId, List<Guid> candidateIds, TimeSpan processingTime, Guid? userId)
    {
        var eventMessage = new ProductDiscoveryEvent
        {
            CommandId = commandId,
            EventType = "AnalysisCompleted",
            IsSuccess = true,
            CandidateIds = candidateIds,
            ProcessedUrls = 1,
            SuccessfulExtractions = 1,
            FailedExtractions = 0,
            ProcessingTime = processingTime,
            UserId = userId
        };

        await _messagePublisher.PublishAsync(eventMessage, "product-discovery-events");
    }

    private async Task PublishBulkSuccessEventAsync(Guid commandId, List<Guid> candidateIds, int processed, 
        int successful, int failed, TimeSpan processingTime, Guid? userId)
    {
        var eventMessage = new ProductDiscoveryEvent
        {
            CommandId = commandId,
            EventType = "BulkAnalysisCompleted",
            IsSuccess = successful > 0,
            CandidateIds = candidateIds,
            ProcessedUrls = processed,
            SuccessfulExtractions = successful,
            FailedExtractions = failed,
            ProcessingTime = processingTime,
            UserId = userId
        };

        await _messagePublisher.PublishAsync(eventMessage, "product-discovery-events");
    }

    private async Task PublishFailureEventAsync(Guid commandId, string url, string errorMessage, Guid? userId)
    {
        var eventMessage = new UrlAnalysisFailedEvent
        {
            Url = url,
            ErrorMessage = errorMessage,
            FailedAt = DateTimeOffset.UtcNow
        };

        await _messagePublisher.PublishAsync(eventMessage, "url-analysis-failed");

        var discoveryEvent = new ProductDiscoveryEvent
        {
            CommandId = commandId,
            EventType = "AnalysisFailed",
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ProcessedUrls = 1,
            SuccessfulExtractions = 0,
            FailedExtractions = 1,
            UserId = userId
        };

        await _messagePublisher.PublishAsync(discoveryEvent, "product-discovery-events");
    }

    public async Task<Result<PagedResponse<ProductDiscoveryCandidateDto>>> GetCandidatesAsync(CandidateFilterRequest filter)
    {
        try
        {
            // Use the status from filter directly (it's already DiscoveryStatus?)
            var status = filter.Status;

            // Build filter for repository - use simplified filtering for now
            var candidates = await _candidateRepository.GetPendingCandidatesAsync(filter.PageNumber, filter.PageSize);

            var candidateDtos = candidates.Items.Select(_mappingService.MapToDto).ToList();
            var pagedResponse = PagedResponse<ProductDiscoveryCandidateDto>.SuccessResult(
                candidateDtos, filter.PageNumber, filter.PageSize, candidates.TotalCount);

            return Result<PagedResponse<ProductDiscoveryCandidateDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candidates with filter");
            return Result<PagedResponse<ProductDiscoveryCandidateDto>>.Failure(ex);
        }
    }

    public async Task<Result<object>> GetDiscoveryStatisticsAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        try
        {
            // Get basic statistics for now
            var allCandidates = await _candidateRepository.GetAllAsync();
            var filteredCandidates = allCandidates.Where(c => c.DiscoveredAt >= startDate && c.DiscoveredAt <= endDate);

            var statistics = new
            {
                TotalCandidates = filteredCandidates.Count(),
                PendingCandidates = filteredCandidates.Count(c => c.Status == DiscoveryStatus.Pending),
                ApprovedCandidates = filteredCandidates.Count(c => c.Status == DiscoveryStatus.Approved),
                RejectedCandidates = filteredCandidates.Count(c => c.Status == DiscoveryStatus.Rejected),
                AverageConfidenceScore = filteredCandidates.Any() ? filteredCandidates.Average(c => (double)c.CategoryConfidenceScore) : 0,
                StartDate = startDate,
                EndDate = endDate
            };

            return Result<object>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discovery statistics");
            return Result<object>.Failure(ex);
        }
    }

    public async Task<Result<DiscoveryResult>> RetryFailedCandidatesAsync(List<Guid> candidateIds)
    {
        try
        {
            var result = new DiscoveryResult
            {
                IsSuccess = true,
                Metadata = new DiscoveryMetadata
                {
                    ProcessedUrls = candidateIds.Count,
                    SuccessfulExtractions = 0,
                    FailedExtractions = 0
                }
            };

            foreach (var candidateId in candidateIds)
            {
                var candidate = await _candidateRepository.GetByIdAsync(candidateId);
                if (candidate != null && candidate.Status == DiscoveryStatus.Rejected)
                {
                    // Retry analysis
                    var retryResult = await AnalyzeUrlAsync(candidate.SourceUrl, candidate.DiscoveredByUserId);
                    if (retryResult.IsSuccess)
                    {
                        result.Metadata.SuccessfulExtractions++;
                        result.Candidates.AddRange(retryResult.Data!.Candidates);
                    }
                    else
                    {
                        result.Metadata.FailedExtractions++;
                        result.Metadata.Errors.Add($"Failed to retry candidate {candidateId}: {retryResult.ErrorMessage}");
                    }
                }
            }

            return Result<DiscoveryResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying failed candidates");
            return Result<DiscoveryResult>.Failure(ex);
        }
    }
}