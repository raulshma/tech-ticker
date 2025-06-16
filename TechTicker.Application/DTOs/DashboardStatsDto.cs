namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Total number of products in the system
    /// </summary>
    public int TotalProducts { get; set; }

    /// <summary>
    /// Total number of categories in the system
    /// </summary>
    public int TotalCategories { get; set; }

    /// <summary>
    /// Number of active product-seller mappings
    /// </summary>
    public int ActiveMappings { get; set; }

    /// <summary>
    /// Number of active alert rules
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// Total number of users in the system (admin only)
    /// </summary>
    public int? TotalUsers { get; set; }
}
