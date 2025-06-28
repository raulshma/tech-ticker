using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for AlertRule operations
/// </summary>
public class AlertRuleService : IAlertRuleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<AlertRuleService> _logger;

    public AlertRuleService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<AlertRuleService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<AlertRuleDto>> CreateAlertRuleAsync(Guid userId, CreateAlertRuleDto createDto)
    {
        try
        {
            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == createDto.CanonicalProductId);
            if (!productExists)
            {
                return Result<AlertRuleDto>.Failure("Product not found.", "PRODUCT_NOT_FOUND");
            }

            // Validate condition type and required fields
            var validationResult = ValidateAlertRule(createDto);
            if (!validationResult.IsSuccess)
            {
                return Result<AlertRuleDto>.Failure(validationResult.ErrorMessage!, validationResult.ErrorCode!);
            }

            var alertRule = _mappingService.MapToEntity(createDto, userId);
            await _unitOfWork.AlertRules.AddAsync(alertRule);
            await _unitOfWork.SaveChangesAsync();

            var alertRuleDto = _mappingService.MapToDto(alertRule);
            _logger.LogInformation("Created alert rule {AlertRuleId} for user {UserId}", alertRule.AlertRuleId, userId);

            return Result<AlertRuleDto>.Success(alertRuleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert rule for user {UserId}", userId);
            return Result<AlertRuleDto>.Failure("An error occurred while creating the alert rule.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<AlertRuleDto>>> GetUserAlertRulesAsync(Guid userId)
    {
        try
        {
            var alertRules = await _unitOfWork.AlertRules.GetByUserIdAsync(userId);
            var alertRuleDtos = alertRules.Select(_mappingService.MapToDto);

            return Result<IEnumerable<AlertRuleDto>>.Success(alertRuleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert rules for user {UserId}", userId);
            return Result<IEnumerable<AlertRuleDto>>.Failure("An error occurred while retrieving alert rules.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<AlertRuleDto>>> GetProductAlertRulesAsync(Guid userId, Guid productId)
    {
        try
        {
            var alertRules = await _unitOfWork.AlertRules.FindAsync(ar =>
                ar.UserId == userId && ar.CanonicalProductId == productId);
            var alertRuleDtos = alertRules.Select(_mappingService.MapToDto);

            return Result<IEnumerable<AlertRuleDto>>.Success(alertRuleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert rules for user {UserId} and product {ProductId}", userId, productId);
            return Result<IEnumerable<AlertRuleDto>>.Failure("An error occurred while retrieving alert rules.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<AlertRuleDto>> UpdateAlertRuleAsync(Guid userId, Guid alertRuleId, UpdateAlertRuleDto updateDto)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.FirstOrDefaultAsync(ar => 
                ar.AlertRuleId == alertRuleId && ar.UserId == userId);
            
            if (alertRule == null)
            {
                return Result<AlertRuleDto>.Failure("Alert rule not found.", "RESOURCE_NOT_FOUND");
            }

            _mappingService.MapToEntity(updateDto, alertRule);
            _unitOfWork.AlertRules.Update(alertRule);
            await _unitOfWork.SaveChangesAsync();

            var alertRuleDto = _mappingService.MapToDto(alertRule);
            _logger.LogInformation("Updated alert rule {AlertRuleId} for user {UserId}", alertRuleId, userId);

            return Result<AlertRuleDto>.Success(alertRuleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert rule {AlertRuleId} for user {UserId}", alertRuleId, userId);
            return Result<AlertRuleDto>.Failure("An error occurred while updating the alert rule.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result> DeleteAlertRuleAsync(Guid userId, Guid alertRuleId)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.FirstOrDefaultAsync(ar => 
                ar.AlertRuleId == alertRuleId && ar.UserId == userId);
            
            if (alertRule == null)
            {
                return Result.Failure("Alert rule not found.", "RESOURCE_NOT_FOUND");
            }

            _unitOfWork.AlertRules.Remove(alertRule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted alert rule {AlertRuleId} for user {UserId}", alertRuleId, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert rule {AlertRuleId} for user {UserId}", alertRuleId, userId);
            return Result.Failure("An error occurred while deleting the alert rule.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<PagedResponse<AlertRuleDto>>> GetAllAlertRulesAsync(
        Guid? userId = null,
        Guid? productId = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        try
        {
            var (alertRules, totalCount) = await _unitOfWork.AlertRules.GetAllAlertsAsync(
                userId, productId, pageNumber, pageSize);

            var alertRuleDtos = alertRules.Select(_mappingService.MapToDto);

            var pagedResponse = PagedResponse<AlertRuleDto>.SuccessResult(
                alertRuleDtos, pageNumber, pageSize, totalCount);

            return Result<PagedResponse<AlertRuleDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all alert rules");
            return Result<PagedResponse<AlertRuleDto>>.Failure("An error occurred while retrieving alert rules.", "INTERNAL_ERROR");
        }
    }

    private static Result ValidateAlertRule(CreateAlertRuleDto createDto)
    {
        return createDto.ConditionType switch
        {
            "PRICE_BELOW" when !createDto.ThresholdValue.HasValue =>
                Result.Failure("ThresholdValue is required for PRICE_BELOW condition.", "VALIDATION_ERROR"),
            "PERCENT_DROP_FROM_LAST" when !createDto.PercentageValue.HasValue =>
                Result.Failure("PercentageValue is required for PERCENT_DROP_FROM_LAST condition.", "VALIDATION_ERROR"),
            "PRICE_BELOW" or "PERCENT_DROP_FROM_LAST" or "BACK_IN_STOCK" => Result.Success(),
            _ => Result.Failure("Invalid condition type.", "VALIDATION_ERROR")
        };
    }



    public async Task<Result<AlertRuleDto>> GetAlertRuleByIdAsync(Guid alertRuleId)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result<AlertRuleDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            var alertRuleDto = _mappingService.MapToDto(alertRule);
            return Result<AlertRuleDto>.Success(alertRuleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert rule {AlertRuleId}", alertRuleId);
            return Result<AlertRuleDto>.Failure("Failed to get alert rule", "GET_ALERT_FAILED");
        }
    }

    public async Task<Result<AlertRuleDto>> AdminUpdateAlertRuleAsync(Guid alertRuleId, UpdateAlertRuleDto updateDto)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result<AlertRuleDto>.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            // Update properties
            if (!string.IsNullOrEmpty(updateDto.ConditionType))
                alertRule.ConditionType = updateDto.ConditionType;
            if (updateDto.ThresholdValue.HasValue)
                alertRule.ThresholdValue = updateDto.ThresholdValue;
            if (updateDto.PercentageValue.HasValue)
                alertRule.PercentageValue = updateDto.PercentageValue;
            if (!string.IsNullOrEmpty(updateDto.SpecificSellerName))
                alertRule.SpecificSellerName = updateDto.SpecificSellerName;
            if (updateDto.NotificationFrequencyMinutes.HasValue)
                alertRule.NotificationFrequencyMinutes = updateDto.NotificationFrequencyMinutes.Value;
            if (updateDto.IsActive.HasValue)
                alertRule.IsActive = updateDto.IsActive.Value;

            alertRule.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.AlertRules.Update(alertRule);
            await _unitOfWork.SaveChangesAsync();

            var alertRuleDto = _mappingService.MapToDto(alertRule);
            return Result<AlertRuleDto>.Success(alertRuleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert rule {AlertRuleId}", alertRuleId);
            return Result<AlertRuleDto>.Failure("Failed to update alert rule", "UPDATE_ALERT_FAILED");
        }
    }

    public async Task<Result> AdminDeleteAlertRuleAsync(Guid alertRuleId)
    {
        try
        {
            var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
            if (alertRule == null)
            {
                return Result.Failure("Alert rule not found", "ALERT_NOT_FOUND");
            }

            _unitOfWork.AlertRules.Remove(alertRule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Admin deleted alert rule {AlertRuleId}", alertRuleId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert rule {AlertRuleId}", alertRuleId);
            return Result.Failure("Failed to delete alert rule", "DELETE_ALERT_FAILED");
        }
    }

    public async Task<Result<AlertBulkOperationResultDto>> BulkOperationAsync(AlertBulkOperationRequestDto request)
    {
        try
        {
            var result = new AlertBulkOperationResultDto
            {
                Operation = request.Operation,
                TotalRequested = request.AlertRuleIds.Count,
                Reason = request.Reason
            };

            foreach (var alertRuleId in request.AlertRuleIds)
            {
                var itemResult = new AlertBulkOperationItemResultDto
                {
                    AlertRuleId = alertRuleId
                };

                try
                {
                    var alertRule = await _unitOfWork.AlertRules.GetByIdAsync(alertRuleId);
                    if (alertRule == null)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = "Alert rule not found";
                        result.Results.Add(itemResult);
                        continue;
                    }

                    switch (request.Operation.ToUpper())
                    {
                        case "ENABLE":
                            itemResult.PreviousValue = alertRule.IsActive.ToString();
                            alertRule.IsActive = true;
                            itemResult.NewValue = "True";
                            itemResult.Success = true;
                            break;

                        case "DISABLE":
                            itemResult.PreviousValue = alertRule.IsActive.ToString();
                            alertRule.IsActive = false;
                            itemResult.NewValue = "False";
                            itemResult.Success = true;
                            break;

                        case "DELETE":
                            _unitOfWork.AlertRules.Remove(alertRule);
                            itemResult.PreviousValue = "Exists";
                            itemResult.NewValue = "Deleted";
                            itemResult.Success = true;
                            break;

                        case "UPDATE_FREQUENCY":
                            if (!request.NewNotificationFrequencyMinutes.HasValue)
                            {
                                itemResult.Success = false;
                                itemResult.ErrorMessage = "NewNotificationFrequencyMinutes is required for UPDATE_FREQUENCY operation";
                                break;
                            }
                            itemResult.PreviousValue = alertRule.NotificationFrequencyMinutes.ToString();
                            alertRule.NotificationFrequencyMinutes = request.NewNotificationFrequencyMinutes.Value;
                            itemResult.NewValue = request.NewNotificationFrequencyMinutes.Value.ToString();
                            itemResult.Success = true;
                            break;

                        case "UPDATE_TYPE":
                            if (string.IsNullOrEmpty(request.NewAlertType))
                            {
                                itemResult.Success = false;
                                itemResult.ErrorMessage = "NewAlertType is required for UPDATE_TYPE operation";
                                break;
                            }
                            itemResult.PreviousValue = alertRule.AlertType;
                            alertRule.AlertType = request.NewAlertType;
                            itemResult.NewValue = request.NewAlertType;
                            itemResult.Success = true;
                            break;

                        default:
                            itemResult.Success = false;
                            itemResult.ErrorMessage = $"Unknown operation: {request.Operation}";
                            break;
                    }

                    if (itemResult.Success && request.Operation.ToUpper() != "DELETE")
                    {
                        alertRule.UpdatedAt = DateTimeOffset.UtcNow;
                        _unitOfWork.AlertRules.Update(alertRule);
                    }
                }
                catch (Exception ex)
                {
                    itemResult.Success = false;
                    itemResult.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Error processing bulk operation for alert rule {AlertRuleId}", alertRuleId);
                }

                result.Results.Add(itemResult);
            }

            result.SuccessfulOperations = result.Results.Count(r => r.Success);
            result.FailedOperations = result.Results.Count(r => !r.Success);

            if (result.SuccessfulOperations > 0)
            {
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Bulk operation {Operation} completed: {Successful}/{Total} successful",
                request.Operation, result.SuccessfulOperations, result.TotalRequested);

            return Result<AlertBulkOperationResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation {Operation}", request.Operation);
            return Result<AlertBulkOperationResultDto>.Failure("Failed to perform bulk operation", "BULK_OPERATION_FAILED");
        }
    }

    public async Task<Result<AlertBulkOperationResultDto>> BulkOperationWithFiltersAsync(AlertBulkOperationWithFiltersRequestDto request)
    {
        try
        {
            if (request.PreviewOnly)
            {
                var previewResult = await PreviewBulkOperationAsync(request);
                if (!previewResult.IsSuccess)
                    return Result<AlertBulkOperationResultDto>.Failure(previewResult.ErrorMessage ?? "Preview failed", previewResult.ErrorCode);

                // Return a preview result formatted as operation result
                var previewAsResult = new AlertBulkOperationResultDto
                {
                    Operation = $"{request.Operation} (PREVIEW)",
                    TotalRequested = previewResult.Data?.TotalMatchingRules ?? 0,
                    SuccessfulOperations = 0,
                    FailedOperations = 0,
                    Reason = request.Reason
                };

                return Result<AlertBulkOperationResultDto>.Success(previewAsResult);
            }

            // Get matching alert rules
            var matchingRules = await GetAlertRulesByFilters(request.Filters);

            if (matchingRules.Count > request.MaxAffectedRules)
            {
                return Result<AlertBulkOperationResultDto>.Failure(
                    $"Operation would affect {matchingRules.Count} rules, which exceeds the maximum limit of {request.MaxAffectedRules}",
                    "TOO_MANY_RULES");
            }

            // Convert to simple bulk operation request
            var bulkRequest = new AlertBulkOperationRequestDto
            {
                AlertRuleIds = matchingRules.Select(r => r.AlertRuleId).ToList(),
                Operation = request.Operation,
                NewNotificationFrequencyMinutes = request.NewNotificationFrequencyMinutes,
                NewAlertType = request.NewAlertType,
                Reason = request.Reason
            };

            return await BulkOperationAsync(bulkRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation with filters");
            return Result<AlertBulkOperationResultDto>.Failure("Failed to perform bulk operation with filters", "BULK_OPERATION_WITH_FILTERS_FAILED");
        }
    }

    public async Task<Result<AlertBulkOperationPreviewDto>> PreviewBulkOperationAsync(AlertBulkOperationWithFiltersRequestDto request)
    {
        try
        {
            var matchingRules = await GetAlertRulesByFilters(request.Filters);

            var preview = new AlertBulkOperationPreviewDto
            {
                Filters = request.Filters,
                Operation = request.Operation,
                TotalMatchingRules = matchingRules.Count,
                RulesWouldBeAffected = matchingRules.Count,
                SampleAffectedRules = matchingRules.Take(10).Select(r => _mappingService.MapToDto(r)).ToList()
            };

            // Add warnings and safety checks
            if (matchingRules.Count > request.MaxAffectedRules)
            {
                preview.IsSafeToExecute = false;
                preview.SafetyMessage = $"Operation would affect {matchingRules.Count} rules, which exceeds the maximum limit of {request.MaxAffectedRules}";
                preview.Warnings.Add(preview.SafetyMessage);
            }
            else if (matchingRules.Count > 50)
            {
                preview.Warnings.Add($"This operation will affect {matchingRules.Count} alert rules. Please review carefully.");
                preview.IsSafeToExecute = true;
            }
            else
            {
                preview.IsSafeToExecute = true;
            }

            if (request.Operation.ToUpper() == "DELETE")
            {
                preview.Warnings.Add("DELETE operations cannot be undone. Please ensure you have backups if needed.");
            }

            return Result<AlertBulkOperationPreviewDto>.Success(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing bulk operation");
            return Result<AlertBulkOperationPreviewDto>.Failure("Failed to preview bulk operation", "PREVIEW_FAILED");
        }
    }

    private async Task<List<AlertRule>> GetAlertRulesByFilters(AlertBulkFilterDto filters)
    {
        var query = await _unitOfWork.AlertRules.GetAllAsync();
        var alertRules = query.AsQueryable();

        if (filters.UserId.HasValue)
            alertRules = alertRules.Where(ar => ar.UserId == filters.UserId.Value);

        if (filters.ProductId.HasValue)
            alertRules = alertRules.Where(ar => ar.CanonicalProductId == filters.ProductId.Value);

        if (!string.IsNullOrEmpty(filters.ConditionType))
            alertRules = alertRules.Where(ar => ar.ConditionType == filters.ConditionType);

        if (!string.IsNullOrEmpty(filters.AlertType))
            alertRules = alertRules.Where(ar => ar.AlertType == filters.AlertType);

        if (filters.IsActive.HasValue)
            alertRules = alertRules.Where(ar => ar.IsActive == filters.IsActive.Value);

        if (filters.CreatedAfter.HasValue)
            alertRules = alertRules.Where(ar => ar.CreatedAt >= filters.CreatedAfter.Value);

        if (filters.CreatedBefore.HasValue)
            alertRules = alertRules.Where(ar => ar.CreatedAt <= filters.CreatedBefore.Value);

        if (filters.LastNotifiedAfter.HasValue)
            alertRules = alertRules.Where(ar => ar.LastNotifiedAt >= filters.LastNotifiedAfter.Value);

        if (filters.LastNotifiedBefore.HasValue)
            alertRules = alertRules.Where(ar => ar.LastNotifiedAt <= filters.LastNotifiedBefore.Value);

        if (filters.MinNotificationFrequency.HasValue)
            alertRules = alertRules.Where(ar => ar.NotificationFrequencyMinutes >= filters.MinNotificationFrequency.Value);

        if (filters.MaxNotificationFrequency.HasValue)
            alertRules = alertRules.Where(ar => ar.NotificationFrequencyMinutes <= filters.MaxNotificationFrequency.Value);

        return alertRules.ToList();
    }
}
