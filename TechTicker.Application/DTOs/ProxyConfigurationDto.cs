using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for proxy configuration display
/// </summary>
public class ProxyConfigurationDto
{
    public Guid ProxyConfigurationId { get; set; }
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string ProxyType { get; set; } = null!;
    public string? Username { get; set; }
    public bool HasPassword { get; set; } // Don't expose actual password
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsHealthy { get; set; }
    public DateTimeOffset? LastTestedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public decimal SuccessRate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxRetries { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Computed properties
    public string DisplayName { get; set; } = null!;
    public bool RequiresAuthentication { get; set; }
    public bool IsReliable { get; set; }
    public string StatusDescription { get; set; } = null!;
}

/// <summary>
/// DTO for creating a new proxy configuration
/// </summary>
public class CreateProxyConfigurationDto
{
    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = null!;

    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProxyType { get; set; } = null!; // HTTP, HTTPS, SOCKS5

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for updating a proxy configuration
/// </summary>
public class UpdateProxyConfigurationDto
{
    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = null!;

    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProxyType { get; set; } = null!;

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for proxy text parsing request
/// </summary>
public class ProxyTextParseDto
{
    [Required]
    public string ProxyText { get; set; } = null!;

    [MaxLength(20)]
    public string DefaultProxyType { get; set; } = "HTTP";
}

/// <summary>
/// DTO for bulk proxy import
/// </summary>
public class BulkProxyImportDto
{
    [Required]
    public List<ProxyImportItemDto> Proxies { get; set; } = new();

    public bool TestBeforeImport { get; set; } = true;
    public bool OverwriteExisting { get; set; } = false;
}

/// <summary>
/// DTO for individual proxy import item
/// </summary>
public class ProxyImportItemDto
{
    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = null!;

    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProxyType { get; set; } = null!;

    [MaxLength(100)]
    public string? Username { get; set; }

    [MaxLength(500)]
    public string? Password { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    // Import validation results
    public bool IsValid { get; set; } = true;
    public List<string> ValidationErrors { get; set; } = new();
    public bool AlreadyExists { get; set; } = false;
}

/// <summary>
/// DTO for proxy test results
/// </summary>
public class ProxyTestResultDto
{
    public Guid ProxyConfigurationId { get; set; }
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string ProxyType { get; set; } = null!;
    public bool IsHealthy { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset TestedAt { get; set; }
}

/// <summary>
/// DTO for bulk proxy testing
/// </summary>
public class BulkProxyTestDto
{
    [Required]
    public List<Guid> ProxyIds { get; set; } = new();

    [MaxLength(500)]
    public string? TestUrl { get; set; } = "https://httpbin.org/ip";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// DTO for proxy statistics
/// </summary>
public class ProxyStatsDto
{
    public int TotalProxies { get; set; }
    public int ActiveProxies { get; set; }
    public int HealthyProxies { get; set; }
    public decimal AverageSuccessRate { get; set; }
    public int ProxiesWithErrors { get; set; }
    public Dictionary<string, int> ProxiesByType { get; set; } = new();
    public Dictionary<string, int> ProxiesByStatus { get; set; } = new();
}

/// <summary>
/// DTO for proxy usage update
/// </summary>
public class ProxyUsageUpdateDto
{
    public Guid ProxyConfigurationId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int ResponseTimeMs { get; set; }
}
