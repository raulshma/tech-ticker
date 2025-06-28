using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for AlertRule operations
/// </summary>
public interface IAlertRuleService
{
    Task<Result<AlertRuleDto>> CreateAlertRuleAsync(Guid userId, CreateAlertRuleDto createDto);
    Task<Result<IEnumerable<AlertRuleDto>>> GetUserAlertRulesAsync(Guid userId);
    Task<Result<IEnumerable<AlertRuleDto>>> GetProductAlertRulesAsync(Guid userId, Guid productId);
    Task<Result<AlertRuleDto>> UpdateAlertRuleAsync(Guid userId, Guid alertRuleId, UpdateAlertRuleDto updateDto);
    Task<Result> DeleteAlertRuleAsync(Guid userId, Guid alertRuleId);
    
    // Admin endpoints
    Task<Result<PagedResponse<AlertRuleDto>>> GetAllAlertRulesAsync(
        Guid? userId = null,
        Guid? productId = null,
        int pageNumber = 1,
        int pageSize = 10);
    Task<Result<AlertRuleDto>> GetAlertRuleByIdAsync(Guid alertRuleId);
    Task<Result<AlertRuleDto>> AdminUpdateAlertRuleAsync(Guid alertRuleId, UpdateAlertRuleDto updateDto);
    Task<Result> AdminDeleteAlertRuleAsync(Guid alertRuleId);

    // Bulk operations
    Task<Result<AlertBulkOperationResultDto>> BulkOperationAsync(AlertBulkOperationRequestDto request);
    Task<Result<AlertBulkOperationResultDto>> BulkOperationWithFiltersAsync(AlertBulkOperationWithFiltersRequestDto request);
    Task<Result<AlertBulkOperationPreviewDto>> PreviewBulkOperationAsync(AlertBulkOperationWithFiltersRequestDto request);
}
