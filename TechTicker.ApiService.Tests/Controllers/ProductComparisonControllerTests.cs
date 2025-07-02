using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.ApiService.Controllers;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.ApiService.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for ProductComparisonController
/// Tests all endpoints with various success and error scenarios
/// </summary>
public class ProductComparisonControllerTests
{
    private readonly Mock<IProductComparisonService> _productComparisonServiceMock;
    private readonly Mock<ILogger<ProductComparisonController>> _loggerMock;
    private readonly ProductComparisonController _controller;

    public ProductComparisonControllerTests()
    {
        _productComparisonServiceMock = new Mock<IProductComparisonService>();
        _loggerMock = new Mock<ILogger<ProductComparisonController>>();
        _controller = new ProductComparisonController(_productComparisonServiceMock.Object);

        // Setup controller context for proper HTTP responses
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region CompareProducts Tests

    [Fact]
    public async Task CompareProducts_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        var comparisonResult = CreateTestComparisonResult();
        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Success(comparisonResult));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResultDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(comparisonResult, apiResponse.Data);
    }

    [Fact]
    public async Task CompareProducts_ShouldReturnBadRequest_WhenInvalidModelState()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.Empty, // Invalid
            ProductId2 = Guid.NewGuid()
        };

        _controller.ModelState.AddModelError("ProductId1", "ProductId1 cannot be empty.");

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("ProductId1 cannot be empty", apiResponse.Message);
    }

    [Fact]
    public async Task CompareProducts_ShouldReturnBadRequest_WhenSameProducts()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CompareProductsRequestDto
        {
            ProductId1 = productId,
            ProductId2 = productId // Same product
        };

        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Failure("Cannot compare a product with itself", "INVALID_INPUT"));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Cannot compare a product with itself", apiResponse.Message);
    }

    [Fact]
    public async Task CompareProducts_ShouldReturnNotFound_WhenProductNotFound()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Product not found", apiResponse.Message);
    }

    [Fact]
    public async Task CompareProducts_ShouldReturnBadRequest_WhenDifferentCategories()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Failure("Products must be from the same category", "CATEGORY_MISMATCH"));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("same category", apiResponse.Message);
    }

    [Fact]
    public async Task CompareProducts_ShouldReturnInternalServerError_WhenUnexpectedError()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Failure("Internal server error", "INTERNAL_ERROR"));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse>(internalServerErrorResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Internal server error", apiResponse.Message);
    }

    [Fact]
    public async Task CompareProducts_ShouldHandleCustomWeights()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid(),
            SpecificationWeights = new Dictionary<string, decimal>
            {
                { "Performance", 0.6m },
                { "Price", 0.4m }
            }
        };

        var comparisonResult = CreateTestComparisonResult();
        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ReturnsAsync(Result<ProductComparisonResultDto>.Success(comparisonResult));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<ProductComparisonResultDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        _productComparisonServiceMock.Verify(x => x.CompareProductsAsync(request), Times.Once);
    }

    [Fact]
    public async Task CompareProducts_ShouldHandleUnexpectedException()
    {
        // Arrange
        var request = new CompareProductsRequestDto
        {
            ProductId1 = Guid.NewGuid(),
            ProductId2 = Guid.NewGuid()
        };

        _productComparisonServiceMock
            .Setup(x => x.CompareProductsAsync(request))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _controller.CompareProducts(request);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse>(internalServerErrorResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("An error occurred", apiResponse.Message);
    }

    #endregion

    #region ValidateProductsForComparison Tests

    [Fact]
    public async Task ValidateProductsForComparison_ShouldReturnOk_WhenProductsValid()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.ValidateProductsForComparisonAsync(productId1, productId2))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _controller.ValidateProductsForComparison(productId1, productId2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.True(apiResponse.Data);
    }

    [Fact]
    public async Task ValidateProductsForComparison_ShouldReturnBadRequest_WhenSameProductIds()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.ValidateProductsForComparisonAsync(productId, productId))
            .ReturnsAsync(Result<bool>.Failure("Cannot compare the same product with itself", "INVALID_INPUT"));

        // Act
        var result = await _controller.ValidateProductsForComparison(productId, productId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Cannot compare a product with itself", apiResponse.Message);
    }

    [Fact]
    public async Task ValidateProductsForComparison_ShouldReturnNotFound_WhenProductNotFound()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.ValidateProductsForComparisonAsync(productId1, productId2))
            .ReturnsAsync(Result<bool>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        // Act
        var result = await _controller.ValidateProductsForComparison(productId1, productId2);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Product not found", apiResponse.Message);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")] // Empty GUID as string
    public async Task ValidateProductsForComparison_ShouldReturnBadRequest_WhenEmptyGuids(string guidString)
    {
        // Arrange
        var productId1 = Guid.Parse(guidString);
        var productId2 = Guid.NewGuid();

        // Act
        var result = await _controller.ValidateProductsForComparison(productId1, productId2);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("cannot be empty", apiResponse.Message);
    }

    [Fact]
    public async Task ValidateProductsForComparison_ShouldHandleException()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.ValidateProductsForComparisonAsync(productId1, productId2))
            .ThrowsAsync(new ArgumentException("Invalid argument"));

        // Act
        var result = await _controller.ValidateProductsForComparison(productId1, productId2);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse>(internalServerErrorResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("An error occurred", apiResponse.Message);
    }

    #endregion

    #region GetComparableProducts Tests

    [Fact]
    public async Task GetComparableProducts_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var search = "test";
        var comparableProducts = new List<ProductDto>
        {
            CreateTestProductDto("Product 1"),
            CreateTestProductDto("Product 2")
        };

        var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
            comparableProducts, 1, 10, 2);

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, search, 1, 10))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(pagedResponse));

        // Act
        var result = await _controller.GetComparableProducts(productId, search);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(2, apiResponse.Data!.Data.Count());
        _productComparisonServiceMock.Verify(x => x.GetComparableProductsAsync(productId, search, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetComparableProducts_ShouldReturnOk_WithDefaultPagination()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var comparableProducts = new List<ProductDto>
        {
            CreateTestProductDto("Product 1")
        };

        var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
            comparableProducts, 1, 10, 1);

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, null, 1, 10))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(pagedResponse));

        // Act
        var result = await _controller.GetComparableProducts(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(1, apiResponse.Data!.Data!.Count());
        _productComparisonServiceMock.Verify(x => x.GetComparableProductsAsync(productId, null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetComparableProducts_ShouldReturnNotFound_WhenProductNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, null, 1, 10))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Failure("Product not found", "PRODUCT_NOT_FOUND"));

        // Act
        var result = await _controller.GetComparableProducts(productId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Product not found", apiResponse.Message);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)] // Assuming max page size is 100
    public async Task GetComparableProducts_ShouldReturnBadRequest_WhenInvalidPagination(int pageNumber, int pageSize)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = await _controller.GetComparableProducts(productId, null, pageNumber, pageSize);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetComparableProducts_ShouldReturnEmpty_WhenNoComparableProducts()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var comparableProducts = new List<ProductDto>();

        var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
            comparableProducts, 1, 10, 0);

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, null, 1, 10))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(pagedResponse));

        // Act
        var result = await _controller.GetComparableProducts(productId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Empty(apiResponse.Data!.Data!);
    }

    [Fact]
    public async Task GetComparableProducts_ShouldHandleSearchFilter()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var search = "test";
        var comparableProducts = new List<ProductDto>
        {
            CreateTestProductDto("Test Product")
        };

        var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
            comparableProducts, 1, 10, 1);

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, search, 1, 10))
            .ReturnsAsync(Result<PagedResponse<ProductDto>>.Success(pagedResponse));

        // Act
        var result = await _controller.GetComparableProducts(productId, search);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResponse<ProductDto>>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Single(apiResponse.Data!.Data!);
        _productComparisonServiceMock.Verify(x => x.GetComparableProductsAsync(productId, search, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetComparableProducts_ShouldHandleException()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _productComparisonServiceMock
            .Setup(x => x.GetComparableProductsAsync(productId, null, 1, 10))
            .ThrowsAsync(new TimeoutException("Request timeout"));

        // Act
        var result = await _controller.GetComparableProducts(productId);

        // Assert
        var internalServerErrorResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse>(internalServerErrorResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("An error occurred", apiResponse.Message);
    }

    #endregion

    #region Helper Methods

    private ProductComparisonResultDto CreateTestComparisonResult()
    {
        return new ProductComparisonResultDto
        {
            Summary = new ProductComparisonSummaryDto
            {
                CategoryName = "Test Category",
                TotalSpecifications = 10,
                MatchingSpecifications = 5,
                DifferentSpecifications = 5,
                Product1OverallScore = 0.8m,
                Product2OverallScore = 0.9m,
                RecommendedProductId = Guid.NewGuid().ToString(),
                RecommendationReason = "Better overall performance"
            },
            Product1 = CreateTestProductWithPrices("Product 1"),
            Product2 = CreateTestProductWithPrices("Product 2"),
            SpecificationComparison = new SpecificationComparisonDto
            {
                Differences = new List<SpecificationDifferenceDto>(),
                Matches = new List<SpecificationMatchDto>(),
                CategoryScores = new Dictionary<string, CategoryScoreDto>()
            },
            PriceAnalysis = new PriceAnalysisDto
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
            },
            RecommendationAnalysis = new RecommendationAnalysisDto
            {
                RecommendedProductId = Guid.NewGuid().ToString(),
                ConfidenceScore = 0.85m,
                PrimaryReason = "Better value for money",
                Factors = new List<RecommendationFactorDto>(),
                Pros = new List<string> { "Lower price", "Good performance" },
                Cons = new List<string> { "Less RAM" },
                UseCase = "Budget-conscious users"
            },
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

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
            }
        };
    }

    private ProductWithCurrentPricesDto CreateTestProductWithPrices(string name)
    {
        var baseProduct = CreateTestProductDto(name);
        return new ProductWithCurrentPricesDto
        {
            ProductId = baseProduct.ProductId,
            Name = baseProduct.Name,
            CategoryId = baseProduct.CategoryId,
            Description = baseProduct.Description,
            Specifications = baseProduct.Specifications,
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
                    LastUpdated = DateTimeOffset.UtcNow
                }
            },
            LowestCurrentPrice = 100.00m,
            HighestCurrentPrice = 100.00m,
            AvailableSellersCount = 1
        };
    }

    #endregion
}