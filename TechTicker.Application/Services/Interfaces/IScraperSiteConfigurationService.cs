using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for ScraperSiteConfiguration operations
/// </summary>
public interface IScraperSiteConfigurationService
{
    Task<Result<ScraperSiteConfigurationDto>> CreateConfigurationAsync(CreateScraperSiteConfigurationDto createDto);
    Task<Result<ScraperSiteConfigurationDto>> GetConfigurationByIdAsync(Guid configId);
    Task<Result<ScraperSiteConfigurationDto>> GetConfigurationByDomainAsync(string domain);
    Task<Result<ScraperSiteConfigurationDto>> UpdateConfigurationAsync(Guid configId, UpdateScraperSiteConfigurationDto updateDto);
    Task<Result> DeleteConfigurationAsync(Guid configId);
}
