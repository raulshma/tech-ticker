using TechTicker.Application.DTOs;
using TechTicker.Shared.Common;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for PriceHistory operations
/// </summary>
public interface IPriceHistoryService
{
    Task<Result<IEnumerable<PriceHistoryDto>>> GetPriceHistoryAsync(
        Guid productId,
        PriceHistoryQueryDto queryDto);
}
