using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechTicker.PriceHistoryService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    canonical_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    stock_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_stock_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_history", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ProductId_Seller_Timestamp",
                table: "price_history",
                columns: new[] { "canonical_product_id", "seller_name", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ProductId_Timestamp",
                table: "price_history",
                columns: new[] { "canonical_product_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Seller_Timestamp",
                table: "price_history",
                columns: new[] { "seller_name", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Timestamp",
                table: "price_history",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_history");
        }
    }
}
