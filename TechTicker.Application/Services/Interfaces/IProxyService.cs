using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for proxy configuration management
/// </summary>
public interface IProxyService
{
    /// <summary>
    /// Get all proxy configurations
    /// </summary>
    Task<Result<IEnumerable<ProxyConfigurationDto>>> GetAllProxiesAsync();

    /// <summary>
    /// Get proxy configuration by ID
    /// </summary>
    Task<Result<ProxyConfigurationDto>> GetProxyByIdAsync(Guid id);

    /// <summary>
    /// Get active proxy configurations
    /// </summary>
    Task<Result<IEnumerable<ProxyConfigurationDto>>> GetActiveProxiesAsync();

    /// <summary>
    /// Get healthy proxy configurations
    /// </summary>
    Task<Result<IEnumerable<ProxyConfigurationDto>>> GetHealthyProxiesAsync();

    /// <summary>
    /// Get proxy configurations by type
    /// </summary>
    Task<Result<IEnumerable<ProxyConfigurationDto>>> GetProxiesByTypeAsync(string proxyType);

    /// <summary>
    /// Create a new proxy configuration
    /// </summary>
    Task<Result<ProxyConfigurationDto>> CreateProxyAsync(CreateProxyConfigurationDto createDto);

    /// <summary>
    /// Update an existing proxy configuration
    /// </summary>
    Task<Result<ProxyConfigurationDto>> UpdateProxyAsync(Guid id, UpdateProxyConfigurationDto updateDto);

    /// <summary>
    /// Delete a proxy configuration
    /// </summary>
    Task<Result<bool>> DeleteProxyAsync(Guid id);

    /// <summary>
    /// Test a single proxy configuration
    /// </summary>
    Task<Result<ProxyTestResultDto>> TestProxyAsync(Guid id, string? testUrl = null, int timeoutSeconds = 30);

    /// <summary>
    /// Test multiple proxy configurations
    /// </summary>
    Task<Result<IEnumerable<ProxyTestResultDto>>> BulkTestProxiesAsync(BulkProxyTestDto testDto);

    /// <summary>
    /// Import proxy configurations in bulk
    /// </summary>
    Task<Result<BulkProxyImportResultDto>> BulkImportProxiesAsync(BulkProxyImportDto importDto);

    /// <summary>
    /// Validate proxy import data
    /// </summary>
    Task<Result<BulkProxyImportValidationDto>> ValidateProxyImportAsync(BulkProxyImportDto importDto);

    /// <summary>
    /// Update proxy usage statistics
    /// </summary>
    Task<Result<bool>> UpdateProxyUsageAsync(ProxyUsageUpdateDto usageDto);

    /// <summary>
    /// Get proxy statistics
    /// </summary>
    Task<Result<ProxyStatsDto>> GetProxyStatsAsync();

    /// <summary>
    /// Enable or disable a proxy configuration
    /// </summary>
    Task<Result<bool>> SetProxyActiveStatusAsync(Guid id, bool isActive);

    /// <summary>
    /// Enable or disable multiple proxy configurations
    /// </summary>
    Task<Result<bool>> BulkSetProxyActiveStatusAsync(IEnumerable<Guid> ids, bool isActive);

    /// <summary>
    /// Get proxy configurations that need health checking
    /// </summary>
    Task<Result<IEnumerable<ProxyConfigurationDto>>> GetProxiesForHealthCheckAsync(TimeSpan maxAge);

    /// <summary>
    /// Update proxy health status
    /// </summary>
    Task<Result<bool>> UpdateProxyHealthAsync(Guid id, bool isHealthy, string? errorMessage = null, string? errorCode = null);

    /// <summary>
    /// Parse proxy string in various formats
    /// </summary>
    Result<ProxyImportItemDto> ParseProxyString(string proxyString);

    /// <summary>
    /// Parse multiple proxy strings from text
    /// </summary>
    Result<IEnumerable<ProxyImportItemDto>> ParseProxyText(string proxyText);
}

/// <summary>
/// DTO for bulk proxy import results
/// </summary>
public class BulkProxyImportResultDto
{
    public int TotalProcessed { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public int SkippedDuplicates { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ProxyConfigurationDto> ImportedProxies { get; set; } = new();
}

/// <summary>
/// DTO for bulk proxy import validation
/// </summary>
public class BulkProxyImportValidationDto
{
    public int TotalProxies { get; set; }
    public int ValidProxies { get; set; }
    public int InvalidProxies { get; set; }
    public int DuplicateProxies { get; set; }
    public List<ProxyImportItemDto> ProxiesToImport { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}
