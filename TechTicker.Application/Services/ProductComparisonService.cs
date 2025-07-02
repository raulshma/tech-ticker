using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Main orchestration service for product comparison functionality
/// </summary>
public class ProductComparisonService : IProductComparisonService
{
    private readonly IProductService _productService;
    private readonly ISpecificationAnalysisEngine _specificationAnalysisEngine;
    private readonly IPriceAnalysisService _priceAnalysisService;
    private readonly IRecommendationGenerationService _recommendationGenerationService;
    private readonly ILogger<ProductComparisonService> _logger;

    public ProductComparisonService(
        IProductService productService,
        ISpecificationAnalysisEngine specificationAnalysisEngine,
        IPriceAnalysisService priceAnalysisService,
        IRecommendationGenerationService recommendationGenerationService,
        ILogger<ProductComparisonService> logger)
    {
        _productService = productService;
        _specificationAnalysisEngine = specificationAnalysisEngine;
        _priceAnalysisService = priceAnalysisService;
        _recommendationGenerationService = recommendationGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Compare two products from the same category with comprehensive analysis
    /// </summary>
    /// <param name="request">Comparison request parameters</param>
    /// <returns>Complete comparison result with analysis</returns>
    public async Task<Result<ProductComparisonResultDto>> CompareProductsAsync(CompareProductsRequestDto request)
    {
        try
        {
            _logger.LogInformation("Starting product comparison for products {ProductId1} and {ProductId2}", 
                request.ProductId1, request.ProductId2);

            // Validate that products can be compared
            var validationResult = await ValidateProductsForComparisonAsync(request.ProductId1, request.ProductId2);
            if (!validationResult.IsSuccess)
            {
                return Result<ProductComparisonResultDto>.Failure(validationResult.ErrorMessage!);
            }

            // Get products with current pricing information sequentially to avoid concurrent DbContext access
            var product1Result = await _productService.GetProductWithCurrentPricesAsync(request.ProductId1);
            var product2Result = await _productService.GetProductWithCurrentPricesAsync(request.ProductId2);

            if (!product1Result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve product {ProductId1}: {Error}", 
                    request.ProductId1, product1Result.ErrorMessage);
                return Result<ProductComparisonResultDto>.Failure($"Failed to retrieve first product: {product1Result.ErrorMessage}");
            }

            if (!product2Result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve product {ProductId2}: {Error}", 
                    request.ProductId2, product2Result.ErrorMessage);
                return Result<ProductComparisonResultDto>.Failure($"Failed to retrieve second product: {product2Result.ErrorMessage}");
            }

            var product1 = product1Result.Data!;
            var product2 = product2Result.Data!;

            // Perform specification analysis
            _logger.LogInformation("Analyzing specifications for products {ProductId1} and {ProductId2}", 
                request.ProductId1, request.ProductId2);

            var specificationComparison = await _specificationAnalysisEngine.AnalyzeSpecificationsAsync(
                product1, product2, request.SpecificationWeights);

            // Calculate overall scores
            var (product1Score, product2Score) = await _specificationAnalysisEngine.CalculateOverallScoresAsync(
                specificationComparison);

            // Perform price analysis if requested
            PriceAnalysisDto? priceAnalysis = null;
            if (request.IncludePriceAnalysis)
            {
                _logger.LogInformation("Analyzing prices for products {ProductId1} and {ProductId2}", 
                    request.ProductId1, request.ProductId2);

                priceAnalysis = await _priceAnalysisService.AnalyzePricesAsync(product1, product2);
            }

            // Generate recommendations if requested
            RecommendationAnalysisDto? recommendationAnalysis = null;
            if (request.GenerateRecommendations)
            {
                _logger.LogInformation("Generating recommendations for products {ProductId1} and {ProductId2}", 
                    request.ProductId1, request.ProductId2);

                var recommendationResult = await _recommendationGenerationService.GenerateRecommendationAsync(
                    product1, product2, specificationComparison, priceAnalysis);

                recommendationAnalysis = recommendationResult;
            }

            // Create summary
            var summary = CreateComparisonSummary(
                product1, product2, specificationComparison, 
                product1Score, product2Score, recommendationAnalysis);

            var result = new ProductComparisonResultDto
            {
                Summary = summary,
                Product1 = product1,
                Product2 = product2,
                SpecificationComparison = specificationComparison,
                PriceAnalysis = priceAnalysis,
                RecommendationAnalysis = recommendationAnalysis,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Successfully completed product comparison for products {ProductId1} and {ProductId2}", 
                request.ProductId1, request.ProductId2);

            return Result<ProductComparisonResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing products {ProductId1} and {ProductId2}", 
                request.ProductId1, request.ProductId2);
            return Result<ProductComparisonResultDto>.Failure($"Comparison failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate that two products can be compared (same category, both exist)
    /// </summary>
    /// <param name="productId1">First product ID</param>
    /// <param name="productId2">Second product ID</param>
    /// <returns>Validation result</returns>
    public async Task<Result<bool>> ValidateProductsForComparisonAsync(Guid productId1, Guid productId2)
    {
        try
        {
            if (productId1 == productId2)
            {
                return Result<bool>.Failure("Cannot compare the same product with itself");
            }

            // Get both products sequentially to avoid concurrent DbContext access
            var product1Result = await _productService.GetProductByIdAsync(productId1);
            var product2Result = await _productService.GetProductByIdAsync(productId2);

            if (!product1Result.IsSuccess)
            {
                return Result<bool>.Failure($"First product not found: {product1Result.ErrorMessage}");
            }

            if (!product2Result.IsSuccess)
            {
                return Result<bool>.Failure($"Second product not found: {product2Result.ErrorMessage}");
            }

            var product1 = product1Result.Data!;
            var product2 = product2Result.Data!;

            // Check if both products are active
            if (!product1.IsActive)
            {
                return Result<bool>.Failure("First product is not active");
            }

            if (!product2.IsActive)
            {
                return Result<bool>.Failure("Second product is not active");
            }

            // Check if products are from the same category
            if (product1.CategoryId != product2.CategoryId)
            {
                return Result<bool>.Failure("Products must be from the same category to be compared");
            }

            _logger.LogInformation("Products {ProductId1} and {ProductId2} validated for comparison", 
                productId1, productId2);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating products {ProductId1} and {ProductId2} for comparison", 
                productId1, productId2);
            return Result<bool>.Failure($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get products that can be compared with a given product (same category)
    /// </summary>
    /// <param name="productId">Base product ID</param>
    /// <param name="search">Optional search term</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of comparable products</returns>
    public async Task<Result<PagedResponse<ProductDto>>> GetComparableProductsAsync(
        Guid productId, 
        string? search = null, 
        int pageNumber = 1, 
        int pageSize = 10)
    {
        try
        {
            // First, get the base product to determine its category
            var baseProductResult = await _productService.GetProductByIdAsync(productId);
            if (!baseProductResult.IsSuccess)
            {
                return Result<PagedResponse<ProductDto>>.Failure($"Base product not found: {baseProductResult.ErrorMessage}");
            }

            var baseProduct = baseProductResult.Data!;

            if (!baseProduct.IsActive)
            {
                return Result<PagedResponse<ProductDto>>.Failure("Base product is not active");
            }

            // Get products from the same category
            var productsResult = await _productService.GetProductsAsync(
                categoryId: baseProduct.CategoryId,
                search: search,
                pageNumber: pageNumber,
                pageSize: pageSize + 1); // Get one extra to account for excluding the base product

            if (!productsResult.IsSuccess)
            {
                return Result<PagedResponse<ProductDto>>.Failure($"Failed to retrieve comparable products: {productsResult.ErrorMessage}");
            }

            var products = productsResult.Data!;

            // Exclude the base product from the results
            var comparableProducts = products.Data!
                .Where(p => p.ProductId != productId && p.IsActive)
                .Take(pageSize)
                .ToList();

            var totalCount = Math.Max(0, products.Pagination.TotalCount - 1); // Subtract 1 for the excluded base product

            var pagedResponse = PagedResponse<ProductDto>.SuccessResult(
                comparableProducts,
                pageNumber,
                pageSize,
                totalCount);

            _logger.LogInformation("Retrieved {Count} comparable products for product {ProductId}", 
                comparableProducts.Count, productId);

            return Result<PagedResponse<ProductDto>>.Success(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving comparable products for product {ProductId}", productId);
            return Result<PagedResponse<ProductDto>>.Failure($"Failed to retrieve comparable products: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a high-level summary of the comparison results
    /// </summary>
    private ProductComparisonSummaryDto CreateComparisonSummary(
        ProductWithCurrentPricesDto product1,
        ProductWithCurrentPricesDto product2,
        SpecificationComparisonDto specificationComparison,
        decimal product1Score,
        decimal product2Score,
        RecommendationAnalysisDto? recommendationAnalysis)
    {
        var totalSpecifications = specificationComparison.Differences.Count() + specificationComparison.Matches.Count();
        var matchingSpecifications = specificationComparison.Matches.Count();
        var differentSpecifications = specificationComparison.Differences.Count();

        // Determine recommended product based on overall scores or recommendation analysis
        string recommendedProductId;
        string recommendationReason;

        if (recommendationAnalysis != null)
        {
            recommendedProductId = recommendationAnalysis.RecommendedProductId;
            recommendationReason = recommendationAnalysis.PrimaryReason;
        }
        else
        {
            if (product1Score > product2Score)
            {
                recommendedProductId = product1.ProductId.ToString();
                recommendationReason = $"Higher overall specification score ({product1Score:F2} vs {product2Score:F2})";
            }
            else if (product2Score > product1Score)
            {
                recommendedProductId = product2.ProductId.ToString();
                recommendationReason = $"Higher overall specification score ({product2Score:F2} vs {product1Score:F2})";
            }
            else
            {
                recommendedProductId = product1.ProductId.ToString();
                recommendationReason = "Products have equivalent scores - consider other factors like price and availability";
            }
        }

        return new ProductComparisonSummaryDto
        {
            CategoryName = product1.Category?.Name ?? "Unknown Category",
            TotalSpecifications = totalSpecifications,
            MatchingSpecifications = matchingSpecifications,
            DifferentSpecifications = differentSpecifications,
            Product1OverallScore = product1Score,
            Product2OverallScore = product2Score,
            RecommendedProductId = recommendedProductId,
            RecommendationReason = recommendationReason
        };
    }
}