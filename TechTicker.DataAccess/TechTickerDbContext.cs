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

            // Configure JSON column for PostgreSQL
            entity.Property(e => e.Specifications).HasColumnType("jsonb");

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

        // Configure ApplicationUser entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
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
        }
    }
}
