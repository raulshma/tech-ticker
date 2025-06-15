using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for ScraperSiteConfiguration entities
/// </summary>
public interface IScraperSiteConfigurationRepository : IRepository<ScraperSiteConfiguration>
{
    Task<ScraperSiteConfiguration?> GetByDomainAsync(string domain);
    Task<bool> DomainExistsAsync(string domain, Guid? excludeId = null);
}
