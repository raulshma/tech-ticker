using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechTicker.Domain.Entities;

namespace TechTicker.DataAccess;

/// <summary>
/// Main database context for TechTicker application
/// </summary>
public class TechTickerDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public TechTickerDbContext(DbContextOptions<TechTickerDbContext> options) : base(options)
    {
    }

    // Domain entities
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductSellerMapping> ProductSellerMappings { get; set; } = null!;
    public DbSet<ScraperSiteConfiguration> ScraperSiteConfigurations { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistory { get; set; } = null!;
    public DbSet<AlertRule> AlertRules { get; set; } = null!;
    public DbSet<AlertHistory> AlertHistories { get; set; } = null!;
    public DbSet<ScraperRunLog> ScraperRunLogs { get; set; } = null!;
    public DbSet<UserNotificationPreferences> UserNotificationPreferences { get; set; } = null!;
    public DbSet<ProxyConfiguration> ProxyConfigurations { get; set; } = null!;

    // RBAC entities
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<AiConfiguration> AiConfigurations { get; set; } = null!;

    // Test Results Management entities
    public DbSet<SavedTestResult> SavedTestResults { get; set; } = null!;
    public DbSet<SavedTestResultTag> SavedTestResultTags { get; set; } = null!;
    public DbSet<TestExecutionHistory> TestExecutionHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.ModelNumber).HasMaxLength(100);
            entity.Property(e => e.SKU).HasMaxLength(100);
            entity.Property(e => e.CategoryId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.AdditionalImageUrls).HasColumnType("jsonb");
            entity.Property(e => e.OriginalImageUrls).HasColumnType("jsonb");
            entity.Property(e => e.NormalizedSpecifications).HasColumnType("jsonb");
            entity.Property(e => e.UncategorizedSpecifications).HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.SKU).IsUnique().HasFilter("\"SKU\" IS NOT NULL");
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);

            // Relationships
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductSellerMapping entity
        modelBuilder.Entity<ProductSellerMapping>(entity =>
        {
            entity.HasKey(e => e.MappingId);
            entity.Property(e => e.CanonicalProductId).IsRequired();
            entity.Property(e => e.SellerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ExactProductUrl).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.IsActiveForScraping).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ScrapingFrequencyOverride).HasMaxLength(50);
            entity.Property(e => e.LastScrapeStatus).HasMaxLength(50);
            entity.Property(e => e.LastScrapeErrorCode).HasMaxLength(50);
            entity.Property(e => e.ConsecutiveFailureCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes
            entity.HasIndex(e => e.CanonicalProductId);
            entity.HasIndex(e => e.IsActiveForScraping);
            entity.HasIndex(e => e.NextScrapeAt);
            entity.HasIndex(e => e.SiteConfigId);

            // Unique constraint
            entity.HasIndex(e => new { e.CanonicalProductId, e.SellerName, e.ExactProductUrl }).IsUnique();

            // Relationships
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.ProductSellerMappings)
                  .HasForeignKey(e => e.CanonicalProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SiteConfiguration)
                  .WithMany(s => s.ProductSellerMappings)
                  .HasForeignKey(e => e.SiteConfigId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ScraperSiteConfiguration entity
        modelBuilder.Entity<ScraperSiteConfiguration>(entity =>
        {
            entity.HasKey(e => e.SiteConfigId);
            entity.Property(e => e.SiteDomain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProductNameSelector).IsRequired();
            entity.Property(e => e.PriceSelector).IsRequired();
            entity.Property(e => e.StockSelector).IsRequired();
            entity.Property(e => e.IsEnabled).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Configure JSON column for PostgreSQL
            entity.Property(e => e.AdditionalHeaders).HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.SiteDomain).IsUnique();
        });

        // Configure PriceHistory entity
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.PriceHistoryId);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.CanonicalProductId).IsRequired();
            entity.Property(e => e.MappingId).IsRequired();
            entity.Property(e => e.SellerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
            entity.Property(e => e.StockStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SourceUrl).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.ScrapedProductNameOnPage).HasMaxLength(512);

            // Indexes
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.CanonicalProductId);
            entity.HasIndex(e => e.SellerName);
            entity.HasIndex(e => new { e.CanonicalProductId, e.SellerName, e.Timestamp });
            entity.HasIndex(e => new { e.MappingId, e.Timestamp });

            // Relationships
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.CanonicalProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Mapping)
                  .WithMany(m => m.PriceHistory)
                  .HasForeignKey(e => e.MappingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AlertRule entity
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.HasKey(e => e.AlertRuleId);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CanonicalProductId).IsRequired();
            entity.Property(e => e.ConditionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ThresholdValue).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PercentageValue).HasColumnType("decimal(5,2)");
            entity.Property(e => e.SpecificSellerName).HasMaxLength(100);
            entity.Property(e => e.NotificationFrequencyMinutes).IsRequired().HasDefaultValue(1440);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.AlertRules)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                  .WithMany(p => p.AlertRules)
                  .HasForeignKey(e => e.CanonicalProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ScraperRunLog entity
        modelBuilder.Entity<ScraperRunLog>(entity =>
        {
            entity.HasKey(e => e.RunId);
            entity.Property(e => e.MappingId).IsRequired();
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TargetUrl).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ExtractedProductName).HasMaxLength(500);
            entity.Property(e => e.ExtractedPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.ExtractedStockStatus).HasMaxLength(100);
            entity.Property(e => e.ExtractedSellerName).HasMaxLength(200);
            entity.Property(e => e.ExtractedPrimaryImageUrl).HasMaxLength(2048);
            entity.Property(e => e.ImageScrapingError).HasMaxLength(500);
            entity.Property(e => e.ErrorCode).HasMaxLength(100);
            entity.Property(e => e.ErrorCategory).HasMaxLength(50);
            entity.Property(e => e.AttemptNumber).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.RawHtmlSnippet).HasMaxLength(2000);
            entity.Property(e => e.DebugNotes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.AdditionalHeaders).HasColumnType("jsonb");
            entity.Property(e => e.Selectors).HasColumnType("jsonb");

            // Indexes for performance
            entity.HasIndex(e => e.MappingId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ErrorCategory);
            entity.HasIndex(e => e.ParentRunId);
            entity.HasIndex(e => new { e.MappingId, e.StartedAt });
            entity.HasIndex(e => new { e.Status, e.StartedAt });

            // Relationships
            entity.HasOne(e => e.Mapping)
                  .WithMany(m => m.ScraperRunLogs)
                  .HasForeignKey(e => e.MappingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentRun)
                  .WithMany(p => p.RetryAttempts)
                  .HasForeignKey(e => e.ParentRunId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ApplicationUser entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Configure one-to-one relationship with UserNotificationPreferences
            entity.HasOne(e => e.NotificationPreferences)
                  .WithOne(p => p.User)
                  .HasForeignKey<UserNotificationPreferences>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UserNotificationPreferences entity
        modelBuilder.Entity<UserNotificationPreferences>(entity =>
        {
            entity.HasKey(e => e.UserNotificationPreferencesId);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.DiscordWebhookUrl).HasMaxLength(500);
            entity.Property(e => e.IsDiscordNotificationEnabled).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.NotificationProductIds).HasColumnType("jsonb"); // PostgreSQL JSONB
            entity.Property(e => e.CustomBotName).HasMaxLength(100);
            entity.Property(e => e.CustomAvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Create index on UserId for performance
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // Configure RolePermission entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.RolePermissionId);
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.PermissionId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Unique constraint to prevent duplicate role-permission assignments
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

            // Relationships
            entity.HasOne(e => e.Role)
                  .WithMany()
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AiConfiguration entity
        modelBuilder.Entity<AiConfiguration>(entity =>
        {
            entity.HasKey(e => e.AiConfigurationId);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.OpenApiCompatibleUrl).HasMaxLength(500);
            entity.Property(e => e.ApiKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Capabilities).HasColumnType("jsonb");
            entity.Property(e => e.SupportedInputTypes).HasColumnType("jsonb");
            entity.Property(e => e.SupportedOutputTypes).HasColumnType("jsonb");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsDefault).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.UpdatedBy).IsRequired();

            // Indexes
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDefault);
            entity.HasIndex(e => new { e.Provider, e.Model }).IsUnique();
        });

        // Configure SavedTestResult entity
        modelBuilder.Entity<SavedTestResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TestUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProfileHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.TestResultJson).HasColumnType("jsonb");
            entity.Property(e => e.ProfileJson).HasColumnType("jsonb");
            entity.Property(e => e.OptionsJson).HasColumnType("jsonb");
            entity.Property(e => e.MetadataJson).HasColumnType("jsonb");
            entity.Property(e => e.ScreenshotsData).HasColumnType("text");
            entity.Property(e => e.VideoRecording).HasColumnType("text");

            // Indexes
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.TestUrl);
            entity.HasIndex(e => e.ProfileHash);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.SavedAt);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.CreatedBy);

            // Relationships
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SavedTestResultTag entity
        modelBuilder.Entity<SavedTestResultTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tag).IsRequired().HasMaxLength(50);

            // Indexes
            entity.HasIndex(e => e.Tag);
            entity.HasIndex(e => e.SavedTestResultId);
            entity.HasIndex(e => new { e.SavedTestResultId, e.Tag }).IsUnique();

            // Relationships
            entity.HasOne(e => e.SavedTestResult)
                  .WithMany(r => r.Tags)
                  .HasForeignKey(e => e.SavedTestResultId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TestExecutionHistory entity
        modelBuilder.Entity<TestExecutionHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TestUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ProfileHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.SessionName).HasMaxLength(200);
            entity.Property(e => e.BrowserEngine).IsRequired().HasMaxLength(20);
            entity.Property(e => e.DeviceType).IsRequired().HasMaxLength(20);

            // Indexes
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.TestUrl);
            entity.HasIndex(e => e.ProfileHash);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.ExecutedBy);
            entity.HasIndex(e => e.BrowserEngine);
            entity.HasIndex(e => e.DeviceType);

            // Relationships
            entity.HasOne(e => e.SavedTestResult)
                  .WithMany()
                  .HasForeignKey(e => e.SavedTestResultId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ExecutedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ExecutedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Category category)
            {
                if (entry.State == EntityState.Added)
                    category.CreatedAt = DateTimeOffset.UtcNow;
                category.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is Product product)
            {
                if (entry.State == EntityState.Added)
                    product.CreatedAt = DateTimeOffset.UtcNow;
                product.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is ProductSellerMapping mapping)
            {
                if (entry.State == EntityState.Added)
                    mapping.CreatedAt = DateTimeOffset.UtcNow;
                mapping.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is ScraperSiteConfiguration config)
            {
                if (entry.State == EntityState.Added)
                    config.CreatedAt = DateTimeOffset.UtcNow;
                config.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is AlertRule alert)
            {
                if (entry.State == EntityState.Added)
                    alert.CreatedAt = DateTimeOffset.UtcNow;
                alert.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is ApplicationUser user)
            {
                if (entry.State == EntityState.Added)
                    user.CreatedAt = DateTimeOffset.UtcNow;
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is ScraperRunLog runLog)
            {
                if (entry.State == EntityState.Added)
                    runLog.CreatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is Permission permission)
            {
                if (entry.State == EntityState.Added)
                    permission.CreatedAt = DateTimeOffset.UtcNow;
                permission.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is RolePermission rolePermission)
            {
                if (entry.State == EntityState.Added)
                    rolePermission.CreatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.Entity is AiConfiguration aiConfig)
            {
                if (entry.State == EntityState.Added)
                    aiConfig.CreatedAt = DateTime.UtcNow;
                aiConfig.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
