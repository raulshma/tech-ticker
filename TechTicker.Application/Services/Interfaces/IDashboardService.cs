using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service interface for Dashboard operations
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    /// <param name="includeUserCount">Whether to include user count (admin only)</param>
    /// <returns>Dashboard statistics</returns>
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(bool includeUserCount = false);
}
