using TechTicker.Application.DTOs;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Engine for analyzing and comparing product specifications
/// </summary>
public interface ISpecificationAnalysisEngine
{
    /// <summary>
    /// Analyze and compare specifications between two products
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="weights">Optional specification weights</param>
    /// <returns>Detailed specification comparison</returns>
    Task<SpecificationComparisonDto> AnalyzeSpecificationsAsync(
        ProductDto product1, 
        ProductDto product2, 
        Dictionary<string, decimal>? weights = null);
    
    /// <summary>
    /// Calculate overall scores for products based on specifications
    /// </summary>
    /// <param name="comparison">Specification comparison result</param>
    /// <returns>Overall scores for both products</returns>
    Task<(decimal product1Score, decimal product2Score)> CalculateOverallScoresAsync(
        SpecificationComparisonDto comparison);
    
    /// <summary>
    /// Get default specification weights for a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Default weights for specifications</returns>
    Task<Dictionary<string, decimal>> GetDefaultSpecificationWeightsAsync(Guid categoryId);
}