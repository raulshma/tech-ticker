using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for managing site configurations
/// </summary>
public interface ISiteConfigurationService
{
    /// <summary>
    /// Creates a new site configuration
    /// </summary>
    Task<Result<SiteConfigurationDto>> CreateConfigurationAsync(SaveSiteConfigurationRequest request, Guid userId);

    /// <summary>
    /// Updates an existing site configuration
    /// </summary>
    Task<Result<SiteConfigurationDto>> UpdateConfigurationAsync(Guid id, SaveSiteConfigurationRequest request, Guid userId);

    /// <summary>
    /// Gets a site configuration by ID
    /// </summary>
    Task<Result<SiteConfigurationDto>> GetConfigurationAsync(Guid id);

    /// <summary>
    /// Gets a site configuration by domain
    /// </summary>
    Task<Result<SiteConfigurationDto>> GetConfigurationByDomainAsync(string domain);

    /// <summary>
    /// Gets site configurations with filtering and pagination
    /// </summary>
    Task<Result<PagedResponse<SiteConfigurationDto>>> GetConfigurationsAsync(SiteConfigurationFilterRequest filter);

    /// <summary>
    /// Gets all active site configurations
    /// </summary>
    Task<Result<List<SiteConfigurationDto>>> GetActiveConfigurationsAsync();

    /// <summary>
    /// Deletes a site configuration
    /// </summary>
    Task<Result> DeleteConfigurationAsync(Guid id);

    /// <summary>
    /// Updates the test result for a site configuration
    /// </summary>
    Task<Result> UpdateTestResultAsync(Guid id, SelectorTestResult testResult);

    /// <summary>
    /// Gets configurations that need testing
    /// </summary>
    Task<Result<List<SiteConfigurationDto>>> GetConfigurationsNeedingTestAsync(TimeSpan maxAge);
}
