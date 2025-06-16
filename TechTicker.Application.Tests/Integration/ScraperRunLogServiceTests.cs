using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using Xunit;

namespace TechTicker.Application.Tests.Integration;

public class ScraperRunLogServiceTests : IDisposable
{
    private readonly TechTickerDbContext _context;
    private readonly IScraperRunLogService _scraperRunLogService;
    private readonly IUnitOfWork _unitOfWork;

    public ScraperRunLogServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<TechTickerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TechTickerDbContext(options);
        _unitOfWork = new UnitOfWork(_context);

        // Setup logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ScraperRunLogService>();

        _scraperRunLogService = new ScraperRunLogService(_unitOfWork, logger);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var category = new Category
        {
            CategoryId = Guid.NewGuid(),
            Name = "Test Category",
            Slug = "test-category",
            Description = "Test category for scraper run log tests",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = "Test Product",
            CategoryId = category.CategoryId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var mapping = new ProductSellerMapping
        {
            MappingId = Guid.NewGuid(),
            CanonicalProductId = product.ProductId,
            SellerName = "Test Seller",
            ExactProductUrl = "https://example.com/product",
            IsActiveForScraping = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Categories.Add(category);
        _context.Products.Add(product);
        _context.ProductSellerMappings.Add(mapping);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateRunLogAsync_ShouldCreateNewRunLog()
    {
        // Arrange
        var mapping = await _context.ProductSellerMappings.FirstAsync();
        var createDto = new CreateScraperRunLogDto
        {
            MappingId = mapping.MappingId,
            TargetUrl = "https://example.com/product",
            UserAgent = "Test User Agent",
            AdditionalHeaders = new Dictionary<string, string> { { "Accept", "text/html" } },
            Selectors = new ScrapingSelectorsDto
            {
                ProductNameSelector = ".product-name",
                PriceSelector = ".price",
                StockSelector = ".stock",
                SellerNameOnPageSelector = ".seller"
            },
            DebugNotes = "Test run"
        };

        // Act
        var result = await _scraperRunLogService.CreateRunLogAsync(createDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);

        var runLog = await _context.ScraperRunLogs.FindAsync(result.Data);
        Assert.NotNull(runLog);
        Assert.Equal(mapping.MappingId, runLog.MappingId);
        Assert.Equal("STARTED", runLog.Status);
        Assert.Equal(createDto.TargetUrl, runLog.TargetUrl);
        Assert.Equal(createDto.UserAgent, runLog.UserAgent);
    }

    [Fact]
    public async Task CompleteRunAsync_ShouldMarkRunAsSuccessful()
    {
        // Arrange
        var mapping = await _context.ProductSellerMappings.FirstAsync();
        var createResult = await _scraperRunLogService.CreateRunLogAsync(new CreateScraperRunLogDto
        {
            MappingId = mapping.MappingId,
            TargetUrl = "https://example.com/product",
            UserAgent = "Test User Agent"
        });

        var completeDto = new CompleteScraperRunDto
        {
            ExtractedProductName = "Test Product Name",
            ExtractedPrice = 99.99m,
            ExtractedStockStatus = "In Stock",
            ExtractedSellerName = "Test Seller",
            ResponseTime = TimeSpan.FromMilliseconds(500),
            PageLoadTime = TimeSpan.FromMilliseconds(300),
            ParsingTime = TimeSpan.FromMilliseconds(200),
            DebugNotes = "Successfully completed"
        };

        // Act
        var result = await _scraperRunLogService.CompleteRunAsync(createResult.Data, completeDto);

        // Assert
        Assert.True(result.IsSuccess);

        var runLog = await _context.ScraperRunLogs.FindAsync(createResult.Data);
        Assert.NotNull(runLog);
        Assert.Equal("SUCCESS", runLog.Status);
        Assert.NotNull(runLog.CompletedAt);
        Assert.NotNull(runLog.Duration);
        Assert.Equal(completeDto.ExtractedProductName, runLog.ExtractedProductName);
        Assert.Equal(completeDto.ExtractedPrice, runLog.ExtractedPrice);
        Assert.Equal(completeDto.ExtractedStockStatus, runLog.ExtractedStockStatus);
    }

    [Fact]
    public async Task FailRunAsync_ShouldMarkRunAsFailed()
    {
        // Arrange
        var mapping = await _context.ProductSellerMappings.FirstAsync();
        var createResult = await _scraperRunLogService.CreateRunLogAsync(new CreateScraperRunLogDto
        {
            MappingId = mapping.MappingId,
            TargetUrl = "https://example.com/product",
            UserAgent = "Test User Agent"
        });

        var failDto = new FailScraperRunDto
        {
            ErrorMessage = "Network timeout",
            ErrorCode = "TIMEOUT",
            ErrorCategory = "NETWORK",
            HttpStatusCode = 408,
            ResponseTime = TimeSpan.FromSeconds(30),
            DebugNotes = "Request timed out after 30 seconds"
        };

        // Act
        var result = await _scraperRunLogService.FailRunAsync(createResult.Data, failDto);

        // Assert
        Assert.True(result.IsSuccess);

        var runLog = await _context.ScraperRunLogs.FindAsync(createResult.Data);
        Assert.NotNull(runLog);
        Assert.Equal("FAILED", runLog.Status);
        Assert.NotNull(runLog.CompletedAt);
        Assert.NotNull(runLog.Duration);
        Assert.Equal(failDto.ErrorMessage, runLog.ErrorMessage);
        Assert.Equal(failDto.ErrorCode, runLog.ErrorCode);
        Assert.Equal(failDto.ErrorCategory, runLog.ErrorCategory);
        Assert.Equal(failDto.HttpStatusCode, runLog.HttpStatusCode);
    }

    [Fact]
    public async Task GetPagedLogsAsync_ShouldReturnFilteredResults()
    {
        // Arrange
        var mapping = await _context.ProductSellerMappings.FirstAsync();
        
        // Create multiple run logs
        for (int i = 0; i < 5; i++)
        {
            var createResult = await _scraperRunLogService.CreateRunLogAsync(new CreateScraperRunLogDto
            {
                MappingId = mapping.MappingId,
                TargetUrl = $"https://example.com/product{i}",
                UserAgent = "Test User Agent"
            });

            if (i % 2 == 0)
            {
                await _scraperRunLogService.CompleteRunAsync(createResult.Data, new CompleteScraperRunDto
                {
                    ExtractedProductName = $"Product {i}",
                    ExtractedPrice = 10.00m * i
                });
            }
            else
            {
                await _scraperRunLogService.FailRunAsync(createResult.Data, new FailScraperRunDto
                {
                    ErrorMessage = $"Error {i}",
                    ErrorCode = "TEST_ERROR"
                });
            }
        }

        var filter = new ScraperRunLogFilterDto
        {
            Page = 1,
            PageSize = 10,
            Status = "SUCCESS"
        };

        // Act
        var result = await _scraperRunLogService.GetPagedLogsAsync(filter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.TotalCount); // 3 successful runs (0, 2, 4)
        Assert.Equal(3, result.Data.Items.Count());
        Assert.All(result.Data.Items, item => Assert.Equal("SUCCESS", item.Status));
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectMetrics()
    {
        // Arrange
        var mapping = await _context.ProductSellerMappings.FirstAsync();
        
        // Create test data: 3 successful, 2 failed
        for (int i = 0; i < 5; i++)
        {
            var createResult = await _scraperRunLogService.CreateRunLogAsync(new CreateScraperRunLogDto
            {
                MappingId = mapping.MappingId,
                TargetUrl = $"https://example.com/product{i}",
                UserAgent = "Test User Agent"
            });

            if (i < 3)
            {
                await _scraperRunLogService.CompleteRunAsync(createResult.Data, new CompleteScraperRunDto
                {
                    ExtractedProductName = $"Product {i}",
                    ExtractedPrice = 10.00m * i,
                    ResponseTime = TimeSpan.FromMilliseconds(100 + i * 50)
                });
            }
            else
            {
                await _scraperRunLogService.FailRunAsync(createResult.Data, new FailScraperRunDto
                {
                    ErrorMessage = $"Error {i}",
                    ErrorCode = "TEST_ERROR",
                    ErrorCategory = "NETWORK"
                });
            }
        }

        // Act
        var result = await _scraperRunLogService.GetStatisticsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        var stats = result.Data!;
        Assert.Equal(5, stats.TotalRuns);
        Assert.Equal(3, stats.SuccessfulRuns);
        Assert.Equal(2, stats.FailedRuns);
        Assert.Equal(60.0, stats.SuccessRate); // 3/5 * 100
        Assert.Contains("NETWORK", stats.ErrorCategoryCounts.Keys);
        Assert.Equal(2, stats.ErrorCategoryCounts["NETWORK"]);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
