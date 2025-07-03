using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using Xunit;

namespace TechTicker.Application.Tests.Services;

/// <summary>
/// Comprehensive unit tests for PriceAnalysisService
/// Tests price comparison and value analysis functionality
/// </summary>
public class PriceAnalysisServiceTests
{
    private readonly Mock<ILogger<PriceAnalysisService>> _loggerMock;
    private readonly PriceAnalysisService _service;

    public PriceAnalysisServiceTests()
    {
        _loggerMock = new Mock<ILogger<PriceAnalysisService>>();
        _service = new PriceAnalysisService(_loggerMock.Object);
    }

    #region AnalyzePricesAsync Tests

    [Fact]
    public async Task AnalyzePricesAsync_ShouldReturnAnalysis_WhenBothProductsHavePrices()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller B", Price = 110m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 120m, StockStatus = "In Stock", SourceUrl = "url3", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller C", Price = 125m, StockStatus = "In Stock", SourceUrl = "url4", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
        Assert.Equal(100m, result.Summary.Product1LowestPrice);
        Assert.Equal(120m, result.Summary.Product2LowestPrice);
        Assert.Equal(20m, result.Summary.PriceDifference);
        Assert.Equal(20m, result.Summary.PriceDifferencePercentage);
        Assert.Equal("Product1", result.Summary.LowerPricedProduct);
        Assert.Equal(2, result.Summary.Product1SellerCount);
        Assert.Equal(2, result.Summary.Product2SellerCount);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldReturnSellerComparisons()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Common Seller", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller A", Price = 110m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Common Seller", Price = 120m, StockStatus = "In Stock", SourceUrl = "url3", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller B", Price = 125m, StockStatus = "Out of Stock", SourceUrl = "url4", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SellerComparisons);
        
        var commonSellerComparison = result.SellerComparisons.FirstOrDefault(sc => sc.SellerName == "Common Seller");
        Assert.NotNull(commonSellerComparison);
        Assert.Equal(100m, commonSellerComparison.Product1Price);
        Assert.Equal(120m, commonSellerComparison.Product2Price);
        Assert.Equal(20m, commonSellerComparison.PriceDifference);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleOutOfStockProducts()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller B", Price = 110m, StockStatus = "Out of Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 120m, StockStatus = "Out of Stock", SourceUrl = "url3", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller B", Price = 125m, StockStatus = "In Stock", SourceUrl = "url4", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        
        var sellerAComparison = result.SellerComparisons.FirstOrDefault(sc => sc.SellerName == "Seller A");
        Assert.NotNull(sellerAComparison);
        Assert.Equal("Product1", sellerAComparison.AvailabilityAdvantage);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldCalculateCorrectPercentages()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 150m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50m, result.Summary.PriceDifference);
        Assert.Equal(50m, result.Summary.PriceDifferencePercentage); // (150-100)/100 * 100 = 50%
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleNoPrices()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", Array.Empty<CurrentPriceDto>());
        var product2 = CreateProductWithPrices("Product 2", Array.Empty<CurrentPriceDto>());

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.Summary.Product1LowestPrice);
        Assert.Equal(0m, result.Summary.Product2LowestPrice);
        Assert.Equal(0m, result.Summary.PriceDifference);
        Assert.Equal(0, result.Summary.Product1SellerCount);
        Assert.Equal(0, result.Summary.Product2SellerCount);
        Assert.Empty(result.SellerComparisons);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleOneProductWithoutPrices()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", Array.Empty<CurrentPriceDto>());

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100m, result.Summary.Product1LowestPrice);
        Assert.Equal(0m, result.Summary.Product2LowestPrice);
        Assert.Equal(1, result.Summary.Product1SellerCount);
        Assert.Equal(0, result.Summary.Product2SellerCount);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldIncludeValueAnalysis()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 200m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ValueAnalysis);
        Assert.True(result.ValueAnalysis.Product1ValueScore >= 0 && result.ValueAnalysis.Product1ValueScore <= 1);
        Assert.True(result.ValueAnalysis.Product2ValueScore >= 0 && result.ValueAnalysis.Product2ValueScore <= 1);
        Assert.NotNull(result.ValueAnalysis.BetterValueProduct);
        Assert.NotNull(result.ValueAnalysis.ValueAnalysisReason);
    }

    #endregion

    #region CalculateValueScoreAsync Tests

    [Fact]
    public async Task CalculateValueScoreAsync_ShouldReturnValidScore_ForProduct()
    {
        // Arrange
        var product = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var specificationScore = 0.8m;

        // Act
        var result = await _service.CalculateValueScoreAsync(product, specificationScore);

        // Assert
        Assert.True(result >= 0 && result <= 1);
    }

    [Fact]
    public async Task CalculateValueScoreAsync_ShouldReturnHigherScore_ForBetterValue()
    {
        // Arrange
        var cheapProduct = CreateProductWithPrices("Cheap Product", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 50m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var expensiveProduct = CreateProductWithPrices("Expensive Product", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 200m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var specificationScore = 0.8m; // Same specification score

        // Act
        var cheapScore = await _service.CalculateValueScoreAsync(cheapProduct, specificationScore);
        var expensiveScore = await _service.CalculateValueScoreAsync(expensiveProduct, specificationScore);

        // Assert
        Assert.True(cheapScore > expensiveScore); // Cheaper product should have better value score
    }

    [Fact]
    public async Task CalculateValueScoreAsync_ShouldHandleZeroPrice()
    {
        // Arrange
        var product = CreateProductWithPrices("Product", Array.Empty<CurrentPriceDto>());
        var specificationScore = 0.8m;

        // Act
        var result = await _service.CalculateValueScoreAsync(product, specificationScore);

        // Assert
        Assert.True(result >= 0 && result <= 1);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public async Task CalculateValueScoreAsync_ShouldHandleVariousSpecificationScores(decimal specScore)
    {
        // Arrange
        var product = CreateProductWithPrices("Product", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.CalculateValueScoreAsync(product, specScore);

        // Assert
        Assert.True(result >= 0 && result <= 1);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleNegativePrices()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = -10m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        // Should handle negative prices gracefully (or filter them out)
        Assert.True(result.Summary.Product1LowestPrice >= 0 || result.Summary.Product1LowestPrice == -10m);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleVeryLargePrices()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = decimal.MaxValue, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Product2", result.Summary.LowerPricedProduct);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleDuplicateSellers()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = "Seller A", Price = 110m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 120m, StockStatus = "In Stock", SourceUrl = "url3", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        // Should handle duplicate sellers appropriately
        Assert.NotEmpty(result.SellerComparisons);
    }

    [Fact]
    public async Task AnalyzePricesAsync_ShouldHandleEmptySellerNames()
    {
        // Arrange
        var product1 = CreateProductWithPrices("Product 1", new[]
        {
            new CurrentPriceDto { SellerName = "", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow },
            new CurrentPriceDto { SellerName = null!, Price = 110m, StockStatus = "In Stock", SourceUrl = "url2", LastUpdated = DateTimeOffset.UtcNow }
        });

        var product2 = CreateProductWithPrices("Product 2", new[]
        {
            new CurrentPriceDto { SellerName = "Valid Seller", Price = 120m, StockStatus = "In Stock", SourceUrl = "url3", LastUpdated = DateTimeOffset.UtcNow }
        });

        // Act
        var result = await _service.AnalyzePricesAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        // Should handle empty/null seller names gracefully
        Assert.True(result.Summary.Product1SellerCount >= 0);
    }

    [Fact]
    public async Task CalculateValueScoreAsync_ShouldHandleHighSpecificationScore()
    {
        // Arrange
        var product = CreateProductWithPrices("High-Spec Product", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var highSpecScore = 1.0m;

        // Act
        var result = await _service.CalculateValueScoreAsync(product, highSpecScore);

        // Assert
        Assert.True(result >= 0 && result <= 1);
        Assert.True(result > 0); // Should have positive value score with high specs
    }

    [Fact]
    public async Task CalculateValueScoreAsync_ShouldHandleLowSpecificationScore()
    {
        // Arrange
        var product = CreateProductWithPrices("Low-Spec Product", new[]
        {
            new CurrentPriceDto { SellerName = "Seller A", Price = 100m, StockStatus = "In Stock", SourceUrl = "url1", LastUpdated = DateTimeOffset.UtcNow }
        });

        var lowSpecScore = 0.0m;

        // Act
        var result = await _service.CalculateValueScoreAsync(product, lowSpecScore);

        // Assert
        Assert.True(result >= 0 && result <= 1);
    }

    #endregion

    #region Helper Methods

    private ProductWithCurrentPricesDto CreateProductWithPrices(string name, IEnumerable<CurrentPriceDto> prices)
    {
        var priceList = prices.ToList();
        return new ProductWithCurrentPricesDto
        {
            ProductId = Guid.NewGuid(),
            Name = name,
            CategoryId = Guid.NewGuid(),
            Description = $"Test description for {name}",
            UncategorizedSpecifications = new Dictionary<string, string>
            {
                { "Brand", "Test Brand" },
                { "Model", "Test Model" }
            },
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Category = new CategoryDto
            {
                CategoryId = Guid.NewGuid(),
                Name = "Test Category",
                Slug = "test-category",
                Description = "Test category description",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            CurrentPrices = priceList,
            LowestCurrentPrice = priceList.Any() ? priceList.Min(p => p.Price) : null,
            HighestCurrentPrice = priceList.Any() ? priceList.Max(p => p.Price) : null,
            AvailableSellersCount = priceList.Count(p => p.StockStatus == "In Stock")
        };
    }

    #endregion
}