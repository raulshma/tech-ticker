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
}
