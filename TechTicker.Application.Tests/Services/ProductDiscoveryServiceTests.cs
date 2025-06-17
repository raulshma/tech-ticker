using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class ProductDiscoveryServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IProductDiscoveryCandidateRepository> _mockCandidateRepository;
    private readonly Mock<IDiscoveryApprovalWorkflowRepository> _mockWorkflowRepository;
    private readonly Mock<IUrlAnalysisService> _mockUrlAnalysisService;
    private readonly Mock<ICategoryPredictionService> _mockCategoryPredictionService;
    private readonly Mock<IProductSimilarityService> _mockProductSimilarityService;
    private readonly Mock<IDiscoveryWorkflowService> _mockDiscoveryWorkflowService;
    private readonly Mock<IMappingService> _mockMappingService;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<ILogger<ProductDiscoveryService>> _mockLogger;
    private readonly ProductDiscoveryService _service;

    public ProductDiscoveryServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCandidateRepository = new Mock<IProductDiscoveryCandidateRepository>();
        _mockWorkflowRepository = new Mock<IDiscoveryApprovalWorkflowRepository>();
        _mockUrlAnalysisService = new Mock<IUrlAnalysisService>();
        _mockCategoryPredictionService = new Mock<ICategoryPredictionService>();
        _mockProductSimilarityService = new Mock<IProductSimilarityService>();
        _mockDiscoveryWorkflowService = new Mock<IDiscoveryWorkflowService>();
        _mockMappingService = new Mock<IMappingService>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockLogger = new Mock<ILogger<ProductDiscoveryService>>();

        _service = new ProductDiscoveryService(
            _mockUnitOfWork.Object,
            _mockCandidateRepository.Object,
            _mockWorkflowRepository.Object,
            _mockUrlAnalysisService.Object,
            _mockCategoryPredictionService.Object,
            _mockProductSimilarityService.Object,
            _mockDiscoveryWorkflowService.Object,
            _mockMappingService.Object,
            _mockMessagePublisher.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AnalyzeUrlAsync_WithValidUrl_ShouldReturnSuccessResult()
    {
        // Arrange
        var url = "https://example.com/product/123";
        var userId = Guid.NewGuid();

        var extractedData = new ProductExtractionResult
        {
            ExtractedProductName = "Test Product",
            ExtractedManufacturer = "Test Manufacturer",
            ExtractedPrice = 99.99m,
            ExtractedDescription = "Test Description"
        };

        var categoryPrediction = new CategoryPrediction
        {
            CategoryId = Guid.NewGuid(),
            ConfidenceScore = 0.85m
        };

        var similarityResults = new List<SimilarProductResult>
        {
            new SimilarProductResult
            {
                ProductId = Guid.NewGuid(),
                SimilarityScore = 0.75m
            }
        };

        var candidateDto = new ProductDiscoveryCandidateDto
        {
            CandidateId = Guid.NewGuid(),
            ExtractedProductName = "Test Product",
            ExtractedManufacturer = "Test Manufacturer",
            ExtractedPrice = 99.99m,
            ExtractedDescription = "Test Description"
        };

        // Mock URL validation
        _mockUrlAnalysisService
            .Setup(x => x.IsValidProductUrlAsync(url))
            .ReturnsAsync(Result<bool>.Success(true));

        _mockUrlAnalysisService
            .Setup(x => x.ExtractProductDataAsync(url))
            .ReturnsAsync(Result<ProductExtractionResult>.Success(extractedData));

        _mockCategoryPredictionService
            .Setup(x => x.PredictCategoryAsync(It.IsAny<ProductExtractionResult>()))
            .ReturnsAsync(Result<CategoryPrediction>.Success(categoryPrediction));

        _mockProductSimilarityService
            .Setup(x => x.FindSimilarProductsAsync(It.IsAny<ProductExtractionResult>(), 5))
            .ReturnsAsync(Result<List<SimilarProductResult>>.Success(similarityResults));

        // Mock mapping service
        _mockMappingService
            .Setup(x => x.MapToDto(It.IsAny<ProductDiscoveryCandidate>()))
            .Returns(candidateDto);

        // Mock message publisher
        _mockMessagePublisher
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AnalyzeUrlAsync(url, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Candidates);
        Assert.Equal("Test Product", result.Data.Candidates.First().ExtractedProductName);

        // Verify that the candidate was saved
        _mockCandidateRepository.Verify(x => x.AddAsync(It.IsAny<ProductDiscoveryCandidate>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeUrlAsync_WithInvalidUrl_ShouldReturnFailureResult()
    {
        // Arrange
        var url = "invalid-url";
        var userId = Guid.NewGuid();

        // Mock URL validation to return failure
        _mockUrlAnalysisService
            .Setup(x => x.IsValidProductUrlAsync(url))
            .ReturnsAsync(Result<bool>.Failure("Invalid URL format"));

        // Act
        var result = await _service.AnalyzeUrlAsync(url, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid URL format", result.ErrorMessage!);
    }

    [Fact]
    public async Task AnalyzeUrlAsync_WhenExtractionFails_ShouldReturnFailureResult()
    {
        // Arrange
        var url = "https://example.com/product/123";
        var userId = Guid.NewGuid();

        // Mock URL validation to succeed first
        _mockUrlAnalysisService
            .Setup(x => x.IsValidProductUrlAsync(url))
            .ReturnsAsync(Result<bool>.Success(true));

        // Mock extraction to fail
        _mockUrlAnalysisService
            .Setup(x => x.ExtractProductDataAsync(url))
            .ReturnsAsync(Result<ProductExtractionResult>.Failure("Failed to extract product data"));

        // Mock message publisher for failure event
        _mockMessagePublisher
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AnalyzeUrlAsync(url, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to extract product data", result.ErrorMessage!);
    }
}
