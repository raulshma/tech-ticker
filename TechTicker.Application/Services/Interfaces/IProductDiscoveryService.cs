using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Main service for product discovery operations
/// </summary>
public interface IProductDiscoveryService
{
    /// <summary>
    /// Analyzes a single URL to extract product information
    /// </summary>
    Task<Result<DiscoveryResult>> AnalyzeUrlAsync(string url, Guid? userId = null);
    
    /// <summary>
    /// Analyzes multiple URLs in batch
    /// </summary>
    Task<Result<DiscoveryResult>> BulkAnalyzeUrlsAsync(IEnumerable<string> urls, Guid? userId = null);
    
    /// <summary>
    /// Retrieves a discovery candidate by ID
    /// </summary>
    Task<Result<ProductDiscoveryCandidateDto>> GetCandidateAsync(Guid candidateId);
    
    /// <summary>
    /// Gets paginated list of pending candidates for review
    /// </summary>
    Task<Result<PagedResponse<ProductDiscoveryCandidateDto>>> GetPendingCandidatesAsync(int page, int pageSize);
    
    /// <summary>
    /// Processes approval/rejection of a discovery candidate
    /// </summary>
    Task<Result<ApprovalResult>> ApproveCandidateAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision);
    
    /// <summary>
    /// Gets paginated list of candidates with filtering
    /// </summary>
    Task<Result<PagedResponse<ProductDiscoveryCandidateDto>>> GetCandidatesAsync(CandidateFilterRequest filter);
    
    /// <summary>
    /// Gets discovery statistics for a time period
    /// </summary>
    Task<Result<object>> GetDiscoveryStatisticsAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Retries failed discovery candidates
    /// </summary>
    Task<Result<DiscoveryResult>> RetryFailedCandidatesAsync(List<Guid> candidateIds);
}