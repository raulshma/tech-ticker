using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Attributes;
using TechTicker.Shared.Authorization;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for proxy configuration operations
/// </summary>
[Route("api/proxies")]
[ApiController]
[Authorize(Roles = "Admin")] // Fallback to Admin role for now, individual methods can override with permissions
public class ProxyController : BaseApiController
{
    private readonly IProxyService _proxyService;

    public ProxyController(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    /// <summary>
    /// Get all proxy configurations
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.ProxiesRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProxyConfigurationDto>>>> GetAllProxies()
    {
        var result = await _proxyService.GetAllProxiesAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get proxy configuration by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProxyConfigurationDto>>> GetProxyById(Guid id)
    {
        var result = await _proxyService.GetProxyByIdAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Get active proxy configurations
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProxyConfigurationDto>>>> GetActiveProxies()
    {
        var result = await _proxyService.GetActiveProxiesAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get healthy proxy configurations
    /// </summary>
    [HttpGet("healthy")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProxyConfigurationDto>>>> GetHealthyProxies()
    {
        var result = await _proxyService.GetHealthyProxiesAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Get proxy configurations by type
    /// </summary>
    [HttpGet("by-type/{proxyType}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProxyConfigurationDto>>>> GetProxiesByType(string proxyType)
    {
        var result = await _proxyService.GetProxiesByTypeAsync(proxyType);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new proxy configuration
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.ProxiesCreate)]
    public async Task<ActionResult<ApiResponse<ProxyConfigurationDto>>> CreateProxy([FromBody] CreateProxyConfigurationDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.CreateProxyAsync(createDto);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetProxyById), new { id = result.Data!.ProxyConfigurationId }, result.Data);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing proxy configuration
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.ProxiesUpdate)]
    public async Task<ActionResult<ApiResponse<ProxyConfigurationDto>>> UpdateProxy(Guid id, [FromBody] UpdateProxyConfigurationDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.UpdateProxyAsync(id, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a proxy configuration
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.ProxiesDelete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProxy(Guid id)
    {
        var result = await _proxyService.DeleteProxyAsync(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Test a single proxy configuration
    /// </summary>
    [HttpPost("{id:guid}/test")]
    [RequirePermission(Permissions.ProxiesTest)]
    public async Task<ActionResult<ApiResponse<ProxyTestResultDto>>> TestProxy(Guid id, [FromQuery] string? testUrl = null, [FromQuery] int timeoutSeconds = 30)
    {
        var result = await _proxyService.TestProxyAsync(id, testUrl, timeoutSeconds);
        return HandleResult(result);
    }

    /// <summary>
    /// Test multiple proxy configurations
    /// </summary>
    [HttpPost("test-bulk")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProxyTestResultDto>>>> BulkTestProxies([FromBody] BulkProxyTestDto testDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.BulkTestProxiesAsync(testDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Validate proxy import data
    /// </summary>
    [HttpPost("validate-import")]
    public async Task<ActionResult<ApiResponse<BulkProxyImportValidationDto>>> ValidateProxyImport([FromBody] BulkProxyImportDto importDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.ValidateProxyImportAsync(importDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Import proxy configurations in bulk
    /// </summary>
    [HttpPost("import-bulk")]
    [RequirePermission(Permissions.ProxiesBulkImport)]
    public async Task<ActionResult<ApiResponse<BulkProxyImportResultDto>>> BulkImportProxies([FromBody] BulkProxyImportDto importDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.BulkImportProxiesAsync(importDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Parse proxy text into import format
    /// </summary>
    [HttpPost("parse-text")]
    public ActionResult<ApiResponse<IEnumerable<ProxyImportItemDto>>> ParseProxyText([FromBody] string proxyText)
    {
        var result = _proxyService.ParseProxyText(proxyText);
        return HandleResult(result);
    }

    /// <summary>
    /// Get proxy statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<ProxyStatsDto>>> GetProxyStats()
    {
        var result = await _proxyService.GetProxyStatsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Enable or disable a proxy configuration
    /// </summary>
    [HttpPatch("{id:guid}/active")]
    public async Task<ActionResult<ApiResponse<bool>>> SetProxyActiveStatus(Guid id, [FromBody] bool isActive)
    {
        var result = await _proxyService.SetProxyActiveStatusAsync(id, isActive);
        return HandleResult(result);
    }

    /// <summary>
    /// Enable or disable multiple proxy configurations
    /// </summary>
    [HttpPatch("bulk-active")]
    public async Task<ActionResult<ApiResponse<bool>>> BulkSetProxyActiveStatus([FromBody] BulkProxyActiveStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.BulkSetProxyActiveStatusAsync(dto.ProxyIds, dto.IsActive);
        return HandleResult(result);
    }

    /// <summary>
    /// Update proxy usage statistics
    /// </summary>
    [HttpPost("usage")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProxyUsage([FromBody] ProxyUsageUpdateDto usageDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _proxyService.UpdateProxyUsageAsync(usageDto);
        return HandleResult(result);
    }
}

/// <summary>
/// DTO for bulk proxy active status update
/// </summary>
public class BulkProxyActiveStatusDto
{
    public List<Guid> ProxyIds { get; set; } = new();
    public bool IsActive { get; set; }
}
