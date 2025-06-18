using Microsoft.Extensions.Logging;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.MessageHandlers;

/// <summary>
/// Handles product discovery events for background processing
/// </summary>
public class ProductDiscoveryEventHandler
{
    private readonly IDiscoveryWorkflowService _workflowService;
    private readonly ILogger<ProductDiscoveryEventHandler> _logger;

    public ProductDiscoveryEventHandler(
        IDiscoveryWorkflowService workflowService,
        ILogger<ProductDiscoveryEventHandler> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task HandleAsync(ProductDiscoveryEvent message)
    {
        try
        {
            _logger.LogInformation("Processing product discovery event {EventId} of type {EventType}", 
                message.EventId, message.EventType);

            switch (message.EventType)
            {
                case "AnalysisCompleted":
                    await HandleAnalysisCompletedAsync(message);
                    break;
                    
                case "BulkAnalysisCompleted":
                    await HandleBulkAnalysisCompletedAsync(message);
                    break;
                    
                case "CandidateCreated":
                    await HandleCandidateCreatedAsync(message);
                    break;
                    
                case "ApprovalProcessed":
                    await HandleApprovalProcessedAsync(message);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown event type {EventType} for event {EventId}", 
                        message.EventType, message.EventId);
                    break;
            }

            _logger.LogInformation("Successfully processed product discovery event {EventId}", message.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product discovery event {EventId}: {Error}", 
                message.EventId, ex.Message);
            throw; // Re-throw to trigger retry mechanism
        }
    }

    private async Task HandleAnalysisCompletedAsync(ProductDiscoveryEvent message)
    {
        if (message.IsSuccess && message.CandidateIds.Any())
        {
            _logger.LogInformation("URL analysis completed successfully. Created {Count} candidates", 
                message.CandidateIds.Count);

            // Check for auto-approval candidates
            var autoApprovalResult = await _workflowService.ProcessAutoApprovalsAsync();
            if (autoApprovalResult.IsSuccess && autoApprovalResult.Data!.Any())
            {
                _logger.LogInformation("Auto-approved {Count} candidates", autoApprovalResult.Data.Count);
            }
        }
        else
        {
            _logger.LogWarning("URL analysis failed: {Error}", message.ErrorMessage);
        }
    }

    private async Task HandleBulkAnalysisCompletedAsync(ProductDiscoveryEvent message)
    {
        _logger.LogInformation("Bulk analysis completed. Processed: {Processed}, Successful: {Successful}, Failed: {Failed}", 
            message.ProcessedUrls, message.SuccessfulExtractions, message.FailedExtractions);

        if (message.IsSuccess && message.CandidateIds.Any())
        {
            // Check for auto-approval candidates
            var autoApprovalResult = await _workflowService.ProcessAutoApprovalsAsync();
            if (autoApprovalResult.IsSuccess && autoApprovalResult.Data!.Any())
            {
                _logger.LogInformation("Auto-approved {Count} candidates from bulk analysis", 
                    autoApprovalResult.Data.Count);
            }
        }
    }

    private async Task HandleCandidateCreatedAsync(ProductDiscoveryEvent message)
    {
        if (message.CandidateIds.Any())
        {
            _logger.LogInformation("New discovery candidate created: {CandidateId}", 
                message.CandidateIds.First());
            
            // Additional processing could be added here, such as:
            // - Sending notifications to admins
            // - Triggering additional analysis
            // - Updating statistics
        }
    }

    private async Task HandleApprovalProcessedAsync(ProductDiscoveryEvent message)
    {
        if (message.CandidateIds.Any())
        {
            _logger.LogInformation("Approval processed for candidate: {CandidateId}", 
                message.CandidateIds.First());
            
            // Additional processing could be added here, such as:
            // - Sending notifications
            // - Updating metrics
            // - Triggering product creation workflows
        }
    }
}
