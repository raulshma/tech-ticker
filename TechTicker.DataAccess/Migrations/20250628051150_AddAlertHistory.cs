using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertHistories",
                columns: table => new
                {
                    AlertHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AlertType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PercentageValue = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SpecificSellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggeringPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TriggeringStockStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductPageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RuleDescription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NotificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NotificationError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotificationSentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WasAlertDeactivated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertHistories", x => x.AlertHistoryId);
                    table.ForeignKey(
                        name: "FK_AlertHistories_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "AlertRuleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertHistories_Products_CanonicalProductId",
                        column: x => x.CanonicalProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistories_AlertRuleId",
                table: "AlertHistories",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistories_CanonicalProductId",
                table: "AlertHistories",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertHistories_UserId",
                table: "AlertHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertHistories");
        }
    }
}
