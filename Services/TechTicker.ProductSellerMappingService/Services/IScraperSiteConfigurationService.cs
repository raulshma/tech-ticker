using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ProductSellerMappingService.Services
{
    /// <summary>
    /// Interface for scraper site configuration service
    /// </summary>
    public interface IScraperSiteConfigurationService
    {
        /// <summary>
        /// Creates a new scraper site configuration
        /// </summary>
        /// <param name="request">The site configuration creation data</param>
        /// <returns>The created site configuration</returns>
        Task<Result<ScraperSiteConfigurationResponse>> CreateSiteConfigurationAsync(CreateScraperSiteConfigurationRequest request);

        /// <summary>
        /// Gets a site configuration by ID
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <returns>The site configuration if found</returns>
        Task<Result<ScraperSiteConfigurationResponse>> GetSiteConfigurationAsync(Guid siteConfigId);

        /// <summary>
        /// Gets a site configuration by domain
        /// </summary>
        /// <param name="domain">The site domain</param>
        /// <returns>The site configuration if found</returns>
        Task<Result<ScraperSiteConfigurationResponse>> GetSiteConfigurationByDomainAsync(string domain);

        /// <summary>
        /// Gets all site configurations with optional filtering
        /// </summary>
        /// <param name="request">Query parameters</param>
        /// <returns>List of site configurations</returns>
        Task<Result<PagedResponse<ScraperSiteConfigurationResponse>>> GetSiteConfigurationsAsync(ScraperSiteConfigurationQueryRequest request);

        /// <summary>
        /// Updates an existing site configuration
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <param name="request">The updated site configuration data</param>
        /// <returns>The updated site configuration</returns>
        Task<Result<ScraperSiteConfigurationResponse>> UpdateSiteConfigurationAsync(Guid siteConfigId, UpdateScraperSiteConfigurationRequest request);

        /// <summary>
        /// Deletes a site configuration
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <returns>Success or failure result</returns>
        Task<Result<bool>> DeleteSiteConfigurationAsync(Guid siteConfigId);
    }
}
