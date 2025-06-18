using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for AI/ML category prediction
/// </summary>
public interface ICategoryPredictionService
{
    /// <summary>
    /// Predicts the category for a product based on extracted data
    /// </summary>
    Task<Result<CategoryPrediction>> PredictCategoryAsync(ProductExtractionResult productData);
    
    /// <summary>
    /// Predicts category using multiple methods and returns ranked results
    /// </summary>
    Task<Result<List<CategoryPrediction>>> GetCategoryPredictionsAsync(ProductExtractionResult productData, int maxResults = 3);
    
    /// <summary>
    /// Validates if a predicted category is reasonable for the product
    /// </summary>
    Task<Result<bool>> ValidateCategoryPredictionAsync(Guid categoryId, ProductExtractionResult productData);
    
    /// <summary>
    /// Trains or updates the ML model with new data (for future ML integration)
    /// </summary>
    Task<Result> UpdateModelAsync(List<TrainingData> trainingData);
    
    /// <summary>
    /// Gets prediction confidence threshold for auto-approval
    /// </summary>
    Task<Result<decimal>> GetConfidenceThresholdAsync(Guid categoryId);
}

/// <summary>
/// Training data for ML model updates
/// </summary>
public class TrainingData
{
    public string ProductName { get; set; } = null!;
    public string? Description { get; set; }
    public Dictionary<string, object>? Specifications { get; set; }
    public Guid CorrectCategoryId { get; set; }
    public string? Manufacturer { get; set; }
}