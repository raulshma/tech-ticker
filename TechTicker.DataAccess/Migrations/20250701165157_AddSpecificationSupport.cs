using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableSpecificationScraping",
                table: "ScraperSiteConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationContainerSelector",
                table: "ScraperSiteConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationParsingOptions",
                table: "ScraperSiteConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationTableSelector",
                table: "ScraperSiteConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecificationCount",
                table: "ScraperRunLogs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationData",
                table: "ScraperRunLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationError",
                table: "ScraperRunLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationMetadata",
                table: "ScraperRunLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationParsingStrategy",
                table: "ScraperRunLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SpecificationParsingTime",
                table: "ScraperRunLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpecificationQualityScore",
                table: "ScraperRunLogs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LatestSpecifications",
                table: "ProductSellerMappings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SpecificationsLastUpdated",
                table: "ProductSellerMappings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpecificationsQualityScore",
                table: "ProductSellerMappings",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableSpecificationScraping",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "SpecificationContainerSelector",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "SpecificationParsingOptions",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "SpecificationTableSelector",
                table: "ScraperSiteConfigurations");

            migrationBuilder.DropColumn(
                name: "SpecificationCount",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationData",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationError",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationMetadata",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationParsingStrategy",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationParsingTime",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "SpecificationQualityScore",
                table: "ScraperRunLogs");

            migrationBuilder.DropColumn(
                name: "LatestSpecifications",
                table: "ProductSellerMappings");

            migrationBuilder.DropColumn(
                name: "SpecificationsLastUpdated",
                table: "ProductSellerMappings");

            migrationBuilder.DropColumn(
                name: "SpecificationsQualityScore",
                table: "ProductSellerMappings");
        }
    }
}
