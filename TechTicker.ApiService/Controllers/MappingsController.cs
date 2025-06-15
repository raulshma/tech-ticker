using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for ProductSellerMapping operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class MappingsController : BaseApiController
{
    private readonly IProductSellerMappingService _mappingService;

    public MappingsController(IProductSellerMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <summary>
    /// Create a new product seller mapping
    /// </summary>
    /// <param name="createDto">Mapping creation data</param>
    /// <returns>Created mapping</returns>
    [HttpPost]
    public async Task<IActionResult> CreateMapping([FromBody] CreateProductSellerMappingDto createDto)
    {
        var result = await _mappingService.CreateMappingAsync(createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get mappings by product ID
    /// </summary>
    /// <param name="canonicalProductId">Product ID</param>
    /// <returns>List of mappings for the product</returns>
    [HttpGet]
    public async Task<IActionResult> GetMappings([FromQuery] Guid? canonicalProductId = null)
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
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveMappings()
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
    [HttpPut("{mappingId:guid}")]
    public async Task<IActionResult> UpdateMapping(Guid mappingId, [FromBody] UpdateProductSellerMappingDto updateDto)
    {
        var result = await _mappingService.UpdateMappingAsync(mappingId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a product seller mapping
    /// </summary>
    /// <param name="mappingId">Mapping ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{mappingId:guid}")]
    public async Task<IActionResult> DeleteMapping(Guid mappingId)
    {
        var result = await _mappingService.DeleteMappingAsync(mappingId);
        return HandleResult(result);
    }
}
