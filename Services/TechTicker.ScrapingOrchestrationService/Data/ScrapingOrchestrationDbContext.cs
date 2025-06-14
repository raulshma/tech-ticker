using Microsoft.EntityFrameworkCore;
using TechTicker.ScrapingOrchestrationService.Models;
using System.Text.Json;

namespace TechTicker.ScrapingOrchestrationService.Data
{
    /// <summary>
    /// Database context for the Scraping Orchestration Service's own entities only.
    /// Shared entities (ProductSellerMapping, ScraperSiteConfiguration) should be accessed 
    /// through ProductSellerMappingDbContext for proper separation of concerns.
    /// </summary>
    public class ScrapingOrchestrationDbContext : DbContext
    {
        public ScrapingOrchestrationDbContext(DbContextOptions<ScrapingOrchestrationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DomainScrapingProfile> DomainScrapingProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure DomainScrapingProfile - this is the only entity this service owns
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
        }
    }
}
