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
/// API controller for AlertRule operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AlertsController : BaseApiController
{
    private readonly IAlertRuleService _alertRuleService;

    public AlertsController(IAlertRuleService alertRuleService)
    {
        _alertRuleService = alertRuleService;
    }

    /// <summary>
    /// Create a new alert rule
    /// </summary>
    /// <param name="createDto">Alert rule creation data</param>
    /// <returns>Created alert rule</returns>
    [HttpPost(Name = "CreateAlert")]
    [RequirePermission(Permissions.AlertRulesCreate)]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> CreateAlert([FromBody] CreateAlertRuleDto createDto)
    {
        var userId = GetCurrentUserId();
        var result = await _alertRuleService.CreateAlertRuleAsync(userId, createDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user's alert rules
    /// </summary>
    /// <returns>List of user's alert rules</returns>
    [HttpGet(Name = "GetUserAlerts")]
    [RequirePermission(Permissions.AlertRulesRead)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AlertRuleDto>>>> GetAlerts()
    {
        var userId = GetCurrentUserId();
        var result = await _alertRuleService.GetUserAlertRulesAsync(userId);
        return HandleResult(result);
    }

    /// <summary>
    /// Get current user's alert rules for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>List of alert rules for the product</returns>
    [HttpGet("product/{productId:guid}", Name = "GetProductAlerts")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AlertRuleDto>>>> GetProductAlerts(Guid productId)
    {
        var userId = GetCurrentUserId();
        var result = await _alertRuleService.GetProductAlertRulesAsync(userId, productId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update an alert rule
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <param name="updateDto">Alert rule update data</param>
    /// <returns>Updated alert rule</returns>
    [HttpPut("{alertRuleId:guid}", Name = "UpdateAlert")]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> UpdateAlert(Guid alertRuleId, [FromBody] UpdateAlertRuleDto updateDto)
    {
        var userId = GetCurrentUserId();
        var result = await _alertRuleService.UpdateAlertRuleAsync(userId, alertRuleId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete an alert rule
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{alertRuleId:guid}", Name = "DeleteAlert")]
    public async Task<ActionResult<ApiResponse>> DeleteAlert(Guid alertRuleId)
    {
        var userId = GetCurrentUserId();
        var result = await _alertRuleService.DeleteAlertRuleAsync(userId, alertRuleId);
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
