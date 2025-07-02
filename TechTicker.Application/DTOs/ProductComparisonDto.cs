using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// Request DTO for comparing two products
/// </summary>
public class CompareProductsRequestDto : IValidatableObject
{
    [Required]
    public Guid ProductId1 { get; set; }
    
    [Required]
    public Guid ProductId2 { get; set; }
    
    /// <summary>
    /// Optional weights for different specification categories for scoring
    /// Key: specification category, Value: weight (0.0 - 1.0)
    /// </summary>
    public Dictionary<string, decimal>? SpecificationWeights { get; set; }
    
    /// <summary>
    /// Whether to include price analysis in the comparison
    /// </summary>
    public bool IncludePriceAnalysis { get; set; } = true;
    
    /// <summary>
    /// Whether to generate AI-powered recommendations
    /// </summary>
    public bool GenerateRecommendations { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Ensure the two product IDs are different
        if (ProductId1 == ProductId2)
        {
            results.Add(new ValidationResult(
                "Cannot compare a product with itself. ProductId1 and ProductId2 must be different.",
                new[] { nameof(ProductId1), nameof(ProductId2) }));
        }

        // Ensure both product IDs are not empty
        if (ProductId1 == Guid.Empty)
        {
            results.Add(new ValidationResult(
                "ProductId1 cannot be empty.",
                new[] { nameof(ProductId1) }));
        }

        if (ProductId2 == Guid.Empty)
        {
            results.Add(new ValidationResult(
                "ProductId2 cannot be empty.",
                new[] { nameof(ProductId2) }));
        }

        // Validate specification weights if provided
        if (SpecificationWeights != null)
        {
            foreach (var weight in SpecificationWeights)
            {
                if (weight.Value < 0 || weight.Value > 1)
                {
                    results.Add(new ValidationResult(
                        $"Specification weight for '{weight.Key}' must be between 0.0 and 1.0.",
                        new[] { nameof(SpecificationWeights) }));
                }

                if (string.IsNullOrWhiteSpace(weight.Key))
                {
                    results.Add(new ValidationResult(
                        "Specification weight keys cannot be null or empty.",
                        new[] { nameof(SpecificationWeights) }));
                }
            }
        }

        return results;
    }
}

/// <summary>
/// Complete result of product comparison analysis
/// </summary>
public class ProductComparisonResultDto
{
    public ProductComparisonSummaryDto Summary { get; set; } = null!;
    public ProductWithCurrentPricesDto Product1 { get; set; } = null!;
    public ProductWithCurrentPricesDto Product2 { get; set; } = null!;
    public SpecificationComparisonDto SpecificationComparison { get; set; } = null!;
    public PriceAnalysisDto? PriceAnalysis { get; set; }
    public RecommendationAnalysisDto? RecommendationAnalysis { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}

/// <summary>
/// High-level summary of the comparison
/// </summary>
public class ProductComparisonSummaryDto
{
    public string CategoryName { get; set; } = null!;
    public int TotalSpecifications { get; set; }
    public int MatchingSpecifications { get; set; }
    public int DifferentSpecifications { get; set; }
    public decimal Product1OverallScore { get; set; }
    public decimal Product2OverallScore { get; set; }
    public string RecommendedProductId { get; set; } = null!;
    public string RecommendationReason { get; set; } = null!;
}

/// <summary>
/// Detailed specification comparison results
/// </summary>
public class SpecificationComparisonDto
{
    public IEnumerable<SpecificationDifferenceDto> Differences { get; set; } = new List<SpecificationDifferenceDto>();
    public IEnumerable<SpecificationMatchDto> Matches { get; set; } = new List<SpecificationMatchDto>();
    public Dictionary<string, CategoryScoreDto> CategoryScores { get; set; } = new Dictionary<string, CategoryScoreDto>();
}

/// <summary>
/// Represents a difference between two product specifications
/// </summary>
public class SpecificationDifferenceDto
{
    public string SpecificationKey { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public object? Product1Value { get; set; }
    public object? Product2Value { get; set; }
    public string? Product1DisplayValue { get; set; }
    public string? Product2DisplayValue { get; set; }
    public ComparisonResultType ComparisonResult { get; set; }
    public decimal? ImpactScore { get; set; }
    public string? AnalysisNote { get; set; }
}

/// <summary>
/// Represents a matching specification between products
/// </summary>
public class SpecificationMatchDto
{
    public string SpecificationKey { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public object Value { get; set; } = null!;
    public string? DisplayValue { get; set; }
}

/// <summary>
/// Score analysis for a specification category
/// </summary>
public class CategoryScoreDto
{
    public string CategoryName { get; set; } = null!;
    public decimal Product1Score { get; set; }
    public decimal Product2Score { get; set; }
    public decimal Weight { get; set; }
    public string? Analysis { get; set; }
}

/// <summary>
/// Result of comparing two specification values
/// </summary>
public enum ComparisonResultType
{
    Product1Better,
    Product2Better,
    Equivalent,
    Product1Only,
    Product2Only,
    Incomparable
}

/// <summary>
/// Comprehensive price analysis between two products
/// </summary>
public class PriceAnalysisDto
{
    public PriceComparisonSummaryDto Summary { get; set; } = null!;
    public IEnumerable<SellerPriceComparisonDto> SellerComparisons { get; set; } = new List<SellerPriceComparisonDto>();
    public ValueAnalysisDto ValueAnalysis { get; set; } = null!;
}

/// <summary>
/// Summary of price differences
/// </summary>
public class PriceComparisonSummaryDto
{
    public decimal Product1LowestPrice { get; set; }
    public decimal Product2LowestPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PriceDifferencePercentage { get; set; }
    public string LowerPricedProduct { get; set; } = null!;
    public int Product1SellerCount { get; set; }
    public int Product2SellerCount { get; set; }
}

/// <summary>
/// Price comparison across different sellers
/// </summary>
public class SellerPriceComparisonDto
{
    public string SellerName { get; set; } = null!;
    public decimal? Product1Price { get; set; }
    public decimal? Product2Price { get; set; }
    public string? Product1StockStatus { get; set; }
    public string? Product2StockStatus { get; set; }
    public decimal? PriceDifference { get; set; }
    public string? AvailabilityAdvantage { get; set; }
}

/// <summary>
/// Value analysis considering price vs specifications
/// </summary>
public class ValueAnalysisDto
{
    public decimal Product1ValueScore { get; set; }
    public decimal Product2ValueScore { get; set; }
    public string BetterValueProduct { get; set; } = null!;
    public string ValueAnalysisReason { get; set; } = null!;
}

/// <summary>
/// AI-powered recommendation analysis
/// </summary>
public class RecommendationAnalysisDto
{
    public string RecommendedProductId { get; set; } = null!;
    public decimal ConfidenceScore { get; set; }
    public string PrimaryReason { get; set; } = null!;
    public IEnumerable<RecommendationFactorDto> Factors { get; set; } = new List<RecommendationFactorDto>();
    public IEnumerable<string> Pros { get; set; } = new List<string>();
    public IEnumerable<string> Cons { get; set; } = new List<string>();
    public string? UseCase { get; set; }
    public string? AlternativeRecommendation { get; set; }
}

/// <summary>
/// Individual factor in the recommendation analysis
/// </summary>
public class RecommendationFactorDto
{
    public string Factor { get; set; } = null!;
    public decimal Weight { get; set; }
    public decimal Product1Score { get; set; }
    public decimal Product2Score { get; set; }
    public string Impact { get; set; } = null!;
}