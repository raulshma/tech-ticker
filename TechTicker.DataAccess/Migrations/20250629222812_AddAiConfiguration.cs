using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAiConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiConfigurations",
                columns: table => new
                {
                    AiConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OpenApiCompatibleUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputTokenLimit = table.Column<int>(type: "integer", nullable: true),
                    OutputTokenLimit = table.Column<int>(type: "integer", nullable: true),
                    Capabilities = table.Column<string>(type: "jsonb", nullable: true),
                    SupportedInputTypes = table.Column<string>(type: "jsonb", nullable: true),
                    SupportedOutputTypes = table.Column<string>(type: "jsonb", nullable: true),
                    RateLimitRpm = table.Column<int>(type: "integer", nullable: true),
                    RateLimitTpm = table.Column<long>(type: "bigint", nullable: true),
                    RateLimitRpd = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiConfigurations", x => x.AiConfigurationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_IsActive",
                table: "AiConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_IsDefault",
                table: "AiConfigurations",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_Provider",
                table: "AiConfigurations",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_AiConfigurations_Provider_Model",
                table: "AiConfigurations",
                columns: new[] { "Provider", "Model" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiConfigurations");
        }
    }
}
