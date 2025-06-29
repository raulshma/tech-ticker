using System.ComponentModel.DataAnnotations;

namespace TechTicker.Domain.Entities;

public class AiConfiguration
{
    public Guid AiConfigurationId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = null!; // "OpenAI", "Google", "Anthropic", etc.
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!; // Display name
    
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? OpenApiCompatibleUrl { get; set; } // Base URL for OpenAPI compatible endpoints
    
    [Required]
    [MaxLength(500)]
    public string ApiKey { get; set; } = null!; // Encrypted
    
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = null!; // Model name like "gemini-2.0-flash-exp"
    
    public int? InputTokenLimit { get; set; }
    public int? OutputTokenLimit { get; set; }
    
    // Capabilities (stored as JSON)
    public string? Capabilities { get; set; } // JSON: ["StructuredOutputs", "Caching", "Tuning", ...]
    
    // Supported data types (stored as JSON)
    public string? SupportedInputTypes { get; set; } // JSON: ["Audio", "Images", "Video", "Text", "PDF"]
    public string? SupportedOutputTypes { get; set; } // JSON: ["Text", "Images", "Audio"]
    
    // Rate limits
    public int? RateLimitRpm { get; set; } // Requests per minute
    public long? RateLimitTpm { get; set; } // Tokens per minute
    public int? RateLimitRpd { get; set; } // Requests per day
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties for audit
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
} 