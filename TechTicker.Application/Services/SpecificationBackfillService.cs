using Microsoft.Extensions.Logging;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities.Canonical;

namespace TechTicker.Application.Services;

/// <summary>
/// One-time (or ad-hoc) backfill service to normalize existing product specifications.
/// </summary>
public class SpecificationBackfillService : ISpecificationBackfillService
{
    private readonly ILogger<SpecificationBackfillService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpecificationNormalizer _normalizer;

    public SpecificationBackfillService(
        ILogger<SpecificationBackfillService> logger,
        IUnitOfWork unitOfWork,
        ISpecificationNormalizer normalizer)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _normalizer = normalizer;
    }

    public async Task<int> BackfillSpecificationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting specification backfill job …");

        var products = await _unitOfWork.Products.GetAllAsync();
        int updated = 0;

        foreach (var product in products)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(product.Specifications))
                continue;

            try
            {
                // Parse existing specs
                var rawDict = product.SpecificationsDict?
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);
                if (rawDict == null || rawDict.Count == 0)
                    continue;

                // Skip if already normalized and non-empty
                if (!string.IsNullOrEmpty(product.NormalizedSpecifications))
                    continue;

                var categoryName = product.Category?.Name ?? string.Empty;
                var (normalized, uncategorized) = _normalizer.Normalize(rawDict, categoryName);

                product.NormalizedSpecificationsDict = normalized;
                product.UncategorizedSpecificationsDict = uncategorized;
                product.UpdatedAt = DateTimeOffset.UtcNow;

                _unitOfWork.Products.Update(product);
                updated++;

                if (updated % 50 == 0)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Backfill progress: {Count} products updated", updated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to backfill product {ProductId}", product.ProductId);
            }
        }

        // Final save
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Specification backfill completed. Updated {Count} products", updated);
        return updated;
    }
} 