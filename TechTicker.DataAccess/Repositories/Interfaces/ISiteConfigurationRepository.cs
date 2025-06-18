using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository for managing site configurations
/// </summary>
public interface ISiteConfigurationRepository : IRepository<SiteConfiguration>
{
    /// <summary>
    /// Gets site configuration by domain
    /// </summary>
    Task<SiteConfiguration?> GetByDomainAsync(string domain);

    /// <summary>
    /// Gets all active site configurations
    /// </summary>
    Task<List<SiteConfiguration>> GetActiveConfigurationsAsync();

    /// <summary>
    /// Gets site configurations with filtering and pagination
    /// </summary>
    Task<(List<SiteConfiguration> Items, int TotalCount)> GetConfigurationsAsync(
        string? domain = null,
        string? siteName = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false);

    /// <summary>
    /// Checks if a domain already exists
    /// </summary>
    Task<bool> DomainExistsAsync(string domain, Guid? excludeId = null);

    /// <summary>
    /// Updates the test result for a site configuration
    /// </summary>
    Task UpdateTestResultAsync(Guid id, string testResult, decimal confidenceScore);

    /// <summary>
    /// Gets configurations that need testing (haven't been tested recently)
    /// </summary>
    Task<List<SiteConfiguration>> GetConfigurationsNeedingTestAsync(TimeSpan maxAge);
}
