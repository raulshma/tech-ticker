using Microsoft.EntityFrameworkCore;
using TechTicker.Shared.Models;

namespace TechTicker.ProductSellerMappingService.Data
{
    /// <summary>
    /// Database context for the Product Seller Mapping Service
    /// </summary>
    public class ProductSellerMappingDbContext(DbContextOptions options) : DbContext(options)
    {        /// <summary>
        /// Product seller mappings
        /// </summary>
        public DbSet<ProductSellerMapping> ProductSellerMappings { get; set; }

        /// <summary>
        /// Scraper site configurations
        /// </summary>
        public DbSet<ScraperSiteConfiguration> ScraperSiteConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ProductSellerMapping entity
            modelBuilder.Entity<ProductSellerMapping>(entity =>
            {
                entity.HasKey(e => e.MappingId);

                entity.Property(e => e.MappingId)
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.CanonicalProductId)
                    .IsRequired();

                entity.Property(e => e.SellerName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ExactProductUrl)
                    .IsRequired()
                    .HasMaxLength(2048);

                entity.Property(e => e.IsActiveForScraping)
                    .HasDefaultValue(true);

                entity.Property(e => e.ScrapingFrequencyOverride)
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");                // Create indexes for common queries
                entity.HasIndex(e => e.CanonicalProductId)
                    .HasDatabaseName("IX_ProductSellerMappings_CanonicalProductId");

                entity.HasIndex(e => e.SellerName)
                    .HasDatabaseName("IX_ProductSellerMappings_SellerName");

                entity.HasIndex(e => e.IsActiveForScraping)
                    .HasDatabaseName("IX_ProductSellerMappings_IsActiveForScraping");

                entity.HasIndex(e => e.NextScrapeAt)
                    .HasDatabaseName("IX_ProductSellerMappings_NextScrapeAt");

                entity.HasIndex(e => new { e.CanonicalProductId, e.SellerName })
                    .IsUnique()
                    .HasDatabaseName("IX_ProductSellerMappings_CanonicalProductId_SellerName_Unique");

                // Configure foreign key relationship with ScraperSiteConfiguration
                entity.HasOne(e => e.SiteConfiguration)
                    .WithMany(s => s.ProductSellerMappings)
                    .HasForeignKey(e => e.SiteConfigId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ScraperSiteConfiguration entity
            modelBuilder.Entity<ScraperSiteConfiguration>(entity =>
            {
                entity.HasKey(e => e.SiteConfigId);

                entity.Property(e => e.SiteConfigId)
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.SiteDomain)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.ProductNameSelector)
                    .IsRequired()
                    .HasColumnType("TEXT");

                entity.Property(e => e.PriceSelector)
                    .IsRequired()
                    .HasColumnType("TEXT");

                entity.Property(e => e.StockSelector)
                    .IsRequired()
                    .HasColumnType("TEXT");

                entity.Property(e => e.SellerNameOnPageSelector)
                    .HasColumnType("TEXT");

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                // Create unique index on SiteDomain
                entity.HasIndex(e => e.SiteDomain)
                    .IsUnique()
                    .HasDatabaseName("IX_ScraperSiteConfigurations_SiteDomain_Unique");
            });
        }
    }
}
