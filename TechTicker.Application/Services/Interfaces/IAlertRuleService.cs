using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;

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
}
