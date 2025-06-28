namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Unit of Work pattern interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ICategoryRepository Categories { get; }
    IProductRepository Products { get; }
    IProductSellerMappingRepository ProductSellerMappings { get; }
    IScraperSiteConfigurationRepository ScraperSiteConfigurations { get; }
    IPriceHistoryRepository PriceHistory { get; }
    IAlertRuleRepository AlertRules { get; }
    IAlertHistoryRepository AlertHistories { get; }
    IScraperRunLogRepository ScraperRunLogs { get; }
    IUserNotificationPreferencesRepository UserNotificationPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
