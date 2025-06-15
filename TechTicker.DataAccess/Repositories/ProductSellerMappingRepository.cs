using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for ProductSellerMapping entities
/// </summary>
public class ProductSellerMappingRepository : Repository<ProductSellerMapping>, IProductSellerMappingRepository
{
    public ProductSellerMappingRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ProductSellerMapping>> GetByProductIdAsync(Guid productId)
    {
        return await _dbSet
            .Include(m => m.SiteConfiguration)
            .Where(m => m.CanonicalProductId == productId)
            .OrderBy(m => m.SellerName)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductSellerMapping>> GetActiveMappingsAsync()
    {
        return await _dbSet
            .Include(m => m.SiteConfiguration)
            .Where(m => m.IsActiveForScraping)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductSellerMapping>> GetMappingsDueForScrapingAsync(int batchSize = 100)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await _dbSet
            .Include(m => m.SiteConfiguration)
            .Where(m => m.IsActiveForScraping && 
                       (m.NextScrapeAt == null || m.NextScrapeAt <= now))
            .OrderBy(m => m.NextScrapeAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<ProductSellerMapping?> GetByIdWithConfigurationAsync(Guid mappingId)
    {
        return await _dbSet
            .Include(m => m.SiteConfiguration)
            .Include(m => m.Product)
            .FirstOrDefaultAsync(m => m.MappingId == mappingId);
    }
}
