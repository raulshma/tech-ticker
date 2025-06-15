using Microsoft.EntityFrameworkCore.Storage;
using TechTicker.DataAccess.Repositories.Interfaces;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TechTickerDbContext _context;
    private IDbContextTransaction? _transaction;

    private ICategoryRepository? _categories;
    private IProductRepository? _products;
    private IProductSellerMappingRepository? _productSellerMappings;
    private IScraperSiteConfigurationRepository? _scraperSiteConfigurations;
    private IPriceHistoryRepository? _priceHistory;
    private IAlertRuleRepository? _alertRules;

    public UnitOfWork(TechTickerDbContext context)
    {
        _context = context;
    }

    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public IProductSellerMappingRepository ProductSellerMappings =>
        _productSellerMappings ??= new ProductSellerMappingRepository(_context);

    public IScraperSiteConfigurationRepository ScraperSiteConfigurations =>
        _scraperSiteConfigurations ??= new ScraperSiteConfigurationRepository(_context);

    public IPriceHistoryRepository PriceHistory =>
        _priceHistory ??= new PriceHistoryRepository(_context);

    public IAlertRuleRepository AlertRules =>
        _alertRules ??= new AlertRuleRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
