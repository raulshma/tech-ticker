using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing user notification preferences
/// </summary>
public interface IUserNotificationPreferencesService
{
    /// <summary>
    /// Get user's notification preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User notification preferences or default if not found</returns>
    Task<Result<UserNotificationPreferencesDto>> GetUserNotificationPreferencesAsync(Guid userId);

    /// <summary>
    /// Update user's notification preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateDto">Updated preferences</param>
    /// <returns>Updated preferences</returns>
    Task<Result<UserNotificationPreferencesDto>> UpdateUserNotificationPreferencesAsync(Guid userId, UpdateUserNotificationPreferencesDto updateDto);

    /// <summary>
    /// Get available products for notification selection
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of products with selection status</returns>
    Task<Result<IEnumerable<NotificationProductSelectionDto>>> GetAvailableProductsForNotificationAsync(Guid userId);

    /// <summary>
    /// Test Discord webhook configuration
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="testDto">Test configuration</param>
    /// <returns>Success or error result</returns>
    Task<Result> TestDiscordWebhookAsync(Guid userId, TestDiscordWebhookDto testDto);

    /// <summary>
    /// Get notification preferences summary
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Summary of notification preferences</returns>
    Task<Result<NotificationPreferencesSummaryDto>> GetNotificationPreferencesSummaryAsync(Guid userId);

    /// <summary>
    /// Get users who should receive notifications for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>List of user notification preferences</returns>
    Task<IEnumerable<UserNotificationPreferencesDto>> GetUsersForProductNotificationAsync(Guid productId);
}
