using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using System.Text.Json;
using Xunit;

namespace TechTicker.Application.Tests.Services;

/// <summary>
/// Comprehensive unit tests for SpecificationAnalysisEngine
/// Tests specification comparison algorithms and scoring logic
/// </summary>
public class SpecificationAnalysisEngineTests
{
    private readonly Mock<ILogger<SpecificationAnalysisEngine>> _loggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductSellerMappingRepository> _productSellerMappingRepositoryMock;
    private readonly SpecificationAnalysisEngine _engine;

    public SpecificationAnalysisEngineTests()
    {
        _loggerMock = new Mock<ILogger<SpecificationAnalysisEngine>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productSellerMappingRepositoryMock = new Mock<IProductSellerMappingRepository>();
        
        // Setup unit of work with repository mock
        _unitOfWorkMock.Setup(u => u.ProductSellerMappings).Returns(_productSellerMappingRepositoryMock.Object);
        
        _engine = new SpecificationAnalysisEngine(_loggerMock.Object, _unitOfWorkMock.Object);
    }

    #region AnalyzeSpecificationsAsync Tests

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldReturnComparison_WhenValidProducts()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "RAM", "8GB" },
            { "Storage", "256GB SSD" },
            { "Processor", "Intel i5" },
            { "Brand", "TestBrand" }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "RAM", "16GB" },
            { "Storage", "512GB SSD" },
            { "Processor", "Intel i7" },
            { "Brand", "TestBrand" }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Differences);
        Assert.NotEmpty(result.Matches);
        Assert.Contains(result.Matches, m => m.SpecificationKey == "Brand");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "RAM");
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleNumericComparisons()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "RAM_GB", 8 },
            { "Storage_GB", 256 },
            { "Price", 599.99m }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "RAM_GB", 16 },
            { "Storage_GB", 512 },
            { "Price", 799.99m }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Differences, diff => 
        {
            Assert.True(diff.ComparisonResult == ComparisonResultType.Product1Better || 
                       diff.ComparisonResult == ComparisonResultType.Product2Better);
        });
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleBooleanComparisons()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "HasWiFi", true },
            { "HasBluetooth", false },
            { "IsWaterproof", true }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "HasWiFi", true },
            { "HasBluetooth", true },
            { "IsWaterproof", false }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Matches, m => m.SpecificationKey == "HasWiFi");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "HasBluetooth");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "IsWaterproof");
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleStringComparisons()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "Color", "Black" },
            { "Brand", "SameBrand" },
            { "Model", "Model1" }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "Color", "White" },
            { "Brand", "SameBrand" },
            { "Model", "Model2" }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Matches, m => m.SpecificationKey == "Brand");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "Color");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "Model");
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldApplyCustomWeights()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "Performance_Score", 85 },
            { "Design_Score", 90 },
            { "Price_Score", 70 }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "Performance_Score", 90 },
            { "Design_Score", 85 },
            { "Price_Score", 75 }
        });

        var customWeights = new Dictionary<string, decimal>
        {
            { "Performance", 0.6m },
            { "Design", 0.3m },
            { "Price", 0.1m }
        };

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2, customWeights);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.CategoryScores);
        
        if (result.CategoryScores.ContainsKey("Performance"))
        {
            Assert.Equal(0.6m, result.CategoryScores["Performance"].Weight);
        }
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleEmptySpecifications()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>());
        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>());
        
        // Setup to return no mappings
        _productSellerMappingRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductSellerMapping, bool>>>()))
            .ReturnsAsync(new List<ProductSellerMapping>());

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Differences);
        Assert.Empty(result.Matches);
        Assert.Empty(result.CategoryScores);
    }
    
    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldFallbackToMappingSpecifications_WhenProductSpecificationsEmpty()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>());
        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>());
        product1.ProductId = productId1;
        product2.ProductId = productId2;
        
        var mappingSpecs1 = new Dictionary<string, object>
        {
            { "RAM", "8GB" },
            { "Storage", "256GB SSD" }
        };
        
        var mappingSpecs2 = new Dictionary<string, object>
        {
            { "RAM", "16GB" },
            { "Storage", "512GB SSD" }
        };
        
        // Mock ProductSellerMapping repository to return mappings with specifications
        var mapping1 = new ProductSellerMapping 
        { 
            MappingId = Guid.NewGuid(),
            CanonicalProductId = productId1,
            LatestSpecifications = JsonSerializer.Serialize(mappingSpecs1),
            SpecificationsQualityScore = 0.8,
            SpecificationsLastUpdated = DateTime.UtcNow
        };
        
        var mapping2 = new ProductSellerMapping 
        { 
            MappingId = Guid.NewGuid(),
            CanonicalProductId = productId2,
            LatestSpecifications = JsonSerializer.Serialize(mappingSpecs2),
            SpecificationsQualityScore = 0.9,
            SpecificationsLastUpdated = DateTime.UtcNow
        };
        
        _productSellerMappingRepositoryMock
            .Setup(r => r.FindAsync(It.Is<System.Linq.Expressions.Expression<Func<ProductSellerMapping, bool>>>(
                expr => expr.Compile().Invoke(mapping1))))
            .ReturnsAsync(new List<ProductSellerMapping> { mapping1 });
            
        _productSellerMappingRepositoryMock
            .Setup(r => r.FindAsync(It.Is<System.Linq.Expressions.Expression<Func<ProductSellerMapping, bool>>>(
                expr => expr.Compile().Invoke(mapping2))))
            .ReturnsAsync(new List<ProductSellerMapping> { mapping2 });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Differences);
        Assert.Contains(result.Differences, d => d.SpecificationKey == "RAM");
        Assert.Contains(result.Differences, d => d.SpecificationKey == "Storage");
        
        // Verify both repositories were queried
        _productSellerMappingRepositoryMock.Verify(
            r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductSellerMapping, bool>>>()), 
            Times.Exactly(2));
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleMissingSpecifications()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "RAM", "8GB" },
            { "Storage", "256GB" },
            { "GPU", "GTX 1060" }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "RAM", "16GB" },
            { "CPU", "Intel i7" }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Differences, d => d.SpecificationKey == "RAM");
        Assert.Contains(result.Differences, d => d.ComparisonResult == ComparisonResultType.Product1Only);
        Assert.Contains(result.Differences, d => d.ComparisonResult == ComparisonResultType.Product2Only);
    }

    #endregion

    #region CalculateOverallScoresAsync Tests

    [Fact]
    public async Task CalculateOverallScoresAsync_ShouldReturnScores_WhenValidComparison()
    {
        // Arrange
        var comparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "Performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.8m,
                        Product2Score = 0.9m,
                        Weight = 0.5m
                    }
                },
                {
                    "Design",
                    new CategoryScoreDto
                    {
                        CategoryName = "Design",
                        Product1Score = 0.9m,
                        Product2Score = 0.7m,
                        Weight = 0.3m
                    }
                },
                {
                    "Value",
                    new CategoryScoreDto
                    {
                        CategoryName = "Value",
                        Product1Score = 0.7m,
                        Product2Score = 0.8m,
                        Weight = 0.2m
                    }
                }
            }
        };

        // Act
        var (product1Score, product2Score) = await _engine.CalculateOverallScoresAsync(comparison);

        // Assert
        Assert.True(product1Score >= 0 && product1Score <= 1);
        Assert.True(product2Score >= 0 && product2Score <= 1);
        
        // Expected calculation: 
        // P1: (0.8 * 0.5) + (0.9 * 0.3) + (0.7 * 0.2) = 0.4 + 0.27 + 0.14 = 0.81
        // P2: (0.9 * 0.5) + (0.7 * 0.3) + (0.8 * 0.2) = 0.45 + 0.21 + 0.16 = 0.82
        Assert.Equal(0.81m, product1Score, 2);
        Assert.Equal(0.82m, product2Score, 2);
    }

    [Fact]
    public async Task CalculateOverallScoresAsync_ShouldReturnZero_WhenEmptyComparison()
    {
        // Arrange
        var comparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>()
        };

        // Act
        var (product1Score, product2Score) = await _engine.CalculateOverallScoresAsync(comparison);

        // Assert
        Assert.Equal(0m, product1Score);
        Assert.Equal(0m, product2Score);
    }

    [Theory]
    [InlineData(1.0, 0.5)]
    [InlineData(0.3, 1.0)]
    [InlineData(0.0, 0.0)]
    public async Task CalculateOverallScoresAsync_ShouldHandleEdgeCaseScores(decimal score1, decimal score2)
    {
        // Arrange
        var comparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "Test",
                    new CategoryScoreDto
                    {
                        CategoryName = "Test",
                        Product1Score = score1,
                        Product2Score = score2,
                        Weight = 1.0m
                    }
                }
            }
        };

        // Act
        var (product1Score, product2Score) = await _engine.CalculateOverallScoresAsync(comparison);

        // Assert
        Assert.Equal(score1, product1Score);
        Assert.Equal(score2, product2Score);
    }

    #endregion

    #region GetDefaultSpecificationWeightsAsync Tests

    [Fact]
    public async Task GetDefaultSpecificationWeightsAsync_ShouldReturnWeights_ForKnownCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var weights = await _engine.GetDefaultSpecificationWeightsAsync(categoryId);

        // Assert
        Assert.NotNull(weights);
        Assert.NotEmpty(weights);
        Assert.All(weights.Values, weight => Assert.True(weight >= 0 && weight <= 1));
        
        // Check that weights sum to approximately 1
        var totalWeight = weights.Values.Sum();
        Assert.True(Math.Abs(totalWeight - 1.0m) < 0.01m);
    }

    [Fact]
    public async Task GetDefaultSpecificationWeightsAsync_ShouldReturnConsistentWeights()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var weights1 = await _engine.GetDefaultSpecificationWeightsAsync(categoryId);
        var weights2 = await _engine.GetDefaultSpecificationWeightsAsync(categoryId);

        // Assert
        Assert.Equal(weights1.Count, weights2.Count);
        foreach (var kvp in weights1)
        {
            Assert.True(weights2.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, weights2[kvp.Key]);
        }
    }

    [Fact]
    public async Task GetDefaultSpecificationWeightsAsync_ShouldIncludeCommonCategories()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        // Act
        var weights = await _engine.GetDefaultSpecificationWeightsAsync(categoryId);

        // Assert
        Assert.NotNull(weights);
        
        // Should include common specification categories
        var expectedCategories = new[] { "Performance", "Design", "Features", "Value", "Build" };
        var hasCommonCategories = expectedCategories.Any(cat => 
            weights.Keys.Any(key => key.Contains(cat, StringComparison.OrdinalIgnoreCase)));
        
        Assert.True(hasCommonCategories);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleNullSpecifications()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", null);
        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object> { { "Test", "Value" } });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Differences, d => d.ComparisonResult == ComparisonResultType.Product2Only);
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleNullValues()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "Feature1", "Value1" },
            { "Feature2", null! }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "Feature1", "Value2" },
            { "Feature2", "Value2" }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Differences);
    }

    [Fact]
    public async Task AnalyzeSpecificationsAsync_ShouldHandleMixedTypes()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "StringSpec", "Text" },
            { "IntSpec", 42 },
            { "DecimalSpec", 3.14m },
            { "BoolSpec", true },
            { "DateSpec", DateTimeOffset.Now }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "StringSpec", "Different Text" },
            { "IntSpec", 84 },
            { "DecimalSpec", 2.71m },
            { "BoolSpec", false },
            { "DateSpec", DateTimeOffset.Now.AddDays(-1) }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Differences);
        Assert.All(result.Differences, diff => 
            Assert.True(Enum.IsDefined(typeof(ComparisonResultType), diff.ComparisonResult)));
    }

    [Theory]
    [InlineData("8GB", "16GB")]
    [InlineData("256GB SSD", "512GB HDD")]
    [InlineData("Intel i5-8400", "AMD Ryzen 5 3600")]
    public async Task AnalyzeSpecificationsAsync_ShouldParseComplexStrings(string spec1, string spec2)
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1", new Dictionary<string, object>
        {
            { "ComplexSpec", spec1 }
        });

        var product2 = CreateTestProduct("Product 2", new Dictionary<string, object>
        {
            { "ComplexSpec", spec2 }
        });

        // Act
        var result = await _engine.AnalyzeSpecificationsAsync(product1, product2);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Differences, d => d.SpecificationKey == "ComplexSpec");
    }

    #endregion

    #region Helper Methods

    private ProductDto CreateTestProduct(string name, Dictionary<string, object>? specifications)
    {
        return new ProductDto
        {
            ProductId = Guid.NewGuid(),
            Name = name,
            CategoryId = Guid.NewGuid(),
            Description = $"Test description for {name}",
            Specifications = specifications ?? new Dictionary<string, object>(),
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
            }
        };
    }

    #endregion
}