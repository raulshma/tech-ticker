using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using Discord.Webhook;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for managing user notification preferences
/// </summary>
public class UserNotificationPreferencesService : IUserNotificationPreferencesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMappingService _mappingService;
    private readonly ILogger<UserNotificationPreferencesService> _logger;

    public UserNotificationPreferencesService(
        IUnitOfWork unitOfWork,
        IMappingService mappingService,
        ILogger<UserNotificationPreferencesService> logger)
    {
        _unitOfWork = unitOfWork;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<Result<UserNotificationPreferencesDto>> GetUserNotificationPreferencesAsync(Guid userId)
    {
        try
        {
            var preferences = await _unitOfWork.UserNotificationPreferences.GetByUserIdAsync(userId);
            
            if (preferences == null)
            {
                // Return default preferences
                var defaultPreferences = new UserNotificationPreferencesDto
                {
                    UserId = userId,
                    IsDiscordNotificationEnabled = false,
                    NotificationProductIds = new List<Guid>(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                
                return Result<UserNotificationPreferencesDto>.Success(defaultPreferences);
            }

            var dto = _mappingService.MapToDto(preferences);
            
            // Load product details if there are selected products
            if (dto.NotificationProductIds.Any())
            {
                var products = new List<ProductDto>();
                foreach (var productId in dto.NotificationProductIds)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(productId);
                    if (product != null)
                    {
                        products.Add(_mappingService.MapToDto(product));
                    }
                }
                dto.NotificationProducts = products;
            }

            return Result<UserNotificationPreferencesDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for user {UserId}", userId);
            return Result<UserNotificationPreferencesDto>.Failure("Failed to get notification preferences", "GET_PREFERENCES_FAILED");
        }
    }

    public async Task<Result<UserNotificationPreferencesDto>> UpdateUserNotificationPreferencesAsync(Guid userId, UpdateUserNotificationPreferencesDto updateDto)
    {
        try
        {
            // Validate product limit
            if (updateDto.NotificationProductIds.Count > 5)
            {
                return Result<UserNotificationPreferencesDto>.Failure("You can select up to 5 products for notifications", "PRODUCT_LIMIT_EXCEEDED");
            }

            // Validate that all selected products exist
            foreach (var productId in updateDto.NotificationProductIds)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                {
                    return Result<UserNotificationPreferencesDto>.Failure($"Product with ID {productId} not found", "PRODUCT_NOT_FOUND");
                }
            }

            var preferences = new UserNotificationPreferences
            {
                DiscordWebhookUrl = updateDto.DiscordWebhookUrl,
                IsDiscordNotificationEnabled = updateDto.IsDiscordNotificationEnabled,
                NotificationProductIdsList = updateDto.NotificationProductIds,
                CustomBotName = updateDto.CustomBotName,
                CustomAvatarUrl = updateDto.CustomAvatarUrl
            };

            var updatedPreferences = await _unitOfWork.UserNotificationPreferences.CreateOrUpdateAsync(userId, preferences);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mappingService.MapToDto(updatedPreferences);
            
            _logger.LogInformation("Updated notification preferences for user {UserId}", userId);
            return Result<UserNotificationPreferencesDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", userId);
            return Result<UserNotificationPreferencesDto>.Failure("Failed to update notification preferences", "UPDATE_PREFERENCES_FAILED");
        }
    }

    public async Task<Result<IEnumerable<NotificationProductSelectionDto>>> GetAvailableProductsForNotificationAsync(Guid userId)
    {
        try
        {
            var userPreferences = await _unitOfWork.UserNotificationPreferences.GetByUserIdAsync(userId);
            var selectedProductIds = userPreferences?.NotificationProductIdsList ?? new List<Guid>();

            // Get products that the user has alert rules for
            var userAlerts = await _unitOfWork.AlertRules.GetByUserIdAsync(userId);
            var productsWithAlerts = userAlerts.Select(a => a.CanonicalProductId).Distinct().ToList();

            var products = new List<NotificationProductSelectionDto>();

            foreach (var productId in productsWithAlerts)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product != null)
                {
                    products.Add(new NotificationProductSelectionDto
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        CategoryName = product.Category?.Name,
                        Manufacturer = product.Manufacturer,
                        IsSelected = selectedProductIds.Contains(product.ProductId),
                        HasActiveAlerts = true
                    });
                }
            }

            return Result<IEnumerable<NotificationProductSelectionDto>>.Success(products.OrderBy(p => p.ProductName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available products for user {UserId}", userId);
            return Result<IEnumerable<NotificationProductSelectionDto>>.Failure("Failed to get available products", "GET_PRODUCTS_FAILED");
        }
    }

    public async Task<Result> TestDiscordWebhookAsync(Guid userId, TestDiscordWebhookDto testDto)
    {
        try
        {
            using var client = new DiscordWebhookClient(testDto.DiscordWebhookUrl);

            var embed = new Discord.EmbedBuilder()
                .WithTitle("🔔 TechTicker Test Notification")
                .WithDescription("This is a test notification to verify your Discord webhook configuration.")
                .WithColor(Discord.Color.Green)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .AddField("✅ Status", "Webhook is working correctly!", false)
                .AddField("👤 User", $"User ID: {userId}", true)
                .AddField("⚙️ Configuration", "Test completed successfully", true)
                .WithFooter("TechTicker Notifications Test");

            await client.SendMessageAsync(
                text: "🧪 **TechTicker Webhook Test**",
                embeds: new[] { embed.Build() },
                username: testDto.CustomBotName ?? "TechTicker",
                avatarUrl: testDto.CustomAvatarUrl);

            _logger.LogInformation("Successfully tested Discord webhook for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Discord webhook for user {UserId}", userId);
            return Result.Failure("Failed to send test notification. Please check your webhook URL.", "WEBHOOK_TEST_FAILED");
        }
    }

    public async Task<Result<NotificationPreferencesSummaryDto>> GetNotificationPreferencesSummaryAsync(Guid userId)
    {
        try
        {
            var preferences = await _unitOfWork.UserNotificationPreferences.GetByUserIdAsync(userId);

            var summary = new NotificationPreferencesSummaryDto
            {
                IsDiscordEnabled = preferences?.IsDiscordNotificationEnabled ?? false,
                HasWebhookConfigured = !string.IsNullOrEmpty(preferences?.DiscordWebhookUrl),
                SelectedProductsCount = preferences?.NotificationProductIdsList.Count ?? 0,
                MaxProductsAllowed = 5
            };

            if (preferences != null && preferences.NotificationProductIdsList.Any())
            {
                var productNames = new List<string>();
                foreach (var productId in preferences.NotificationProductIdsList)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(productId);
                    if (product != null)
                    {
                        productNames.Add(product.Name);
                    }
                }
                summary.SelectedProductNames = productNames;
            }

            return Result<NotificationPreferencesSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences summary for user {UserId}", userId);
            return Result<NotificationPreferencesSummaryDto>.Failure("Failed to get preferences summary", "GET_SUMMARY_FAILED");
        }
    }

    public async Task<IEnumerable<UserNotificationPreferencesDto>> GetUsersForProductNotificationAsync(Guid productId)
    {
        try
        {
            var preferences = await _unitOfWork.UserNotificationPreferences.GetUsersWithNotificationsForProductAsync(productId);
            return preferences.Select(p => _mappingService.MapToDto(p));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for product notification {ProductId}", productId);
            return Enumerable.Empty<UserNotificationPreferencesDto>();
        }
    }
}
