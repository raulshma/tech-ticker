using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for DiscoveryApprovalWorkflow entities
/// </summary>
public interface IDiscoveryApprovalWorkflowRepository : IRepository<DiscoveryApprovalWorkflow>
{
    /// <summary>
    /// Gets all workflow entries for a specific candidate
    /// </summary>
    Task<IEnumerable<DiscoveryApprovalWorkflow>> GetWorkflowByCandidateIdAsync(Guid candidateId);

    /// <summary>
    /// Gets the latest workflow entry for a specific candidate
    /// </summary>
    Task<DiscoveryApprovalWorkflow?> GetLatestWorkflowForCandidateAsync(Guid candidateId);

    /// <summary>
    /// Gets workflow entries by reviewer
    /// </summary>
    Task<(IEnumerable<DiscoveryApprovalWorkflow> Items, int TotalCount)> GetWorkflowByReviewerAsync(
        Guid reviewerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets workflow entries by action type
    /// </summary>
    Task<IEnumerable<DiscoveryApprovalWorkflow>> GetWorkflowByActionAsync(
        WorkflowAction action,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    /// <summary>
    /// Gets workflow with full candidate and reviewer details
    /// </summary>
    Task<DiscoveryApprovalWorkflow?> GetWorkflowWithDetailsAsync(Guid workflowId);

    /// <summary>
    /// Gets approval statistics for dashboard
    /// </summary>
    Task<Dictionary<WorkflowAction, int>> GetApprovalStatsAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    /// <summary>
    /// Gets reviewer performance statistics
    /// </summary>
    Task<(int TotalReviews, int Approvals, int Rejections, TimeSpan AverageReviewTime)> GetReviewerStatsAsync(
        Guid reviewerId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    /// <summary>
    /// Checks if a candidate has any pending reviews
    /// </summary>
    Task<bool> HasPendingReviewAsync(Guid candidateId);
}