using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for ProductDiscoveryCandidate entities
/// </summary>
public interface IProductDiscoveryCandidateRepository : IRepository<ProductDiscoveryCandidate>
{
    /// <summary>
    /// Gets paginated list of candidates with optional filtering
    /// </summary>
    Task<(IEnumerable<ProductDiscoveryCandidate> Items, int TotalCount)> GetCandidatesAsync(
        DiscoveryStatus? status = null,
        string? discoveryMethod = null,
        Guid? discoveredByUserId = null,
        DateTimeOffset? discoveredAfter = null,
        DateTimeOffset? discoveredBefore = null,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets a candidate by ID with all related entities
    /// </summary>
    Task<ProductDiscoveryCandidate?> GetCandidateWithDetailsAsync(Guid candidateId);

    /// <summary>
    /// Gets candidates that are pending review
    /// </summary>
    Task<(IEnumerable<ProductDiscoveryCandidate> Items, int TotalCount)> GetPendingCandidatesAsync(
        int pageNumber = 1, 
        int pageSize = 10);

    /// <summary>
    /// Gets candidates by similarity to an existing product
    /// </summary>
    Task<IEnumerable<ProductDiscoveryCandidate>> GetSimilarCandidatesAsync(
        Guid productId, 
        decimal minSimilarityScore = 0.8m);

    /// <summary>
    /// Gets candidates by suggested category
    /// </summary>
    Task<IEnumerable<ProductDiscoveryCandidate>> GetCandidatesByCategoryAsync(
        Guid categoryId, 
        decimal minConfidenceScore = 0.7m);

    /// <summary>
    /// Gets candidates discovered from a specific URL
    /// </summary>
    Task<ProductDiscoveryCandidate?> GetCandidateBySourceUrlAsync(string sourceUrl);

    /// <summary>
    /// Gets discovery statistics for dashboard
    /// </summary>
    Task<(int TotalCandidates, int PendingCandidates, int ApprovedCandidates, int RejectedCandidates)> GetDiscoveryStatsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    /// <summary>
    /// Updates candidate status
    /// </summary>
    Task<bool> UpdateCandidateStatusAsync(Guid candidateId, DiscoveryStatus status, string? rejectionReason = null);
}