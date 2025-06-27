using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddImageFieldsToProductAndSiteConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageSelector",
                table: "ScraperSiteConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalImageUrls",
                table: "Products",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImageLastUpdated",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalImageUrls",
                table: "Products",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageUrl",
                table: "Products",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageSelector",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "AdditionalImageUrls",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageLastUpdated",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OriginalImageUrls",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PrimaryImageUrl",
                table: "Products");
        }
    }
}
