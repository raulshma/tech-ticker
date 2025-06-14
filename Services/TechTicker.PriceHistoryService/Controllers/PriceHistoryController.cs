using Microsoft.AspNetCore.Mvc;
using TechTicker.PriceHistoryService.Models;
using TechTicker.PriceHistoryService.Services;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Attributes;

namespace TechTicker.PriceHistoryService.Controllers
{
    /// <summary>
    /// API controller for querying price history data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PriceHistoryController : BaseApiController
    {
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly ILogger<PriceHistoryController> _logger;

        public PriceHistoryController(
            IPriceHistoryService priceHistoryService,
            ILogger<PriceHistoryController> logger)
        {
            _priceHistoryService = priceHistoryService;
            _logger = logger;
        }

        /// <summary>
        /// Gets price history for a specific product with optional filtering
        /// </summary>
        /// <param name="canonicalProductId">The ID of the canonical product</param>
        /// <param name="sellerName">Optional seller name filter</param>
        /// <param name="startDate">Optional start date for filtering</param>
        /// <param name="endDate">Optional end date for filtering</param>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 50, max: 1000)</param>
        /// <returns>Paginated price history data</returns>
        [HttpGet("products/{canonicalProductId:guid}")]
        [ReadOnlyAccess]
        public async Task<IActionResult> GetPriceHistory(
            Guid canonicalProductId,
            [FromQuery] string? sellerName = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            _logger.LogInformation("Received price history request for product {ProductId} by user {UserId}", 
                canonicalProductId, CurrentUserId);

            var request = new PriceHistoryQueryRequest
            {
                CanonicalProductId = canonicalProductId,
                SellerName = sellerName,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = Math.Min(pageSize, ApplicationConstants.Pagination.MaxPageSize)
            };

            var result = await _priceHistoryService.GetPriceHistoryAsync(request);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to retrieve price history for product {ProductId}: {Error}",
                    canonicalProductId, result.ErrorMessage);
                return HandleResult(result);
            }

            return Ok(result.Data, "Price history retrieved successfully");
        }        /// <summary>
        /// Gets price statistics for a product/seller combination
        /// </summary>
        /// <param name="canonicalProductId">The ID of the canonical product</param>
        /// <param name="sellerName">Optional seller name filter</param>
        /// <returns>Price statistics including min, max, average prices</returns>
        [HttpGet("products/{canonicalProductId:guid}/statistics")]
        public async Task<IActionResult> GetPriceStatistics(
            Guid canonicalProductId,
            [FromQuery] string? sellerName = null)
        {
            _logger.LogInformation("Received price statistics request for product {ProductId}, seller {Seller}",
                canonicalProductId, sellerName);

            var result = await _priceHistoryService.GetPriceStatisticsAsync(canonicalProductId, sellerName);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to retrieve price statistics for product {ProductId}: {Error}",
                    canonicalProductId, result.ErrorMessage);
                return HandleResult(result);
            }

            return Ok(result.Data, "Price statistics retrieved successfully");
        }

        /// <summary>
        /// Gets the latest price for a product from a specific seller
        /// </summary>
        /// <param name="canonicalProductId">The ID of the canonical product</param>
        /// <param name="sellerName">The seller name</param>
        /// <returns>Latest price data or null if not found</returns>
        [HttpGet("products/{canonicalProductId:guid}/sellers/{sellerName}/latest")]
        public async Task<IActionResult> GetLatestPrice(
            Guid canonicalProductId,
            string sellerName)
        {
            _logger.LogInformation("Received latest price request for product {ProductId} from seller {Seller}",
                canonicalProductId, sellerName);

            var result = await _priceHistoryService.GetLatestPriceAsync(canonicalProductId, sellerName);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to retrieve latest price for product {ProductId} from seller {Seller}: {Error}",
                    canonicalProductId, sellerName, result.ErrorMessage);
                return HandleResult(result);
            }

            if (result.Data == null)
            {
                return NotFound($"No price data found for product {canonicalProductId} from seller {sellerName}");
            }

            return Ok(result.Data, "Latest price retrieved successfully");
        }

        /// <summary>
        /// Gets all sellers that have price data for a specific product
        /// </summary>
        /// <param name="canonicalProductId">The ID of the canonical product</param>
        /// <returns>List of seller names</returns>
        [HttpGet("products/{canonicalProductId:guid}/sellers")]
        public async Task<IActionResult> GetSellersForProduct(Guid canonicalProductId)
        {
            _logger.LogInformation("Received sellers request for product {ProductId}", canonicalProductId);

            var result = await _priceHistoryService.GetSellersForProductAsync(canonicalProductId);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to retrieve sellers for product {ProductId}: {Error}",
                    canonicalProductId, result.ErrorMessage);
                return HandleResult(result);
            }

            return Ok(result.Data, "Sellers retrieved successfully");
        }
    }
}
