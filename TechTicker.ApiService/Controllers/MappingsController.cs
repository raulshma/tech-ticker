using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for ProductSellerMapping operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User,Admin,Moderator")] // Allow User, Admin, and Moderator roles
public class MappingsController : BaseApiController
{
    private readonly IProductSellerMappingService _mappingService;
    private readonly IScrapingOrchestrationService _scrapingOrchestrationService;

    public MappingsController(
        IProductSellerMappingService mappingService,
        IScrapingOrchestrationService scrapingOrchestrationService)
    {
        _mappingService = mappingService;
        _scrapingOrchestrationService = scrapingOrchestrationService;
    }

    /// <summary>
    /// Create a new product seller mapping
    /// </summary>
    /// <param name="createDto">Mapping creation data</param>
    /// <returns>Created mapping</returns>
    [HttpPost(Name = "CreateMapping")]
    public async Task<ActionResult<ApiResponse<ProductSellerMappingDto>>> CreateMapping([FromBody] CreateProductSellerMappingDto createDto)
    {
        var result = await _mappingService.CreateMappingAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get mappings by product ID
    /// </summary>
    /// <param name="canonicalProductId">Product ID</param>
    /// <returns>List of mappings for the product</returns>
    [HttpGet(Name = "GetMappings")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductSellerMappingDto>>>> GetMappings([FromQuery] Guid? canonicalProductId = null)
    {
        if (canonicalProductId.HasValue)
        {
            var result = await _mappingService.GetMappingsByProductIdAsync(canonicalProductId.Value);
            return HandleResult(result);
        }

        // If no product ID specified, return all active mappings
        var activeResult = await _mappingService.GetActiveMappingsAsync();
        return HandleResult(activeResult);
    }

    /// <summary>
    /// Get all active mappings (for scraping orchestrator)
    /// </summary>
    /// <returns>List of active mappings</returns>
    [HttpGet("active", Name = "GetActiveMappings")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductSellerMappingDto>>>> GetActiveMappings()
    {
        var result = await _mappingService.GetActiveMappingsAsync();
        return HandleResult(result);
    }

    /// <summary>
    /// Update a product seller mapping
    /// </summary>
    /// <param name="mappingId">Mapping ID</param>
    /// <param name="updateDto">Mapping update data</param>
    /// <returns>Updated mapping</returns>
    [HttpPut("{mappingId:guid}", Name = "UpdateMapping")]
    public async Task<ActionResult<ApiResponse<ProductSellerMappingDto>>> UpdateMapping(Guid mappingId, [FromBody] UpdateProductSellerMappingDto updateDto)
    {
        var result = await _mappingService.UpdateMappingAsync(mappingId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a product seller mapping
    /// </summary>
    /// <param name="mappingId">Mapping ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{mappingId:guid}", Name = "DeleteMapping")]
    public async Task<ActionResult<ApiResponse>> DeleteMapping(Guid mappingId)
    {
        var result = await _mappingService.DeleteMappingAsync(mappingId);
        return HandleResult(result);
    }

    /// <summary>
    /// Trigger manual scraping for a specific mapping
    /// </summary>
    /// <param name="mappingId">Mapping ID to scrape</param>
    /// <returns>Success or error response</returns>
    [HttpPost("{mappingId:guid}/scrape-now", Name = "TriggerManualScraping")]
    public async Task<ActionResult<ApiResponse>> TriggerManualScraping(Guid mappingId)
    {
        var success = await _scrapingOrchestrationService.TriggerManualScrapingAsync(mappingId);

        if (success)
        {
            var result = Result.Success();
            return HandleResult(result);
        }
        else
        {
            var result = Result.Failure("Failed to trigger scraping. The mapping may not exist or may be inactive.", "SCRAPING_TRIGGER_FAILED");
            return HandleResult(result);
        }
    }
}
