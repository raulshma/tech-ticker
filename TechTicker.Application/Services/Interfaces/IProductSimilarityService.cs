using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for product similarity matching
/// </summary>
public interface IProductSimilarityService
{
    /// <summary>
    /// Finds similar products based on extracted product data
    /// </summary>
    Task<Result<List<SimilarProductResult>>> FindSimilarProductsAsync(ProductExtractionResult productData, int maxResults = 5);
    
    /// <summary>
    /// Calculates similarity score between two products
    /// </summary>
    Task<Result<decimal>> CalculateSimilarityScoreAsync(Guid productId, ProductExtractionResult candidateData);
    
    /// <summary>
    /// Finds exact matches (potential duplicates)
    /// </summary>
    Task<Result<List<SimilarProductResult>>> FindExactMatchesAsync(ProductExtractionResult productData);
    
    /// <summary>
    /// Validates if a similarity match is reasonable
    /// </summary>
    Task<Result<bool>> ValidateSimilarityMatchAsync(Guid productId, ProductExtractionResult candidateData, decimal similarityScore);
    
    /// <summary>
    /// Gets similarity threshold for duplicate detection
    /// </summary>
    Task<Result<decimal>> GetDuplicateThresholdAsync();
    
    /// <summary>
    /// Updates similarity algorithms based on feedback
    /// </summary>
    Task<Result> UpdateSimilarityFeedbackAsync(Guid productId, ProductExtractionResult candidateData, bool isActuallySimilar);
}