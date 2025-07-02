using TechTicker.Application.DTOs;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// AI-powered recommendation generation service
/// </summary>
public interface IRecommendationGenerationService
{
    /// <summary>
    /// Generate AI-powered recommendations based on comparison data
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="specificationComparison">Specification analysis</param>
    /// <param name="priceAnalysis">Optional price analysis</param>
    /// <returns>Recommendation analysis</returns>
    Task<RecommendationAnalysisDto> GenerateRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        SpecificationComparisonDto specificationComparison,
        PriceAnalysisDto? priceAnalysis = null);
    
    /// <summary>
    /// Generate contextual recommendations based on use case
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="useCase">Specific use case (e.g., "gaming", "productivity")</param>
    /// <returns>Use case specific recommendation</returns>
    Task<RecommendationAnalysisDto> GenerateContextualRecommendationAsync(
        ProductDto product1,
        ProductDto product2,
        string useCase);
}