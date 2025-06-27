namespace TechTicker.NotificationWorker.Configuration;

/// <summary>
/// Configuration for Discord webhook settings
/// </summary>
public class DiscordConfiguration
{
    public const string SectionName = "Discord";

    public string WebhookUrl { get; set; } = string.Empty;
    public string BotName { get; set; } = "TechTicker";
    public string AvatarUrl { get; set; } = string.Empty;
    public bool EnableDiscordNotifications { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}
