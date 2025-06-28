using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Represents a proxy configuration for web scraping
/// </summary>
public class ProxyConfiguration
{
    [Key]
    public Guid ProxyConfigurationId { get; set; }

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
    public string? Password { get; set; } // Should be encrypted

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsHealthy { get; set; } = true;

    public DateTimeOffset? LastTestedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal SuccessRate { get; set; } = 0.0m; // Percentage 0-100

    public int TotalRequests { get; set; } = 0;

    public int SuccessfulRequests { get; set; } = 0;

    public int FailedRequests { get; set; } = 0;

    public int ConsecutiveFailures { get; set; } = 0;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;

    [MaxLength(500)]
    public string? LastErrorMessage { get; set; }

    [MaxLength(50)]
    public string? LastErrorCode { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Computed properties
    [NotMapped]
    public string DisplayName => $"{ProxyType}://{Host}:{Port}";

    [NotMapped]
    public bool RequiresAuthentication => !string.IsNullOrEmpty(Username);

    [NotMapped]
    public bool IsReliable => SuccessRate >= 80.0m && ConsecutiveFailures < 3;

    [NotMapped]
    public string StatusDescription
    {
        get
        {
            if (!IsActive) return "Disabled";
            if (!IsHealthy) return "Unhealthy";
            if (ConsecutiveFailures >= 3) return "Failing";
            if (SuccessRate >= 90) return "Excellent";
            if (SuccessRate >= 80) return "Good";
            if (SuccessRate >= 60) return "Fair";
            return "Poor";
        }
    }

    // Helper methods
    public void RecordSuccess()
    {
        TotalRequests++;
        SuccessfulRequests++;
        ConsecutiveFailures = 0;
        LastUsedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdateSuccessRate();
    }

    public void RecordFailure(string? errorMessage = null, string? errorCode = null)
    {
        TotalRequests++;
        FailedRequests++;
        ConsecutiveFailures++;
        LastErrorMessage = errorMessage;
        LastErrorCode = errorCode;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        // Mark as unhealthy after 3 consecutive failures
        if (ConsecutiveFailures >= 3)
        {
            IsHealthy = false;
        }
        
        UpdateSuccessRate();
    }

    public void UpdateHealthStatus(bool isHealthy, string? errorMessage = null, string? errorCode = null)
    {
        IsHealthy = isHealthy;
        LastTestedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        
        if (!isHealthy)
        {
            LastErrorMessage = errorMessage;
            LastErrorCode = errorCode;
        }
        else
        {
            // Clear error info on successful health check
            LastErrorMessage = null;
            LastErrorCode = null;
        }
    }

    private void UpdateSuccessRate()
    {
        if (TotalRequests > 0)
        {
            SuccessRate = Math.Round((decimal)SuccessfulRequests / TotalRequests * 100, 2);
        }
    }
}
