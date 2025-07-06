using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechTicker.ApiService.Services;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Application.Services.AI;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.DataAccess.Seeders;
using TechTicker.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add database context
builder.AddNpgsqlDbContext<TechTickerDbContext>("techticker-db");

// Add RabbitMQ client
builder.AddRabbitMQClient("messaging");

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<TechTickerDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Configure SignalR token handling
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            // If the request is for our SignalR hub...
            if (!string.IsNullOrEmpty(accessToken) && 
                path.StartsWithSegments("/hubs"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Add Authorization with permission-based policies
builder.Services.AddAuthorization(options =>
{
    // Add permission-based policies
    foreach (var permission in TechTicker.Shared.Constants.Permissions.GetAllPermissions())
    {
        options.AddPolicy($"Permission.{permission}", policy =>
            policy.Requirements.Add(new TechTicker.Shared.Authorization.PermissionRequirement(permission)));
    }
});

// Register authorization handler
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, TechTicker.Application.Authorization.PermissionAuthorizationHandler>();

// Configure messaging
builder.Services.Configure<MessagingConfiguration>(
    builder.Configuration.GetSection(MessagingConfiguration.SectionName));

// Configure proxy health monitoring
builder.Services.Configure<ProxyHealthMonitorConfiguration>(
    builder.Configuration.GetSection(ProxyHealthMonitorConfiguration.SectionName));

// Add repositories and services
builder.Services.AddHttpContextAccessor(); // Required for services to access HttpContext for user identification
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAiConfigurationRepository, AiConfigurationRepository>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductSellerMappingService, ProductSellerMappingService>();
builder.Services.AddScoped<IScraperSiteConfigurationService, ScraperSiteConfigurationService>();
builder.Services.AddScoped<IPriceHistoryService, PriceHistoryService>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, TechTicker.Application.Services.RoleService>();
builder.Services.AddScoped<IProxyService, ProxyService>();

// Add HTTP client for proxy testing
builder.Services.AddHttpClient<ProxyService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Add proxy health monitoring service
builder.Services.AddHostedService<ProxyHealthMonitorService>();
builder.Services.AddScoped<IUserNotificationPreferencesService, UserNotificationPreferencesService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IScraperRunLogService, ScraperRunLogService>();
builder.Services.AddScoped<IScrapingOrchestrationService, ScrapingOrchestrationService>();
builder.Services.AddScoped<IAlertTestingService, AlertTestingService>();
builder.Services.AddScoped<IAlertPerformanceMonitoringService, AlertPerformanceMonitoringService>();

// Add AI services
var aiConfigKey = builder.Configuration["AiConfiguration:EncryptionKey"] ?? throw new Exception("Missing AiConfiguration:EncryptionKey in appsettings");
builder.Services.AddScoped<IAiConfigurationService>(sp =>
    new AiConfigurationService(
        sp.GetRequiredService<IAiConfigurationRepository>(),
        sp.GetRequiredService<ILogger<AiConfigurationService>>(),
        sp.GetServices<IAiProvider>(),
        aiConfigKey));
builder.Services.AddScoped<IAiGenerationService, AiGenerationService>();
builder.Services.AddScoped<IAiProvider, GoogleGeminiAiProvider>();
builder.Services.AddHttpClient<GoogleGeminiAiProvider>();

// Add browser automation testing services
builder.Services.AddScoped<IBrowserAutomationTestService, BrowserAutomationTestService>();
builder.Services.AddScoped<ITestResultsManagementService, TestResultsManagementService>();
builder.Services.AddScoped<IPerformanceTracker, PerformanceTracker>();
builder.Services.AddScoped<INetworkMonitor, NetworkMonitor>();

// Add Browser Automation Test Repositories (Phase 4: Database Integration)
builder.Services.AddScoped<ISavedTestResultRepository, SavedTestResultRepository>();
builder.Services.AddScoped<ITestExecutionHistoryRepository, TestExecutionHistoryRepository>();

// Add Alert History Repository
builder.Services.AddScoped<IAlertHistoryRepository, AlertHistoryRepository>();

// Add messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();
builder.Services.AddSingleton<IMessageConsumer, RabbitMQConsumer>();

// Add background services
builder.Services.AddHostedService<PricePointConsumerService>();

// Add controllers
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TechTicker API", Version = "v1" });

    // Configure custom operation IDs to avoid duplicates
    c.CustomOperationIds(apiDesc =>
    {
        // First, try to get the explicit operation name from the route name
        if (!string.IsNullOrEmpty(apiDesc.ActionDescriptor.AttributeRouteInfo?.Name))
        {
            return apiDesc.ActionDescriptor.AttributeRouteInfo.Name;
        }

        // Fallback to action name for cleaner method names
        var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
        return actionName;
    });

    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Alert Processing Service
builder.Services.AddScoped<IAlertProcessingService, AlertProcessingService>();

// Add Product Comparison Services
builder.Services.AddScoped<IProductComparisonService, ProductComparisonService>();
builder.Services.AddScoped<ISpecificationAnalysisEngine, SpecificationAnalysisEngine>();
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddScoped<IRecommendationGenerationService, RecommendationGenerationService>();

// Add Browser Automation Test Services
builder.Services.AddScoped<IBrowserAutomationWebSocketService, BrowserAutomationWebSocketService>();

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map default endpoints for Aspire
app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<TechTicker.ApiService.Hubs.BrowserAutomationTestHub>("/hubs/browser-automation-test");

// Configure static file serving for images
var imageStoragePath = builder.Configuration["ImageStorage:BasePath"] ?? "C:\\TechTicker\\Images\\Products";
if (Directory.Exists(imageStoragePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(imageStoragePath),
        RequestPath = "/images/products"
    });
}

app.UseStaticFiles();

// Initialize database and roles
await InitializeDatabaseAsync(app);

// Configure SignalR broadcasting handler
TechTicker.Application.Services.BrowserAutomationWebSocketService.OnBroadcastRequested += async (groupName, method, data) =>
{
    var hubContext = app.Services.GetService<IHubContext<TechTicker.ApiService.Hubs.BrowserAutomationTestHub>>();
    if (hubContext != null)
    {
        await hubContext.Clients.Group(groupName).SendAsync(method, data);
    }
};

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TechTickerDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Apply pending migrations or ensure database is created
    await HandleDatabaseMigrationAsync(context);

    // Create roles if they don't exist
    string[] roles = { "Admin", "User", "Moderator" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
        }
    }

    // Create default admin user if it doesn't exist
    var adminEmail = "admin@techticker.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Create default moderator user if it doesn't exist
    var moderatorEmail = "moderator@techticker.com";
    var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);
    if (moderatorUser == null)
    {
        moderatorUser = new ApplicationUser
        {
            UserName = moderatorEmail,
            Email = moderatorEmail,
            FirstName = "Moderator",
            LastName = "User",
            IsActive = true
        };

        var result = await userManager.CreateAsync(moderatorUser, "Moderator123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(moderatorUser, "Moderator");
        }
    }

    // Create default regular user if it doesn't exist
    var userEmail = "user@techticker.com";
    var regularUser = await userManager.FindByEmailAsync(userEmail);
    if (regularUser == null)
    {
        regularUser = new ApplicationUser
        {
            UserName = userEmail,
            Email = userEmail,
            FirstName = "Regular",
            LastName = "User",
            IsActive = true
        };

        var result = await userManager.CreateAsync(regularUser, "User123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(regularUser, "User");
        }
    }

    // Seed permissions and role-permission mappings
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await PermissionSeeder.SeedAsync(context, roleManager, logger);
}

static async Task HandleDatabaseMigrationAsync(TechTickerDbContext context)
{
    try
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();
        Console.WriteLine("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration error: {ex.Message}");

        // If migration fails due to existing tables, try to handle gracefully
        if (ex.Message.Contains("already exists"))
        {
            Console.WriteLine("Tables already exist. Attempting to continue...");
            // In a real scenario, you might want to check and mark migrations as applied
        }
        else
        {
            throw;
        }
    }
}

// Make Program class accessible for testing
public partial class Program { }
