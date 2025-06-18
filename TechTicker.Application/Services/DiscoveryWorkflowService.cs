using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;
using System.Text.Json;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing discovery approval workflow
/// </summary>
public class DiscoveryWorkflowService : IDiscoveryWorkflowService
{
    private readonly IProductDiscoveryCandidateRepository _candidateRepository;
    private readonly IDiscoveryApprovalWorkflowRepository _workflowRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductSellerMappingRepository _mappingRepository;
    private readonly IMappingService _mappingService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<DiscoveryWorkflowService> _logger;
    private readonly ProductDiscoveryOptions _options;

    public DiscoveryWorkflowService(
        IProductDiscoveryCandidateRepository candidateRepository,
        IDiscoveryApprovalWorkflowRepository workflowRepository,
        IProductRepository productRepository,
        IProductSellerMappingRepository mappingRepository,
        IMappingService mappingService,
        IMessagePublisher messagePublisher,
        IOptions<ProductDiscoveryOptions> options,
        ILogger<DiscoveryWorkflowService> logger)
    {
        _candidateRepository = candidateRepository;
        _workflowRepository = workflowRepository;
        _productRepository = productRepository;
        _mappingRepository = mappingRepository;
        _mappingService = mappingService;
        _messagePublisher = messagePublisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<DiscoveryApprovalWorkflow>> StartApprovalWorkflowAsync(Guid candidateId, Guid reviewerId)
    {
        try
        {
            _logger.LogInformation("Starting approval workflow for candidate {CandidateId} by reviewer {ReviewerId}", 
                candidateId, reviewerId);

            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<DiscoveryApprovalWorkflow>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            if (candidate.Status != DiscoveryStatus.Pending)
            {
                return Result<DiscoveryApprovalWorkflow>.Failure(
                    $"Candidate is not in pending status. Current status: {candidate.Status}", "INVALID_STATUS");
            }

            // Update candidate status
            candidate.Status = DiscoveryStatus.UnderReview;
            candidate.UpdatedAt = DateTimeOffset.UtcNow;
            _candidateRepository.Update(candidate);

            // Create workflow entry
            var workflow = new DiscoveryApprovalWorkflow
            {
                WorkflowId = Guid.NewGuid(),
                CandidateId = candidateId,
                ReviewerId = reviewerId,
                Action = WorkflowAction.StartReview,
                Comments = "Review started",
                ActionDate = DateTimeOffset.UtcNow
            };

            await _workflowRepository.AddAsync(workflow);

            _logger.LogInformation("Started approval workflow {WorkflowId} for candidate {CandidateId}", 
                workflow.WorkflowId, candidateId);

            return Result<DiscoveryApprovalWorkflow>.Success(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting approval workflow for candidate {CandidateId}", candidateId);
            return Result<DiscoveryApprovalWorkflow>.Failure(ex);
        }
    }

    public async Task<Result<ApprovalResult>> ProcessApprovalAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision)
    {
        try
        {
            _logger.LogInformation("Processing approval for candidate {CandidateId} by reviewer {ReviewerId} with action {Action}", 
                candidateId, reviewerId, decision.Action);

            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<ApprovalResult>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            var result = decision.Action switch
            {
                WorkflowAction.Approve => await ProcessSimpleApprovalAsync(candidate, reviewerId, decision),
                WorkflowAction.ApproveWithModifications => await ProcessApprovalWithModificationsAsync(candidate, reviewerId, decision),
                _ => Result<ApprovalResult>.Failure($"Invalid action for approval: {decision.Action}")
            };

            if (result.IsSuccess)
            {
                // Publish approval event
                await PublishApprovalEventAsync(candidateId, result.Data!.CreatedProductId, reviewerId, decision);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for candidate {CandidateId}", candidateId);
            return Result<ApprovalResult>.Failure(ex);
        }
    }

    public async Task<Result<ApprovalResult>> ProcessRejectionAsync(Guid candidateId, Guid reviewerId, string reason, string? comments = null)
    {
        try
        {
            _logger.LogInformation("Processing rejection for candidate {CandidateId} by reviewer {ReviewerId}", 
                candidateId, reviewerId);

            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<ApprovalResult>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            // Update candidate status
            candidate.Status = DiscoveryStatus.Rejected;
            candidate.RejectionReason = reason;
            candidate.UpdatedAt = DateTimeOffset.UtcNow;
            _candidateRepository.Update(candidate);

            // Create workflow entry
            var workflow = new DiscoveryApprovalWorkflow
            {
                WorkflowId = Guid.NewGuid(),
                CandidateId = candidateId,
                ReviewerId = reviewerId,
                Action = WorkflowAction.Reject,
                Comments = comments ?? reason,
                ActionDate = DateTimeOffset.UtcNow
            };

            await _workflowRepository.AddAsync(workflow);

            var result = new ApprovalResult
            {
                IsSuccess = true,
                CandidateId = candidateId,
                NewStatus = DiscoveryStatus.Rejected,
                WorkflowNotes = $"Rejected: {reason}"
            };

            // Publish rejection event
            await PublishRejectionEventAsync(candidateId, reviewerId, reason, comments);

            _logger.LogInformation("Rejected candidate {CandidateId} with reason: {Reason}", candidateId, reason);

            return Result<ApprovalResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rejection for candidate {CandidateId}", candidateId);
            return Result<ApprovalResult>.Failure(ex);
        }
    }

    public async Task<Result<ApprovalResult>> RequestModificationsAsync(Guid candidateId, Guid reviewerId, Dictionary<string, object> modifications, string? comments = null)
    {
        try
        {
            _logger.LogInformation("Requesting modifications for candidate {CandidateId} by reviewer {ReviewerId}", 
                candidateId, reviewerId);

            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<ApprovalResult>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            // Update candidate status
            candidate.Status = DiscoveryStatus.RequiresMoreInfo;
            candidate.UpdatedAt = DateTimeOffset.UtcNow;
            _candidateRepository.Update(candidate);

            // Create workflow entry
            var workflow = new DiscoveryApprovalWorkflow
            {
                WorkflowId = Guid.NewGuid(),
                CandidateId = candidateId,
                ReviewerId = reviewerId,
                Action = WorkflowAction.RequestModification,
                Comments = comments,
                Modifications = JsonSerializer.Serialize(modifications),
                ActionDate = DateTimeOffset.UtcNow
            };

            await _workflowRepository.AddAsync(workflow);

            var result = new ApprovalResult
            {
                IsSuccess = true,
                CandidateId = candidateId,
                NewStatus = DiscoveryStatus.RequiresMoreInfo,
                WorkflowNotes = "Modifications requested"
            };

            _logger.LogInformation("Requested modifications for candidate {CandidateId}", candidateId);

            return Result<ApprovalResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting modifications for candidate {CandidateId}", candidateId);
            return Result<ApprovalResult>.Failure(ex);
        }
    }

    public async Task<Result<ApprovalResult>> ApproveWithModificationsAsync(Guid candidateId, Guid reviewerId, Dictionary<string, object> modifications, string? comments = null)
    {
        try
        {
            var decision = new ApprovalDecision
            {
                Action = WorkflowAction.ApproveWithModifications,
                Comments = comments,
                Modifications = modifications,
                CreateProduct = true
            };

            return await ProcessApprovalAsync(candidateId, reviewerId, decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving with modifications for candidate {CandidateId}", candidateId);
            return Result<ApprovalResult>.Failure(ex);
        }
    }

    public async Task<Result<List<DiscoveryApprovalWorkflow>>> GetWorkflowHistoryAsync(Guid candidateId)
    {
        try
        {
            var workflows = await _workflowRepository.FindAsync(w => w.CandidateId == candidateId);
            return Result<List<DiscoveryApprovalWorkflow>>.Success(workflows.OrderBy(w => w.ActionDate).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow history for candidate {CandidateId}", candidateId);
            return Result<List<DiscoveryApprovalWorkflow>>.Failure(ex);
        }
    }

    public async Task<Result<List<ProductDiscoveryCandidateDto>>> GetPendingWorkflowItemsAsync(Guid reviewerId, int maxItems = 50)
    {
        try
        {
            var result = await _candidateRepository.GetPendingCandidatesAsync(1, maxItems);
            var candidates = result.Items;

            var dtos = new List<ProductDiscoveryCandidateDto>();
            foreach (var candidate in candidates)
            {
                var dto = _mappingService.MapToDto(candidate);
                dtos.Add(dto);
            }

            return Result<List<ProductDiscoveryCandidateDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending workflow items for reviewer {ReviewerId}", reviewerId);
            return Result<List<ProductDiscoveryCandidateDto>>.Failure(ex);
        }
    }

    public async Task<Result<List<ApprovalResult>>> ProcessAutoApprovalsAsync(decimal? confidenceThreshold = null)
    {
        try
        {
            var threshold = confidenceThreshold ?? _options.AutoApprovalThreshold;
            _logger.LogInformation("Processing auto-approvals with confidence threshold {Threshold}", threshold);

            var result = await _candidateRepository.GetPendingCandidatesAsync(1, 100);
            var pendingCandidates = result.Items;

            var approvalResults = new List<ApprovalResult>();

            foreach (var candidate in pendingCandidates)
            {
                // Check if candidate meets auto-approval criteria
                if (candidate.CategoryConfidenceScore >= threshold &&
                    candidate.SimilarityScore < _options.SimilarityScoreThreshold) // Not too similar to existing products
                {
                    try
                    {
                        var autoDecision = new ApprovalDecision
                        {
                            Action = WorkflowAction.Approve,
                            Comments = $"Auto-approved with confidence {candidate.CategoryConfidenceScore:P1}",
                            CreateProduct = true
                        };

                        var systemUserId = Guid.Empty; // System user for auto-approvals
                        var approvalResult = await ProcessApprovalAsync(candidate.CandidateId, systemUserId, autoDecision);

                        if (approvalResult.IsSuccess)
                        {
                            approvalResults.Add(approvalResult.Data!);
                            _logger.LogInformation("Auto-approved candidate {CandidateId} with confidence {Confidence}", 
                                candidate.CandidateId, candidate.CategoryConfidenceScore);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to auto-approve candidate {CandidateId}", candidate.CandidateId);
                    }
                }
            }

            _logger.LogInformation("Processed {Count} auto-approvals", approvalResults.Count);
            return Result<List<ApprovalResult>>.Success(approvalResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auto-approvals");
            return Result<List<ApprovalResult>>.Failure(ex);
        }
    }

    public async Task<Result<Guid>> CreateProductFromCandidateAsync(Guid candidateId, Dictionary<string, object>? modifications = null)
    {
        try
        {
            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result<Guid>.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            // Apply modifications if provided
            var productName = GetModifiedValue(modifications, "ProductName", candidate.ExtractedProductName);
            var manufacturer = GetModifiedValue(modifications, "Manufacturer", candidate.ExtractedManufacturer);
            var modelNumber = GetModifiedValue(modifications, "ModelNumber", candidate.ExtractedModelNumber);
            var categoryId = GetModifiedValue(modifications, "CategoryId", candidate.SuggestedCategoryId?.ToString())?.let(s => Guid.TryParse(s, out var id) ? id : candidate.SuggestedCategoryId) ?? candidate.SuggestedCategoryId;

            // Create new product
            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                Name = productName ?? "Unknown Product",
                Manufacturer = manufacturer,
                ModelNumber = modelNumber,
                CategoryId = categoryId ?? Guid.Empty,
                Description = candidate.ExtractedDescription,
                Specifications = candidate.ExtractedSpecifications,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _productRepository.AddAsync(product);

            // Create product-seller mapping for the discovered URL
            var mapping = new ProductSellerMapping
            {
                MappingId = Guid.NewGuid(),
                CanonicalProductId = product.ProductId,
                SellerName = ExtractSellerFromUrl(candidate.SourceUrl),
                ExactProductUrl = candidate.SourceUrl,
                IsActiveForScraping = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _mappingRepository.AddAsync(mapping);

            _logger.LogInformation("Created product {ProductId} from candidate {CandidateId}", 
                product.ProductId, candidateId);

            return Result<Guid>.Success(product.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product from candidate {CandidateId}", candidateId);
            return Result<Guid>.Failure(ex);
        }
    }

    public async Task<Result> UpdateCandidateStatusAsync(Guid candidateId, DiscoveryStatus newStatus, string? reason = null)
    {
        try
        {
            var candidate = await _candidateRepository.GetByIdAsync(candidateId);
            if (candidate == null)
            {
                return Result.Failure("Candidate not found", "CANDIDATE_NOT_FOUND");
            }

            candidate.Status = newStatus;
            candidate.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                candidate.RejectionReason = reason;
            }

            _candidateRepository.Update(candidate);

            _logger.LogInformation("Updated candidate {CandidateId} status to {Status}", candidateId, newStatus);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating candidate {CandidateId} status", candidateId);
            return Result.Failure(ex);
        }
    }

    public async Task<Result<bool>> ValidateWorkflowPermissionsAsync(Guid userId, WorkflowAction action)
    {
        try
        {
            // For now, implement basic permission checking
            // In a real system, this would check user roles and permissions

            // Simulate async work for future implementation
            await Task.Delay(1, CancellationToken.None);

            // System user (Guid.Empty) can perform auto-approvals
            if (userId == Guid.Empty && action == WorkflowAction.Approve)
            {
                return Result<bool>.Success(true);
            }

            // All other users can perform all actions for now
            // TODO: Implement proper role-based authorization
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow permissions for user {UserId}", userId);
            return Result<bool>.Failure(ex);
        }
    }

    private async Task<Result<ApprovalResult>> ProcessSimpleApprovalAsync(ProductDiscoveryCandidate candidate, Guid reviewerId, ApprovalDecision decision)
    {
        // Update candidate status
        candidate.Status = DiscoveryStatus.Approved;
        candidate.UpdatedAt = DateTimeOffset.UtcNow;
        _candidateRepository.Update(candidate);

        // Create workflow entry
        var workflow = new DiscoveryApprovalWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            CandidateId = candidate.CandidateId,
            ReviewerId = reviewerId,
            Action = WorkflowAction.Approve,
            Comments = decision.Comments,
            ActionDate = DateTimeOffset.UtcNow
        };

        await _workflowRepository.AddAsync(workflow);

        Guid? createdProductId = null;
        if (decision.CreateProduct)
        {
            var productResult = await CreateProductFromCandidateAsync(candidate.CandidateId, decision.Modifications);
            if (productResult.IsSuccess)
            {
                createdProductId = productResult.Data;
            }
        }

        return Result<ApprovalResult>.Success(new ApprovalResult
        {
            IsSuccess = true,
            CandidateId = candidate.CandidateId,
            NewStatus = DiscoveryStatus.Approved,
            CreatedProductId = createdProductId,
            WorkflowNotes = "Approved"
        });
    }

    private async Task<Result<ApprovalResult>> ProcessApprovalWithModificationsAsync(ProductDiscoveryCandidate candidate, Guid reviewerId, ApprovalDecision decision)
    {
        // Update candidate status
        candidate.Status = DiscoveryStatus.Approved;
        candidate.UpdatedAt = DateTimeOffset.UtcNow;
        _candidateRepository.Update(candidate);

        // Create workflow entry
        var workflow = new DiscoveryApprovalWorkflow
        {
            WorkflowId = Guid.NewGuid(),
            CandidateId = candidate.CandidateId,
            ReviewerId = reviewerId,
            Action = WorkflowAction.ApproveWithModifications,
            Comments = decision.Comments,
            Modifications = decision.Modifications != null ? JsonSerializer.Serialize(decision.Modifications) : null,
            ActionDate = DateTimeOffset.UtcNow
        };

        await _workflowRepository.AddAsync(workflow);

        Guid? createdProductId = null;
        if (decision.CreateProduct)
        {
            var productResult = await CreateProductFromCandidateAsync(candidate.CandidateId, decision.Modifications);
            if (productResult.IsSuccess)
            {
                createdProductId = productResult.Data;
            }
        }

        return Result<ApprovalResult>.Success(new ApprovalResult
        {
            IsSuccess = true,
            CandidateId = candidate.CandidateId,
            NewStatus = DiscoveryStatus.Approved,
            CreatedProductId = createdProductId,
            WorkflowNotes = "Approved with modifications"
        });
    }

    private static string? GetModifiedValue(Dictionary<string, object>? modifications, string key, string? originalValue)
    {
        if (modifications?.ContainsKey(key) == true)
        {
            return modifications[key]?.ToString();
        }
        return originalValue;
    }

    private static string ExtractSellerFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return "Unknown Seller";
        }
    }

    private async Task PublishApprovalEventAsync(Guid candidateId, Guid? productId, Guid reviewerId, ApprovalDecision decision)
    {
        var approvalEvent = new CandidateApprovedEvent
        {
            CandidateId = candidateId,
            CreatedProductId = productId ?? Guid.Empty,
            ReviewerId = reviewerId,
            Action = decision.Action.ToString(),
            Comments = decision.Comments,
            Modifications = decision.Modifications
        };

        await _messagePublisher.PublishAsync(approvalEvent, "candidate-approved");
    }

    private async Task PublishRejectionEventAsync(Guid candidateId, Guid reviewerId, string reason, string? comments)
    {
        var rejectionEvent = new CandidateRejectedEvent
        {
            CandidateId = candidateId,
            ReviewerId = reviewerId,
            Reason = reason,
            Comments = comments
        };

        await _messagePublisher.PublishAsync(rejectionEvent, "candidate-rejected");
    }

    public async Task<Result<ApprovalResult>> ApproveCandidateAsync(Guid candidateId, Guid reviewerId, ApprovalDecision decision)
    {
        return await ProcessApprovalAsync(candidateId, reviewerId, decision);
    }

    public async Task<Result<ApprovalResult>> RejectCandidateAsync(Guid candidateId, Guid reviewerId, string reason, string? comments = null)
    {
        return await ProcessRejectionAsync(candidateId, reviewerId, reason, comments);
    }
}

// Extension method for null-conditional operations
public static class ObjectExtensions
{
    public static TResult? let<T, TResult>(this T? obj, Func<T, TResult> func) where T : class
    {
        return obj != null ? func(obj) : default;
    }
}