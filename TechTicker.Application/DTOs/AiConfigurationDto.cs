using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

public class AiConfigurationDto
{
    public Guid AiConfigurationId { get; set; }
    public string Provider { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? OpenApiCompatibleUrl { get; set; }
    public string Model { get; set; } = null!;
    public int? InputTokenLimit { get; set; }
    public int? OutputTokenLimit { get; set; }
    public List<string>? Capabilities { get; set; }
    public List<string>? SupportedInputTypes { get; set; }
    public List<string>? SupportedOutputTypes { get; set; }
    public int? RateLimitRpm { get; set; }
    public long? RateLimitTpm { get; set; }
    public int? RateLimitRpd { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    
    // Internal property for AI providers - not exposed in API
    internal string? DecryptedApiKey { get; set; }
}

public class CreateAiConfigurationDto
{
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? OpenApiCompatibleUrl { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string ApiKey { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = null!;
    
    [Range(1, int.MaxValue, ErrorMessage = "Input token limit must be greater than 0")]
    public int? InputTokenLimit { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Output token limit must be greater than 0")]
    public int? OutputTokenLimit { get; set; }
    
    public List<string>? Capabilities { get; set; }
    public List<string>? SupportedInputTypes { get; set; }
    public List<string>? SupportedOutputTypes { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "RPM must be greater than 0")]
    public int? RateLimitRpm { get; set; }
    
    [Range(1, long.MaxValue, ErrorMessage = "TPM must be greater than 0")]
    public long? RateLimitTpm { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "RPD must be greater than 0")]
    public int? RateLimitRpd { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

public class UpdateAiConfigurationDto
{
    [MaxLength(200)]
    public string? Name { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? OpenApiCompatibleUrl { get; set; }
    
    [MaxLength(500)]
    public string? ApiKey { get; set; }
    
    [MaxLength(100)]
    public string? Model { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Input token limit must be greater than 0")]
    public int? InputTokenLimit { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Output token limit must be greater than 0")]
    public int? OutputTokenLimit { get; set; }
    
    public List<string>? Capabilities { get; set; }
    public List<string>? SupportedInputTypes { get; set; }
    public List<string>? SupportedOutputTypes { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "RPM must be greater than 0")]
    public int? RateLimitRpm { get; set; }
    
    [Range(1, long.MaxValue, ErrorMessage = "TPM must be greater than 0")]
    public long? RateLimitTpm { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "RPD must be greater than 0")]
    public int? RateLimitRpd { get; set; }
    
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
}

public class BrowserActionGenerationRequestDto
{
    [Required]
    [MinLength(10, ErrorMessage = "Instructions must be at least 10 characters long")]
    [MaxLength(2000, ErrorMessage = "Instructions cannot exceed 2000 characters")]
    public string Instructions { get; set; } = null!;
    
    [MaxLength(1000)]
    public string? Context { get; set; } // Additional context about the page/site
    
    public Guid? AiConfigurationId { get; set; } // If not provided, use default
}

public class BrowserActionGenerationResponseDto
{
    public List<GeneratedBrowserAction> Actions { get; set; } = new();
    public string? Explanation { get; set; } // AI's explanation for the generated actions
    public int TokensUsed { get; set; }
    public string Model { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GeneratedBrowserAction
{
    public string ActionType { get; set; } = null!;
    public string? Selector { get; set; }
    public int? Repeat { get; set; }
    public int? DelayMs { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; } // AI's explanation for this action
}

public class AiProviderModelsDto
{
    public List<string> Models { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class GenericAiRequestDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Input text is required")]
    [MaxLength(10000, ErrorMessage = "Input text cannot exceed 10,000 characters")]
    public string InputText { get; set; } = null!;
    
    [MaxLength(5000, ErrorMessage = "System prompt cannot exceed 5,000 characters")]
    public string? SystemPrompt { get; set; }
    
    [MaxLength(2000, ErrorMessage = "Context cannot exceed 2,000 characters")]
    public string? Context { get; set; }
    
    public string? JsonSchema { get; set; } // Optional JSON schema for structured output
    
    public Guid? AiConfigurationId { get; set; } // If not provided, use default
    
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double? Temperature { get; set; } = 0.7;
    
    [Range(1, 8192, ErrorMessage = "Max tokens must be between 1 and 8192")]
    public int? MaxTokens { get; set; }
}

public class GenericAiResponseDto
{
    public string Response { get; set; } = null!;
    public int TokensUsed { get; set; }
    public string Model { get; set; } = null!;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsStructuredOutput { get; set; }
    public DateTime GeneratedAt { get; set; }
} 