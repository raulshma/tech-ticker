using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFeaturesEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ProductDiscoveryCandidates",
                type: "integer",
                maxLength: 50,
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                table: "DiscoveryApprovalWorkflows",
                type: "integer",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProductDiscoveryCandidates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "DiscoveryApprovalWorkflows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50);
        }
    }
}
