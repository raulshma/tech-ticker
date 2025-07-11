﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TechTicker.DataAccess;

#nullable disable

namespace TechTicker.DataAccess.Migrations
{
    [DbContext(typeof(TechTickerDbContext))]
    [Migration("20250627230638_AddUserNotificationPreferences")]
    partial class AddUserNotificationPreferences
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.AlertRule", b =>
                {
                    b.Property<Guid>("AlertRuleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CanonicalProductId")
                        .HasColumnType("uuid");

                    b.Property<string>("ConditionType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTimeOffset?>("LastNotifiedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("NotificationFrequencyMinutes")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1440);

                    b.Property<decimal?>("PercentageValue")
                        .HasColumnType("decimal(5,2)");

                    b.Property<string>("SpecificSellerName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<decimal?>("ThresholdValue")
                        .HasColumnType("decimal(10,2)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("AlertRuleId");

                    b.HasIndex("CanonicalProductId");

                    b.HasIndex("UserId");

                    b.ToTable("AlertRules");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("FirstName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("LastName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Category", b =>
                {
                    b.Property<Guid>("CategoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Slug")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("CategoryId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("Slug")
                        .IsUnique();

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Permission", b =>
                {
                    b.Property<Guid>("PermissionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Category")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("PermissionId");

                    b.HasIndex("Category");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Permissions");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.PriceHistory", b =>
                {
                    b.Property<Guid>("PriceHistoryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CanonicalProductId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("MappingId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("ScrapedProductNameOnPage")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("SellerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("SourceUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<string>("StockStatus")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("PriceHistoryId");

                    b.HasIndex("CanonicalProductId");

                    b.HasIndex("SellerName");

                    b.HasIndex("Timestamp");

                    b.HasIndex("MappingId", "Timestamp");

                    b.HasIndex("CanonicalProductId", "SellerName", "Timestamp");

                    b.ToTable("PriceHistory");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("ProductId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AdditionalImageUrls")
                        .HasColumnType("jsonb");

                    b.Property<Guid>("CategoryId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("ImageLastUpdated")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("Manufacturer")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("ModelNumber")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("OriginalImageUrls")
                        .HasColumnType("jsonb");

                    b.Property<string>("PrimaryImageUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<string>("SKU")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Specifications")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("ProductId");

                    b.HasIndex("CategoryId");

                    b.HasIndex("IsActive");

                    b.HasIndex("Name");

                    b.HasIndex("SKU")
                        .IsUnique()
                        .HasFilter("\"SKU\" IS NOT NULL");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ProductSellerMapping", b =>
                {
                    b.Property<Guid>("MappingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CanonicalProductId")
                        .HasColumnType("uuid");

                    b.Property<int>("ConsecutiveFailureCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0);

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ExactProductUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<bool>("IsActiveForScraping")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("LastScrapeErrorCode")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("LastScrapeStatus")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<DateTimeOffset?>("LastScrapedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("NextScrapeAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ScrapingFrequencyOverride")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("SellerName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<Guid?>("SiteConfigId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("MappingId");

                    b.HasIndex("CanonicalProductId");

                    b.HasIndex("IsActiveForScraping");

                    b.HasIndex("NextScrapeAt");

                    b.HasIndex("SiteConfigId");

                    b.HasIndex("CanonicalProductId", "SellerName", "ExactProductUrl")
                        .IsUnique();

                    b.ToTable("ProductSellerMappings");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.RolePermission", b =>
                {
                    b.Property<Guid>("RolePermissionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("RolePermissionId");

                    b.HasIndex("PermissionId");

                    b.HasIndex("RoleId", "PermissionId")
                        .IsUnique();

                    b.ToTable("RolePermissions");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ScraperRunLog", b =>
                {
                    b.Property<Guid>("RunId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AdditionalHeaders")
                        .HasColumnType("jsonb");

                    b.Property<int>("AttemptNumber")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1);

                    b.Property<DateTimeOffset?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DebugNotes")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("interval");

                    b.Property<string>("ErrorCategory")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("ErrorCode")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("text");

                    b.Property<string>("ErrorStackTrace")
                        .HasColumnType("text");

                    b.Property<string>("ExtractedAdditionalImageUrls")
                        .HasColumnType("text");

                    b.Property<string>("ExtractedOriginalImageUrls")
                        .HasColumnType("text");

                    b.Property<decimal?>("ExtractedPrice")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("ExtractedPrimaryImageUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<string>("ExtractedProductName")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("ExtractedSellerName")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("ExtractedStockStatus")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int?>("HttpStatusCode")
                        .HasColumnType("integer");

                    b.Property<int?>("ImageProcessingCount")
                        .HasColumnType("integer");

                    b.Property<string>("ImageScrapingError")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<int?>("ImageUploadCount")
                        .HasColumnType("integer");

                    b.Property<Guid>("MappingId")
                        .HasColumnType("uuid");

                    b.Property<TimeSpan?>("PageLoadTime")
                        .HasColumnType("interval");

                    b.Property<Guid?>("ParentRunId")
                        .HasColumnType("uuid");

                    b.Property<TimeSpan?>("ParsingTime")
                        .HasColumnType("interval");

                    b.Property<string>("RawHtmlSnippet")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<long?>("ResponseSizeBytes")
                        .HasColumnType("bigint");

                    b.Property<TimeSpan?>("ResponseTime")
                        .HasColumnType("interval");

                    b.Property<string>("Selectors")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.Property<string>("TargetUrl")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)");

                    b.Property<string>("UserAgent")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.HasKey("RunId");

                    b.HasIndex("ErrorCategory");

                    b.HasIndex("MappingId");

                    b.HasIndex("ParentRunId");

                    b.HasIndex("StartedAt");

                    b.HasIndex("Status");

                    b.HasIndex("MappingId", "StartedAt");

                    b.HasIndex("Status", "StartedAt");

                    b.ToTable("ScraperRunLogs");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ScraperSiteConfiguration", b =>
                {
                    b.Property<Guid>("SiteConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AdditionalHeaders")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("DefaultUserAgent")
                        .HasColumnType("text");

                    b.Property<string>("ImageSelector")
                        .HasColumnType("text");

                    b.Property<bool>("IsEnabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<string>("PriceSelector")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ProductNameSelector")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("SellerNameOnPageSelector")
                        .HasColumnType("text");

                    b.Property<string>("SiteDomain")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("StockSelector")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("SiteConfigId");

                    b.HasIndex("SiteDomain")
                        .IsUnique();

                    b.ToTable("ScraperSiteConfigurations");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.UserNotificationPreferences", b =>
                {
                    b.Property<Guid>("UserNotificationPreferencesId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CustomAvatarUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<string>("CustomBotName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("DiscordWebhookUrl")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)");

                    b.Property<bool>("IsDiscordNotificationEnabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<string>("NotificationProductIds")
                        .HasColumnType("jsonb");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("UserNotificationPreferencesId");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("UserNotificationPreferences");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.AlertRule", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.Product", "Product")
                        .WithMany("AlertRules")
                        .HasForeignKey("CanonicalProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", "User")
                        .WithMany("AlertRules")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Product");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.PriceHistory", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("CanonicalProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TechTicker.Domain.Entities.ProductSellerMapping", "Mapping")
                        .WithMany("PriceHistory")
                        .HasForeignKey("MappingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Mapping");

                    b.Navigation("Product");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Product", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.Category", "Category")
                        .WithMany("Products")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ProductSellerMapping", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.Product", "Product")
                        .WithMany("ProductSellerMappings")
                        .HasForeignKey("CanonicalProductId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TechTicker.Domain.Entities.ScraperSiteConfiguration", "SiteConfiguration")
                        .WithMany("ProductSellerMappings")
                        .HasForeignKey("SiteConfigId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Product");

                    b.Navigation("SiteConfiguration");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.RolePermission", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.Permission", "Permission")
                        .WithMany("RolePermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", "Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Permission");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ScraperRunLog", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.ProductSellerMapping", "Mapping")
                        .WithMany("ScraperRunLogs")
                        .HasForeignKey("MappingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TechTicker.Domain.Entities.ScraperRunLog", "ParentRun")
                        .WithMany("RetryAttempts")
                        .HasForeignKey("ParentRunId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Mapping");

                    b.Navigation("ParentRun");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.UserNotificationPreferences", b =>
                {
                    b.HasOne("TechTicker.Domain.Entities.ApplicationUser", "User")
                        .WithOne("NotificationPreferences")
                        .HasForeignKey("TechTicker.Domain.Entities.UserNotificationPreferences", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ApplicationUser", b =>
                {
                    b.Navigation("AlertRules");

                    b.Navigation("NotificationPreferences");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Category", b =>
                {
                    b.Navigation("Products");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Permission", b =>
                {
                    b.Navigation("RolePermissions");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.Product", b =>
                {
                    b.Navigation("AlertRules");

                    b.Navigation("ProductSellerMappings");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ProductSellerMapping", b =>
                {
                    b.Navigation("PriceHistory");

                    b.Navigation("ScraperRunLogs");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ScraperRunLog", b =>
                {
                    b.Navigation("RetryAttempts");
                });

            modelBuilder.Entity("TechTicker.Domain.Entities.ScraperSiteConfiguration", b =>
                {
                    b.Navigation("ProductSellerMappings");
                });
#pragma warning restore 612, 618
        }
    }
}
