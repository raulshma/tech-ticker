using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

public interface IAiConfigurationRepository
{
    Task<IEnumerable<AiConfiguration>> GetAllAsync();
    Task<AiConfiguration?> GetByIdAsync(Guid id);
    Task<AiConfiguration?> GetDefaultConfigurationAsync();
    Task<AiConfiguration?> GetByProviderAndModelAsync(string provider, string model);
    Task<AiConfiguration> CreateAsync(AiConfiguration configuration);
    Task<AiConfiguration> UpdateAsync(AiConfiguration configuration);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SetDefaultAsync(Guid id);
    Task<IEnumerable<AiConfiguration>> GetActiveConfigurationsAsync();
    Task<bool> ExistsAsync(string provider, string model, Guid? excludeId = null);
} 