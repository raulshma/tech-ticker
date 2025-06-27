using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// User-specific notification preferences for Discord notifications
/// </summary>
public class UserNotificationPreferences
{
    [Key]
    public Guid UserNotificationPreferencesId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User's Discord webhook URL for receiving notifications
    /// </summary>
    [MaxLength(500)]
    public string? DiscordWebhookUrl { get; set; }

    /// <summary>
    /// Whether Discord notifications are enabled for this user
    /// </summary>
    public bool IsDiscordNotificationEnabled { get; set; } = false;

    /// <summary>
    /// JSON array of product IDs (up to 5) that the user wants to receive notifications for
    /// </summary>
    public string? NotificationProductIds { get; set; }

    /// <summary>
    /// Custom bot name for Discord notifications (optional)
    /// </summary>
    [MaxLength(100)]
    public string? CustomBotName { get; set; }

    /// <summary>
    /// Custom avatar URL for Discord notifications (optional)
    /// </summary>
    [MaxLength(500)]
    public string? CustomAvatarUrl { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    // Helper property for working with product IDs as a list
    [NotMapped]
    public List<Guid> NotificationProductIdsList
    {
        get
        {
            if (string.IsNullOrEmpty(NotificationProductIds))
                return new List<Guid>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(NotificationProductIds) ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }
        set
        {
            // Limit to maximum 5 products
            var limitedList = value?.Take(5).ToList() ?? new List<Guid>();
            NotificationProductIds = limitedList.Count > 0 
                ? System.Text.Json.JsonSerializer.Serialize(limitedList) 
                : null;
        }
    }

    /// <summary>
    /// Check if notifications are enabled for a specific product
    /// </summary>
    /// <param name="productId">Product ID to check</param>
    /// <returns>True if notifications are enabled for this product</returns>
    public bool IsNotificationEnabledForProduct(Guid productId)
    {
        return IsDiscordNotificationEnabled && 
               !string.IsNullOrEmpty(DiscordWebhookUrl) && 
               NotificationProductIdsList.Contains(productId);
    }

    /// <summary>
    /// Add a product to the notification list (up to 5 products)
    /// </summary>
    /// <param name="productId">Product ID to add</param>
    /// <returns>True if added successfully, false if limit reached or already exists</returns>
    public bool AddNotificationProduct(Guid productId)
    {
        var currentList = NotificationProductIdsList;
        
        if (currentList.Contains(productId))
            return false; // Already exists
            
        if (currentList.Count >= 5)
            return false; // Limit reached
            
        currentList.Add(productId);
        NotificationProductIdsList = currentList;
        return true;
    }

    /// <summary>
    /// Remove a product from the notification list
    /// </summary>
    /// <param name="productId">Product ID to remove</param>
    /// <returns>True if removed successfully</returns>
    public bool RemoveNotificationProduct(Guid productId)
    {
        var currentList = NotificationProductIdsList;
        var removed = currentList.Remove(productId);
        
        if (removed)
        {
            NotificationProductIdsList = currentList;
        }
        
        return removed;
    }
}
