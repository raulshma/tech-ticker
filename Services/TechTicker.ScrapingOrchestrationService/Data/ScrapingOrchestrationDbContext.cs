using Microsoft.EntityFrameworkCore;
using TechTicker.ScrapingOrchestrationService.Models;
using TechTicker.Shared.Models;
using System.Text.Json;

namespace TechTicker.ScrapingOrchestrationService.Data
{
    public class ScrapingOrchestrationDbContext : DbContext
    {
        public ScrapingOrchestrationDbContext(DbContextOptions<ScrapingOrchestrationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DomainScrapingProfile> DomainScrapingProfiles { get; set; }
        
        // Read-only access to ProductSellerMappings and ScraperSiteConfigurations
        public DbSet<ProductSellerMapping> ProductSellerMappings { get; set; }
        public DbSet<ScraperSiteConfiguration> ScraperSiteConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DomainScrapingProfile
            modelBuilder.Entity<DomainScrapingProfile>(entity =>
            {
                entity.HasKey(e => e.Domain);
                entity.Property(e => e.Domain).HasMaxLength(255);
                
                // Store UserAgentList as JSON
                entity.Property(e => e.UserAgentList)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

                // Store HeaderProfiles as JSON
                entity.Property(e => e.HeaderProfiles)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, Dictionary<string, string>>());

                entity.HasIndex(e => e.NextAllowedRequestAt);
            });

            // Configure ProductSellerMapping (read-only, already defined in the shared model)
            modelBuilder.Entity<ProductSellerMapping>(entity =>
            {
                entity.HasKey(e => e.MappingId);
                entity.Property(e => e.SellerName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ExactProductUrl).HasMaxLength(2048).IsRequired();
                entity.Property(e => e.ScrapingFrequencyOverride).HasMaxLength(50);
                
                entity.HasIndex(e => e.IsActiveForScraping);
                entity.HasIndex(e => e.NextScrapeAt);

                // Configure relationship with SiteConfiguration
                entity.HasOne(e => e.SiteConfiguration)
                    .WithMany(s => s.ProductSellerMappings)
                    .HasForeignKey(e => e.SiteConfigId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure ScraperSiteConfiguration (read-only, already defined in the shared model)
            modelBuilder.Entity<ScraperSiteConfiguration>(entity =>
            {
                entity.HasKey(e => e.SiteConfigId);
                entity.Property(e => e.SiteDomain).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.SiteDomain).IsUnique();
            });
        }
    }
}
