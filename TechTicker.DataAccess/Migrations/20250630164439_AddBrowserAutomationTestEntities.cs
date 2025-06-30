using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBrowserAutomationTestEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedTestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TestUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    ActionsExecuted = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    ProfileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TestResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    OptionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    ScreenshotsData = table.Column<string>(type: "text", nullable: true),
                    VideoRecording = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTestResults_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedTestResultTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedTestResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTestResultTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTestResultTags_SavedTestResults_SavedTestResultId",
                        column: x => x.SavedTestResultId,
                        principalTable: "SavedTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestExecutionHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SavedTestResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProfileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    ActionsExecuted = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    ExecutedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BrowserEngine = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestExecutionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestExecutionHistory_AspNetUsers_ExecutedBy",
                        column: x => x.ExecutedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestExecutionHistory_SavedTestResults_SavedTestResultId",
                        column: x => x.SavedTestResultId,
                        principalTable: "SavedTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_CreatedBy",
                table: "SavedTestResults",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_ExecutedAt",
                table: "SavedTestResults",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_Name",
                table: "SavedTestResults",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_ProfileHash",
                table: "SavedTestResults",
                column: "ProfileHash");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_SavedAt",
                table: "SavedTestResults",
                column: "SavedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_Success",
                table: "SavedTestResults",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResults_TestUrl",
                table: "SavedTestResults",
                column: "TestUrl");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResultTags_SavedTestResultId",
                table: "SavedTestResultTags",
                column: "SavedTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResultTags_SavedTestResultId_Tag",
                table: "SavedTestResultTags",
                columns: new[] { "SavedTestResultId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedTestResultTags_Tag",
                table: "SavedTestResultTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_BrowserEngine",
                table: "TestExecutionHistory",
                column: "BrowserEngine");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_DeviceType",
                table: "TestExecutionHistory",
                column: "DeviceType");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_ExecutedAt",
                table: "TestExecutionHistory",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_ExecutedBy",
                table: "TestExecutionHistory",
                column: "ExecutedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_ProfileHash",
                table: "TestExecutionHistory",
                column: "ProfileHash");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_SavedTestResultId",
                table: "TestExecutionHistory",
                column: "SavedTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_SessionId",
                table: "TestExecutionHistory",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_Success",
                table: "TestExecutionHistory",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionHistory_TestUrl",
                table: "TestExecutionHistory",
                column: "TestUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedTestResultTags");

            migrationBuilder.DropTable(
                name: "TestExecutionHistory");

            migrationBuilder.DropTable(
                name: "SavedTestResults");
        }
    }
}
