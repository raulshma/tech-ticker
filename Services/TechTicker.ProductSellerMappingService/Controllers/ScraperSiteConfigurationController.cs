using Microsoft.AspNetCore.Mvc;
using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.ProductSellerMappingService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ProductSellerMappingService.Controllers
{
    /// <summary>
    /// Controller for managing scraper site configurations
    /// </summary>
    [Route("api/site-configs")]
    [ApiController]
    public class ScraperSiteConfigurationController : BaseApiController
    {
        private readonly IScraperSiteConfigurationService _siteConfigService;

        /// <summary>
        /// Initializes a new instance of the ScraperSiteConfigurationController
        /// </summary>
        /// <param name="siteConfigService">The site configuration service</param>
        public ScraperSiteConfigurationController(IScraperSiteConfigurationService siteConfigService)
        {
            _siteConfigService = siteConfigService;
        }

        /// <summary>
        /// Creates a new scraper site configuration
        /// </summary>
        /// <param name="request">The site configuration creation data</param>
        /// <returns>The created site configuration</returns>
        /// <response code="201">Site configuration created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="409">Site configuration for this domain already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ScraperSiteConfigurationResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateSiteConfiguration([FromBody] CreateScraperSiteConfigurationRequest request)
        {
            var result = await _siteConfigService.CreateSiteConfigurationAsync(request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }            return CreatedAtAction(
                nameof(GetSiteConfiguration), 
                new { siteConfigId = result.Data!.SiteConfigId }, 
                result.Data);
        }

        /// <summary>
        /// Gets a site configuration by ID
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <returns>The site configuration</returns>
        /// <response code="200">Site configuration found</response>
        /// <response code="404">Site configuration not found</response>
        [HttpGet("{siteConfigId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ScraperSiteConfigurationResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetSiteConfiguration(Guid siteConfigId)
        {
            var result = await _siteConfigService.GetSiteConfigurationAsync(siteConfigId);
            return HandleResult(result);
        }        /// <summary>
        /// Gets site configurations with optional filtering
        /// </summary>
        /// <param name="domain">Filter by site domain</param>
        /// <param name="search">Search term for domain</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of site configurations</returns>
        /// <response code="200">Site configurations retrieved successfully</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ScraperSiteConfigurationResponse>), 200)]
        public async Task<IActionResult> GetSiteConfigurations(
            [FromQuery] string? domain = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var request = new ScraperSiteConfigurationQueryRequest
            {
                SiteDomain = domain,
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _siteConfigService.GetSiteConfigurationsAsync(request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return OkPagedDirect(result.Data!);
        }

        /// <summary>
        /// Gets a site configuration by domain
        /// </summary>
        /// <param name="domainName">The site domain name</param>
        /// <returns>The site configuration</returns>
        /// <response code="200">Site configuration found</response>
        /// <response code="404">Site configuration not found</response>
        [HttpGet("by-domain")]
        [ProducesResponseType(typeof(ApiResponse<ScraperSiteConfigurationResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]        public async Task<IActionResult> GetSiteConfigurationByDomain([FromQuery] string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                return BadRequest("Domain name is required");
            }

            var result = await _siteConfigService.GetSiteConfigurationByDomainAsync(domainName);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates an existing site configuration
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <param name="request">The updated site configuration data</param>
        /// <returns>The updated site configuration</returns>
        /// <response code="200">Site configuration updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="404">Site configuration not found</response>
        /// <response code="409">Domain conflict with existing configuration</response>
        [HttpPut("{siteConfigId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ScraperSiteConfigurationResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateSiteConfiguration(Guid siteConfigId, [FromBody] UpdateScraperSiteConfigurationRequest request)
        {
            var result = await _siteConfigService.UpdateSiteConfigurationAsync(siteConfigId, request);
            return HandleResult(result);
        }

        /// <summary>
        /// Deletes a site configuration
        /// </summary>
        /// <param name="siteConfigId">The site configuration ID</param>
        /// <returns>Success or failure result</returns>
        /// <response code="200">Site configuration deleted successfully</response>
        /// <response code="404">Site configuration not found</response>
        /// <response code="409">Site configuration is still referenced by mappings</response>
        [HttpDelete("{siteConfigId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteSiteConfiguration(Guid siteConfigId)
        {
            var result = await _siteConfigService.DeleteSiteConfigurationAsync(siteConfigId);
            return HandleResult(result);
        }
    }
}
