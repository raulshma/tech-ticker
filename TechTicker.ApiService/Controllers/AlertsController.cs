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

    /// <summary>
    /// Get all alert rules (Admin only)
    /// </summary>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="productId">Filter by product ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of alert rules</returns>
    [HttpGet("admin", Name = "AlertsAdminGetAll")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<PagedResponse<AlertRuleDto>>> GetAllAlerts(
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _alertRuleService.GetAllAlertRulesAsync(userId, productId, pageNumber, pageSize);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Get alert rule by ID (Admin only)
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <returns>Alert rule details</returns>
    [HttpGet("admin/{alertRuleId:guid}", Name = "GetAlertById")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> GetAlertById(Guid alertRuleId)
    {
        var result = await _alertRuleService.GetAlertRuleByIdAsync(alertRuleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Update any alert rule (Admin only)
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <param name="updateDto">Alert rule update data</param>
    /// <returns>Updated alert rule</returns>
    [HttpPut("admin/{alertRuleId:guid}", Name = "AdminUpdateAlert")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertRuleDto>>> AdminUpdateAlert(Guid alertRuleId, [FromBody] UpdateAlertRuleDto updateDto)
    {
        var result = await _alertRuleService.AdminUpdateAlertRuleAsync(alertRuleId, updateDto);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete any alert rule (Admin only)
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("admin/{alertRuleId:guid}", Name = "AdminDeleteAlert")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse>> AdminDeleteAlert(Guid alertRuleId)
    {
        var result = await _alertRuleService.AdminDeleteAlertRuleAsync(alertRuleId);
        return HandleResult(result);
    }

    /// <summary>
    /// Perform bulk operations on alert rules (Admin only)
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <returns>Bulk operation results</returns>
    [HttpPost("admin/bulk-operation", Name = "BulkOperation")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertBulkOperationResultDto>>> BulkOperation(
        [FromBody] AlertBulkOperationRequestDto request)
    {
        var result = await _alertRuleService.BulkOperationAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Perform bulk operations on alert rules with filters (Admin only)
    /// </summary>
    /// <param name="request">Bulk operation with filters request</param>
    /// <returns>Bulk operation results</returns>
    [HttpPost("admin/bulk-operation-filtered", Name = "BulkOperationWithFilters")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertBulkOperationResultDto>>> BulkOperationWithFilters(
        [FromBody] AlertBulkOperationWithFiltersRequestDto request)
    {
        var result = await _alertRuleService.BulkOperationWithFiltersAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Preview bulk operation results without executing (Admin only)
    /// </summary>
    /// <param name="request">Bulk operation preview request</param>
    /// <returns>Preview of what would be affected</returns>
    [HttpPost("admin/bulk-operation-preview", Name = "PreviewBulkOperation")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertBulkOperationPreviewDto>>> PreviewBulkOperation(
        [FromBody] AlertBulkOperationWithFiltersRequestDto request)
    {
        // Force preview mode
        request.PreviewOnly = true;
        var result = await _alertRuleService.PreviewBulkOperationAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk enable alert rules for current user
    /// </summary>
    /// <param name="alertRuleIds">List of alert rule IDs to enable</param>
    /// <returns>Bulk operation results</returns>
    [HttpPost("bulk-enable", Name = "BulkEnable")]
    [RequirePermission(Permissions.AlertRulesUpdate)]
    public async Task<ActionResult<ApiResponse<AlertBulkOperationResultDto>>> BulkEnable(
        [FromBody] List<Guid> alertRuleIds)
    {
        var request = new AlertBulkOperationRequestDto
        {
            AlertRuleIds = alertRuleIds,
            Operation = "ENABLE",
            Reason = "User bulk enable operation"
        };

        var result = await _alertRuleService.BulkOperationAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk disable alert rules for current user
    /// </summary>
    /// <param name="alertRuleIds">List of alert rule IDs to disable</param>
    /// <returns>Bulk operation results</returns>
    [HttpPost("bulk-disable", Name = "BulkDisable")]
    [RequirePermission(Permissions.AlertRulesUpdate)]
    public async Task<ActionResult<ApiResponse<AlertBulkOperationResultDto>>> BulkDisable(
        [FromBody] List<Guid> alertRuleIds)
    {
        var request = new AlertBulkOperationRequestDto
        {
            AlertRuleIds = alertRuleIds,
            Operation = "DISABLE",
            Reason = "User bulk disable operation"
        };

        var result = await _alertRuleService.BulkOperationAsync(request);
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
