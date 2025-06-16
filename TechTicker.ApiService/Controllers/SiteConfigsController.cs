using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for ScraperSiteConfiguration operations
/// </summary>
[Route("api/site-configs")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SiteConfigsController : BaseApiController
{
    private readonly IScraperSiteConfigurationService _siteConfigService;

    public SiteConfigsController(IScraperSiteConfigurationService siteConfigService)
    {
        _siteConfigService = siteConfigService;
    }

    /// <summary>
    /// Get all scraper site configurations
    /// </summary>
    /// <returns>List of all site configurations</returns>
    [HttpGet("all")]
    [Authorize(Roles = "User,Admin")] // Allow both User and Admin roles for dropdown usage
    public async Task<ActionResult<ApiResponse<IEnumerable<ScraperSiteConfigurationDto>>>> GetAllSiteConfigs()
    {
        var result = await _siteConfigService.GetAllConfigurationsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new scraper site configuration
    /// </summary>
    /// <param name="createDto">Site configuration creation data</param>
    /// <returns>Created site configuration</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ScraperSiteConfigurationDto>>> CreateSiteConfig([FromBody] CreateScraperSiteConfigurationDto createDto)
    {
        var result = await _siteConfigService.CreateConfigurationAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get site configuration by domain
    /// </summary>
    /// <param name="domain">Domain name</param>
    /// <returns>Site configuration</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ScraperSiteConfigurationDto>>> GetSiteConfigByDomain([FromQuery] string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            var errorResponse = ApiResponse<ScraperSiteConfigurationDto>.FailureResult("Domain parameter is required.", 400);
            errorResponse.CorrelationId = CorrelationId;
            return BadRequest(errorResponse);
        }

        var result = await _siteConfigService.GetConfigurationByDomainAsync(domain);
        return HandleResult(result);
    }

    /// <summary>
    /// Get site configuration by ID
    /// </summary>
    /// <param name="siteConfigId">Site configuration ID</param>
    /// <returns>Site configuration</returns>
    [HttpGet("{siteConfigId:guid}")]
    public async Task<ActionResult<ApiResponse<ScraperSiteConfigurationDto>>> GetSiteConfig(Guid siteConfigId)
    {
        var result = await _siteConfigService.GetConfigurationByIdAsync(siteConfigId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a scraper site configuration
    /// </summary>
    /// <param name="siteConfigId">Site configuration ID</param>
    /// <param name="updateDto">Site configuration update data</param>
    /// <returns>Updated site configuration</returns>
    [HttpPut("{siteConfigId:guid}")]
    public async Task<ActionResult<ApiResponse<ScraperSiteConfigurationDto>>> UpdateSiteConfig(Guid siteConfigId, [FromBody] UpdateScraperSiteConfigurationDto updateDto)
    {
        var result = await _siteConfigService.UpdateConfigurationAsync(siteConfigId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a scraper site configuration
    /// </summary>
    /// <param name="siteConfigId">Site configuration ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{siteConfigId:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteSiteConfig(Guid siteConfigId)
    {
        var result = await _siteConfigService.DeleteConfigurationAsync(siteConfigId);
        return HandleResult(result);
    }
}
