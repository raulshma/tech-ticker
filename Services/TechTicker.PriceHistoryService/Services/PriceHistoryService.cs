using Microsoft.EntityFrameworkCore;
using TechTicker.PriceHistoryService.Data;
using TechTicker.PriceHistoryService.Messages;
using TechTicker.PriceHistoryService.Models;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Utilities;

namespace TechTicker.PriceHistoryService.Services
{
    /// <summary>
    /// Service for managing price history data operations
    /// </summary>
    public class PriceHistoryService : IPriceHistoryService
    {
        private readonly PriceHistoryDbContext _dbContext;
        private readonly ILogger<PriceHistoryService> _logger;

        public PriceHistoryService(
            PriceHistoryDbContext dbContext,
            ILogger<PriceHistoryService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Result<long>> StorePricePointAsync(PricePointRecordedEvent pricePoint)
        {
            try
            {
                _logger.LogDebug("Storing price point for product {ProductId} from {Seller} at price {Price}",
                    pricePoint.CanonicalProductId, pricePoint.SellerName, pricePoint.Price);

                var record = new PriceHistoryRecord
                {
                    Timestamp = pricePoint.Timestamp,
                    CanonicalProductId = pricePoint.CanonicalProductId,
                    SellerName = pricePoint.SellerName,
                    Price = pricePoint.Price,
                    StockStatus = pricePoint.StockStatus,
                    SourceUrl = pricePoint.SourceUrl,
                    ProductName = pricePoint.ProductName,
                    OriginalStockStatus = pricePoint.OriginalStockStatus,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _dbContext.PriceHistory.Add(record);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully stored price point {RecordId} for product {ProductId}",
                    record.Id, pricePoint.CanonicalProductId);

                return Result<long>.Success(record.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing price point for product {ProductId}", pricePoint.CanonicalProductId);
                return Result<long>.Failure(ex.Message, ApplicationConstants.ErrorCodes.InternalServerError);
            }
        }

        public async Task<Result<PriceHistoryQueryResponse>> GetPriceHistoryAsync(PriceHistoryQueryRequest request)
        {
            try
            {
                _logger.LogDebug("Querying price history for product {ProductId}, seller {Seller}, page {Page}",
                    request.CanonicalProductId, request.SellerName, request.PageNumber);

                // Validate page parameters
                if (request.PageNumber < 1)
                    request.PageNumber = ApplicationConstants.Pagination.DefaultPageNumber;

                if (request.PageSize < 1 || request.PageSize > ApplicationConstants.Pagination.MaxPageSize)
                    request.PageSize = ApplicationConstants.Pagination.DefaultPageSize;

                var query = _dbContext.PriceHistory
                    .Where(p => p.CanonicalProductId == request.CanonicalProductId);

                // Apply seller filter if specified
                if (!string.IsNullOrWhiteSpace(request.SellerName))
                {
                    query = query.Where(p => p.SellerName == request.SellerName);
                }

                // Apply date filters if specified
                if (request.StartDate.HasValue)
                {
                    query = query.Where(p => p.Timestamp >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(p => p.Timestamp <= request.EndDate.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination and ordering
                var pricePoints = await query
                    .OrderByDescending(p => p.Timestamp)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new PriceHistoryPointDto
                    {
                        Timestamp = p.Timestamp,
                        Price = p.Price,
                        StockStatus = p.StockStatus,
                        SourceUrl = p.SourceUrl,
                        ProductName = p.ProductName,
                        OriginalStockStatus = p.OriginalStockStatus
                    })
                    .ToListAsync();

                var response = new PriceHistoryQueryResponse
                {
                    PricePoints = pricePoints,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    HasNextPage = (request.PageNumber * request.PageSize) < totalCount,
                    HasPreviousPage = request.PageNumber > 1
                };

                _logger.LogDebug("Retrieved {Count} price points for product {ProductId}",
                    pricePoints.Count, request.CanonicalProductId);

                return Result<PriceHistoryQueryResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying price history for product {ProductId}", request.CanonicalProductId);
                return Result<PriceHistoryQueryResponse>.Failure(ex.Message, ApplicationConstants.ErrorCodes.InternalServerError);
            }
        }

        public async Task<Result<PriceStatisticsDto>> GetPriceStatisticsAsync(Guid canonicalProductId, string? sellerName = null)
        {
            try
            {
                _logger.LogDebug("Calculating price statistics for product {ProductId}, seller {Seller}",
                    canonicalProductId, sellerName);

                var query = _dbContext.PriceHistory
                    .Where(p => p.CanonicalProductId == canonicalProductId);

                if (!string.IsNullOrWhiteSpace(sellerName))
                {
                    query = query.Where(p => p.SellerName == sellerName);
                }

                var statistics = await query
                    .GroupBy(p => new { p.CanonicalProductId, p.SellerName })
                    .Select(g => new
                    {
                        g.Key.CanonicalProductId,
                        g.Key.SellerName,
                        MinPrice = g.Min(p => p.Price),
                        MaxPrice = g.Max(p => p.Price),
                        AveragePrice = g.Average(p => p.Price),
                        FirstRecorded = g.Min(p => p.Timestamp),
                        LastRecorded = g.Max(p => p.Timestamp),
                        TotalRecords = g.Count(),
                        LatestRecord = g.OrderByDescending(p => p.Timestamp).FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (statistics == null)
                {
                    return Result<PriceStatisticsDto>.Failure(
                        "No price history found for the specified product/seller combination",
                        ApplicationConstants.ErrorCodes.ResourceNotFound);
                }

                var result = new PriceStatisticsDto
                {
                    CanonicalProductId = statistics.CanonicalProductId,
                    SellerName = statistics.SellerName,
                    CurrentPrice = statistics.LatestRecord?.Price ?? 0,
                    MinPrice = statistics.MinPrice,
                    MaxPrice = statistics.MaxPrice,
                    AveragePrice = Math.Round(statistics.AveragePrice, 2),
                    FirstRecorded = statistics.FirstRecorded,
                    LastRecorded = statistics.LastRecorded,
                    TotalRecords = statistics.TotalRecords,
                    CurrentStockStatus = statistics.LatestRecord?.StockStatus
                };

                _logger.LogDebug("Calculated statistics for product {ProductId}: Min={Min}, Max={Max}, Avg={Avg}",
                    canonicalProductId, result.MinPrice, result.MaxPrice, result.AveragePrice);

                return Result<PriceStatisticsDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price statistics for product {ProductId}", canonicalProductId);
                return Result<PriceStatisticsDto>.Failure(ex.Message, ApplicationConstants.ErrorCodes.InternalServerError);
            }
        }

        public async Task<Result<PriceHistoryPointDto?>> GetLatestPriceAsync(Guid canonicalProductId, string sellerName)
        {
            try
            {
                _logger.LogDebug("Getting latest price for product {ProductId} from seller {Seller}",
                    canonicalProductId, sellerName);

                var latestRecord = await _dbContext.PriceHistory
                    .Where(p => p.CanonicalProductId == canonicalProductId && p.SellerName == sellerName)
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => new PriceHistoryPointDto
                    {
                        Timestamp = p.Timestamp,
                        Price = p.Price,
                        StockStatus = p.StockStatus,
                        SourceUrl = p.SourceUrl,
                        ProductName = p.ProductName,
                        OriginalStockStatus = p.OriginalStockStatus
                    })
                    .FirstOrDefaultAsync();

                return Result<PriceHistoryPointDto?>.Success(latestRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest price for product {ProductId} from seller {Seller}",
                    canonicalProductId, sellerName);
                return Result<PriceHistoryPointDto?>.Failure(ex.Message, ApplicationConstants.ErrorCodes.InternalServerError);
            }
        }

        public async Task<Result<List<string>>> GetSellersForProductAsync(Guid canonicalProductId)
        {
            try
            {
                _logger.LogDebug("Getting sellers for product {ProductId}", canonicalProductId);

                var sellers = await _dbContext.PriceHistory
                    .Where(p => p.CanonicalProductId == canonicalProductId)
                    .Select(p => p.SellerName)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                return Result<List<string>>.Success(sellers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sellers for product {ProductId}", canonicalProductId);
                return Result<List<string>>.Failure(ex.Message, ApplicationConstants.ErrorCodes.InternalServerError);
            }
        }
    }
}
