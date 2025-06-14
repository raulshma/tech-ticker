using Microsoft.EntityFrameworkCore;
using TechTicker.PriceHistoryService.Models;

namespace TechTicker.PriceHistoryService.Data
{
    /// <summary>
    /// Entity Framework context for price history data
    /// </summary>
    public class PriceHistoryDbContext : DbContext
    {
        public PriceHistoryDbContext(DbContextOptions<PriceHistoryDbContext> options) : base(options)
        {
        }

        public DbSet<PriceHistoryRecord> PriceHistory { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PriceHistoryRecord
            modelBuilder.Entity<PriceHistoryRecord>(entity =>
            {
                // Primary key
                entity.HasKey(e => e.Id);

                // Indexes for efficient querying
                entity.HasIndex(e => new { e.CanonicalProductId, e.Timestamp })
                    .HasDatabaseName("IX_PriceHistory_ProductId_Timestamp");

                entity.HasIndex(e => new { e.CanonicalProductId, e.SellerName, e.Timestamp })
                    .HasDatabaseName("IX_PriceHistory_ProductId_Seller_Timestamp");

                entity.HasIndex(e => e.Timestamp)
                    .HasDatabaseName("IX_PriceHistory_Timestamp");

                entity.HasIndex(e => new { e.SellerName, e.Timestamp })
                    .HasDatabaseName("IX_PriceHistory_Seller_Timestamp");

                // Configure properties
                entity.Property(e => e.Timestamp)
                    .HasColumnType("timestamptz")
                    .IsRequired();

                entity.Property(e => e.CanonicalProductId)
                    .IsRequired();

                entity.Property(e => e.SellerName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(e => e.StockStatus)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.SourceUrl)
                    .HasMaxLength(2048);

                entity.Property(e => e.ProductName)
                    .HasMaxLength(500);

                entity.Property(e => e.OriginalStockStatus)
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();
            });
        }
    }
}
