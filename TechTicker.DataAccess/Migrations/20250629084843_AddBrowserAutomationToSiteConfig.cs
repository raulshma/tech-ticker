using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBrowserAutomationToSiteConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrowserAutomationProfile",
                table: "ScraperSiteConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresBrowserAutomation",
                table: "ScraperSiteConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrowserAutomationProfile",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "RequiresBrowserAutomation",
                table: "ScraperSiteConfigurations");
        }
    }
}
