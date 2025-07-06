using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

/// <summary>
/// Comprehensive unit tests for ProductComparisonService
/// Tests all methods with success scenarios, error scenarios, and edge cases
/// </summary>
public class ProductComparisonServiceTests
{
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<ISpecificationAnalysisEngine> _specificationAnalysisEngineMock;
    private readonly Mock<IPriceAnalysisService> _priceAnalysisServiceMock;
    private readonly Mock<IRecommendationGenerationService> _recommendationGenerationServiceMock;
    private readonly Mock<ILogger<ProductComparisonService>> _loggerMock;
    private readonly ProductComparisonService _service;

    public ProductComparisonServiceTests()
    {
        _productServiceMock = new Mock<IProductService>();
        _specificationAnalysisEngineMock = new Mock<ISpecificationAnalysisEngine>();
        _priceAnalysisServiceMock = new Mock<IPriceAnalysisService>();
        _recommendationGenerationServiceMock = new Mock<IRecommendationGenerationService>();
        _loggerMock = new Mock<ILogger<ProductComparisonService>>();

        _service = new ProductComparisonService(
            _productServiceMock.Object,
            _specificationAnalysisEngineMock.Object,
            _priceAnalysisServiceMock.Object,
            _recommendationGenerationServiceMock.Object,
            _loggerMock.Object);
    }

    #region CompareProductsAsync Tests

    [Fact]
    public async Task CompareProductsAsync_ShouldReturnSuccess_WhenValidProductsFromSameCategory()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2,
            IncludePriceAnalysis = true,
            GenerateRecommendations = true
        };

        var product1 = CreateTestProductWithPrices(productId1, categoryId, "Product 1");
        var product2 = CreateTestProductWithPrices(productId2, categoryId, "Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();
        var recommendation = CreateTestRecommendation();

        SetupSuccessfulMocks(product1, product2, specificationComparison, priceAnalysis, recommendation);

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Category", result.Data.Summary.CategoryName);
        Assert.NotNull(result.Data.PriceAnalysis);
        Assert.NotNull(result.Data.RecommendationAnalysis);
        Assert.True(result.Data.GeneratedAt > DateTimeOffset.UtcNow.AddMinutes(-1));

        VerifyMockInteractions(productId1, productId2, true, true);
    }

    [Fact]
    public async Task CompareProductsAsync_ShouldReturnSuccess_WithoutPriceAnalysis_WhenDisabled()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2,
            IncludePriceAnalysis = false,
            GenerateRecommendations = true
        };

        var product1 = CreateTestProductWithPrices(productId1, categoryId, "Product 1");
        var product2 = CreateTestProductWithPrices(productId2, categoryId, "Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var recommendation = CreateTestRecommendation();

        SetupSuccessfulMocks(product1, product2, specificationComparison, null, recommendation);

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data.PriceAnalysis);
        Assert.NotNull(result.Data.RecommendationAnalysis);

        _priceAnalysisServiceMock.Verify(x => x.AnalyzePricesAsync(It.IsAny<ProductWithCurrentPricesDto>(), It.IsAny<ProductWithCurrentPricesDto>()), Times.Never);
    }

    [Fact]
    public async Task CompareProductsAsync_ShouldReturnSuccess_WithoutRecommendations_WhenDisabled()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2,
            IncludePriceAnalysis = true,
            GenerateRecommendations = false
        };

        var product1 = CreateTestProductWithPrices(productId1, categoryId, "Product 1");
        var product2 = CreateTestProductWithPrices(productId2, categoryId, "Product 2");
        var specificationComparison = CreateTestSpecificationComparison();
        var priceAnalysis = CreateTestPriceAnalysis();

        SetupSuccessfulMocks(product1, product2, specificationComparison, priceAnalysis, null);

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PriceAnalysis);
        Assert.Null(result.Data.RecommendationAnalysis);

        _recommendationGenerationServiceMock.Verify(x => x.GenerateRecommendationAsync(It.IsAny<ProductDto>(), It.IsAny<ProductDto>(), It.IsAny<SpecificationComparisonDto>(), It.IsAny<PriceAnalysisDto?>()), Times.Never);
    }

    [Fact]
    public async Task CompareProductsAsync_ShouldReturnFailure_WhenProductsFromDifferentCategories()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2
        };

        var productDto1 = CreateTestProductDto(productId1, Guid.NewGuid(), "Product 1");
        var productDto2 = CreateTestProductDto(productId2, Guid.NewGuid(), "Product 2");

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId1))
            .ReturnsAsync(Result<ProductDto>.Success(productDto1));
        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId2))
            .ReturnsAsync(Result<ProductDto>.Success(productDto2));

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("same category", result.ErrorMessage);
    }

    [Fact]
    public async Task CompareProductsAsync_ShouldReturnFailure_WhenFirstProductNotFound()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2
        };

        var productDto1 = CreateTestProductDto(productId1, Guid.NewGuid(), "Product 1");

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId1))
            .ReturnsAsync(Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND"));
        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId2))
            .ReturnsAsync(Result<ProductDto>.Success(productDto1));

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Product not found", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CompareProductsAsync_ShouldHandleEdgeCases_WithNullOrEmptyProductNames(string? productName)
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId1,
            ProductId2 = productId2
        };

        var product1 = CreateTestProductWithPrices(productId1, categoryId, productName);
        var product2 = CreateTestProductWithPrices(productId2, categoryId, "Product 2");
        var specificationComparison = CreateTestSpecificationComparison();

        SetupSuccessfulMocks(product1, product2, specificationComparison, null, null);

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    #endregion

    #region ValidateProductsForComparisonAsync Tests

    [Fact]
    public async Task ValidateProductsForComparisonAsync_ShouldReturnSuccess_WhenProductsValid()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product1 = CreateTestProductDto(productId1, categoryId, "Product 1");
        var product2 = CreateTestProductDto(productId2, categoryId, "Product 2");

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId1))
            .ReturnsAsync(Result<ProductDto>.Success(product1));
        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId2))
            .ReturnsAsync(Result<ProductDto>.Success(product2));

        // Act
        var result = await _service.ValidateProductsForComparisonAsync(productId1, productId2);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task ValidateProductsForComparisonAsync_ShouldReturnFailure_WhenSameProductId()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = await _service.ValidateProductsForComparisonAsync(productId, productId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("same product", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateProductsForComparisonAsync_ShouldReturnFailure_WhenFirstProductNotFound()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId1))
            .ReturnsAsync(Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        // Act
        var result = await _service.ValidateProductsForComparisonAsync(productId1, productId2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    #endregion

    #region GetComparableProductsAsync Tests

    [Fact]
    public async Task GetComparableProductsAsync_ShouldReturnProducts_WhenValidCategory()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var search = "test";
        var pageNumber = 1;
        var pageSize = 10;

        var testProduct = CreateTestProductDto(productId, categoryId, "Test Product");
        var comparableProducts = new List<ProductDto>
        {
            CreateTestProductDto(Guid.NewGuid(), categoryId, "Product 1"),
            CreateTestProductDto(Guid.NewGuid(), categoryId, "Product 2"),
            CreateTestProductDto(Guid.NewGuid(), categoryId, "Product 3")
        };

        var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
            comparableProducts, pageNumber, pageSize, comparableProducts.Count);

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(Result<ProductDto>.Success(testProduct));
        _productServiceMock.Setup(x => x.GetProductsAsync(categoryId, search, pageNumber, pageSize + 1))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(pagedResponse));

        // Act
        var result = await _service.GetComparableProductsAsync(productId, search, pageNumber, pageSize);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.Data!.Any());
    }

    [Fact]
    public async Task GetComparableProductsAsync_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(Result<ProductDto>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        // Act
        var result = await _service.GetComparableProductsAsync(productId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task GetComparableProductsAsync_ShouldReturnEmpty_WhenNoComparableProducts()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var testProduct = CreateTestProductDto(productId, categoryId, "Test Product");

        var emptyResponse = PagedResponse<ProductDto>.SuccessResult(
            new List<ProductDto>(), 1, 10, 0);

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(Result<ProductDto>.Success(testProduct));
        _productServiceMock.Setup(x => x.GetProductsAsync(categoryId, null, 1, 11))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(emptyResponse));

        // Act
        var result = await _service.GetComparableProductsAsync(productId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!.Data!);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task CompareProductsAsync_ShouldHandleUnexpectedException()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        _productServiceMock.Setup(x => x.GetProductByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected database error"));

        // Act
        var result = await _service.CompareProductsAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Validation failed: Unexpected database error", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateProductsForComparisonAsync_ShouldHandleException()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        _productServiceMock.Setup(x => x.GetProductByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new ArgumentException("Invalid argument"));

        // Act
        var result = await _service.ValidateProductsForComparisonAsync(productId1, productId2);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Validation failed", result.ErrorMessage);
    }

    #endregion

    #region Helper Methods

    private ProductDto CreateTestProductDto(Guid id, Guid categoryId, string? name = null)
    {
        return new ProductDto
        {
            ProductId = id,
            Name = name ?? "Test Product",
            CategoryId = categoryId,
            Description = "Test Description",
            UncategorizedSpecifications = new Dictionary<string, string>
            {
                { "Brand", "Test Brand" },
                { "Model", "Test Model" },
                { "Price", "100.00" }
            },
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Category = new CategoryDto
            {
                CategoryId = categoryId,
                Name = "Test Category",
                Slug = "test-category",
                Description = "Test Category Description",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
    }

    private ProductWithCurrentPricesDto CreateTestProductWithPrices(Guid id, Guid categoryId, string? name = null)
    {
        var baseProduct = CreateTestProductDto(id, categoryId, name);
        return new ProductWithCurrentPricesDto
        {
            ProductId = baseProduct.ProductId,
            Name = baseProduct.Name,
            CategoryId = baseProduct.CategoryId,
            Description = baseProduct.Description,
            NormalizedSpecifications = baseProduct.NormalizedSpecifications,
            UncategorizedSpecifications = baseProduct.UncategorizedSpecifications,
            IsActive = baseProduct.IsActive,
            CreatedAt = baseProduct.CreatedAt,
            UpdatedAt = baseProduct.UpdatedAt,
            Category = baseProduct.Category,
            CurrentPrices = new List<CurrentPriceDto>
            {
                new CurrentPriceDto
                {
                    SellerName = "Test Seller",
                    Price = 100.00m,
                    StockStatus = "In Stock",
                    SourceUrl = "https://test-seller.com/product",
                    LastUpdated = DateTimeOffset.UtcNow,
                    ScrapedProductNameOnPage = name ?? "Test Product"
                }
            },
            LowestCurrentPrice = 100.00m,
            HighestCurrentPrice = 100.00m,
            AvailableSellersCount = 1
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
                    SpecificationKey = "RAM",
                    DisplayName = "Memory",
                    Category = "Performance",
                    Product1Value = "8GB",
                    Product2Value = "16GB",
                    ComparisonResult = ComparisonResultType.Product2Better,
                    ImpactScore = 0.8m
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
                    "Performance",
                    new CategoryScoreDto
                    {
                        CategoryName = "Performance",
                        Product1Score = 0.7m,
                        Product2Score = 0.9m,
                        Weight = 0.5m
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
                Product1ValueScore = 0.8m,
                Product2ValueScore = 0.75m,
                BetterValueProduct = "Product1",
                ValueAnalysisReason = "Better price-to-performance ratio"
            }
        };
    }

    private RecommendationAnalysisDto CreateTestRecommendation()
    {
        return new RecommendationAnalysisDto
        {
            RecommendedProductId = Guid.NewGuid().ToString(),
            ConfidenceScore = 0.85m,
            PrimaryReason = "Better value for money",
            Factors = new List<RecommendationFactorDto>(),
            Pros = new List<string> { "Lower price", "Good performance" },
            Cons = new List<string> { "Less RAM" },
            UseCase = "Budget-conscious users"
        };
    }

    private void SetupSuccessfulMocks(ProductWithCurrentPricesDto product1, ProductWithCurrentPricesDto product2, 
        SpecificationComparisonDto specificationComparison, 
        PriceAnalysisDto? priceAnalysis, 
        RecommendationAnalysisDto? recommendation)
    {
        // Setup validation calls
        _productServiceMock.Setup(x => x.GetProductByIdAsync(product1.ProductId))
            .ReturnsAsync(Result<ProductDto>.Success(product1));
        _productServiceMock.Setup(x => x.GetProductByIdAsync(product2.ProductId))
            .ReturnsAsync(Result<ProductDto>.Success(product2));

        // Setup product with prices calls
        _productServiceMock.Setup(x => x.GetProductWithCurrentPricesAsync(product1.ProductId))
            .ReturnsAsync(Result<ProductWithCurrentPricesDto>.Success(product1));
        _productServiceMock.Setup(x => x.GetProductWithCurrentPricesAsync(product2.ProductId))
            .ReturnsAsync(Result<ProductWithCurrentPricesDto>.Success(product2));

        _specificationAnalysisEngineMock.Setup(x => x.AnalyzeSpecificationsAsync(product1, product2, It.IsAny<Dictionary<string, decimal>?>()))
            .ReturnsAsync(specificationComparison);

        _specificationAnalysisEngineMock.Setup(x => x.CalculateOverallScoresAsync(specificationComparison))
            .ReturnsAsync((0.8m, 0.9m));

        if (priceAnalysis != null)
        {
            _priceAnalysisServiceMock.Setup(x => x.AnalyzePricesAsync(product1, product2))
                .ReturnsAsync(priceAnalysis);
        }

        if (recommendation != null)
        {
            _recommendationGenerationServiceMock.Setup(x => x.GenerateRecommendationAsync(product1, product2, specificationComparison, priceAnalysis))
                .ReturnsAsync(recommendation);
        }
    }

    private void VerifyMockInteractions(Guid productId1, Guid productId2, bool includePriceAnalysis, bool generateRecommendations)
    {
        _productServiceMock.Verify(x => x.GetProductByIdAsync(productId1), Times.Once);
        _productServiceMock.Verify(x => x.GetProductByIdAsync(productId2), Times.Once);
        _productServiceMock.Verify(x => x.GetProductWithCurrentPricesAsync(productId1), Times.Once);
        _productServiceMock.Verify(x => x.GetProductWithCurrentPricesAsync(productId2), Times.Once);
        _specificationAnalysisEngineMock.Verify(x => x.AnalyzeSpecificationsAsync(It.IsAny<ProductWithCurrentPricesDto>(), It.IsAny<ProductWithCurrentPricesDto>(), It.IsAny<Dictionary<string, decimal>?>()), Times.Once);

        if (includePriceAnalysis)
        {
            _priceAnalysisServiceMock.Verify(x => x.AnalyzePricesAsync(It.IsAny<ProductWithCurrentPricesDto>(), It.IsAny<ProductWithCurrentPricesDto>()), Times.Once);
        }

        if (generateRecommendations)
        {
            _recommendationGenerationServiceMock.Verify(x => x.GenerateRecommendationAsync(It.IsAny<ProductDto>(), It.IsAny<ProductDto>(), It.IsAny<SpecificationComparisonDto>(), It.IsAny<PriceAnalysisDto?>()), Times.Once);
        }
    }

    #endregion
}