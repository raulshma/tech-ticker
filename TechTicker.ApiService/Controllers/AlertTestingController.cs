using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Common;
using TechTicker.Shared.Constants;
using TechTicker.Shared.Controllers;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for testing and simulating alert rules
/// </summary>
[Route("api/alert-testing")]
[ApiController]
[Authorize]
public class AlertTestingController : BaseApiController
{
    private readonly IAlertTestingService _alertTestingService;
    private readonly ILogger<AlertTestingController> _logger;

    public AlertTestingController(
        IAlertTestingService alertTestingService,
        ILogger<AlertTestingController> logger)
    {
        _alertTestingService = alertTestingService;
        _logger = logger;
    }

    /// <summary>
    /// Test an existing alert rule against a specific price point
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <param name="testPricePoint">Price point to test against</param>
    /// <returns>Test results</returns>
    [HttpPost("{alertRuleId:guid}/test-point")]
    [RequirePermission(Permissions.AlertRulesRead)]
    public async Task<ActionResult<ApiResponse<AlertTestResultDto>>> TestAlertRule(
        Guid alertRuleId, 
        [FromBody] TestPricePointDto testPricePoint)
    {
        var result = await _alertTestingService.TestAlertRuleAsync(alertRuleId, testPricePoint);
        return HandleResult(result);
    }

    /// <summary>
    /// Test an existing alert rule against historical price data
    /// </summary>
    /// <param name="request">Test request with filters</param>
    /// <returns>Test results</returns>
    [HttpPost("test-history")]
    [RequirePermission(Permissions.AlertRulesRead)]
    public async Task<ActionResult<ApiResponse<AlertTestResultDto>>> TestAlertRuleAgainstHistory(
        [FromBody] AlertTestRequestDto request)
    {
        var result = await _alertTestingService.TestAlertRuleAgainstHistoryAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Simulate a new alert rule definition without saving it
    /// </summary>
    /// <param name="request">Simulation request</param>
    /// <returns>Simulation results</returns>
    [HttpPost("simulate")]
    [RequirePermission(Permissions.AlertRulesCreate)]
    public async Task<ActionResult<ApiResponse<AlertTestResultDto>>> SimulateAlertRule(
        [FromBody] AlertRuleSimulationRequestDto request)
    {
        var result = await _alertTestingService.SimulateAlertRuleAsync(request);
        return HandleResult(result);
    }

    /// <summary>
    /// Get performance metrics for an alert rule
    /// </summary>
    /// <param name="alertRuleId">Alert rule ID</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>Performance metrics</returns>
    [HttpGet("{alertRuleId:guid}/performance")]
    [RequirePermission(Permissions.AlertRulesRead)]
    public async Task<ActionResult<ApiResponse<AlertPerformanceMetricsDto>>> GetAlertRulePerformance(
        Guid alertRuleId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var result = await _alertTestingService.GetAlertRulePerformanceAsync(alertRuleId, startDate, endDate);
        return HandleResult(result);
    }

    /// <summary>
    /// Validate an alert rule configuration
    /// </summary>
    /// <param name="alertRule">Alert rule to validate</param>
    /// <returns>Validation results</returns>
    [HttpPost("validate")]
    [RequirePermission(Permissions.AlertRulesCreate)]
    public async Task<ActionResult<ApiResponse<AlertRuleValidationResultDto>>> ValidateAlertRule(
        [FromBody] TestAlertRuleDto alertRule)
    {
        var result = await _alertTestingService.ValidateAlertRuleAsync(alertRule);
        return HandleResult(result);
    }

    /// <summary>
    /// Test multiple alert rules against a price point (Admin only)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="testPricePoint">Price point to test against</param>
    /// <returns>Test results for all alert rules for the product</returns>
    [HttpPost("admin/test-all/{productId:guid}")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public Task<ActionResult<ApiResponse<List<AlertTestResultDto>>>> TestAllAlertRulesForProduct(
        Guid productId,
        [FromBody] TestPricePointDto testPricePoint)
    {
        try
        {
            // This would require additional service method - placeholder for now
            var response = ApiResponse<List<AlertTestResultDto>>.FailureResult(
                "Feature not yet implemented", 501);
            return Task.FromResult<ActionResult<ApiResponse<List<AlertTestResultDto>>>>(StatusCode(501, response));
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<List<AlertTestResultDto>>.FailureResult(
                $"Failed to test alert rules: {ex.Message}", 500);
            errorResponse.CorrelationId = CorrelationId;
            return Task.FromResult<ActionResult<ApiResponse<List<AlertTestResultDto>>>>(StatusCode(500, errorResponse));
        }
    }

    /// <summary>
    /// Get alert testing statistics (Admin only)
    /// </summary>
    /// <param name="startDate">Statistics start date</param>
    /// <param name="endDate">Statistics end date</param>
    /// <returns>Testing statistics</returns>
    [HttpGet("admin/statistics")]
    [RequirePermission(Permissions.AlertRulesManage)]
    public async Task<ActionResult<ApiResponse<AlertTestingStatsDto>>> GetAlertTestingStatistics(
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        try
        {
            var result = await _alertTestingService.GetAlertTestingStatisticsAsync(startDate, endDate);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert testing statistics");
            return StatusCode(500, ApiResponse<AlertTestingStatsDto>.FailureResult("Internal server error", 500));
        }
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


