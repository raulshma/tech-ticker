using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDiscoveryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductDiscoveryCandidates",
                columns: table => new
                {
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ExtractedProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExtractedManufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExtractedModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExtractedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ExtractedImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExtractedDescription = table.Column<string>(type: "text", nullable: true),
                    ExtractedSpecifications = table.Column<string>(type: "jsonb", nullable: true),
                    SuggestedCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0m),
                    SimilarProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    SimilarityScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0m),
                    DiscoveryMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DiscoveredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDiscoveryCandidates", x => x.CandidateId);
                    table.ForeignKey(
                        name: "FK_ProductDiscoveryCandidates_AspNetUsers_DiscoveredByUserId",
                        column: x => x.DiscoveredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductDiscoveryCandidates_Categories_SuggestedCategoryId",
                        column: x => x.SuggestedCategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductDiscoveryCandidates_Products_SimilarProductId",
                        column: x => x.SimilarProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryApprovalWorkflows",
                columns: table => new
                {
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    Modifications = table.Column<string>(type: "jsonb", nullable: true),
                    ActionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryApprovalWorkflows", x => x.WorkflowId);
                    table.ForeignKey(
                        name: "FK_DiscoveryApprovalWorkflows_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscoveryApprovalWorkflows_ProductDiscoveryCandidates_Cand~",
                        column: x => x.CandidateId,
                        principalTable: "ProductDiscoveryCandidates",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryApprovalWorkflows_Action",
                table: "DiscoveryApprovalWorkflows",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryApprovalWorkflows_ActionDate",
                table: "DiscoveryApprovalWorkflows",
                column: "ActionDate");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryApprovalWorkflows_CandidateId",
                table: "DiscoveryApprovalWorkflows",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryApprovalWorkflows_ReviewerId",
                table: "DiscoveryApprovalWorkflows",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_DiscoveredAt",
                table: "ProductDiscoveryCandidates",
                column: "DiscoveredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_DiscoveredByUserId",
                table: "ProductDiscoveryCandidates",
                column: "DiscoveredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_DiscoveryMethod",
                table: "ProductDiscoveryCandidates",
                column: "DiscoveryMethod");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_SimilarProductId",
                table: "ProductDiscoveryCandidates",
                column: "SimilarProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_Status",
                table: "ProductDiscoveryCandidates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDiscoveryCandidates_SuggestedCategoryId",
                table: "ProductDiscoveryCandidates",
                column: "SuggestedCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveryApprovalWorkflows");

            migrationBuilder.DropTable(
                name: "ProductDiscoveryCandidates");
        }
    }
}