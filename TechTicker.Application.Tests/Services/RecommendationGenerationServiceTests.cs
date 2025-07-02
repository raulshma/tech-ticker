using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

/// <summary>
/// Comprehensive unit tests for RecommendationGenerationService
/// Tests AI-powered recommendation generation and contextual analysis
/// </summary>
public class RecommendationGenerationServiceTests
{
    private readonly Mock<IAiGenerationService> _aiGenerationServiceMock;
    private readonly Mock<ILogger<RecommendationGenerationService>> _loggerMock;
    private readonly RecommendationGenerationService _service;

    public RecommendationGenerationServiceTests()
    {
        _aiGenerationServiceMock = new Mock<IAiGenerationService>();
        _loggerMock = new Mock<ILogger<RecommendationGenerationService>>();
        _service = new RecommendationGenerationService(
            _aiGenerationServiceMock.Object,
            _loggerMock.Object);
    }

    #region GenerateRecommendationAsync Tests

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldReturnRecommendation_WhenValidInputs()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();

        // Setup AI service to be unavailable to test fallback logic
        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false));

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RecommendedProductId == product1.ProductId.ToString() || 
                   result.RecommendedProductId == product2.ProductId.ToString());
        Assert.True(result.ConfidenceScore >= 0 && result.ConfidenceScore <= 1);
        Assert.NotNull(result.PrimaryReason);
        Assert.NotEmpty(result.Factors);
        Assert.NotEmpty(result.Pros);
        Assert.NotEmpty(result.Cons);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldUseAiWhenAvailable()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();

        var aiResponse = new GenericAiResponseDto
        {
            Response = "Product 2 is recommended due to better performance and value.",
            TokensUsed = 50
        };

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(true));

        _aiGenerationServiceMock
            .Setup(x => x.GenerateGenericResponseAsync(It.IsAny<GenericAiRequestDto>()))
            .ReturnsAsync(Result<GenericAiResponseDto>.Success(aiResponse));

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.PrimaryReason);
        _aiGenerationServiceMock.Verify(x => x.GenerateGenericResponseAsync(It.IsAny<GenericAiRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldFallbackToBasicLogic_WhenAiServiceFails()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(true));

        _aiGenerationServiceMock
            .Setup(x => x.GenerateGenericResponseAsync(It.IsAny<GenericAiRequestDto>()))
            .ThrowsAsync(new Exception("AI service unavailable"));

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RecommendedProductId == product1.ProductId.ToString() || 
                   result.RecommendedProductId == product2.ProductId.ToString());
        Assert.True(result.ConfidenceScore > 0);
        Assert.NotNull(result.PrimaryReason);
        Assert.NotEmpty(result.Factors);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldRecommendBetterSpecProduct_WhenSignificantDifference()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.3m,
                        Product2Score = 0.9m, // Significantly better
                        Weight = 1.0m
                    }
                }
            },
            Differences = new List<SpecificationDifferenceDto>
            {
                new SpecificationDifferenceDto
                {
                    SpecificationKey = "Performance",
                    ComparisonResult = ComparisonResultType.Product2Better
                }
            },
            Matches = new List<SpecificationMatchDto>()
        };
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product2.ProductId.ToString(), result.RecommendedProductId);
        Assert.Contains("superior", result.PrimaryReason.ToLower());
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldHandleNullPriceAnalysis()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        PriceAnalysisDto? priceAnalysis = null;

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RecommendedProductId == product1.ProductId.ToString() || 
                   result.RecommendedProductId == product2.ProductId.ToString());
        Assert.True(result.ConfidenceScore > 0);
        Assert.NotNull(result.PrimaryReason);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldIncludeRelevantFactors()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Factors);
        
        var factorNames = result.Factors.Select(f => f.Factor).ToList();
        Assert.Contains(factorNames, name => name.Contains("Performance") || name.Contains("Price") || name.Contains("Value"));
    }

    #endregion

    #region GenerateContextualRecommendationAsync Tests

    [Fact]
    public async Task GenerateContextualRecommendationAsync_ShouldReturnContextualRecommendation_WhenValidInputs()
    {
        // Arrange
        var product1 = CreateTestProductDto("Gaming Laptop");
        var product2 = CreateTestProductDto("Business Laptop");
        var context = "gaming";

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateContextualRecommendationAsync(product1, product2, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.RecommendedProductId == product1.ProductId.ToString() || 
                   result.RecommendedProductId == product2.ProductId.ToString());
        Assert.Equal(context, result.UseCase);
        Assert.NotNull(result.AlternativeRecommendation);
    }

    [Fact]
    public async Task GenerateContextualRecommendationAsync_ShouldUseAiWhenAvailable()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var context = "business";

        var aiResponse = new GenericAiResponseDto
        {
            Response = "For business use, Product 1 is recommended due to better productivity features.",
            TokensUsed = 50
        };

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(true));

        _aiGenerationServiceMock
            .Setup(x => x.GenerateGenericResponseAsync(It.IsAny<GenericAiRequestDto>()))
            .ReturnsAsync(Result<GenericAiResponseDto>.Success(aiResponse));

        // Act
        var result = await _service.GenerateContextualRecommendationAsync(product1, product2, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(context, result.UseCase);
        _aiGenerationServiceMock.Verify(x => x.GenerateGenericResponseAsync(It.IsAny<GenericAiRequestDto>()), Times.Once);
    }

    [Theory]
    [InlineData("gaming")]
    [InlineData("productivity")]
    [InlineData("budget")]
    public async Task GenerateContextualRecommendationAsync_ShouldHandleDifferentContexts(string context)
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateContextualRecommendationAsync(product1, product2, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(context, result.UseCase);
        Assert.Contains(context, result.PrimaryReason.ToLower());
    }

    [Fact]
    public async Task GenerateContextualRecommendationAsync_ShouldHandleEmptyContext()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var context = "";

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateContextualRecommendationAsync(product1, product2, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(context, result.UseCase);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldHandleEmptySpecificationComparison()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var emptyComparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>(),
            Differences = new List<SpecificationDifferenceDto>(),
            Matches = new List<SpecificationMatchDto>()
        };
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, emptyComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore >= 0 && result.ConfidenceScore <= 1);
        Assert.NotNull(result.PrimaryReason);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldHandleIdenticalProducts()
    {
        // Arrange
        var product1 = CreateTestProductDto("Identical Product");
        var product2 = CreateTestProductDto("Identical Product");
        var specificationComparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.8m,
                        Product2Score = 0.8m, // Identical scores
                        Weight = 1.0m
                    }
                }
            },
            Differences = new List<SpecificationDifferenceDto>(),
            Matches = new List<SpecificationMatchDto>()
        };
        var priceAnalysis = new PriceAnalysisDto
        {
            Summary = new PriceComparisonSummaryDto
            {
                Product1LowestPrice = 100m,
                Product2LowestPrice = 100m, // Same price
                LowerPricedProduct = "Equal"
            },
            ValueAnalysis = new ValueAnalysisDto
            {
                Product1ValueScore = 80m,
                Product2ValueScore = 80m, // Same value
                BetterValueProduct = "Equal",
                ValueAnalysisReason = "Similar value proposition"
            },
            SellerComparisons = new List<SellerPriceComparisonDto>()
        };

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore < 0.8m); // Should have lower confidence for similar products
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldHandleExtremeValues()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0m, // Minimum
                        Product2Score = 1m, // Maximum
                        Weight = 1.0m
                    }
                }
            },
            Differences = new List<SpecificationDifferenceDto>
            {
                new SpecificationDifferenceDto
                {
                    SpecificationKey = "Performance",
                    ComparisonResult = ComparisonResultType.Product2Better
                }
            },
            Matches = new List<SpecificationMatchDto>()
        };
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product2.ProductId.ToString(), result.RecommendedProductId);
        Assert.True(result.ConfidenceScore > 0.6m); // Should have good confidence for clear winner
    }

    [Fact]
    public async Task GenerateContextualRecommendationAsync_ShouldHandleNullContext()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        string? context = null;

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateContextualRecommendationAsync(product1, product2, context!);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GenerateRecommendationAsync_ShouldSetAppropriateConfidenceScore()
    {
        // Arrange
        var product1 = CreateTestProductDto("Product 1");
        var product2 = CreateTestProductDto("Product 2");
        var specificationComparison = new SpecificationComparisonDto
        {
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.3m,
                        Product2Score = 0.9m, // Clear winner
                        Weight = 1.0m
                    }
                }
            },
            Differences = new List<SpecificationDifferenceDto>
            {
                new SpecificationDifferenceDto
                {
                    SpecificationKey = "Performance",
                    ComparisonResult = ComparisonResultType.Product2Better
                }
            },
            Matches = new List<SpecificationMatchDto>()
        };
        var priceAnalysis = CreateTestPriceAnalysis();

        _aiGenerationServiceMock
            .Setup(x => x.IsAiConfigurationAvailableAsync())
            .ReturnsAsync(Result<bool>.Success(false)); // Force fallback

        // Act
        var result = await _service.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore >= 0.6m); // Should have reasonable confidence for clear difference
        Assert.True(result.ConfidenceScore <= 1.0m);
    }

    #endregion

    #region Helper Methods

    private ProductDto CreateTestProductDto(string name)
    {
        return new ProductDto
        {
            ProductId = Guid.NewGuid(),
            Name = name,
            CategoryId = Guid.NewGuid(),
            Description = $"Test description for {name}",
            Specifications = new Dictionary<string, object>
            {
                { "Brand", "Test Brand" },
                { "Model", "Test Model" },
                { "Performance_Score", 85 }
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
            }
        };
    }

    private SpecificationComparisonDto CreateTestSpecificationComparison()
    {
        return new SpecificationComparisonDto
        {
            Differences = new List<SpecificationDifferenceDto>
            {
                new SpecificationDifferenceDto
                {
                    SpecificationKey = "Performance",
                    DisplayName = "Performance Score",
                    Category = "Performance",
                    Product1Value = 80,
                    Product2Value = 90,
                    ComparisonResult = ComparisonResultType.Product2Better,
                    ImpactScore = 0.7m
                }
            },
            Matches = new List<SpecificationMatchDto>
            {
                new SpecificationMatchDto
                {
                    SpecificationKey = "Brand",
                    DisplayName = "Brand",
                    Category = "General",
                    Value = "Test Brand"
                }
            },
            CategoryScores = new Dictionary<string, CategoryScoreDto>
            {
                {
                    "performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.8m,
                        Product2Score = 0.9m,
                        Weight = 0.6m
                    }
                }
            }
        };
    }

    private PriceAnalysisDto CreateTestPriceAnalysis()
    {
        return new PriceAnalysisDto
        {
            Summary = new PriceComparisonSummaryDto
            {
                Product1LowestPrice = 100m,
                Product2LowestPrice = 120m,
                PriceDifference = 20m,
                PriceDifferencePercentage = 20m,
                LowerPricedProduct = "Product1",
                Product1SellerCount = 2,
                Product2SellerCount = 3
            },
            SellerComparisons = new List<SellerPriceComparisonDto>(),
            ValueAnalysis = new ValueAnalysisDto
            {
                Product1ValueScore = 85m,
                Product2ValueScore = 75m,
                BetterValueProduct = "Product1",
                ValueAnalysisReason = "Better price-to-performance ratio"
            }
        };
    }

    #endregion
}