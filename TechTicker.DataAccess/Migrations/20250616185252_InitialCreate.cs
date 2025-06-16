using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "ScraperSiteConfigurations",
                columns: table => new
                {
                    SiteConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ProductNameSelector = table.Column<string>(type: "text", nullable: false),
                    PriceSelector = table.Column<string>(type: "text", nullable: false),
                    StockSelector = table.Column<string>(type: "text", nullable: false),
                    SellerNameOnPageSelector = table.Column<string>(type: "text", nullable: true),
                    DefaultUserAgent = table.Column<string>(type: "text", nullable: true),
                    AdditionalHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSiteConfigurations", x => x.SiteConfigId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Specifications = table.Column<string>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConditionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PercentageValue = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SpecificSellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NotificationFrequencyMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 1440),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastNotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.AlertRuleId);
                    table.ForeignKey(
                        name: "FK_AlertRules_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertRules_Products_CanonicalProductId",
                        column: x => x.CanonicalProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSellerMappings",
                columns: table => new
                {
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExactProductUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsActiveForScraping = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ScrapingFrequencyOverride = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SiteConfigId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastScrapedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextScrapeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastScrapeStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastScrapeErrorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSellerMappings", x => x.MappingId);
                    table.ForeignKey(
                        name: "FK_ProductSellerMappings_Products_CanonicalProductId",
                        column: x => x.CanonicalProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSellerMappings_ScraperSiteConfigurations_SiteConfigId",
                        column: x => x.SiteConfigId,
                        principalTable: "ScraperSiteConfigurations",
                        principalColumn: "SiteConfigId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    PriceHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CanonicalProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StockStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ScrapedProductNameOnPage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.PriceHistoryId);
                    table.ForeignKey(
                        name: "FK_PriceHistory_ProductSellerMappings_MappingId",
                        column: x => x.MappingId,
                        principalTable: "ProductSellerMappings",
                        principalColumn: "MappingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceHistory_Products_CanonicalProductId",
                        column: x => x.CanonicalProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperRunLogs",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalHeaders = table.Column<string>(type: "jsonb", nullable: true),
                    Selectors = table.Column<string>(type: "jsonb", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ResponseSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ExtractedProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExtractedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ExtractedStockStatus = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExtractedSellerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "text", nullable: true),
                    ErrorCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ParentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    PageLoadTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ParsingTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    RawHtmlSnippet = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DebugNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperRunLogs", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_ScraperRunLogs_ProductSellerMappings_MappingId",
                        column: x => x.MappingId,
                        principalTable: "ProductSellerMappings",
                        principalColumn: "MappingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperRunLogs_ScraperRunLogs_ParentRunId",
                        column: x => x.ParentRunId,
                        principalTable: "ScraperRunLogs",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_CanonicalProductId",
                table: "AlertRules",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_UserId",
                table: "AlertRules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_CanonicalProductId",
                table: "PriceHistory",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_CanonicalProductId_SellerName_Timestamp",
                table: "PriceHistory",
                columns: new[] { "CanonicalProductId", "SellerName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_MappingId_Timestamp",
                table: "PriceHistory",
                columns: new[] { "MappingId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_SellerName",
                table: "PriceHistory",
                column: "SellerName");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_Timestamp",
                table: "PriceHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true,
                filter: "\"SKU\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_CanonicalProductId",
                table: "ProductSellerMappings",
                column: "CanonicalProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_CanonicalProductId_SellerName_ExactPr~",
                table: "ProductSellerMappings",
                columns: new[] { "CanonicalProductId", "SellerName", "ExactProductUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_IsActiveForScraping",
                table: "ProductSellerMappings",
                column: "IsActiveForScraping");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_NextScrapeAt",
                table: "ProductSellerMappings",
                column: "NextScrapeAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSellerMappings_SiteConfigId",
                table: "ProductSellerMappings",
                column: "SiteConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_ErrorCategory",
                table: "ScraperRunLogs",
                column: "ErrorCategory");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_MappingId",
                table: "ScraperRunLogs",
                column: "MappingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_MappingId_StartedAt",
                table: "ScraperRunLogs",
                columns: new[] { "MappingId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_ParentRunId",
                table: "ScraperRunLogs",
                column: "ParentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_StartedAt",
                table: "ScraperRunLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_Status",
                table: "ScraperRunLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRunLogs_Status_StartedAt",
                table: "ScraperRunLogs",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScraperSiteConfigurations_SiteDomain",
                table: "ScraperSiteConfigurations",
                column: "SiteDomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "ScraperRunLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ProductSellerMappings");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "ScraperSiteConfigurations");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
