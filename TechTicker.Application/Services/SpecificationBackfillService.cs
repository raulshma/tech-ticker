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

    public Task<int> BackfillSpecificationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Specification backfill job skipped - legacy specification column has been removed");
        
        // Legacy specification column has been removed, so this service is no longer functional
        // All new products should use the normalized specification system directly
        return Task.FromResult(0);
    }
} 