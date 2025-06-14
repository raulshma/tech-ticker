using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.ScrapingOrchestrationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainScrapingProfiles",
                columns: table => new
                {
                    Domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UserAgentList = table.Column<string>(type: "text", nullable: false),
                    HeaderProfiles = table.Column<string>(type: "text", nullable: false),
                    MinDelayMs = table.Column<int>(type: "integer", nullable: false),
                    MaxDelayMs = table.Column<int>(type: "integer", nullable: false),
                    LastRequestAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAllowedRequestAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainScrapingProfiles", x => x.Domain);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainScrapingProfiles_NextAllowedRequestAt",
                table: "DomainScrapingProfiles",
                column: "NextAllowedRequestAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainScrapingProfiles");
        }
    }
}
