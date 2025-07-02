using TechTicker.Application.DTOs;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for analyzing price differences and value propositions
/// </summary>
public interface IPriceAnalysisService
{
    /// <summary>
    /// Analyze price differences and value proposition between two products
    /// </summary>
    /// <param name="product1">First product with pricing</param>
    /// <param name="product2">Second product with pricing</param>
    /// <returns>Comprehensive price analysis</returns>
    Task<PriceAnalysisDto> AnalyzePricesAsync(
        ProductWithCurrentPricesDto product1, 
        ProductWithCurrentPricesDto product2);
    
    /// <summary>
    /// Calculate value score considering price and specifications
    /// </summary>
    /// <param name="product">Product with pricing</param>
    /// <param name="specificationScore">Product's specification score</param>
    /// <returns>Value score</returns>
    Task<decimal> CalculateValueScoreAsync(
        ProductWithCurrentPricesDto product, 
        decimal specificationScore);
}