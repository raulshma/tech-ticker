using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.ApiService.Services;

/// <summary>
/// Service implementation for Dashboard operations
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IProductSellerMappingService _mappingService;
    private readonly IAlertRuleService _alertRuleService;
    private readonly IUserService _userService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IProductService productService,
        ICategoryService categoryService,
        IProductSellerMappingService mappingService,
        IAlertRuleService alertRuleService,
        IUserService userService,
        ILogger<DashboardService> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _mappingService = mappingService;
        _alertRuleService = alertRuleService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(bool includeUserCount = false)
    {
        try
        {
            _logger.LogInformation("Getting dashboard statistics, includeUserCount: {IncludeUserCount}", includeUserCount);

            var stats = new DashboardStatsDto();

            // Get total products count
            var productsResult = await _productService.GetProductsAsync(pageNumber: 1, pageSize: 1);
            if (productsResult.IsSuccess && productsResult.Data != null)
            {
                stats.TotalProducts = (int)productsResult.Data.Pagination.TotalCount;
            }
            else
            {
                _logger.LogWarning("Failed to get products count: {Error}", productsResult.ErrorMessage);
                stats.TotalProducts = 0;
            }

            // Get total categories count
            var categoriesResult = await _categoryService.GetAllCategoriesAsync();
            if (categoriesResult.IsSuccess && categoriesResult.Data != null)
            {
                stats.TotalCategories = categoriesResult.Data.Count();
            }
            else
            {
                _logger.LogWarning("Failed to get categories count: {Error}", categoriesResult.ErrorMessage);
                stats.TotalCategories = 0;
            }

            // Get active mappings count
            var mappingsResult = await _mappingService.GetActiveMappingsAsync();
            if (mappingsResult.IsSuccess && mappingsResult.Data != null)
            {
                stats.ActiveMappings = mappingsResult.Data.Count();
            }
            else
            {
                _logger.LogWarning("Failed to get active mappings count: {Error}", mappingsResult.ErrorMessage);
                stats.ActiveMappings = 0;
            }

            // Get active alerts count
            var alertsResult = await _alertRuleService.GetAllAlertRulesAsync(pageNumber: 1, pageSize: 1);
            if (alertsResult.IsSuccess && alertsResult.Data != null)
            {
                stats.ActiveAlerts = (int)alertsResult.Data.Pagination.TotalCount;
            }
            else
            {
                _logger.LogWarning("Failed to get alerts count: {Error}", alertsResult.ErrorMessage);
                stats.ActiveAlerts = 0;
            }

            // Get total users count (admin only)
            if (includeUserCount)
            {
                var usersResult = await _userService.GetAllUsersAsync(pageNumber: 1, pageSize: 1);
                if (usersResult.IsSuccess && usersResult.Data != null)
                {
                    stats.TotalUsers = (int)usersResult.Data.Pagination.TotalCount;
                }
                else
                {
                    _logger.LogWarning("Failed to get users count: {Error}", usersResult.ErrorMessage);
                    stats.TotalUsers = 0;
                }
            }

            _logger.LogInformation("Dashboard statistics retrieved successfully: Products={Products}, Categories={Categories}, Mappings={Mappings}, Alerts={Alerts}, Users={Users}",
                stats.TotalProducts, stats.TotalCategories, stats.ActiveMappings, stats.ActiveAlerts, stats.TotalUsers);

            return Result<DashboardStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return Result<DashboardStatsDto>.Failure("An error occurred while retrieving dashboard statistics.", "INTERNAL_ERROR");
        }
    }
}
