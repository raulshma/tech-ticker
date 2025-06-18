using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// Controller for managing site configurations and AI-powered selector generation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SiteConfigurationController : BaseApiController
{
    private readonly ISiteConfigurationService _siteConfigurationService;
    private readonly IAISelectorGenerationService _aiSelectorService;

    public SiteConfigurationController(
        ISiteConfigurationService siteConfigurationService,
        IAISelectorGenerationService aiSelectorService)
    {
        _siteConfigurationService = siteConfigurationService;
        _aiSelectorService = aiSelectorService;
    }

    /// <summary>
    /// Gets all site configurations with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SiteConfigurationDto>), 200)]
    public async Task<ActionResult<PagedResponse<SiteConfigurationDto>>> GetConfigurations(
        [FromQuery] SiteConfigurationFilterRequest filter)
    {
        var result = await _siteConfigurationService.GetConfigurationsAsync(filter);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Gets a specific site configuration by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SiteConfigurationDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<SiteConfigurationDto>>> GetConfiguration(Guid id)
    {
        var result = await _siteConfigurationService.GetConfigurationAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a site configuration by domain
    /// </summary>
    [HttpGet("by-domain/{domain}")]
    [ProducesResponseType(typeof(ApiResponse<SiteConfigurationDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<SiteConfigurationDto>>> GetConfigurationByDomain(string domain)
    {
        var result = await _siteConfigurationService.GetConfigurationByDomainAsync(domain);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all active site configurations
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<SiteConfigurationDto>>), 200)]
    public async Task<ActionResult<ApiResponse<List<SiteConfigurationDto>>>> GetActiveConfigurations()
    {
        var result = await _siteConfigurationService.GetActiveConfigurationsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new site configuration
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SiteConfigurationDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<SiteConfigurationDto>>> CreateConfiguration(
        [FromBody] SaveSiteConfigurationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var result = await _siteConfigurationService.CreateConfigurationAsync(request, userId);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetConfiguration),
                new { id = result.Data!.Id },
                ApiResponse<SiteConfigurationDto>.SuccessResult(result.Data, "Site configuration created successfully"));
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing site configuration
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SiteConfigurationDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<SiteConfigurationDto>>> UpdateConfiguration(
        Guid id,
        [FromBody] SaveSiteConfigurationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var result = await _siteConfigurationService.UpdateConfigurationAsync(id, request, userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes a site configuration
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> DeleteConfiguration(Guid id)
    {
        var result = await _siteConfigurationService.DeleteConfigurationAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates CSS selectors from HTML content using AI
    /// </summary>
    [HttpPost("generate-selectors")]
    [ProducesResponseType(typeof(ApiResponse<SelectorGenerationResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<SelectorGenerationResult>>> GenerateSelectors(
        [FromBody] GenerateSelectorsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _aiSelectorService.GenerateSelectorsAsync(request.HtmlContent, request.Domain);
        return HandleResult(result);
    }

    /// <summary>
    /// Tests CSS selectors against HTML content
    /// </summary>
    [HttpPost("test-selectors")]
    [ProducesResponseType(typeof(ApiResponse<SelectorTestResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<SelectorTestResult>>> TestSelectors(
        [FromBody] TestSelectorsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _aiSelectorService.TestSelectorsAsync(request.HtmlContent, request.Selectors);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets improvement suggestions for CSS selectors
    /// </summary>
    [HttpPost("suggest-improvements")]
    [ProducesResponseType(typeof(ApiResponse<List<SelectorSuggestion>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<List<SelectorSuggestion>>>> SuggestImprovements(
        [FromBody] SelectorImprovementRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _aiSelectorService.SuggestImprovementsAsync(
            request.HtmlContent,
            request.CurrentSelectors,
            request.TestResult);
        return HandleResult(result);
    }

    private Guid GetCurrentUserId()
    {
        return string.IsNullOrEmpty(CurrentUserId) ? Guid.Empty : Guid.Parse(CurrentUserId);
    }
}
