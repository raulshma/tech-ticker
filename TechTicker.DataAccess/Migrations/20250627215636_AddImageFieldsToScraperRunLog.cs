using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddImageFieldsToScraperRunLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractedAdditionalImageUrls",
                table: "ScraperRunLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedOriginalImageUrls",
                table: "ScraperRunLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtractedPrimaryImageUrl",
                table: "ScraperRunLogs",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageProcessingCount",
                table: "ScraperRunLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageScrapingError",
                table: "ScraperRunLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageUploadCount",
                table: "ScraperRunLogs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtractedAdditionalImageUrls",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "ExtractedOriginalImageUrls",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "ExtractedPrimaryImageUrl",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "ImageProcessingCount",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "ImageScrapingError",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "ImageUploadCount",
                table: "ScraperRunLogs");
        }
    }
}
