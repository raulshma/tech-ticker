using TechTicker.Application.DTOs;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for managing discovery approval workflow
/// </summary>
public interface IDiscoveryWorkflowService
{
    /// <summary>
    /// Starts the approval workflow for a discovery candidate
    /// </summary>
    Task<Result<DiscoveryApprovalWorkflow>> StartApprovalWorkflowAsync(Guid candidateId, Guid reviewerId);
    
    /// <summary>
    /// Processes an approval decision
    /// </summary>
    Task<Result<ApprovalResult>> ProcessApprovalAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision);
    
    /// <summary>
    /// Processes a rejection decision
    /// </summary>
    Task<Result<ApprovalResult>> ProcessRejectionAsync(Guid candidateId, Guid reviewerId, string reason, string? comments = null);
    
    /// <summary>
    /// Requests modifications to a candidate
    /// </summary>
    Task<Result<ApprovalResult>> RequestModificationsAsync(Guid candidateId, Guid reviewerId, Dictionary<string, object> modifications, string? comments = null);
    
    /// <summary>
    /// Approves a candidate with modifications
    /// </summary>
    Task<Result<ApprovalResult>> ApproveWithModificationsAsync(Guid candidateId, Guid reviewerId, Dictionary<string, object> modifications, string? comments = null);
    
    /// <summary>
    /// Gets workflow history for a candidate
    /// </summary>
    Task<Result<List<DiscoveryApprovalWorkflow>>> GetWorkflowHistoryAsync(Guid candidateId);
    
    /// <summary>
    /// Gets pending workflow items for a reviewer
    /// </summary>
    Task<Result<List<ProductDiscoveryCandidateDto>>> GetPendingWorkflowItemsAsync(Guid reviewerId, int maxItems = 50);
    
    /// <summary>
    /// Automatically approves candidates that meet auto-approval criteria
    /// </summary>
    Task<Result<List<ApprovalResult>>> ProcessAutoApprovalsAsync(decimal confidenceThreshold = 0.95m);
    
    /// <summary>
    /// Creates a product from an approved candidate
    /// </summary>
    Task<Result<Guid>> CreateProductFromCandidateAsync(Guid candidateId, Dictionary<string, object>? modifications = null);
    
    /// <summary>
    /// Updates candidate status
    /// </summary>
    Task<Result> UpdateCandidateStatusAsync(Guid candidateId, DiscoveryStatus newStatus, string? reason = null);
    
    /// <summary>
    /// Validates workflow permissions for a user
    /// </summary>
    Task<Result<bool>> ValidateWorkflowPermissionsAsync(Guid userId, WorkflowAction action);
    
    /// <summary>
    /// Approves a candidate with simplified interface
    /// </summary>
    Task<Result<ApprovalResult>> ApproveCandidateAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision);
    
    /// <summary>
    /// Rejects a candidate with simplified interface
    /// </summary>
    Task<Result<ApprovalResult>> RejectCandidateAsync(Guid candidateId, Guid reviewerId, string reason, string? comments = null);
}