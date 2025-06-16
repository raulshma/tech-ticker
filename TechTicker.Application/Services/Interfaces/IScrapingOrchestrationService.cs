namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for orchestrating scraping operations
/// </summary>
public interface IScrapingOrchestrationService
{
    Task ScheduleScrapingJobsAsync();
    Task ProcessScrapingResultAsync(Guid mappingId, bool wasSuccessful, string? errorMessage = null);
    Task<bool> TriggerManualScrapingAsync(Guid mappingId);
}
