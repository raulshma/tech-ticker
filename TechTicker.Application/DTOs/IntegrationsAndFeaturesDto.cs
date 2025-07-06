using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

public class IntegrationsAndFeaturesDto
{
    public List<FeatureDto> Features { get; set; } = new();
    public List<IntegrationDto> Integrations { get; set; } = new();
    public SystemHealthDto SystemHealth { get; set; } = new();
}

public class FeatureDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Category { get; set; } = null!;
    public FeatureStatus Status { get; set; }
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public List<string> RequiredRoles { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public DateTime? LastUsed { get; set; }
    public int? UsageCount { get; set; }
    public List<ConfigurationStepDto> ConfigurationSteps { get; set; } = new();
}

public class IntegrationDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Type { get; set; } = null!; // "External API", "Database", "Message Queue", etc.
    public IntegrationStatus Status { get; set; }
    public string? Icon { get; set; }
    public string? Version { get; set; }
    public string? ConfigurationRoute { get; set; }
    public bool IsRequired { get; set; }
    public bool IsHealthy { get; set; }
    public string? HealthMessage { get; set; }
    public DateTime? LastHealthCheck { get; set; }
    public List<ConfigurationStepDto> ConfigurationSteps { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConfigurationStepDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public bool IsRequired { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
    public int Order { get; set; }
}

public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public double HealthScore { get; set; } // 0-100
    public int TotalFeatures { get; set; }
    public int ActiveFeatures { get; set; }
    public int TotalIntegrations { get; set; }
    public int HealthyIntegrations { get; set; }
    public int ConfigurationIssues { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public enum FeatureStatus
{
    Active,
    Inactive,
    NeedsConfiguration,
    Disabled,
    Unavailable
}

public enum IntegrationStatus
{
    Connected,
    Disconnected,
    NeedsConfiguration,
    Error,
    Disabled
}

public class FeatureUsageDto
{
    public string FeatureId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
}

public class IntegrationHealthCheckDto
{
    public string IntegrationId { get; set; } = null!;
    public bool IsHealthy { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedAt { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class ConfigurationGuideDto
{
    public string FeatureOrIntegrationId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<ConfigurationStepDto> Steps { get; set; } = new();
    public List<string> Prerequisites { get; set; } = new();
    public string? DocumentationUrl { get; set; }
    public string? VideoUrl { get; set; }
} 