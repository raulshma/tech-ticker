using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for user notification preferences
/// </summary>
public class UserNotificationPreferencesDto
{
    public Guid UserNotificationPreferencesId { get; set; }
    public Guid UserId { get; set; }
    public string? DiscordWebhookUrl { get; set; }
    public bool IsDiscordNotificationEnabled { get; set; }
    public List<Guid> NotificationProductIds { get; set; } = new();
    public string? CustomBotName { get; set; }
    public string? CustomAvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Additional properties for UI
    public List<ProductDto>? NotificationProducts { get; set; }
}

/// <summary>
/// DTO for creating or updating user notification preferences
/// </summary>
public class UpdateUserNotificationPreferencesDto
{
    [Url(ErrorMessage = "Please enter a valid Discord webhook URL")]
    public string? DiscordWebhookUrl { get; set; }
    
    public bool IsDiscordNotificationEnabled { get; set; }
    
    [MaxLength(5, ErrorMessage = "You can select up to 5 products for notifications")]
    public List<Guid> NotificationProductIds { get; set; } = new();
    
    [MaxLength(100, ErrorMessage = "Bot name cannot exceed 100 characters")]
    public string? CustomBotName { get; set; }
    
    [Url(ErrorMessage = "Please enter a valid avatar URL")]
    public string? CustomAvatarUrl { get; set; }
}

/// <summary>
/// DTO for testing Discord webhook
/// </summary>
public class TestDiscordWebhookDto
{
    [Required]
    [Url(ErrorMessage = "Please enter a valid Discord webhook URL")]
    public string DiscordWebhookUrl { get; set; } = null!;
    
    public string? CustomBotName { get; set; }
    public string? CustomAvatarUrl { get; set; }
}

/// <summary>
/// DTO for product selection in notification preferences
/// </summary>
public class NotificationProductSelectionDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? CategoryName { get; set; }
    public string? Manufacturer { get; set; }
    public bool IsSelected { get; set; }
    public bool HasActiveAlerts { get; set; }
}

/// <summary>
/// DTO for notification preferences summary
/// </summary>
public class NotificationPreferencesSummaryDto
{
    public bool IsDiscordEnabled { get; set; }
    public bool HasWebhookConfigured { get; set; }
    public int SelectedProductsCount { get; set; }
    public int MaxProductsAllowed { get; set; } = 5;
    public List<string> SelectedProductNames { get; set; } = new();
}
