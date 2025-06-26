using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

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
                return Result<IEnumerable<PriceHistoryDto>>.Failure("Product not found.", "PRODUCT_NOT_FOUND");
            }

            var priceHistory = await _unitOfWork.PriceHistory.GetPriceHistoryAsync(
                productId,
                queryDto.SellerName,
                queryDto.StartDate,
                queryDto.EndDate,
                queryDto.Limit);

            var priceHistoryDtos = priceHistory.Select(_mappingService.MapToDto);

            return Result<IEnumerable<PriceHistoryDto>>.Success(priceHistoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving price history for product {ProductId}", productId);
            return Result<IEnumerable<PriceHistoryDto>>.Failure("An error occurred while retrieving price history.", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<IEnumerable<CurrentPriceDto>>> GetCurrentPricesAsync(Guid productId)
    {
        try
        {
            // Validate product exists
            var productExists = await _unitOfWork.Products.ExistsAsync(p => p.ProductId == productId);
            if (!productExists)
            {
                return Result<IEnumerable<CurrentPriceDto>>.Failure("Product not found.", "PRODUCT_NOT_FOUND");
            }

            var currentPrices = await _unitOfWork.PriceHistory.GetCurrentPricesAsync(productId);
            var currentPriceDtos = currentPrices.Select(_mappingService.MapToCurrentPriceDto);

            return Result<IEnumerable<CurrentPriceDto>>.Success(currentPriceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current prices for product {ProductId}", productId);
            return Result<IEnumerable<CurrentPriceDto>>.Failure("An error occurred while retrieving current prices.", "INTERNAL_ERROR");
        }
    }
}
