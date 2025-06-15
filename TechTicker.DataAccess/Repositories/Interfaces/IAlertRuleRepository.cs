using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for AlertRule entities
/// </summary>
public interface IAlertRuleRepository : IRepository<AlertRule>
{
    Task<IEnumerable<AlertRule>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AlertRule>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<AlertRule>> GetActiveAlertsForProductAsync(Guid productId, string? sellerName = null);
    Task<(IEnumerable<AlertRule> Items, int TotalCount)> GetAllAlertsAsync(
        Guid? userId = null,
        Guid? productId = null,
        int pageNumber = 1,
        int pageSize = 10);
}
