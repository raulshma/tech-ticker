using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// AI-powered recommendation generation service
/// </summary>
public class RecommendationGenerationService : IRecommendationGenerationService
{
    private readonly IAiGenerationService _aiGenerationService;
    private readonly ILogger<RecommendationGenerationService> _logger;

    // Recommendation factors and their default weights
    private readonly Dictionary<string, decimal> _defaultFactorWeights = new()
    {
        { "Performance", 0.30m },
        { "Price", 0.25m },
        { "Value", 0.20m },
        { "Features", 0.15m },
        { "Availability", 0.10m }
    };

    public RecommendationGenerationService(
        IAiGenerationService aiGenerationService,
        ILogger<RecommendationGenerationService> logger)
    {
        _aiGenerationService = aiGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Generate AI-powered recommendations based on comparison data
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="specificationComparison">Specification analysis</param>
    /// <param name="priceAnalysis">Optional price analysis</param>
    /// <returns>Recommendation analysis</returns>
    public async Task<RecommendationAnalysisDto> GenerateRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        SpecificationComparisonDto specificationComparison,
        PriceAnalysisDto? priceAnalysis = null)
    {
        try
        {
            _logger.LogInformation("Generating recommendations for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);

            // Generate factors analysis
            var factors = await GenerateRecommendationFactorsAsync(
                product1, product2, specificationComparison, priceAnalysis);

            // Calculate scores based on factors
            var (product1Score, product2Score) = CalculateProductScores(factors);

            // Determine recommended product
            var recommendedProductId = product1Score >= product2Score 
                ? product1.ProductId.ToString() 
                : product2.ProductId.ToString();

            // Calculate confidence score
            var confidenceScore = CalculateConfidenceScore(product1Score, product2Score, factors);

            // Generate primary reason
            var primaryReason = GeneratePrimaryReason(factors, product1Score, product2Score, product1, product2);

            // Generate pros and cons
            var (pros, cons) = GenerateProsAndCons(factors, product1Score, product2Score, product1, product2);

            // Try to generate AI-enhanced recommendation if available
            var aiEnhancedRecommendation = await TryGenerateAiRecommendationAsync(
                product1, product2, specificationComparison, priceAnalysis);

            var recommendation = new RecommendationAnalysisDto
            {
                RecommendedProductId = recommendedProductId,
                ConfidenceScore = confidenceScore,
                PrimaryReason = aiEnhancedRecommendation?.PrimaryReason ?? primaryReason,
                Factors = factors,
                Pros = aiEnhancedRecommendation?.Pros ?? pros,
                Cons = aiEnhancedRecommendation?.Cons ?? cons,
                UseCase = null,
                AlternativeRecommendation = GenerateAlternativeRecommendation(product1Score, product2Score)
            };

            _logger.LogInformation("Generated recommendation for products {Product1} and {Product2}: {Recommended} with confidence {Confidence:F2}", 
                product1.ProductId, product2.ProductId, recommendedProductId, confidenceScore);

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Generate contextual recommendations based on use case
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="useCase">Specific use case (e.g., "gaming", "productivity")</param>
    /// <returns>Use case specific recommendation</returns>
    public async Task<RecommendationAnalysisDto> GenerateContextualRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        string useCase)
    {
        try
        {
            _logger.LogInformation("Generating contextual recommendations for use case '{UseCase}' comparing products {Product1} and {Product2}", 
                useCase, product1.ProductId, product2.ProductId);

            // Get use case specific weights
            var useCaseWeights = GetUseCaseWeights(useCase);

            // Generate factors with use case considerations
            var factors = await GenerateContextualFactorsAsync(product1, product2, useCase, useCaseWeights);

            // Calculate scores with use case weights
            var (product1Score, product2Score) = CalculateProductScores(factors);

            var recommendedProductId = product1Score >= product2Score 
                ? product1.ProductId.ToString() 
                : product2.ProductId.ToString();

            var confidenceScore = CalculateConfidenceScore(product1Score, product2Score, factors);

            // Generate use case specific reasoning
            var primaryReason = GenerateUseCaseSpecificReason(useCase, factors, product1Score, product2Score, product1, product2);

            // Try AI-enhanced contextual recommendation
            var aiEnhancedRecommendation = await TryGenerateAiContextualRecommendationAsync(
                product1, product2, useCase);

            var (pros, cons) = GenerateContextualProsAndCons(factors, product1Score, product2Score, product1, product2, useCase);

            var recommendation = new RecommendationAnalysisDto
            {
                RecommendedProductId = recommendedProductId,
                ConfidenceScore = confidenceScore,
                PrimaryReason = aiEnhancedRecommendation?.PrimaryReason ?? primaryReason,
                Factors = factors,
                Pros = aiEnhancedRecommendation?.Pros ?? pros,
                Cons = aiEnhancedRecommendation?.Cons ?? cons,
                UseCase = useCase,
                AlternativeRecommendation = GenerateUseCaseAlternativeRecommendation(product1Score, product2Score, useCase)
            };

            _logger.LogInformation("Generated contextual recommendation for use case '{UseCase}': {Recommended} with confidence {Confidence:F2}", 
                useCase, recommendedProductId, confidenceScore);

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contextual recommendations for use case '{UseCase}' comparing products {Product1} and {Product2}", 
                useCase, product1.ProductId, product2.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Generate recommendation factors based on comparison data
    /// </summary>
    private async Task<IEnumerable<RecommendationFactorDto>> GenerateRecommendationFactorsAsync(
        ProductDto product1,
        ProductDto product2,
        SpecificationComparisonDto specificationComparison,
        PriceAnalysisDto? priceAnalysis)
    {
        var factors = new List<RecommendationFactorDto>();

        // Performance factor
        var performanceScore = await CalculatePerformanceScoresAsync(product1, product2, specificationComparison);
        factors.Add(new RecommendationFactorDto
        {
            Factor = "Performance",
            Weight = _defaultFactorWeights["Performance"],
            Product1Score = performanceScore.Product1Score,
            Product2Score = performanceScore.Product2Score,
            Impact = performanceScore.Impact
        });

        // Features factor
        var featuresScore = CalculateFeaturesScores(specificationComparison);
        factors.Add(new RecommendationFactorDto
        {
            Factor = "Features",
            Weight = _defaultFactorWeights["Features"],
            Product1Score = featuresScore.Product1Score,
            Product2Score = featuresScore.Product2Score,
            Impact = featuresScore.Impact
        });

        // Price and Value factors (if price analysis is available)
        if (priceAnalysis != null)
        {
            var priceScore = CalculatePriceScores(priceAnalysis);
            factors.Add(new RecommendationFactorDto
            {
                Factor = "Price",
                Weight = _defaultFactorWeights["Price"],
                Product1Score = priceScore.Product1Score,
                Product2Score = priceScore.Product2Score,
                Impact = priceScore.Impact
            });

            var valueScore = CalculateValueScores(priceAnalysis);
            factors.Add(new RecommendationFactorDto
            {
                Factor = "Value",
                Weight = _defaultFactorWeights["Value"],
                Product1Score = valueScore.Product1Score,
                Product2Score = valueScore.Product2Score,
                Impact = valueScore.Impact
            });

            var availabilityScore = CalculateAvailabilityScores(priceAnalysis);
            factors.Add(new RecommendationFactorDto
            {
                Factor = "Availability",
                Weight = _defaultFactorWeights["Availability"],
                Product1Score = availabilityScore.Product1Score,
                Product2Score = availabilityScore.Product2Score,
                Impact = availabilityScore.Impact
            });
        }

        return await Task.FromResult(factors);
    }

    /// <summary>
    /// Generate contextual factors based on use case
    /// </summary>
    private async Task<IEnumerable<RecommendationFactorDto>> GenerateContextualFactorsAsync(
        ProductDto product1,
        ProductDto product2,
        string useCase,
        Dictionary<string, decimal> useCaseWeights)
    {
        // For now, generate basic factors with use case weights
        // In a real implementation, this would analyze specifications contextually
        var factors = new List<RecommendationFactorDto>();

        foreach (var weight in useCaseWeights)
        {
            var scores = CalculateUseCaseSpecificScores(product1, product2, weight.Key, useCase);
            factors.Add(new RecommendationFactorDto
            {
                Factor = weight.Key,
                Weight = weight.Value,
                Product1Score = scores.Product1Score,
                Product2Score = scores.Product2Score,
                Impact = scores.Impact
            });
        }

        return await Task.FromResult(factors);
    }

    /// <summary>
    /// Try to generate AI-enhanced recommendation
    /// </summary>
    private async Task<RecommendationAnalysisDto?> TryGenerateAiRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        SpecificationComparisonDto specificationComparison,
        PriceAnalysisDto? priceAnalysis)
    {
        try
        {
            // Check if AI service is available
            var aiAvailableResult = await _aiGenerationService.IsAiConfigurationAvailableAsync();
            if (!aiAvailableResult.IsSuccess || !aiAvailableResult.Data)
            {
                _logger.LogInformation("AI service not available for recommendation generation");
                return null;
            }

            // Prepare AI prompt
            var prompt = BuildRecommendationPrompt(product1, product2, specificationComparison, priceAnalysis);

            var aiRequest = new GenericAiRequestDto
            {
                InputText = prompt,
                SystemPrompt = "You are an expert product comparison analyst. Provide clear, concise recommendations based on the data provided.",
                Temperature = 0.7,
                MaxTokens = 500
            };

            var aiResult = await _aiGenerationService.GenerateGenericResponseAsync(aiRequest);
            if (aiResult.IsSuccess && aiResult.Data != null)
            {
                return ParseAiRecommendation(aiResult.Data.Response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI recommendation, falling back to rule-based recommendation");
        }

        return null;
    }

    /// <summary>
    /// Try to generate AI-enhanced contextual recommendation
    /// </summary>
    private async Task<RecommendationAnalysisDto?> TryGenerateAiContextualRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        string useCase)
    {
        try
        {
            var aiAvailableResult = await _aiGenerationService.IsAiConfigurationAvailableAsync();
            if (!aiAvailableResult.IsSuccess || !aiAvailableResult.Data)
            {
                return null;
            }

            var prompt = BuildContextualRecommendationPrompt(product1, product2, useCase);

            var aiRequest = new GenericAiRequestDto
            {
                InputText = prompt,
                SystemPrompt = $"You are an expert in {useCase} use cases. Recommend the best product for this specific scenario.",
                Temperature = 0.7,
                MaxTokens = 400
            };

            var aiResult = await _aiGenerationService.GenerateGenericResponseAsync(aiRequest);
            if (aiResult.IsSuccess && aiResult.Data != null)
            {
                return ParseAiRecommendation(aiResult.Data.Response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI contextual recommendation for use case '{UseCase}'", useCase);
        }

        return null;
    }

    /// <summary>
    /// Calculate product scores based on factors
    /// </summary>
    private (decimal Product1Score, decimal Product2Score) CalculateProductScores(IEnumerable<RecommendationFactorDto> factors)
    {
        decimal product1Score = 0;
        decimal product2Score = 0;

        foreach (var factor in factors)
        {
            product1Score += factor.Product1Score * factor.Weight;
            product2Score += factor.Product2Score * factor.Weight;
        }

        return (product1Score, product2Score);
    }

    /// <summary>
    /// Calculate confidence score based on score difference and factor reliability
    /// </summary>
    private decimal CalculateConfidenceScore(decimal product1Score, decimal product2Score, IEnumerable<RecommendationFactorDto> factors)
    {
        var scoreDifference = Math.Abs(product1Score - product2Score);
        var maxPossibleScore = factors.Sum(f => f.Weight);
        
        if (maxPossibleScore == 0) return 0.5m;

        var normalizedDifference = scoreDifference / maxPossibleScore;
        
        // Base confidence starts at 60% and increases with score difference
        var confidence = 0.6m + (normalizedDifference * 0.4m);
        
        return Math.Min(confidence, 1.0m);
    }

    /// <summary>
    /// Get use case specific weights
    /// </summary>
    private Dictionary<string, decimal> GetUseCaseWeights(string? useCase)
    {
        return useCase?.ToLowerInvariant() switch
        {
            "gaming" => new Dictionary<string, decimal>
            {
                { "Performance", 0.50m },
                { "Graphics", 0.25m },
                { "Price", 0.15m },
                { "Features", 0.10m }
            },
            "productivity" => new Dictionary<string, decimal>
            {
                { "Performance", 0.35m },
                { "Memory", 0.25m },
                { "Features", 0.20m },
                { "Value", 0.20m }
            },
            "budget" => new Dictionary<string, decimal>
            {
                { "Price", 0.40m },
                { "Value", 0.30m },
                { "Performance", 0.20m },
                { "Features", 0.10m }
            },
            _ => new Dictionary<string, decimal>(_defaultFactorWeights)
        };
    }

    // Additional helper methods would be implemented here for:
    // - CalculatePerformanceScoresAsync
    // - CalculateFeaturesScores
    // - CalculatePriceScores
    // - CalculateValueScores
    // - CalculateAvailabilityScores
    // - CalculateUseCaseSpecificScores
    // - GeneratePrimaryReason
    // - GenerateProsAndCons
    // - GenerateAlternativeRecommendation
    // - BuildRecommendationPrompt
    // - ParseAiRecommendation
    // etc.

    /// <summary>
    /// Calculate performance scores (placeholder implementation)
    /// </summary>
    private async Task<(decimal Product1Score, decimal Product2Score, string Impact)> CalculatePerformanceScoresAsync(
        ProductDto product1, ProductDto product2, SpecificationComparisonDto specificationComparison)
    {
        // Implementation would analyze performance-related specifications
        var performanceCategory = specificationComparison.CategoryScores.GetValueOrDefault("performance");
        if (performanceCategory != null)
        {
            return await Task.FromResult((performanceCategory.Product1Score, performanceCategory.Product2Score, "Significant performance differences detected"));
        }
        
        return await Task.FromResult((0.5m, 0.5m, "Performance data not available"));
    }

    /// <summary>
    /// Calculate features scores (placeholder implementation)
    /// </summary>
    private (decimal Product1Score, decimal Product2Score, string Impact) CalculateFeaturesScores(SpecificationComparisonDto specificationComparison)
    {
        var product1Advantages = specificationComparison.Differences.Count(d => 
            d.ComparisonResult == ComparisonResultType.Product1Better || 
            d.ComparisonResult == ComparisonResultType.Product1Only);
        
        var product2Advantages = specificationComparison.Differences.Count(d => 
            d.ComparisonResult == ComparisonResultType.Product2Better || 
            d.ComparisonResult == ComparisonResultType.Product2Only);

        var totalDifferences = product1Advantages + product2Advantages;
        if (totalDifferences == 0)
            return (0.5m, 0.5m, "Products have similar feature sets");

        var product1Score = (decimal)product1Advantages / totalDifferences;
        var product2Score = (decimal)product2Advantages / totalDifferences;

        return (product1Score, product2Score, $"Feature comparison shows {Math.Abs(product1Advantages - product2Advantages)} advantage differences");
    }

    /// <summary>
    /// Calculate price scores (placeholder implementation)
    /// </summary>
    private (decimal Product1Score, decimal Product2Score, string Impact) CalculatePriceScores(PriceAnalysisDto priceAnalysis)
    {
        var price1 = priceAnalysis.Summary.Product1LowestPrice;
        var price2 = priceAnalysis.Summary.Product2LowestPrice;

        if (price1 <= 0 && price2 <= 0)
            return (0.5m, 0.5m, "No pricing data available");

        if (price1 <= 0)
            return (0m, 1m, "Only second product has pricing");

        if (price2 <= 0)
            return (1m, 0m, "Only first product has pricing");

        // Lower price gets higher score
        var lowerPrice = Math.Min(price1, price2);
        var higherPrice = Math.Max(price1, price2);
        
        var product1Score = price1 == lowerPrice ? 1m : lowerPrice / price1;
        var product2Score = price2 == lowerPrice ? 1m : lowerPrice / price2;

        var priceDiffPercent = ((higherPrice - lowerPrice) / lowerPrice) * 100;
        return (product1Score, product2Score, $"Price difference of {priceDiffPercent:F1}% impacts value proposition");
    }

    /// <summary>
    /// Calculate value scores (placeholder implementation)
    /// </summary>
    private (decimal Product1Score, decimal Product2Score, string Impact) CalculateValueScores(PriceAnalysisDto priceAnalysis)
    {
        return (priceAnalysis.ValueAnalysis.Product1ValueScore / 100m, 
                priceAnalysis.ValueAnalysis.Product2ValueScore / 100m, 
                priceAnalysis.ValueAnalysis.ValueAnalysisReason);
    }

    /// <summary>
    /// Calculate availability scores (placeholder implementation)
    /// </summary>
    private (decimal Product1Score, decimal Product2Score, string Impact) CalculateAvailabilityScores(PriceAnalysisDto priceAnalysis)
    {
        var seller1Count = priceAnalysis.Summary.Product1SellerCount;
        var seller2Count = priceAnalysis.Summary.Product2SellerCount;

        var maxSellers = Math.Max(seller1Count, seller2Count);
        if (maxSellers == 0)
            return (0.5m, 0.5m, "No availability data");

        var product1Score = maxSellers > 0 ? (decimal)seller1Count / maxSellers : 0m;
        var product2Score = maxSellers > 0 ? (decimal)seller2Count / maxSellers : 0m;

        return (product1Score, product2Score, $"Availability varies with {Math.Abs(seller1Count - seller2Count)} seller difference");
    }

    /// <summary>
    /// Calculate use case specific scores (placeholder implementation)
    /// </summary>
    private (decimal Product1Score, decimal Product2Score, string Impact) CalculateUseCaseSpecificScores(
        ProductDto product1, ProductDto product2, string factor, string useCase)
    {
        // This would analyze specifications relevant to the specific use case
        // For now, return neutral scores
        return (0.5m, 0.5m, $"{factor} analysis for {useCase} use case");
    }

    /// <summary>
    /// Generate primary reason (placeholder implementation)
    /// </summary>
    private string GeneratePrimaryReason(IEnumerable<RecommendationFactorDto> factors, decimal product1Score, decimal product2Score, ProductDto product1, ProductDto product2)
    {
        var winningProduct = product1Score >= product2Score ? product1.Name : product2.Name;
        var topFactor = factors.OrderByDescending(f => Math.Abs(f.Product1Score - f.Product2Score) * f.Weight).FirstOrDefault();
        
        if (topFactor != null)
        {
            return $"{winningProduct} is recommended primarily due to superior {topFactor.Factor.ToLower()} characteristics.";
        }
        
        return $"{winningProduct} is recommended based on overall comparison analysis.";
    }

    /// <summary>
    /// Generate pros and cons (placeholder implementation)
    /// </summary>
    private (IEnumerable<string> Pros, IEnumerable<string> Cons) GenerateProsAndCons(
        IEnumerable<RecommendationFactorDto> factors, decimal product1Score, decimal product2Score, 
        ProductDto product1, ProductDto product2)
    {
        var pros = new List<string>();
        var cons = new List<string>();

        var winningProduct = product1Score >= product2Score ? product1 : product2;
        var losingProduct = product1Score < product2Score ? product1 : product2;

        pros.Add($"{winningProduct.Name} offers better overall value proposition");
        pros.Add("Strong performance in key comparison areas");

        cons.Add($"May be more expensive than {losingProduct.Name}");
        cons.Add("Consider specific use case requirements");

        return (pros, cons);
    }

    /// <summary>
    /// Generate alternative recommendation (placeholder implementation)
    /// </summary>
    private string? GenerateAlternativeRecommendation(decimal product1Score, decimal product2Score)
    {
        var scoreDiff = Math.Abs(product1Score - product2Score);
        if (scoreDiff < 0.1m)
        {
            return "Products are very similar - consider additional factors like brand preference or specific features.";
        }
        
        return null;
    }

    /// <summary>
    /// Generate use case specific reason (placeholder implementation)
    /// </summary>
    private string GenerateUseCaseSpecificReason(string useCase, IEnumerable<RecommendationFactorDto> factors, 
        decimal product1Score, decimal product2Score, ProductDto product1, ProductDto product2)
    {
        var winningProduct = product1Score >= product2Score ? product1.Name : product2.Name;
        return $"For {useCase} use case, {winningProduct} provides the best combination of relevant features and value.";
    }

    /// <summary>
    /// Generate contextual pros and cons (placeholder implementation)
    /// </summary>
    private (IEnumerable<string> Pros, IEnumerable<string> Cons) GenerateContextualProsAndCons(
        IEnumerable<RecommendationFactorDto> factors, decimal product1Score, decimal product2Score, 
        ProductDto product1, ProductDto product2, string useCase)
    {
        var pros = new List<string> { $"Optimized for {useCase} scenarios" };
        var cons = new List<string> { $"May not be ideal for other use cases" };
        return (pros, cons);
    }

    /// <summary>
    /// Generate use case alternative recommendation (placeholder implementation)
    /// </summary>
    private string? GenerateUseCaseAlternativeRecommendation(decimal product1Score, decimal product2Score, string useCase)
    {
        return $"Consider your specific {useCase} requirements when making the final decision.";
    }

    /// <summary>
    /// Build recommendation prompt for AI (placeholder implementation)
    /// </summary>
    private string BuildRecommendationPrompt(ProductDto product1, ProductDto product2, 
        SpecificationComparisonDto specificationComparison, PriceAnalysisDto? priceAnalysis)
    {
        return $"Compare {product1.Name} vs {product2.Name} and provide recommendation with reasoning.";
    }

    /// <summary>
    /// Build contextual recommendation prompt for AI (placeholder implementation)
    /// </summary>
    private string BuildContextualRecommendationPrompt(ProductDto product1, ProductDto product2, string useCase)
    {
        return $"For {useCase} use case, compare {product1.Name} vs {product2.Name} and recommend the better option.";
    }

    /// <summary>
    /// Parse AI recommendation response (placeholder implementation)
    /// </summary>
    private RecommendationAnalysisDto? ParseAiRecommendation(string aiResponse)
    {
        // This would parse the AI response and extract structured recommendation data
        return null;
    }
}