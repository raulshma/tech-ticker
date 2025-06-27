using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.DTOs;
using TechTicker.Application.Messages;
using TechTicker.Application.Services.Interfaces;
using TechTicker.NotificationWorker.Configuration;

namespace TechTicker.NotificationWorker.Services;

/// <summary>
/// Service for sending Discord webhook notifications
/// </summary>
public class DiscordService
{
    private readonly DiscordConfiguration _discordConfig;
    private readonly ILogger<DiscordService> _logger;
    private readonly IUserNotificationPreferencesService _notificationPreferencesService;

    public DiscordService(
        IOptions<DiscordConfiguration> discordConfig,
        ILogger<DiscordService> logger,
        IUserNotificationPreferencesService notificationPreferencesService)
    {
        _discordConfig = discordConfig.Value;
        _logger = logger;
        _notificationPreferencesService = notificationPreferencesService;
    }

    public async Task SendAlertNotificationAsync(AlertTriggeredEvent alertEvent)
    {
        try
        {
            // Get user-specific notification preferences
            var userPreferences = await _notificationPreferencesService.GetUserNotificationPreferencesAsync(alertEvent.UserId);

            if (!userPreferences.IsSuccess || userPreferences.Data == null)
            {
                _logger.LogInformation("No notification preferences found for user {UserId}", alertEvent.UserId);
                return;
            }

            var preferences = userPreferences.Data;

            // Check if Discord notifications are enabled for this user
            if (!preferences.IsDiscordNotificationEnabled)
            {
                _logger.LogInformation("Discord notifications are disabled for user {UserId}", alertEvent.UserId);
                return;
            }

            // Check if user has configured a webhook URL
            if (string.IsNullOrEmpty(preferences.DiscordWebhookUrl))
            {
                _logger.LogWarning("Discord webhook URL is not configured for user {UserId}", alertEvent.UserId);
                return;
            }

            // Check if user wants notifications for this specific product
            if (!preferences.NotificationProductIds.Contains(alertEvent.CanonicalProductId))
            {
                _logger.LogInformation("User {UserId} has not enabled notifications for product {ProductId}",
                    alertEvent.UserId, alertEvent.CanonicalProductId);
                return;
            }

            await SendDiscordNotificationAsync(alertEvent, preferences);

            _logger.LogInformation("Successfully sent Discord alert notification for user {UserId}, product {ProductName}",
                alertEvent.UserId, alertEvent.ProductName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord alert notification for user {UserId}", alertEvent.UserId);
            throw;
        }
    }

    private async Task SendDiscordNotificationAsync(AlertTriggeredEvent alertEvent, UserNotificationPreferencesDto preferences)
    {
        using var client = new DiscordWebhookClient(preferences.DiscordWebhookUrl!);

        var embed = CreateAlertEmbed(alertEvent);
        var message = CreateAlertMessage(alertEvent);

        var botName = !string.IsNullOrEmpty(preferences.CustomBotName)
            ? preferences.CustomBotName
            : _discordConfig.BotName;

        var avatarUrl = !string.IsNullOrEmpty(preferences.CustomAvatarUrl)
            ? preferences.CustomAvatarUrl
            : _discordConfig.AvatarUrl;

        await client.SendMessageAsync(
            text: message,
            embeds: new[] { embed.Build() },
            username: botName,
            avatarUrl: string.IsNullOrEmpty(avatarUrl) ? null : avatarUrl);
    }

    private string CreateAlertMessage(AlertTriggeredEvent alertEvent)
    {
        var userName = !string.IsNullOrEmpty(alertEvent.UserFirstName)
            ? alertEvent.UserFirstName
            : "User";

        return alertEvent.RuleDescription switch
        {
            var desc when desc.Contains("Price below") => $"🔔 **Price Alert for {userName}!** {alertEvent.ProductName} is now **${alertEvent.TriggeringPrice:F2}**",
            var desc when desc.Contains("drop") => $"📉 **Price Drop Alert for {userName}!** {alertEvent.ProductName} price dropped!",
            var desc when desc.Contains("Back in stock") => $"📦 **Stock Alert for {userName}!** {alertEvent.ProductName} is back in stock!",
            _ => $"🔔 **TechTicker Alert for {userName}!** {alertEvent.ProductName}"
        };
    }

    private Discord.EmbedBuilder CreateAlertEmbed(AlertTriggeredEvent alertEvent)
    {
        var categoryInfo = !string.IsNullOrEmpty(alertEvent.ProductCategoryName)
            ? $" • {alertEvent.ProductCategoryName}"
            : "";

        var color = alertEvent.RuleDescription switch
        {
            var desc when desc.Contains("Price below") => Discord.Color.Green,
            var desc when desc.Contains("drop") => Discord.Color.Orange,
            var desc when desc.Contains("Back in stock") => Discord.Color.Blue,
            _ => Discord.Color.Purple
        };

        var embed = new Discord.EmbedBuilder()
            .WithTitle($"🔔 TechTicker Alert")
            .WithDescription($"**{alertEvent.ProductName}**{categoryInfo}")
            .WithColor(color)
            .WithTimestamp(alertEvent.Timestamp)
            .AddField("💰 Current Price", $"${alertEvent.TriggeringPrice:F2}", true)
            .AddField("🏪 Seller", alertEvent.SellerName, true)
            .AddField("📦 Stock Status", alertEvent.TriggeringStockStatus, true)
            .AddField("⚙️ Alert Rule", alertEvent.RuleDescription, false)
            .WithFooter("TechTicker Notifications");

        if (!string.IsNullOrEmpty(alertEvent.ProductPageUrl))
        {
            embed.WithUrl(alertEvent.ProductPageUrl);
        }

        return embed;
    }
}
