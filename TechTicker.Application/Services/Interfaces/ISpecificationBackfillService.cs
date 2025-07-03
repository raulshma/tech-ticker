namespace TechTicker.Application.Services.Interfaces;

public interface ISpecificationBackfillService
{
    /// <summary>
    /// Processes existing products and populates normalized & uncategorized specification fields.
    /// </summary>
    /// <param name="cancellationToken">Token for cancellation</param>
    /// <returns>Total number of products updated</returns>
    Task<int> BackfillSpecificationsAsync(CancellationToken cancellationToken = default);
} 