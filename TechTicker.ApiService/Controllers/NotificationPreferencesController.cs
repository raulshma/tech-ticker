using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for user notification preferences
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationPreferencesController : BaseApiController
{
    private readonly IUserNotificationPreferencesService _notificationPreferencesService;

    public NotificationPreferencesController(IUserNotificationPreferencesService notificationPreferencesService)
    {
        _notificationPreferencesService = notificationPreferencesService;
    }

    /// <summary>
    /// Get current user's notification preferences
    /// </summary>
    /// <returns>User notification preferences</returns>
    [HttpGet(Name = "GetNotificationPreferences")]
    public async Task<ActionResult<ApiResponse<UserNotificationPreferencesDto>>> GetNotificationPreferences()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationPreferencesService.GetUserNotificationPreferencesAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update current user's notification preferences
    /// </summary>
    /// <param name="updateDto">Updated notification preferences</param>
    /// <returns>Updated notification preferences</returns>
    [HttpPut(Name = "UpdateNotificationPreferences")]
    public async Task<ActionResult<ApiResponse<UserNotificationPreferencesDto>>> UpdateNotificationPreferences([FromBody] UpdateUserNotificationPreferencesDto updateDto)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationPreferencesService.UpdateUserNotificationPreferencesAsync(userId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get available products for notification selection
    /// </summary>
    /// <returns>List of products with selection status</returns>
    [HttpGet("products", Name = "GetAvailableProductsForNotification")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NotificationProductSelectionDto>>>> GetAvailableProductsForNotification()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationPreferencesService.GetAvailableProductsForNotificationAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Test Discord webhook configuration
    /// </summary>
    /// <param name="testDto">Test configuration</param>
    /// <returns>Success or error result</returns>
    [HttpPost("test-webhook", Name = "TestDiscordWebhook")]
    public async Task<ActionResult<ApiResponse>> TestDiscordWebhook([FromBody] TestDiscordWebhookDto testDto)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationPreferencesService.TestDiscordWebhookAsync(userId, testDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get notification preferences summary
    /// </summary>
    /// <returns>Summary of notification preferences</returns>
    [HttpGet("summary", Name = "GetNotificationPreferencesSummary")]
    public async Task<ActionResult<ApiResponse<NotificationPreferencesSummaryDto>>> GetNotificationPreferencesSummary()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationPreferencesService.GetNotificationPreferencesSummaryAsync(userId);
        return HandleResult(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}
