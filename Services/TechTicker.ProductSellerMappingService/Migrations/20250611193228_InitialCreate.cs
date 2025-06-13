using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.ProductSellerMappingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSellerMappings",
                columns: table => new
                {
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExactProductUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsActiveForScraping = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ScrapingFrequencyOverride = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SiteSpecificSelectorsId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastScrapedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextScrapeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSellerMappings", x => x.MappingId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_CanonicalProductId",
                table: "ProductSellerMappings",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_CanonicalProductId_SellerName_Unique",
                table: "ProductSellerMappings",
                columns: new[] { "CanonicalProductId", "SellerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_IsActiveForScraping",
                table: "ProductSellerMappings",
                column: "IsActiveForScraping");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_SellerName",
                table: "ProductSellerMappings",
                column: "SellerName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSellerMappings");
        }
    }
}
