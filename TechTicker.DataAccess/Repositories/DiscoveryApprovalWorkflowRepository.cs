using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for DiscoveryApprovalWorkflow entities
/// </summary>
public class DiscoveryApprovalWorkflowRepository : Repository<DiscoveryApprovalWorkflow>, IDiscoveryApprovalWorkflowRepository
{
    public DiscoveryApprovalWorkflowRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DiscoveryApprovalWorkflow>> GetWorkflowByCandidateIdAsync(Guid candidateId)
    {
        return await _dbSet
            .Include(w => w.Reviewer)
            .Include(w => w.Candidate)
            .Where(w => w.CandidateId == candidateId)
            .OrderBy(w => w.ActionDate)
            .ToListAsync();
    }

    public async Task<DiscoveryApprovalWorkflow?> GetLatestWorkflowForCandidateAsync(Guid candidateId)
    {
        return await _dbSet
            .Include(w => w.Reviewer)
            .Include(w => w.Candidate)
            .Where(w => w.CandidateId == candidateId)
            .OrderByDescending(w => w.ActionDate)
            .FirstOrDefaultAsync();
    }

    public async Task<(IEnumerable<DiscoveryApprovalWorkflow> Items, int TotalCount)> GetWorkflowByReviewerAsync(
        Guid reviewerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = _dbSet
            .Include(w => w.Reviewer)
            .Include(w => w.Candidate)
            .Where(w => w.ReviewerId == reviewerId);

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.ActionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.ActionDate <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(w => w.ActionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<DiscoveryApprovalWorkflow>> GetWorkflowByActionAsync(
        WorkflowAction action,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var query = _dbSet
            .Include(w => w.Reviewer)
            .Include(w => w.Candidate)
            .Where(w => w.Action == action);

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.ActionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.ActionDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(w => w.ActionDate)
            .ToListAsync();
    }

    public async Task<DiscoveryApprovalWorkflow?> GetWorkflowWithDetailsAsync(Guid workflowId)
    {
        return await _dbSet
            .Include(w => w.Reviewer)
            .Include(w => w.Candidate)
                .ThenInclude(c => c.SuggestedCategory)
            .Include(w => w.Candidate)
                .ThenInclude(c => c.SimilarProduct)
            .FirstOrDefaultAsync(w => w.WorkflowId == workflowId);
    }

    public async Task<Dictionary<WorkflowAction, int>> GetApprovalStatsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var query = _dbSet.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.ActionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.ActionDate <= toDate.Value);
        }

        var stats = await query
            .GroupBy(w => w.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        // Ensure all actions are represented
        foreach (WorkflowAction action in Enum.GetValues<WorkflowAction>())
        {
            if (!stats.ContainsKey(action))
            {
                stats[action] = 0;
            }
        }

        return stats;
    }

    public async Task<(int TotalReviews, int Approvals, int Rejections, TimeSpan AverageReviewTime)> GetReviewerStatsAsync(
        Guid reviewerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var query = _dbSet
            .Include(w => w.Candidate)
            .Where(w => w.ReviewerId == reviewerId);

        if (fromDate.HasValue)
        {
            query = query.Where(w => w.ActionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(w => w.ActionDate <= toDate.Value);
        }

        var workflows = await query.ToListAsync();

        var totalReviews = workflows.Count;
        var approvals = workflows.Count(w => w.Action == WorkflowAction.Approve || w.Action == WorkflowAction.ApproveWithModifications);
        var rejections = workflows.Count(w => w.Action == WorkflowAction.Reject);

        // Calculate average review time (time from candidate discovery to first review action)
        var reviewTimes = workflows
            .Where(w => w.Action != WorkflowAction.StartReview)
            .Select(w => w.ActionDate - w.Candidate.DiscoveredAt)
            .Where(t => t.TotalMinutes > 0) // Filter out invalid times
            .ToList();

        var averageReviewTime = reviewTimes.Any() 
            ? TimeSpan.FromTicks((long)reviewTimes.Average(t => t.Ticks))
            : TimeSpan.Zero;

        return (totalReviews, approvals, rejections, averageReviewTime);
    }

    public async Task<bool> HasPendingReviewAsync(Guid candidateId)
    {
        var latestWorkflow = await GetLatestWorkflowForCandidateAsync(candidateId);
        
        // If no workflow exists or the latest action is StartReview, it's pending
        return latestWorkflow == null || 
               latestWorkflow.Action == WorkflowAction.StartReview ||
               latestWorkflow.Action == WorkflowAction.RequestModification;
    }
}