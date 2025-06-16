using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Common;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// API controller for Dashboard operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <returns>Dashboard statistics including product count, category count, active mappings, and active alerts. User count is included for admin users.</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        // Check if user is admin to include user count
        var isAdmin = User.IsInRole("Admin");
        
        var result = await _dashboardService.GetDashboardStatsAsync(includeUserCount: isAdmin);
        return HandleResult(result);
    }
}
