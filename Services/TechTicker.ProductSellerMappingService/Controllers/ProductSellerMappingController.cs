using Microsoft.AspNetCore.Mvc;
using TechTicker.ProductSellerMappingService.DTOs;
using TechTicker.ProductSellerMappingService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ProductSellerMappingService.Controllers
{
    /// <summary>
    /// Controller for managing product seller mappings
    /// </summary>
    [Route("api/mappings")]
    [ApiController]
    public class ProductSellerMappingController : BaseApiController
    {
        private readonly IProductSellerMappingService _mappingService;

        /// <summary>
        /// Initializes a new instance of the ProductSellerMappingController
        /// </summary>
        /// <param name="mappingService">The mapping service</param>
        public ProductSellerMappingController(IProductSellerMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        /// <summary>
        /// Creates a new product seller mapping
        /// </summary>
        /// <param name="request">The mapping creation data</param>
        /// <returns>The created mapping</returns>
        /// <response code="201">Mapping created successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="409">Mapping for this product and seller already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductSellerMappingResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateMapping([FromBody] CreateProductSellerMappingRequest request)
        {
            var result = await _mappingService.CreateMappingAsync(request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return CreatedAtAction(
                nameof(GetMapping), 
                new { mappingId = result.Data!.MappingId }, 
                result.Data
            );
        }

        /// <summary>
        /// Gets mappings with optional filtering
        /// </summary>
        /// <param name="query">Query parameters for filtering and pagination</param>
        /// <returns>Paginated list of mappings</returns>
        /// <response code="200">Mappings retrieved successfully</response>
        /// <response code="400">Invalid query parameters</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<ProductSellerMappingResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> GetMappings([FromQuery] ProductSellerMappingQueryRequest query)
        {
            var result = await _mappingService.GetMappingsAsync(query);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return OkPagedDirect(result.Data!);
        }

        /// <summary>
        /// Gets all active mappings (used by Orchestrator for scraping)
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 100)</param>
        /// <returns>Paginated list of active mappings</returns>
        /// <response code="200">Active mappings retrieved successfully</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(PagedResponse<ProductSellerMappingResponse>), 200)]
        public async Task<IActionResult> GetActiveMappings([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            var result = await _mappingService.GetActiveMappingsAsync(pageNumber, pageSize);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return OkPagedDirect(result.Data!);
        }

        /// <summary>
        /// Gets a specific mapping by ID
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <returns>The mapping details</returns>
        /// <response code="200">Mapping found</response>
        /// <response code="404">Mapping not found</response>
        [HttpGet("{mappingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductSellerMappingResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetMapping(Guid mappingId)
        {
            var result = await _mappingService.GetMappingByIdAsync(mappingId);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok(result.Data!, "Mapping retrieved successfully");
        }

        /// <summary>
        /// Updates an existing mapping
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <param name="request">The mapping update data</param>
        /// <returns>The updated mapping</returns>
        /// <response code="200">Mapping updated successfully</response>
        /// <response code="400">Invalid input data</response>
        /// <response code="404">Mapping not found</response>
        /// <response code="409">Mapping for this product and seller already exists</response>
        [HttpPut("{mappingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductSellerMappingResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateMapping(Guid mappingId, [FromBody] UpdateProductSellerMappingRequest request)
        {
            var result = await _mappingService.UpdateMappingAsync(mappingId, request);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok(result.Data!, "Mapping updated successfully");
        }

        /// <summary>
        /// Deletes a mapping
        /// </summary>
        /// <param name="mappingId">The mapping ID</param>
        /// <returns>Confirmation of deletion</returns>
        /// <response code="200">Mapping deleted successfully</response>
        /// <response code="404">Mapping not found</response>
        [HttpDelete("{mappingId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteMapping(Guid mappingId)
        {
            var result = await _mappingService.DeleteMappingAsync(mappingId);
            
            if (result.IsFailure)
            {
                return HandleResult(result);
            }

            return Ok("Mapping deleted successfully");
        }
    }
}
