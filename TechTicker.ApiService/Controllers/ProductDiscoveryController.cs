using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Product Discovery operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ProductDiscoveryController : BaseApiController
{
    private readonly IProductDiscoveryService _productDiscoveryService;
    private readonly IDiscoveryWorkflowService _workflowService;
    private readonly IProductSimilarityService _similarityService;

    public ProductDiscoveryController(
        IProductDiscoveryService productDiscoveryService,
        IDiscoveryWorkflowService workflowService,
        IProductSimilarityService similarityService)
    {
        _productDiscoveryService = productDiscoveryService;
        _workflowService = workflowService;
        _similarityService = similarityService;
    }

    /// <summary>
    /// Analyzes a single URL to extract product information
    /// </summary>
    /// <param name="request">URL analysis request</param>
    /// <returns>Discovery result with extracted product information</returns>
    /// <response code="200">URL analyzed successfully</response>
    /// <response code="400">Invalid request or URL analysis failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpPost("analyze-url")]
    [ProducesResponseType(typeof(ApiResponse<DiscoveryResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<DiscoveryResult>>> AnalyzeUrl([FromBody] AnalyzeUrlRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequestGeneric<DiscoveryResult>(string.Join("; ", errors));
        }

        var userId = string.IsNullOrEmpty(CurrentUserId) ? (Guid?)null : Guid.Parse(CurrentUserId);
        var result = await _productDiscoveryService.AnalyzeUrlAsync(request.Url, userId);

        if (result.IsSuccess)
        {
            return Ok(result.Data!, "URL analyzed successfully");
        }

        return result.ErrorCode switch
        {
            "INVALID_URL" => BadRequestGeneric<DiscoveryResult>("The provided URL is invalid or cannot be accessed"),
            "EXTRACTION_FAILED" => BadRequestGeneric<DiscoveryResult>("Failed to extract product information from the URL"),
            "UNSUPPORTED_SITE" => BadRequestGeneric<DiscoveryResult>("The website is not supported for product discovery"),
            _ => BadRequestGeneric<DiscoveryResult>(result.ErrorMessage ?? "URL analysis failed")
        };
    }

    /// <summary>
    /// Analyzes multiple URLs in batch to extract product information
    /// </summary>
    /// <param name="request">Bulk analysis request</param>
    /// <returns>Discovery results for all URLs</returns>
    /// <response code="200">URLs analyzed successfully</response>
    /// <response code="400">Invalid request or bulk analysis failed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpPost("bulk-analyze")]
    [ProducesResponseType(typeof(ApiResponse<DiscoveryResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<DiscoveryResult>>> BulkAnalyzeUrls([FromBody] BulkAnalyzeRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequestGeneric<DiscoveryResult>(string.Join("; ", errors));
        }

        // Validate URLs
        var invalidUrls = request.Urls.Where(url => !Uri.TryCreate(url, UriKind.Absolute, out _)).ToList();
        if (invalidUrls.Any())
        {
            return BadRequestGeneric<DiscoveryResult>($"The following URLs are invalid: {string.Join(", ", invalidUrls)}");
        }

        var userId = string.IsNullOrEmpty(CurrentUserId) ? (Guid?)null : Guid.Parse(CurrentUserId);
        var result = await _productDiscoveryService.BulkAnalyzeUrlsAsync(request.Urls, userId);

        if (result.IsSuccess)
        {
            return Ok(result.Data!, $"Bulk analysis completed. Processed {result.Data!.Metadata.ProcessedUrls} URLs");
        }

        return BadRequestGeneric<DiscoveryResult>(result.ErrorMessage ?? "Bulk analysis failed");
    }

    /// <summary>
    /// Gets paginated list of discovery candidates with optional filtering
    /// </summary>
    /// <param name="filter">Filter criteria for candidates</param>
    /// <returns>Paginated list of discovery candidates</returns>
    /// <response code="200">Candidates retrieved successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpGet("candidates")]
    [ProducesResponseType(typeof(PagedResponse<ProductDiscoveryCandidateDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<PagedResponse<ProductDiscoveryCandidateDto>>> GetCandidates([FromQuery] CandidateFilterRequest filter)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            var errorResponse = PagedResponse<ProductDiscoveryCandidateDto>.FailureResult(
                string.Join("; ", errors), 400);
            errorResponse.CorrelationId = CorrelationId;
            return StatusCode(400, errorResponse);
        }

        var result = await _productDiscoveryService.GetCandidatesAsync(filter);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Gets specific discovery candidate details by ID
    /// </summary>
    /// <param name="candidateId">Candidate ID</param>
    /// <returns>Discovery candidate details</returns>
    /// <response code="200">Candidate found</response>
    /// <response code="404">Candidate not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpGet("candidates/{candidateId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDiscoveryCandidateDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<ProductDiscoveryCandidateDto>>> GetCandidate(Guid candidateId)
    {
        var result = await _productDiscoveryService.GetCandidateAsync(candidateId);
        return HandleResult(result);
    }

    /// <summary>
    /// Approves a discovery candidate and optionally creates a product
    /// </summary>
    /// <param name="candidateId">Candidate ID</param>
    /// <param name="decision">Approval decision with optional modifications</param>
    /// <returns>Approval result</returns>
    /// <response code="200">Candidate approved successfully</response>
    /// <response code="400">Invalid approval request</response>
    /// <response code="404">Candidate not found</response>
    /// <response code="409">Candidate already processed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpPost("candidates/{candidateId:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<ApprovalResult>>> ApproveCandidate(
        Guid candidateId, 
        [FromBody] ApprovalRequest decision)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequestGeneric<ApprovalResult>(string.Join("; ", errors));
        }

        if (string.IsNullOrEmpty(CurrentUserId) || !Guid.TryParse(CurrentUserId, out var reviewerId))
        {
            return BadRequestGeneric<ApprovalResult>("Unable to identify reviewer user ID");
        }

        var approvalDecision = new ApprovalDecision
        {
            Action = decision.Action,
            Comments = decision.Comments,
            Modifications = decision.Modifications,
            CreateProduct = decision.CreateProduct,
            CategoryOverride = decision.CategoryOverride,
            ProductNameOverride = decision.ProductNameOverride
        };

        var result = await _workflowService.ApproveCandidateAsync(candidateId, reviewerId, approvalDecision);
        return HandleResult(result);
    }

    /// <summary>
    /// Rejects a discovery candidate with a reason
    /// </summary>
    /// <param name="candidateId">Candidate ID</param>
    /// <param name="request">Rejection request with reason</param>
    /// <returns>Rejection result</returns>
    /// <response code="200">Candidate rejected successfully</response>
    /// <response code="400">Invalid rejection request</response>
    /// <response code="404">Candidate not found</response>
    /// <response code="409">Candidate already processed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpPost("candidates/{candidateId:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<ApprovalResult>>> RejectCandidate(
        Guid candidateId, 
        [FromBody] RejectionRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequestGeneric<ApprovalResult>(string.Join("; ", errors));
        }

        if (string.IsNullOrEmpty(CurrentUserId) || !Guid.TryParse(CurrentUserId, out var reviewerId))
        {
            return BadRequestGeneric<ApprovalResult>("Unable to identify reviewer user ID");
        }

        var result = await _workflowService.RejectCandidateAsync(candidateId, reviewerId, request.Reason, request.Comments);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets similar products for a discovery candidate
    /// </summary>
    /// <param name="candidateId">Candidate ID</param>
    /// <param name="limit">Maximum number of similar products to return</param>
    /// <returns>List of similar products</returns>
    /// <response code="200">Similar products found</response>
    /// <response code="404">Candidate not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpGet("similar-products/{candidateId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<SimilarProductResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<List<SimilarProductResult>>>> GetSimilarProducts(
        Guid candidateId,
        [FromQuery, Range(1, 50)] int limit = 10)
    {
        var candidateResult = await _productDiscoveryService.GetCandidateAsync(candidateId);
        if (!candidateResult.IsSuccess)
        {
            return NotFoundGeneric<List<SimilarProductResult>>(candidateResult.ErrorMessage ?? "Candidate not found");
        }

        // Convert candidate to extraction result for similarity service
        var extractionResult = new ProductExtractionResult
        {
            IsSuccess = true,
            SourceUrl = candidateResult.Data!.SourceUrl,
            ExtractedProductName = candidateResult.Data.ExtractedProductName,
            ExtractedManufacturer = candidateResult.Data.ExtractedManufacturer,
            ExtractedModelNumber = candidateResult.Data.ExtractedModelNumber,
            ExtractedPrice = candidateResult.Data.ExtractedPrice,
            ExtractedImageUrl = candidateResult.Data.ExtractedImageUrl,
            ExtractedDescription = candidateResult.Data.ExtractedDescription,
            ExtractedSpecifications = candidateResult.Data.ExtractedSpecifications
        };

        var similarProductsResult = await _similarityService.FindSimilarProductsAsync(extractionResult, limit);
        return HandleResult(similarProductsResult);
    }

    /// <summary>
    /// Gets discovery statistics and performance metrics
    /// </summary>
    /// <param name="request">Report period request</param>
    /// <returns>Discovery statistics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="400">Invalid date range</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<object>>> GetDiscoveryStatistics([FromQuery] ReportPeriodRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequestGeneric<object>(string.Join("; ", errors));
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequestGeneric<object>("Start date must be before end date");
        }

        var result = await _productDiscoveryService.GetDiscoveryStatisticsAsync(request.StartDate, request.EndDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Retries failed discovery candidates
    /// </summary>
    /// <param name="candidateIds">List of candidate IDs to retry</param>
    /// <returns>Retry results</returns>
    /// <response code="200">Retry operation completed</response>
    /// <response code="400">Invalid candidate IDs</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin access required</response>
    [HttpPost("retry")]
    [ProducesResponseType(typeof(ApiResponse<DiscoveryResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ApiResponse<DiscoveryResult>>> RetryFailedCandidates([FromBody] List<Guid> candidateIds)
    {
        if (candidateIds == null || !candidateIds.Any())
        {
            return BadRequestGeneric<DiscoveryResult>("At least one candidate ID must be provided");
        }

        if (candidateIds.Count > 50)
        {
            return BadRequestGeneric<DiscoveryResult>("Cannot retry more than 50 candidates at once");
        }

        var result = await _productDiscoveryService.RetryFailedCandidatesAsync(candidateIds);
        return HandleResult(result);
    }
}