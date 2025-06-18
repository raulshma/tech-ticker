using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Utilities;
using System.Text.RegularExpressions;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for AI/ML category prediction using rule-based logic
/// </summary>
public class CategoryPredictionService : ICategoryPredictionService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryPredictionService> _logger;

    // Rule-based category mapping patterns
    private static readonly Dictionary<string, List<CategoryRule>> CategoryRules = new()
    {
        {
            "Electronics", new List<CategoryRule>
            {
                new("laptop|notebook|computer", 0.9m, new[] { "laptop", "computer", "notebook", "PC" }),
                new("smartphone|phone|mobile", 0.9m, new[] { "phone", "smartphone", "mobile" }),
                new("tablet|ipad", 0.9m, new[] { "tablet", "iPad" }),
                new("headphone|earphone|earbuds", 0.8m, new[] { "headphone", "earphone", "earbuds" }),
                new("camera|photography", 0.8m, new[] { "camera", "lens", "photography" }),
                new("television|tv|monitor", 0.9m, new[] { "TV", "television", "monitor", "display" }),
                new("processor|cpu|gpu|graphics", 0.8m, new[] { "CPU", "GPU", "processor", "graphics" }),
                new("memory|ram|storage|ssd|hdd", 0.8m, new[] { "RAM", "memory", "SSD", "HDD", "storage" })
            }
        },
        {
            "Home & Garden", new List<CategoryRule>
            {
                new("furniture|chair|table|desk", 0.9m, new[] { "furniture", "chair", "table", "desk" }),
                new("kitchen|cooking|appliance", 0.8m, new[] { "kitchen", "cooking", "appliance" }),
                new("garden|outdoor|patio", 0.8m, new[] { "garden", "outdoor", "patio" }),
                new("lighting|lamp|light", 0.8m, new[] { "lighting", "lamp", "light" })
            }
        },
        {
            "Clothing & Accessories", new List<CategoryRule>
            {
                new("shirt|t-shirt|blouse", 0.9m, new[] { "shirt", "t-shirt", "blouse" }),
                new("pants|jeans|trousers", 0.9m, new[] { "pants", "jeans", "trousers" }),
                new("shoes|sneakers|boots", 0.9m, new[] { "shoes", "sneakers", "boots" }),
                new("watch|jewelry|accessories", 0.8m, new[] { "watch", "jewelry", "accessories" })
            }
        },
        {
            "Sports & Outdoors", new List<CategoryRule>
            {
                new("fitness|exercise|gym", 0.8m, new[] { "fitness", "exercise", "gym", "workout" }),
                new("camping|hiking|outdoor", 0.8m, new[] { "camping", "hiking", "outdoor" }),
                new("sports|athletic|recreation", 0.8m, new[] { "sports", "athletic", "recreation" })
            }
        },
        {
            "Books & Media", new List<CategoryRule>
            {
                new("book|novel|textbook", 0.9m, new[] { "book", "novel", "textbook" }),
                new("music|cd|vinyl|audio", 0.8m, new[] { "music", "CD", "vinyl", "audio" }),
                new("movie|dvd|blu-ray|video", 0.8m, new[] { "movie", "DVD", "Blu-ray", "video" })
            }
        },
        {
            "Automotive", new List<CategoryRule>
            {
                new("car|vehicle|auto|automotive", 0.9m, new[] { "car", "vehicle", "auto", "automotive" }),
                new("tire|wheel|brake|engine", 0.8m, new[] { "tire", "wheel", "brake", "engine" }),
                new("parts|accessories|tools", 0.7m, new[] { "parts", "accessories", "tools" })
            }
        }
    };

    // Manufacturer-based category hints
    private static readonly Dictionary<string, string> ManufacturerCategoryHints = new()
    {
        // Electronics
        { "apple", "Electronics" },
        { "samsung", "Electronics" },
        { "sony", "Electronics" },
        { "lg", "Electronics" },
        { "dell", "Electronics" },
        { "hp", "Electronics" },
        { "lenovo", "Electronics" },
        { "asus", "Electronics" },
        { "acer", "Electronics" },
        { "microsoft", "Electronics" },
        { "intel", "Electronics" },
        { "amd", "Electronics" },
        { "nvidia", "Electronics" },
        
        // Automotive
        { "ford", "Automotive" },
        { "toyota", "Automotive" },
        { "honda", "Automotive" },
        { "bmw", "Automotive" },
        { "mercedes", "Automotive" },
        
        // Sports
        { "nike", "Sports & Outdoors" },
        { "adidas", "Sports & Outdoors" },
        { "puma", "Sports & Outdoors" },
        { "under armour", "Sports & Outdoors" }
    };

    public CategoryPredictionService(
        ICategoryRepository categoryRepository,
        ILogger<CategoryPredictionService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<Result<CategoryPrediction>> PredictCategoryAsync(ProductExtractionResult productData)
    {
        try
        {
            _logger.LogInformation("Predicting category for product: {ProductName}", productData.ExtractedProductName);

            var predictions = await GetCategoryPredictionsAsync(productData, 1);
            if (predictions.IsFailure || predictions.Data == null || !predictions.Data.Any())
            {
                return Result<CategoryPrediction>.Failure("No category prediction could be made");
            }

            return Result<CategoryPrediction>.Success(predictions.Data.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting category for product: {ProductName}", productData.ExtractedProductName);
            return Result<CategoryPrediction>.Failure(ex);
        }
    }

    public async Task<Result<List<CategoryPrediction>>> GetCategoryPredictionsAsync(ProductExtractionResult productData, int maxResults = 3)
    {
        try
        {
            var allCategories = await _categoryRepository.GetAllAsync();
            var predictions = new List<CategoryPrediction>();

            // Combine all text for analysis
            var combinedText = string.Join(" ", new[]
            {
                productData.ExtractedProductName ?? "",
                productData.ExtractedDescription ?? "",
                productData.ExtractedManufacturer ?? "",
                string.Join(" ", productData.ExtractedSpecifications?.Values?.Select(v => v?.ToString()) ?? new string[0])
            }).ToLowerInvariant();

            // Rule-based prediction
            foreach (var categoryRule in CategoryRules)
            {
                var categoryName = categoryRule.Key;
                var category = allCategories.FirstOrDefault(c => 
                    c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                
                if (category == null) continue;

                var bestRule = GetBestMatchingRule(combinedText, categoryRule.Value);
                if (bestRule != null)
                {
                    predictions.Add(new CategoryPrediction
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.Name,
                        ConfidenceScore = bestRule.BaseConfidence,
                        PredictionMethod = "RULE_BASED",
                        PredictionMetadata = new Dictionary<string, object>
                        {
                            ["MatchedPattern"] = bestRule.Pattern,
                            ["MatchedKeywords"] = bestRule.Keywords,
                            ["MatchedText"] = combinedText
                        }
                    });
                }
            }

            // Manufacturer-based hint
            if (!string.IsNullOrWhiteSpace(productData.ExtractedManufacturer))
            {
                var manufacturerHint = GetManufacturerCategoryHint(productData.ExtractedManufacturer);
                if (manufacturerHint != null)
                {
                    var hintCategory = allCategories.FirstOrDefault(c => 
                        c.Name.Equals(manufacturerHint, StringComparison.OrdinalIgnoreCase));
                    
                    if (hintCategory != null)
                    {
                        var existingPrediction = predictions.FirstOrDefault(p => p.CategoryId == hintCategory.CategoryId);
                        if (existingPrediction != null)
                        {
                            // Boost confidence if manufacturer hint matches rule-based prediction
                            existingPrediction.ConfidenceScore = Math.Min(1.0m, existingPrediction.ConfidenceScore + 0.1m);
                            existingPrediction.PredictionMetadata!["ManufacturerBoost"] = true;
                        }
                        else
                        {
                            predictions.Add(new CategoryPrediction
                            {
                                CategoryId = hintCategory.CategoryId,
                                CategoryName = hintCategory.Name,
                                ConfidenceScore = 0.6m,
                                PredictionMethod = "MANUFACTURER_HINT",
                                PredictionMetadata = new Dictionary<string, object>
                                {
                                    ["Manufacturer"] = productData.ExtractedManufacturer,
                                    ["HintCategory"] = manufacturerHint
                                }
                            });
                        }
                    }
                }
            }

            // Fallback to general category if no specific match
            if (!predictions.Any())
            {
                var generalCategory = allCategories.FirstOrDefault(c => 
                    c.Name.Equals("General", StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Equals("Other", StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Equals("Miscellaneous", StringComparison.OrdinalIgnoreCase));

                if (generalCategory != null)
                {
                    predictions.Add(new CategoryPrediction
                    {
                        CategoryId = generalCategory.CategoryId,
                        CategoryName = generalCategory.Name,
                        ConfidenceScore = 0.3m,
                        PredictionMethod = "FALLBACK",
                        PredictionMetadata = new Dictionary<string, object>
                        {
                            ["Reason"] = "No specific category match found"
                        }
                    });
                }
            }

            // Sort by confidence and take top results
            var topPredictions = predictions
                .OrderByDescending(p => p.ConfidenceScore)
                .Take(maxResults)
                .ToList();

            _logger.LogInformation("Generated {Count} category predictions for product: {ProductName}", 
                topPredictions.Count, productData.ExtractedProductName);

            return Result<List<CategoryPrediction>>.Success(topPredictions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category predictions");
            return Result<List<CategoryPrediction>>.Failure(ex);
        }
    }

    public async Task<Result<bool>> ValidateCategoryPredictionAsync(Guid categoryId, ProductExtractionResult productData)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
            {
                return Result<bool>.Success(false);
            }

            var predictions = await GetCategoryPredictionsAsync(productData, 5);
            if (predictions.IsFailure)
            {
                return Result<bool>.Success(false);
            }

            var matchingPrediction = predictions.Data!.FirstOrDefault(p => p.CategoryId == categoryId);
            var isValid = matchingPrediction != null && matchingPrediction.ConfidenceScore >= 0.3m;

            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating category prediction for category {CategoryId}", categoryId);
            return Result<bool>.Failure(ex);
        }
    }

    public async Task<Result> UpdateModelAsync(List<TrainingData> trainingData)
    {
        try
        {
            _logger.LogInformation("Received {Count} training data items for model update", trainingData.Count);

            // For now, this is a placeholder for future ML model integration
            // In a real implementation, this would update ML model weights or retrain the model

            // Simulate async work for future ML integration
            await Task.Delay(1, CancellationToken.None);

            _logger.LogInformation("Training data logged for future ML model integration");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model with training data");
            return Result.Failure(ex);
        }
    }

    public async Task<Result<decimal>> GetConfidenceThresholdAsync(Guid categoryId)
    {
        try
        {
            // Default confidence thresholds based on category type
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
            {
                return Result<decimal>.Success(0.7m); // Default threshold
            }

            // Category-specific thresholds
            var threshold = category.Name.ToLowerInvariant() switch
            {
                var name when name.Contains("electronics") => 0.8m,
                var name when name.Contains("automotive") => 0.9m,
                var name when name.Contains("clothing") => 0.7m,
                var name when name.Contains("books") => 0.9m,
                var name when name.Contains("sports") => 0.7m,
                var name when name.Contains("home") || name.Contains("garden") => 0.6m,
                _ => 0.7m
            };

            return Result<decimal>.Success(threshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting confidence threshold for category {CategoryId}", categoryId);
            return Result<decimal>.Failure(ex);
        }
    }

    private static CategoryRule? GetBestMatchingRule(string text, List<CategoryRule> rules)
    {
        CategoryRule? bestRule = null;
        var bestScore = 0m;

        foreach (var rule in rules)
        {
            var score = CalculateRuleScore(text, rule);
            if (score > bestScore)
            {
                bestScore = score;
                bestRule = rule;
            }
        }

        return bestScore >= 0.1m ? bestRule : null;
    }

    private static decimal CalculateRuleScore(string text, CategoryRule rule)
    {
        // Check regex pattern match
        var patternMatch = Regex.IsMatch(text, rule.Pattern, RegexOptions.IgnoreCase);
        if (!patternMatch) return 0m;

        // Calculate keyword match score
        var keywordMatches = rule.Keywords.Count(keyword => 
            text.Contains(keyword.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        
        var keywordScore = rule.Keywords.Any() ? (decimal)keywordMatches / rule.Keywords.Length : 1m;
        
        // Combine pattern match with keyword score
        return rule.BaseConfidence * keywordScore;
    }

    private static string? GetManufacturerCategoryHint(string manufacturer)
    {
        var manufacturerLower = manufacturer.ToLowerInvariant();
        return ManufacturerCategoryHints.FirstOrDefault(kvp => 
            manufacturerLower.Contains(kvp.Key)).Value;
    }

    private class CategoryRule
    {
        public string Pattern { get; }
        public decimal BaseConfidence { get; }
        public string[] Keywords { get; }

        public CategoryRule(string pattern, decimal baseConfidence, string[] keywords)
        {
            Pattern = pattern;
            BaseConfidence = baseConfidence;
            Keywords = keywords;
        }
    }
}