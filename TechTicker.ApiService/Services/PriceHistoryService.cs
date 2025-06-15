using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for PriceHistory operations
/// </summary>
public class PriceHistoryService : IPriceHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<PriceHistoryService> _logger;

    public PriceHistoryService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<PriceHistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PriceHistoryDto>>> GetPriceHistoryAsync(
        Guid productId,
        PriceHistoryQueryDto queryDto)
    {
        try
        {
            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == productId);
            if (!productExists)
            {
                return Result<IEnumerable<PriceHistoryDto>>.FailureResult("Product not found.", "PRODUCT_NOT_FOUND");
            }

            var priceHistory = await _unitOfWork.PriceHistory.GetPriceHistoryAsync(
                productId,
                queryDto.SellerName,
                queryDto.StartDate,
                queryDto.EndDate,
                queryDto.Limit);

            var priceHistoryDtos = priceHistory.Select(_mappingService.MapToDto);

            return Result<IEnumerable<PriceHistoryDto>>.SuccessResult(priceHistoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for product {ProductId}", productId);
            return Result<IEnumerable<PriceHistoryDto>>.FailureResult("An error occurred while retrieving price history.", "INTERNAL_ERROR");
        }
    }
}
