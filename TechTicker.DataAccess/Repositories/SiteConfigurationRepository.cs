using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for site configurations
/// </summary>
public class SiteConfigurationRepository : Repository<SiteConfiguration>, ISiteConfigurationRepository
{
    public SiteConfigurationRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<SiteConfiguration?> GetByDomainAsync(string domain)
    {
        return await _context.SiteConfigurations
            .Include(sc => sc.CreatedByUser)
            .Include(sc => sc.UpdatedByUser)
            .FirstOrDefaultAsync(sc => sc.Domain.ToLower() == domain.ToLower() && sc.IsActive);
    }

    public async Task<List<SiteConfiguration>> GetActiveConfigurationsAsync()
    {
        return await _context.SiteConfigurations
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.Domain)
            .ToListAsync();
    }

    public async Task<(List<SiteConfiguration> Items, int TotalCount)> GetConfigurationsAsync(
        string? domain = null,
        string? siteName = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = _context.SiteConfigurations
            .Include(sc => sc.CreatedByUser)
            .Include(sc => sc.UpdatedByUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(domain))
        {
            query = query.Where(sc => sc.Domain.ToLower().Contains(domain.ToLower()));
        }

        if (!string.IsNullOrEmpty(siteName))
        {
            query = query.Where(sc => sc.SiteName.ToLower().Contains(siteName.ToLower()));
        }

        if (isActive.HasValue)
        {
            query = query.Where(sc => sc.IsActive == isActive.Value);
        }

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "domain" => sortDescending ? query.OrderByDescending(sc => sc.Domain) : query.OrderBy(sc => sc.Domain),
            "sitename" => sortDescending ? query.OrderByDescending(sc => sc.SiteName) : query.OrderBy(sc => sc.SiteName),
            "createdat" => sortDescending ? query.OrderByDescending(sc => sc.CreatedAt) : query.OrderBy(sc => sc.CreatedAt),
            "updatedat" => sortDescending ? query.OrderByDescending(sc => sc.UpdatedAt) : query.OrderBy(sc => sc.UpdatedAt),
            "confidencescore" => sortDescending ? query.OrderByDescending(sc => sc.ConfidenceScore) : query.OrderBy(sc => sc.ConfidenceScore),
            _ => query.OrderBy(sc => sc.Domain)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> DomainExistsAsync(string domain, Guid? excludeId = null)
    {
        var query = _context.SiteConfigurations
            .Where(sc => sc.Domain.ToLower() == domain.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(sc => sc.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task UpdateTestResultAsync(Guid id, string testResult, decimal confidenceScore)
    {
        var config = await _context.SiteConfigurations.FindAsync(id);
        if (config != null)
        {
            config.LastTestResult = testResult;
            config.ConfidenceScore = confidenceScore;
            config.LastTestedAt = DateTime.UtcNow;
            config.UpdatedAt = DateTime.UtcNow;

            _context.SiteConfigurations.Update(config);
        }
    }

    public async Task<List<SiteConfiguration>> GetConfigurationsNeedingTestAsync(TimeSpan maxAge)
    {
        var cutoffDate = DateTime.UtcNow - maxAge;
        
        return await _context.SiteConfigurations
            .Where(sc => sc.IsActive && 
                        (sc.LastTestedAt == null || sc.LastTestedAt < cutoffDate))
            .OrderBy(sc => sc.LastTestedAt ?? DateTime.MinValue)
            .ToListAsync();
    }

    public override async Task<SiteConfiguration?> GetByIdAsync(Guid id)
    {
        return await _context.SiteConfigurations
            .Include(sc => sc.CreatedByUser)
            .Include(sc => sc.UpdatedByUser)
            .FirstOrDefaultAsync(sc => sc.Id == id);
    }

    public override async Task<IEnumerable<SiteConfiguration>> GetAllAsync()
    {
        return await _context.SiteConfigurations
            .Include(sc => sc.CreatedByUser)
            .Include(sc => sc.UpdatedByUser)
            .OrderBy(sc => sc.Domain)
            .ToListAsync();
    }
}
