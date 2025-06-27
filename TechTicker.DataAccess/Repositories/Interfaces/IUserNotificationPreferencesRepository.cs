using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories.Interfaces;

/// <summary>
/// Repository interface for UserNotificationPreferences entities
/// </summary>
public interface IUserNotificationPreferencesRepository : IRepository<UserNotificationPreferences>
{
    /// <summary>
    /// Get notification preferences by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User notification preferences or null if not found</returns>
    Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Get all users who have Discord notifications enabled for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>List of user notification preferences</returns>
    Task<IEnumerable<UserNotificationPreferences>> GetUsersWithNotificationsForProductAsync(Guid productId);

    /// <summary>
    /// Get all users who have Discord notifications enabled
    /// </summary>
    /// <returns>List of user notification preferences with Discord enabled</returns>
    Task<IEnumerable<UserNotificationPreferences>> GetUsersWithDiscordEnabledAsync();

    /// <summary>
    /// Create or update user notification preferences
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="preferences">Notification preferences</param>
    /// <returns>Updated preferences</returns>
    Task<UserNotificationPreferences> CreateOrUpdateAsync(Guid userId, UserNotificationPreferences preferences);
}
