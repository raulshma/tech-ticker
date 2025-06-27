using Microsoft.EntityFrameworkCore;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess.Repositories;

/// <summary>
/// Repository implementation for UserNotificationPreferences entities
/// </summary>
public class UserNotificationPreferencesRepository : Repository<UserNotificationPreferences>, IUserNotificationPreferencesRepository
{
    public UserNotificationPreferencesRepository(TechTickerDbContext context) : base(context)
    {
    }

    public async Task<UserNotificationPreferences?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<IEnumerable<UserNotificationPreferences>> GetUsersWithNotificationsForProductAsync(Guid productId)
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.IsDiscordNotificationEnabled && 
                       !string.IsNullOrEmpty(p.DiscordWebhookUrl) &&
                       !string.IsNullOrEmpty(p.NotificationProductIds))
            .ToListAsync()
            .ContinueWith(task => 
            {
                // Filter in memory for JSON contains check (more efficient than SQL JSON queries)
                return task.Result.Where(p => p.NotificationProductIdsList.Contains(productId));
            });
    }

    public async Task<IEnumerable<UserNotificationPreferences>> GetUsersWithDiscordEnabledAsync()
    {
        return await _dbSet
            .Include(p => p.User)
            .Where(p => p.IsDiscordNotificationEnabled && 
                       !string.IsNullOrEmpty(p.DiscordWebhookUrl))
            .ToListAsync();
    }

    public async Task<UserNotificationPreferences> CreateOrUpdateAsync(Guid userId, UserNotificationPreferences preferences)
    {
        var existing = await GetByUserIdAsync(userId);
        
        if (existing != null)
        {
            // Update existing preferences
            existing.DiscordWebhookUrl = preferences.DiscordWebhookUrl;
            existing.IsDiscordNotificationEnabled = preferences.IsDiscordNotificationEnabled;
            existing.NotificationProductIds = preferences.NotificationProductIds;
            existing.CustomBotName = preferences.CustomBotName;
            existing.CustomAvatarUrl = preferences.CustomAvatarUrl;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            
            Update(existing);
            return existing;
        }
        else
        {
            // Create new preferences
            preferences.UserId = userId;
            preferences.UserNotificationPreferencesId = Guid.NewGuid();
            preferences.CreatedAt = DateTimeOffset.UtcNow;
            preferences.UpdatedAt = DateTimeOffset.UtcNow;
            
            await AddAsync(preferences);
            return preferences;
        }
    }
}
