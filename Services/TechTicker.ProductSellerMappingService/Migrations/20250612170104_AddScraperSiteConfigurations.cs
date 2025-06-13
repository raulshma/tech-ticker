using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.ProductSellerMappingService.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperSiteConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SiteSpecificSelectorsId",
                table: "ProductSellerMappings",
                newName: "SiteConfigId");

            migrationBuilder.CreateTable(
                name: "ScraperSiteConfigurations",
                columns: table => new
                {
                    SiteConfigId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SiteDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProductNameSelector = table.Column<string>(type: "TEXT", nullable: false),
                    PriceSelector = table.Column<string>(type: "TEXT", nullable: false),
                    StockSelector = table.Column<string>(type: "TEXT", nullable: false),
                    SellerNameOnPageSelector = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSiteConfigurations", x => x.SiteConfigId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_NextScrapeAt",
                table: "ProductSellerMappings",
                column: "NextScrapeAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_SiteConfigId",
                table: "ProductSellerMappings",
                column: "SiteConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSiteConfigurations_SiteDomain_Unique",
                table: "ScraperSiteConfigurations",
                column: "SiteDomain",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSellerMappings_ScraperSiteConfigurations_SiteConfigId",
                table: "ProductSellerMappings",
                column: "SiteConfigId",
                principalTable: "ScraperSiteConfigurations",
                principalColumn: "SiteConfigId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSellerMappings_ScraperSiteConfigurations_SiteConfigId",
                table: "ProductSellerMappings");

            migrationBuilder.DropTable(
                name: "ScraperSiteConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_ProductSellerMappings_NextScrapeAt",
                table: "ProductSellerMappings");

            migrationBuilder.DropIndex(
                name: "IX_ProductSellerMappings_SiteConfigId",
                table: "ProductSellerMappings");

            migrationBuilder.RenameColumn(
                name: "SiteConfigId",
                table: "ProductSellerMappings",
                newName: "SiteSpecificSelectorsId");
        }
    }
}
