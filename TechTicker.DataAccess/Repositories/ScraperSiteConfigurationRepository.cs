using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for ScraperSiteConfiguration entities
/// </summary>
public class ScraperSiteConfigurationRepository : Repository<ScraperSiteConfiguration>, IScraperSiteConfigurationRepository
{
    public ScraperSiteConfigurationRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<ScraperSiteConfiguration?> GetByDomainAsync(string domain)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.SiteDomain == domain);
    }

    public async Task<bool> DomainExistsAsync(string domain, Guid? excludeId = null)
    {
        var query = _dbSet.Where(s => s.SiteDomain == domain);
        
        if (excludeId.HasValue)
        {
            query = query.Where(s => s.SiteConfigId != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
