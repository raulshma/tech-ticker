using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechTicker.ApiService.Services;
using TechTicker.Application.Configuration;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;

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
});

// Configure messaging
builder.Services.Configure<MessagingConfiguration>(
    builder.Configuration.GetSection(MessagingConfiguration.SectionName));

// Add repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register individual repositories for direct injection
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductSellerMappingRepository, ProductSellerMappingRepository>();
builder.Services.AddScoped<IScraperSiteConfigurationRepository, ScraperSiteConfigurationRepository>();
builder.Services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
builder.Services.AddScoped<IScraperRunLogRepository, ScraperRunLogRepository>();
builder.Services.AddScoped<IProductDiscoveryCandidateRepository, ProductDiscoveryCandidateRepository>();
builder.Services.AddScoped<IDiscoveryApprovalWorkflowRepository, DiscoveryApprovalWorkflowRepository>();

// Register application services
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductSellerMappingService, ProductSellerMappingService>();
builder.Services.AddScoped<IScraperSiteConfigurationService, ScraperSiteConfigurationService>();
builder.Services.AddScoped<IPriceHistoryService, PriceHistoryService>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IScraperRunLogService, ScraperRunLogService>();
builder.Services.AddScoped<IScrapingOrchestrationService, ScrapingOrchestrationService>();

// Configure Product Discovery options with validation
builder.Services.Configure<ProductDiscoveryOptions>(
    builder.Configuration.GetSection(ProductDiscoveryOptions.SectionName));
builder.Services.AddOptions<ProductDiscoveryOptions>()
    .Bind(builder.Configuration.GetSection(ProductDiscoveryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add Product Discovery services
builder.Services.AddScoped<IProductDiscoveryService, ProductDiscoveryService>();
builder.Services.AddScoped<IUrlAnalysisService, UrlAnalysisService>();
builder.Services.AddScoped<ICategoryPredictionService, CategoryPredictionService>();
builder.Services.AddScoped<IProductSimilarityService, ProductSimilarityService>();
builder.Services.AddScoped<IDiscoveryWorkflowService, DiscoveryWorkflowService>();

// Add messaging services
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();



// Add controllers
builder.Services.AddControllers();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TechTicker API", Version = "v1" });
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

// Initialize database and roles
await InitializeDatabaseAsync(app);

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
    string[] roles = { "Admin", "User" };
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
}

static async Task HandleDatabaseMigrationAsync(TechTickerDbContext context)
{
    try
    {
        await context.Database.EnsureCreatedAsync();
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
