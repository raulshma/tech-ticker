using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for ProductDiscoveryCandidate entities
/// </summary>
public class ProductDiscoveryCandidateRepository : Repository<ProductDiscoveryCandidate>, IProductDiscoveryCandidateRepository
{
    public ProductDiscoveryCandidateRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<ProductDiscoveryCandidate> Items, int TotalCount)> GetCandidatesAsync(
        DiscoveryStatus? status = null,
        string? discoveryMethod = null,
        Guid? discoveredByUserId = null,
        DateTimeOffset? discoveredAfter = null,
        DateTimeOffset? discoveredBefore = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _dbSet
            .Include(c => c.SuggestedCategory)
            .Include(c => c.SimilarProduct)
            .Include(c => c.DiscoveredByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(discoveryMethod))
        {
            query = query.Where(c => c.DiscoveryMethod == discoveryMethod);
        }

        if (discoveredByUserId.HasValue)
        {
            query = query.Where(c => c.DiscoveredByUserId == discoveredByUserId.Value);
        }

        if (discoveredAfter.HasValue)
        {
            query = query.Where(c => c.DiscoveredAt >= discoveredAfter.Value);
        }

        if (discoveredBefore.HasValue)
        {
            query = query.Where(c => c.DiscoveredAt <= discoveredBefore.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.DiscoveredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ProductDiscoveryCandidate?> GetCandidateWithDetailsAsync(Guid candidateId)
    {
        return await _dbSet
            .Include(c => c.SuggestedCategory)
            .Include(c => c.SimilarProduct)
            .Include(c => c.DiscoveredByUser)
            .Include(c => c.ApprovalWorkflows)
                .ThenInclude(w => w.Reviewer)
            .FirstOrDefaultAsync(c => c.CandidateId == candidateId);
    }

    public async Task<(IEnumerable<ProductDiscoveryCandidate> Items, int TotalCount)> GetPendingCandidatesAsync(
        int pageNumber = 1, 
        int pageSize = 10)
    {
        return await GetCandidatesAsync(
            status: DiscoveryStatus.Pending,
            pageNumber: pageNumber,
            pageSize: pageSize);
    }

    public async Task<IEnumerable<ProductDiscoveryCandidate>> GetSimilarCandidatesAsync(
        Guid productId, 
        decimal minSimilarityScore = 0.8m)
    {
        return await _dbSet
            .Include(c => c.SuggestedCategory)
            .Include(c => c.SimilarProduct)
            .Where(c => c.SimilarProductId == productId && c.SimilarityScore >= minSimilarityScore)
            .OrderByDescending(c => c.SimilarityScore)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductDiscoveryCandidate>> GetCandidatesByCategoryAsync(
        Guid categoryId, 
        decimal minConfidenceScore = 0.7m)
    {
        return await _dbSet
            .Include(c => c.SuggestedCategory)
            .Include(c => c.SimilarProduct)
            .Where(c => c.SuggestedCategoryId == categoryId && c.CategoryConfidenceScore >= minConfidenceScore)
            .OrderByDescending(c => c.CategoryConfidenceScore)
            .ToListAsync();
    }

    public async Task<ProductDiscoveryCandidate?> GetCandidateBySourceUrlAsync(string sourceUrl)
    {
        return await _dbSet
            .Include(c => c.SuggestedCategory)
            .Include(c => c.SimilarProduct)
            .Include(c => c.DiscoveredByUser)
            .FirstOrDefaultAsync(c => c.SourceUrl == sourceUrl);
    }

    public async Task<(int TotalCandidates, int PendingCandidates, int ApprovedCandidates, int RejectedCandidates)> GetDiscoveryStatsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.DiscoveredAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(c => c.DiscoveredAt <= toDate.Value);
        }

        var totalCandidates = await query.CountAsync();
        var pendingCandidates = await query.CountAsync(c => c.Status == DiscoveryStatus.Pending);
        var approvedCandidates = await query.CountAsync(c => c.Status == DiscoveryStatus.Approved);
        var rejectedCandidates = await query.CountAsync(c => c.Status == DiscoveryStatus.Rejected);

        return (totalCandidates, pendingCandidates, approvedCandidates, rejectedCandidates);
    }

    public async Task<bool> UpdateCandidateStatusAsync(Guid candidateId, DiscoveryStatus status, string? rejectionReason = null)
    {
        var candidate = await _dbSet.FindAsync(candidateId);
        if (candidate == null)
            return false;

        candidate.Status = status;
        candidate.RejectionReason = rejectionReason;
        candidate.UpdatedAt = DateTimeOffset.UtcNow;

        return true;
    }
}