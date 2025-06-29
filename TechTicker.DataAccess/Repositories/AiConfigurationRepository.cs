using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

public class AiConfigurationRepository : IAiConfigurationRepository
{
    private readonly TechTickerDbContext _context;

    public AiConfigurationRepository(TechTickerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AiConfiguration>> GetAllAsync()
    {
        return await _context.AiConfigurations
            .OrderBy(c => c.Provider)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<AiConfiguration?> GetByIdAsync(Guid id)
    {
        return await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.AiConfigurationId == id);
    }

    public async Task<AiConfiguration?> GetDefaultConfigurationAsync()
    {
        return await _context.AiConfigurations
            .Where(c => c.IsDefault && c.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<AiConfiguration?> GetByProviderAndModelAsync(string provider, string model)
    {
        return await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.Provider == provider && c.Model == model);
    }

    public async Task<AiConfiguration> CreateAsync(AiConfiguration configuration)
    {
        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;
        
        // If this is set as default, unset other defaults
        if (configuration.IsDefault)
        {
            await UnsetAllDefaultsAsync();
        }

        _context.AiConfigurations.Add(configuration);
        await _context.SaveChangesAsync();
        return configuration;
    }

    public async Task<AiConfiguration> UpdateAsync(AiConfiguration configuration)
    {
        var existing = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.AiConfigurationId == configuration.AiConfigurationId);
        
        if (existing == null)
        {
            throw new InvalidOperationException($"AI configuration with ID {configuration.AiConfigurationId} not found");
        }

        // If this is set as default, unset other defaults
        if (configuration.IsDefault && !existing.IsDefault)
        {
            await UnsetAllDefaultsAsync();
        }

        // Update properties
        existing.Name = configuration.Name;
        existing.Description = configuration.Description;
        existing.OpenApiCompatibleUrl = configuration.OpenApiCompatibleUrl;
        existing.ApiKey = configuration.ApiKey;
        existing.Model = configuration.Model;
        existing.InputTokenLimit = configuration.InputTokenLimit;
        existing.OutputTokenLimit = configuration.OutputTokenLimit;
        existing.Capabilities = configuration.Capabilities;
        existing.SupportedInputTypes = configuration.SupportedInputTypes;
        existing.SupportedOutputTypes = configuration.SupportedOutputTypes;
        existing.RateLimitRpm = configuration.RateLimitRpm;
        existing.RateLimitTpm = configuration.RateLimitTpm;
        existing.RateLimitRpd = configuration.RateLimitRpd;
        existing.IsActive = configuration.IsActive;
        existing.IsDefault = configuration.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = configuration.UpdatedBy;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var configuration = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.AiConfigurationId == id);
        
        if (configuration == null)
        {
            return false;
        }

        _context.AiConfigurations.Remove(configuration);
        await _context.SaveChangesAsync();
        
        // If we deleted the default, ensure we have another default if any configs remain
        if (configuration.IsDefault)
        {
            var remaining = await _context.AiConfigurations
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();
            
            if (remaining != null)
            {
                remaining.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var configuration = await _context.AiConfigurations
            .FirstOrDefaultAsync(c => c.AiConfigurationId == id);
        
        if (configuration == null || !configuration.IsActive)
        {
            return false;
        }

        // Unset all other defaults
        await UnsetAllDefaultsAsync();
        
        // Set this as default
        configuration.IsDefault = true;
        configuration.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AiConfiguration>> GetActiveConfigurationsAsync()
    {
        return await _context.AiConfigurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.Provider)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string provider, string model, Guid? excludeId = null)
    {
        var query = _context.AiConfigurations
            .Where(c => c.Provider == provider && c.Model == model);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.AiConfigurationId != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    private async Task UnsetAllDefaultsAsync()
    {
        var defaultConfigs = await _context.AiConfigurations
            .Where(c => c.IsDefault)
            .ToListAsync();
        
        foreach (var config in defaultConfigs)
        {
            config.IsDefault = false;
            config.UpdatedAt = DateTime.UtcNow;
        }
    }
} 