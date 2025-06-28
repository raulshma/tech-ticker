namespace TechTicker.Shared.Constants;

/// <summary>
/// Defines all available permissions in the system
/// </summary>
public static class Permissions
{
    // Product Management
    public const string ProductsCreate = "Products.Create";
    public const string ProductsRead = "Products.Read";
    public const string ProductsUpdate = "Products.Update";
    public const string ProductsDelete = "Products.Delete";
    public const string ProductsManageCategories = "Products.ManageCategories";

    // Price Monitoring
    public const string PriceHistoryRead = "PriceHistory.Read";
    public const string AlertRulesCreate = "AlertRules.Create";
    public const string AlertRulesRead = "AlertRules.Read";
    public const string AlertRulesUpdate = "AlertRules.Update";
    public const string AlertRulesDelete = "AlertRules.Delete";
    public const string AlertRulesManage = "AlertRules.Manage";

    // Scraper Management
    public const string ScrapersConfigure = "Scrapers.Configure";
    public const string ScrapersRun = "Scrapers.Run";
    public const string ScrapersViewLogs = "Scrapers.ViewLogs";
    public const string ScrapersManageSites = "Scrapers.ManageSites";

    // User Management
    public const string UsersCreate = "Users.Create";
    public const string UsersRead = "Users.Read";
    public const string UsersUpdate = "Users.Update";
    public const string UsersDelete = "Users.Delete";
    public const string UsersManageRoles = "Users.ManageRoles";

    // System Administration
    public const string SystemViewLogs = "System.ViewLogs";
    public const string SystemManageSettings = "System.ManageSettings";
    public const string SystemManagePermissions = "System.ManagePermissions";

    /// <summary>
    /// Gets all permissions grouped by category
    /// </summary>
    public static Dictionary<string, string[]> GetPermissionsByCategory()
    {
        return new Dictionary<string, string[]>
        {
            ["Products"] = new[]
            {
                ProductsCreate, ProductsRead, ProductsUpdate, ProductsDelete, ProductsManageCategories
            },
            ["PriceMonitoring"] = new[]
            {
                PriceHistoryRead, AlertRulesCreate, AlertRulesRead, AlertRulesUpdate, AlertRulesDelete, AlertRulesManage
            },
            ["Scrapers"] = new[]
            {
                ScrapersConfigure, ScrapersRun, ScrapersViewLogs, ScrapersManageSites
            },
            ["Users"] = new[]
            {
                UsersCreate, UsersRead, UsersUpdate, UsersDelete, UsersManageRoles
            },
            ["System"] = new[]
            {
                SystemViewLogs, SystemManageSettings, SystemManagePermissions
            }
        };
    }

    /// <summary>
    /// Gets all permissions as a flat list
    /// </summary>
    public static string[] GetAllPermissions()
    {
        return GetPermissionsByCategory().Values.SelectMany(x => x).ToArray();
    }
}
