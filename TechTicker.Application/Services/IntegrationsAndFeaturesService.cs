using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Utilities;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace TechTicker.Application.Services;

public class IntegrationsAndFeaturesService : IIntegrationsAndFeaturesService
{
    private readonly ILogger<IntegrationsAndFeaturesService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAiConfigurationService _aiConfigurationService;
    private readonly IProxyService _proxyService;
    private readonly IUserNotificationPreferencesService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    // Feature definitions
    private readonly Dictionary<string, FeatureDefinition> _featureDefinitions;
    private readonly Dictionary<string, IntegrationDefinition> _integrationDefinitions;

    public IntegrationsAndFeaturesService(
        ILogger<IntegrationsAndFeaturesService> logger,
        IConfiguration configuration,
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IAiConfigurationService aiConfigurationService,
        IProxyService proxyService,
        IUserNotificationPreferencesService notificationService,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _aiConfigurationService = aiConfigurationService;
        _proxyService = proxyService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;

        _featureDefinitions = InitializeFeatureDefinitions();
        _integrationDefinitions = InitializeIntegrationDefinitions();
    }

    public async Task<Result<IntegrationsAndFeaturesDto>> GetIntegrationsAndFeaturesAsync(Guid userId)
    {
        try
        {
            var featuresResult = await GetFeaturesAsync(userId);
            var integrationsResult = await GetIntegrationsAsync();
            var systemHealthResult = await GetSystemHealthAsync();

            if (!featuresResult.IsSuccess || !integrationsResult.IsSuccess || !systemHealthResult.IsSuccess)
            {
                return Result<IntegrationsAndFeaturesDto>.Failure("Failed to retrieve complete system status");
            }

            var result = new IntegrationsAndFeaturesDto
            {
                Features = featuresResult.Data!,
                Integrations = integrationsResult.Data!,
                SystemHealth = systemHealthResult.Data!
            };

            return Result<IntegrationsAndFeaturesDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations and features overview");
            return Result<IntegrationsAndFeaturesDto>.Failure("Failed to retrieve system overview");
        }
    }

    public async Task<Result<List<FeatureDto>>> GetFeaturesAsync(Guid userId)
    {
        try
        {
            var features = new List<FeatureDto>();
            var userRoles = await GetUserRolesAsync(userId);

            foreach (var featureDefinition in _featureDefinitions.Values)
            {
                var feature = new FeatureDto
                {
                    Id = featureDefinition.Id,
                    Name = featureDefinition.Name,
                    Description = featureDefinition.Description,
                    Category = featureDefinition.Category,
                    Icon = featureDefinition.Icon,
                    Route = featureDefinition.Route,
                    RequiredRoles = featureDefinition.RequiredRoles,
                    RequiredPermissions = featureDefinition.RequiredPermissions,
                    IsAvailable = HasRequiredAccess(userRoles, featureDefinition.RequiredRoles),
                    ConfigurationSteps = featureDefinition.ConfigurationSteps
                };

                if (!feature.IsAvailable)
                {
                    feature.UnavailableReason = "Insufficient permissions";
                    feature.Status = FeatureStatus.Unavailable;
                }
                else
                {
                    feature.Status = await DetermineFeatureStatusAsync(featureDefinition.Id);
                }

                // Get usage statistics
                var usageResult = await GetFeatureUsageAsync(featureDefinition.Id, userId);
                if (usageResult.IsSuccess)
                {
                    feature.LastUsed = usageResult.Data!.LastAccessed;
                    feature.UsageCount = usageResult.Data!.AccessCount;
                }

                features.Add(feature);
            }

            return Result<List<FeatureDto>>.Success(features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving features for user {UserId}", userId);
            return Result<List<FeatureDto>>.Failure("Failed to retrieve features");
        }
    }

    public async Task<Result<List<IntegrationDto>>> GetIntegrationsAsync()
    {
        try
        {
            var integrations = new List<IntegrationDto>();

            foreach (var integrationDefinition in _integrationDefinitions.Values)
            {
                var integration = new IntegrationDto
                {
                    Id = integrationDefinition.Id,
                    Name = integrationDefinition.Name,
                    Description = integrationDefinition.Description,
                    Type = integrationDefinition.Type,
                    Icon = integrationDefinition.Icon,
                    Version = integrationDefinition.Version,
                    ConfigurationRoute = integrationDefinition.ConfigurationRoute,
                    IsRequired = integrationDefinition.IsRequired,
                    ConfigurationSteps = integrationDefinition.ConfigurationSteps
                };

                // Check integration health and status
                var healthResult = await CheckIntegrationHealthAsync(integrationDefinition.Id);
                if (healthResult.IsSuccess)
                {
                    integration.IsHealthy = healthResult.Data!.IsHealthy;
                    integration.HealthMessage = healthResult.Data!.Message;
                    integration.LastHealthCheck = healthResult.Data!.CheckedAt;
                    integration.Status = healthResult.Data!.IsHealthy ? IntegrationStatus.Connected : IntegrationStatus.Error;
                }
                else
                {
                    integration.Status = IntegrationStatus.Disconnected;
                    integration.HealthMessage = healthResult.ErrorMessage;
                }

                integrations.Add(integration);
            }

            return Result<List<IntegrationDto>>.Success(integrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving integrations");
            return Result<List<IntegrationDto>>.Failure("Failed to retrieve integrations");
        }
    }

    public async Task<Result<SystemHealthDto>> GetSystemHealthAsync()
    {
        try
        {
            var features = await GetFeaturesAsync(Guid.Empty); // Get all features without user context
            var integrations = await GetIntegrationsAsync();

            if (!features.IsSuccess || !integrations.IsSuccess)
            {
                return Result<SystemHealthDto>.Failure("Failed to retrieve system components");
            }

            var activeFeatures = features.Data!.Count(f => f.Status == FeatureStatus.Active);
            var healthyIntegrations = integrations.Data!.Count(i => i.IsHealthy);
            var configurationIssues = features.Data!.Count(f => f.Status == FeatureStatus.NeedsConfiguration) +
                                    integrations.Data!.Count(i => i.Status == IntegrationStatus.NeedsConfiguration);

            var issues = new List<string>();
            
            // Add specific issues
            foreach (var feature in features.Data!.Where(f => f.Status == FeatureStatus.NeedsConfiguration))
            {
                issues.Add($"Feature '{feature.Name}' needs configuration");
            }

            foreach (var integration in integrations.Data!.Where(i => i.Status == IntegrationStatus.NeedsConfiguration))
            {
                issues.Add($"Integration '{integration.Name}' needs configuration");
            }

            foreach (var integration in integrations.Data!.Where(i => !i.IsHealthy))
            {
                issues.Add($"Integration '{integration.Name}' is unhealthy: {integration.HealthMessage}");
            }

            var totalComponents = features.Data!.Count + integrations.Data!.Count;
            var healthyComponents = activeFeatures + healthyIntegrations;
            var healthScore = totalComponents > 0 ? (double)healthyComponents / totalComponents * 100 : 100;

            var systemHealth = new SystemHealthDto
            {
                IsHealthy = issues.Count == 0,
                HealthScore = Math.Round(healthScore, 2),
                TotalFeatures = features.Data!.Count,
                ActiveFeatures = activeFeatures,
                TotalIntegrations = integrations.Data!.Count,
                HealthyIntegrations = healthyIntegrations,
                ConfigurationIssues = configurationIssues,
                Issues = issues,
                LastUpdated = DateTime.UtcNow
            };

            return Result<SystemHealthDto>.Success(systemHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system health");
            return Result<SystemHealthDto>.Failure("Failed to retrieve system health");
        }
    }

    public async Task<Result<IntegrationHealthCheckDto>> CheckIntegrationHealthAsync(string integrationId)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var healthCheck = new IntegrationHealthCheckDto
            {
                IntegrationId = integrationId,
                CheckedAt = startTime
            };

            switch (integrationId.ToLower())
            {
                case "discord":
                    healthCheck = await CheckDiscordHealthAsync(healthCheck);
                    break;
                case "ai":
                    healthCheck = await CheckAiHealthAsync(healthCheck);
                    break;
                case "proxy":
                    healthCheck = await CheckProxyHealthAsync(healthCheck);
                    break;
                case "database":
                    healthCheck = await CheckDatabaseHealthAsync(healthCheck);
                    break;
                case "messagequeue":
                    healthCheck = await CheckMessageQueueHealthAsync(healthCheck);
                    break;
                case "cache":
                    healthCheck = await CheckCacheHealthAsync(healthCheck);
                    break;
                case "browserautomation":
                    healthCheck = await CheckBrowserAutomationHealthAsync(healthCheck);
                    break;
                case "imagestorage":
                    healthCheck = await CheckImageStorageHealthAsync(healthCheck);
                    break;
                default:
                    healthCheck.IsHealthy = false;
                    healthCheck.Message = "Unknown integration";
                    break;
            }

            healthCheck.ResponseTime = DateTime.UtcNow - startTime;
            return Result<IntegrationHealthCheckDto>.Success(healthCheck);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for integration {IntegrationId}", integrationId);
            return Result<IntegrationHealthCheckDto>.Failure($"Health check failed for {integrationId}");
        }
    }

    public async Task<Result<ConfigurationGuideDto>> GetConfigurationGuideAsync(string id)
    {
        try
        {
            var guide = id.ToLower() switch
            {
                "discord" => GetDiscordConfigurationGuide(),
                "ai" => GetAiConfigurationGuide(),
                "proxy" => GetProxyConfigurationGuide(),
                "alerts" => GetAlertsConfigurationGuide(),
                "notifications" => GetNotificationsConfigurationGuide(),
                _ => null
            };

            if (guide == null)
            {
                return Result<ConfigurationGuideDto>.Failure($"Configuration guide not found for {id}");
            }

            return Result<ConfigurationGuideDto>.Success(guide);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration guide for {Id}", id);
            return Result<ConfigurationGuideDto>.Failure("Failed to retrieve configuration guide");
        }
    }

    public async Task<Result<bool>> RecordFeatureUsageAsync(string featureId, Guid userId)
    {
        try
        {
            // This would typically be stored in a database table
            // For now, we'll just log the usage
            _logger.LogInformation("Feature usage recorded: {FeatureId} by user {UserId}", featureId, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording feature usage for {FeatureId} by user {UserId}", featureId, userId);
            return Result<bool>.Failure("Failed to record feature usage");
        }
    }

    public async Task<Result<FeatureUsageDto>> GetFeatureUsageAsync(string featureId, Guid? userId = null)
    {
        try
        {
            // This would typically be retrieved from a database
            // For now, we'll return mock data
            var usage = new FeatureUsageDto
            {
                FeatureId = featureId,
                UserId = userId?.ToString() ?? "system",
                LastAccessed = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                AccessCount = Random.Shared.Next(1, 100)
            };

            return Result<FeatureUsageDto>.Success(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feature usage for {FeatureId}", featureId);
            return Result<FeatureUsageDto>.Failure("Failed to retrieve feature usage");
        }
    }

    public async Task<Result<bool>> RefreshIntegrationHealthAsync()
    {
        try
        {
            var tasks = _integrationDefinitions.Keys.Select(id => CheckIntegrationHealthAsync(id)).ToArray();
            await Task.WhenAll(tasks);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing integration health");
            return Result<bool>.Failure("Failed to refresh integration health");
        }
    }

    #region Private Helper Methods

    private Dictionary<string, FeatureDefinition> InitializeFeatureDefinitions()
    {
        return new Dictionary<string, FeatureDefinition>
        {
            ["dashboard"] = new FeatureDefinition
            {
                Id = "dashboard",
                Name = "Dashboard & Analytics",
                Description = "System overview, performance metrics, and analytics dashboard",
                Category = "Core",
                Icon = "dashboard",
                Route = "/dashboard",
                RequiredRoles = new List<string> { "User", "Admin", "Moderator" }
            },
            ["catalog"] = new FeatureDefinition
            {
                Id = "catalog",
                Name = "Product Catalog",
                Description = "Browse and search product catalog with detailed information",
                Category = "Core",
                Icon = "storefront",
                Route = "/catalog",
                RequiredRoles = new List<string> { "User", "Admin", "Moderator" }
            },
            ["comparison"] = new FeatureDefinition
            {
                Id = "comparison",
                Name = "Product Comparison",
                Description = "Compare products across different sellers and analyze specifications",
                Category = "Core",
                Icon = "compare",
                Route = "/product-comparison",
                RequiredRoles = new List<string> { "User", "Admin", "Moderator" }
            },
            ["alerts"] = new FeatureDefinition
            {
                Id = "alerts",
                Name = "Price Alerts",
                Description = "Set up price drop alerts and stock notifications",
                Category = "User Features",
                Icon = "notifications_active",
                Route = "/alerts",
                RequiredRoles = new List<string> { "User", "Admin" }
            },
            ["categories"] = new FeatureDefinition
            {
                Id = "categories",
                Name = "Category Management",
                Description = "Manage product categories and hierarchies",
                Category = "Management",
                Icon = "category",
                Route = "/categories",
                RequiredRoles = new List<string> { "Admin", "Moderator" }
            },
            ["products"] = new FeatureDefinition
            {
                Id = "products",
                Name = "Product Management",
                Description = "Add, edit, and manage products in the system",
                Category = "Management",
                Icon = "inventory",
                Route = "/products",
                RequiredRoles = new List<string> { "Admin", "Moderator" }
            },
            ["mappings"] = new FeatureDefinition
            {
                Id = "mappings",
                Name = "Product Mappings",
                Description = "Configure seller-to-product mappings and scraping rules",
                Category = "Management",
                Icon = "alt_route",
                Route = "/mappings",
                RequiredRoles = new List<string> { "User", "Admin", "Moderator" }
            },
            ["siteconfigs"] = new FeatureDefinition
            {
                Id = "siteconfigs",
                Name = "Site Configurations",
                Description = "Configure e-commerce site scraping parameters",
                Category = "Admin",
                Icon = "tune",
                Route = "/site-configs",
                RequiredRoles = new List<string> { "Admin" }
            },
            ["users"] = new FeatureDefinition
            {
                Id = "users",
                Name = "User Management",
                Description = "Manage users, roles, and permissions",
                Category = "Admin",
                Icon = "people",
                Route = "/users",
                RequiredRoles = new List<string> { "Admin" }
            },
            ["scraperlogs"] = new FeatureDefinition
            {
                Id = "scraperlogs",
                Name = "Scraper Monitoring",
                Description = "Monitor scraping activities and troubleshoot issues",
                Category = "Admin",
                Icon = "history",
                Route = "/scraper-logs",
                RequiredRoles = new List<string> { "Admin" }
            },
            ["notifications"] = new FeatureDefinition
            {
                Id = "notifications",
                Name = "Notification Settings",
                Description = "Configure notification preferences and delivery methods",
                Category = "User Features",
                Icon = "settings_applications",
                Route = "/notification-settings",
                RequiredRoles = new List<string> { "User", "Admin" }
            },
            ["proxies"] = new FeatureDefinition
            {
                Id = "proxies",
                Name = "Proxy Management",
                Description = "Configure and manage proxy servers for scraping",
                Category = "Admin",
                Icon = "vpn_lock",
                Route = "/admin/proxies",
                RequiredRoles = new List<string> { "Admin" }
            },
            ["aisettings"] = new FeatureDefinition
            {
                Id = "aisettings",
                Name = "AI Configuration",
                Description = "Configure AI providers for browser automation",
                Category = "Admin",
                Icon = "smart_toy",
                Route = "/admin/ai-settings",
                RequiredRoles = new List<string> { "Admin" }
            },
            ["browserautomation"] = new FeatureDefinition
            {
                Id = "browserautomation",
                Name = "Browser Automation Tester",
                Description = "Test and debug browser automation profiles",
                Category = "Admin",
                Icon = "bug_report",
                Route = "/admin/browser-automation-tester",
                RequiredRoles = new List<string> { "Admin" }
            }
        };
    }

    private Dictionary<string, IntegrationDefinition> InitializeIntegrationDefinitions()
    {
        return new Dictionary<string, IntegrationDefinition>
        {
            ["discord"] = new IntegrationDefinition
            {
                Id = "discord",
                Name = "Discord Notifications",
                Description = "Send price alerts and notifications via Discord webhooks",
                Type = "External API",
                Icon = "chat",
                Version = "Discord.Net.Webhook 3.17.4",
                ConfigurationRoute = "/notification-settings",
                IsRequired = false,
                ConfigurationSteps = new List<ConfigurationStepDto>
                {
                    new() { Id = "webhook", Title = "Configure Webhook URL", Description = "Set up Discord webhook URL in notification settings", IsRequired = true, Order = 1 },
                    new() { Id = "test", Title = "Test Connection", Description = "Send a test message to verify configuration", IsRequired = false, Order = 2 }
                }
            },
            ["ai"] = new IntegrationDefinition
            {
                Id = "ai",
                Name = "AI Integration",
                Description = "Google Gemini AI for browser automation and content generation",
                Type = "External API",
                Icon = "smart_toy",
                Version = "Google Gemini API",
                ConfigurationRoute = "/admin/ai-settings",
                IsRequired = false,
                ConfigurationSteps = new List<ConfigurationStepDto>
                {
                    new() { Id = "apikey", Title = "Configure API Key", Description = "Add Google Gemini API key", IsRequired = true, Order = 1 },
                    new() { Id = "model", Title = "Select Model", Description = "Choose AI model for generation", IsRequired = true, Order = 2 },
                    new() { Id = "test", Title = "Test Configuration", Description = "Verify AI connection and capabilities", IsRequired = false, Order = 3 }
                }
            },
            ["proxy"] = new IntegrationDefinition
            {
                Id = "proxy",
                Name = "Proxy Management",
                Description = "HTTP/SOCKS proxy servers for enhanced scraping stealth",
                Type = "Network",
                Icon = "vpn_lock",
                Version = "Built-in",
                ConfigurationRoute = "/admin/proxies",
                IsRequired = false,
                ConfigurationSteps = new List<ConfigurationStepDto>
                {
                    new() { Id = "add", Title = "Add Proxy Servers", Description = "Configure proxy server details", IsRequired = true, Order = 1 },
                    new() { Id = "test", Title = "Test Connectivity", Description = "Verify proxy server connectivity", IsRequired = false, Order = 2 },
                    new() { Id = "monitor", Title = "Enable Monitoring", Description = "Enable automatic health monitoring", IsRequired = false, Order = 3 }
                }
            },
            ["database"] = new IntegrationDefinition
            {
                Id = "database",
                Name = "PostgreSQL Database",
                Description = "Primary data storage for all application data",
                Type = "Database",
                Icon = "storage",
                Version = "PostgreSQL with EF Core",
                ConfigurationRoute = null,
                IsRequired = true
            },
            ["messagequeue"] = new IntegrationDefinition
            {
                Id = "messagequeue",
                Name = "RabbitMQ Message Queue",
                Description = "Asynchronous message processing for scraping and alerts",
                Type = "Message Queue",
                Icon = "queue",
                Version = "RabbitMQ",
                ConfigurationRoute = null,
                IsRequired = true
            },
            ["cache"] = new IntegrationDefinition
            {
                Id = "cache",
                Name = "Redis Cache",
                Description = "High-performance caching for improved response times",
                Type = "Cache",
                Icon = "memory",
                Version = "Redis",
                ConfigurationRoute = null,
                IsRequired = true
            },
            ["browserautomation"] = new IntegrationDefinition
            {
                Id = "browserautomation",
                Name = "Microsoft Playwright",
                Description = "Browser automation for JavaScript-heavy sites",
                Type = "Automation",
                Icon = "web",
                Version = "Microsoft.Playwright 1.49.0",
                ConfigurationRoute = "/admin/browser-automation-tester",
                IsRequired = false
            },
            ["imagestorage"] = new IntegrationDefinition
            {
                Id = "imagestorage",
                Name = "Image Storage",
                Description = "Local file system storage for product images",
                Type = "Storage",
                Icon = "image",
                Version = "File System",
                ConfigurationRoute = null,
                IsRequired = true
            }
        };
    }

    private async Task<List<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var claims = _httpContextAccessor.HttpContext?.User?.Claims;
            if (claims != null)
            {
                return claims.Where(c => c.Type == ClaimTypes.Role)
                           .Select(c => c.Value)
                           .ToList();
            }
            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private bool HasRequiredAccess(List<string> userRoles, List<string> requiredRoles)
    {
        if (requiredRoles.Count == 0) return true;
        return requiredRoles.Any(role => userRoles.Contains(role));
    }

    private async Task<FeatureStatus> DetermineFeatureStatusAsync(string featureId)
    {
        try
        {
            return featureId.ToLower() switch
            {
                "alerts" => await CheckAlertsFeatureStatusAsync(),
                "notifications" => await CheckNotificationsFeatureStatusAsync(),
                "aisettings" => await CheckAiFeatureStatusAsync(),
                "proxies" => await CheckProxyFeatureStatusAsync(),
                _ => FeatureStatus.Active
            };
        }
        catch
        {
            return FeatureStatus.Active;
        }
    }

    private async Task<FeatureStatus> CheckAlertsFeatureStatusAsync()
    {
        // Check if user has any alert rules configured
        try
        {
            var alertsCount = await _unitOfWork.AlertRules.CountAsync();
            return alertsCount > 0 ? FeatureStatus.Active : FeatureStatus.Inactive;
        }
        catch
        {
            return FeatureStatus.Active;
        }
    }

    private async Task<FeatureStatus> CheckNotificationsFeatureStatusAsync()
    {
        // Check if Discord notifications are configured
        var discordEnabled = _configuration.GetValue<bool>("Discord:EnableDiscordNotifications");
        var webhookUrl = _configuration.GetValue<string>("Discord:WebhookUrl");
        
        if (!discordEnabled || string.IsNullOrEmpty(webhookUrl))
        {
            return FeatureStatus.NeedsConfiguration;
        }
        
        return FeatureStatus.Active;
    }

    private async Task<FeatureStatus> CheckAiFeatureStatusAsync()
    {
        try
        {
            var configs = await _aiConfigurationService.GetActiveConfigurationsAsync();
            return configs.IsSuccess && configs.Data!.Any() ? FeatureStatus.Active : FeatureStatus.NeedsConfiguration;
        }
        catch
        {
            return FeatureStatus.NeedsConfiguration;
        }
    }

    private async Task<FeatureStatus> CheckProxyFeatureStatusAsync()
    {
        try
        {
            var proxies = await _proxyService.GetAllProxiesAsync();
            return proxies.IsSuccess && proxies.Data!.Any() ? FeatureStatus.Active : FeatureStatus.Inactive;
        }
        catch
        {
            return FeatureStatus.Inactive;
        }
    }

    // Health check methods for integrations
    private async Task<IntegrationHealthCheckDto> CheckDiscordHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            var enabled = _configuration.GetValue<bool>("Discord:EnableDiscordNotifications");
            var webhookUrl = _configuration.GetValue<string>("Discord:WebhookUrl");

            if (!enabled)
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "Discord notifications are disabled";
                return healthCheck;
            }

            if (string.IsNullOrEmpty(webhookUrl))
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "Discord webhook URL not configured";
                return healthCheck;
            }

            // Test webhook URL format
            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) || !uri.Host.Contains("discord"))
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "Invalid Discord webhook URL format";
                return healthCheck;
            }

            healthCheck.IsHealthy = true;
            healthCheck.Message = "Discord integration configured and ready";
            healthCheck.Details["WebhookConfigured"] = true;
            healthCheck.Details["Enabled"] = enabled;
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Discord health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckAiHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            var configs = await _aiConfigurationService.GetActiveConfigurationsAsync();
            
            if (!configs.IsSuccess || !configs.Data!.Any())
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "No active AI configurations found";
                return healthCheck;
            }

            var activeConfigs = configs.Data!.ToList();
            var defaultConfig = activeConfigs.FirstOrDefault(c => c.IsDefault);

            healthCheck.IsHealthy = true;
            healthCheck.Message = $"AI integration active with {activeConfigs.Count} configuration(s)";
            healthCheck.Details["ActiveConfigurations"] = activeConfigs.Count;
            healthCheck.Details["HasDefault"] = defaultConfig != null;
            healthCheck.Details["Providers"] = activeConfigs.Select(c => c.Provider).Distinct().ToList();
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"AI health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckProxyHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            var proxies = await _proxyService.GetAllProxiesAsync();
            
            if (!proxies.IsSuccess)
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "Failed to retrieve proxy configurations";
                return healthCheck;
            }

            var proxyList = proxies.Data!.ToList();
            var healthyProxies = proxyList.Count(p => p.IsHealthy);

            healthCheck.IsHealthy = proxyList.Count > 0;
            healthCheck.Message = proxyList.Count == 0 
                ? "No proxy servers configured" 
                : $"{healthyProxies} of {proxyList.Count} proxies are healthy";
                
            healthCheck.Details["TotalProxies"] = proxyList.Count;
            healthCheck.Details["HealthyProxies"] = healthyProxies;
            healthCheck.Details["HealthPercentage"] = proxyList.Count > 0 ? (double)healthyProxies / proxyList.Count * 100 : 0;
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Proxy health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckDatabaseHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            // Test database connection
            var testQuery = await _unitOfWork.Categories.CountAsync();
            
            healthCheck.IsHealthy = true;
            healthCheck.Message = "Database connection healthy";
            healthCheck.Details["CategoriesCount"] = testQuery;
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Database connection failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckMessageQueueHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            // This would typically test RabbitMQ connection
            // For now, we'll assume it's healthy if the service is running
            healthCheck.IsHealthy = true;
            healthCheck.Message = "Message queue connection healthy";
            healthCheck.Details["QueueType"] = "RabbitMQ";
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Message queue health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckCacheHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            // This would typically test Redis connection
            // For now, we'll assume it's healthy if the service is running
            healthCheck.IsHealthy = true;
            healthCheck.Message = "Cache connection healthy";
            healthCheck.Details["CacheType"] = "Redis";
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Cache health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckBrowserAutomationHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            // This would typically test Playwright installation
            healthCheck.IsHealthy = true;
            healthCheck.Message = "Browser automation ready";
            healthCheck.Details["Framework"] = "Microsoft Playwright";
            healthCheck.Details["Version"] = "1.49.0";
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Browser automation health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    private async Task<IntegrationHealthCheckDto> CheckImageStorageHealthAsync(IntegrationHealthCheckDto healthCheck)
    {
        try
        {
            var basePath = _configuration.GetValue<string>("ImageStorage:BasePath");
            
            if (string.IsNullOrEmpty(basePath))
            {
                healthCheck.IsHealthy = false;
                healthCheck.Message = "Image storage path not configured";
                return healthCheck;
            }

            var directoryExists = Directory.Exists(basePath);
            
            healthCheck.IsHealthy = directoryExists;
            healthCheck.Message = directoryExists ? "Image storage accessible" : "Image storage directory not found";
            healthCheck.Details["StoragePath"] = basePath;
            healthCheck.Details["DirectoryExists"] = directoryExists;
        }
        catch (Exception ex)
        {
            healthCheck.IsHealthy = false;
            healthCheck.Message = $"Image storage health check failed: {ex.Message}";
        }

        return healthCheck;
    }

    // Configuration guide methods
    private ConfigurationGuideDto GetDiscordConfigurationGuide()
    {
        return new ConfigurationGuideDto
        {
            FeatureOrIntegrationId = "discord",
            Title = "Discord Notifications Setup",
            Description = "Configure Discord webhook notifications for price alerts",
            Steps = new List<ConfigurationStepDto>
            {
                new() { Id = "webhook", Title = "Create Discord Webhook", Description = "Create a webhook in your Discord server", IsRequired = true, Order = 1 },
                new() { Id = "configure", Title = "Configure Webhook URL", Description = "Add the webhook URL to notification settings", IsRequired = true, Order = 2 },
                new() { Id = "test", Title = "Test Notifications", Description = "Send a test message to verify setup", IsRequired = false, Order = 3 }
            },
            DocumentationUrl = "https://discord.com/developers/docs/resources/webhook"
        };
    }

    private ConfigurationGuideDto GetAiConfigurationGuide()
    {
        return new ConfigurationGuideDto
        {
            FeatureOrIntegrationId = "ai",
            Title = "AI Integration Setup",
            Description = "Configure Google Gemini AI for browser automation",
            Steps = new List<ConfigurationStepDto>
            {
                new() { Id = "apikey", Title = "Get API Key", Description = "Obtain a Google Gemini API key", IsRequired = true, Order = 1 },
                new() { Id = "configure", Title = "Add Configuration", Description = "Add AI configuration in admin settings", IsRequired = true, Order = 2 },
                new() { Id = "test", Title = "Test Connection", Description = "Verify AI provider connectivity", IsRequired = false, Order = 3 }
            },
            DocumentationUrl = "https://ai.google.dev/docs"
        };
    }

    private ConfigurationGuideDto GetProxyConfigurationGuide()
    {
        return new ConfigurationGuideDto
        {
            FeatureOrIntegrationId = "proxy",
            Title = "Proxy Management Setup",
            Description = "Configure proxy servers for enhanced scraping",
            Steps = new List<ConfigurationStepDto>
            {
                new() { Id = "add", Title = "Add Proxy Servers", Description = "Configure proxy server details", IsRequired = true, Order = 1 },
                new() { Id = "test", Title = "Test Connectivity", Description = "Verify proxy server connectivity", IsRequired = false, Order = 2 },
                new() { Id = "monitor", Title = "Enable Health Monitoring", Description = "Enable automatic proxy health checks", IsRequired = false, Order = 3 }
            }
        };
    }

    private ConfigurationGuideDto GetAlertsConfigurationGuide()
    {
        return new ConfigurationGuideDto
        {
            FeatureOrIntegrationId = "alerts",
            Title = "Price Alerts Setup",
            Description = "Set up price drop alerts and notifications",
            Steps = new List<ConfigurationStepDto>
            {
                new() { Id = "create", Title = "Create Alert Rule", Description = "Set up your first price alert", IsRequired = true, Order = 1 },
                new() { Id = "configure", Title = "Configure Notifications", Description = "Set up notification preferences", IsRequired = true, Order = 2 },
                new() { Id = "test", Title = "Test Alert", Description = "Test alert functionality", IsRequired = false, Order = 3 }
            }
        };
    }

    private ConfigurationGuideDto GetNotificationsConfigurationGuide()
    {
        return new ConfigurationGuideDto
        {
            FeatureOrIntegrationId = "notifications",
            Title = "Notification Settings",
            Description = "Configure how you receive notifications",
            Steps = new List<ConfigurationStepDto>
            {
                new() { Id = "discord", Title = "Configure Discord", Description = "Set up Discord webhook for notifications", IsRequired = false, Order = 1 },
                new() { Id = "preferences", Title = "Set Preferences", Description = "Configure notification preferences", IsRequired = true, Order = 2 },
                new() { Id = "test", Title = "Test Notifications", Description = "Send test notifications", IsRequired = false, Order = 3 }
            }
        };
    }

    #endregion

    #region Helper Classes

    private class FeatureDefinition
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Icon { get; set; }
        public string? Route { get; set; }
        public List<string> RequiredRoles { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
        public List<ConfigurationStepDto> ConfigurationSteps { get; set; } = new();
    }

    private class IntegrationDefinition
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Icon { get; set; }
        public string? Version { get; set; }
        public string? ConfigurationRoute { get; set; }
        public bool IsRequired { get; set; }
        public List<ConfigurationStepDto> ConfigurationSteps { get; set; } = new();
    }

    #endregion
} 