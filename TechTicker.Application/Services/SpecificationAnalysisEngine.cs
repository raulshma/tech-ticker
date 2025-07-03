using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using System.Text.Json;

namespace TechTicker.Application.Services;

/// <summary>
/// Engine for analyzing and comparing product specifications
/// </summary>
public class SpecificationAnalysisEngine : ISpecificationAnalysisEngine
{
    private readonly ILogger<SpecificationAnalysisEngine> _logger;
    private readonly IUnitOfWork _unitOfWork;

    // Default weights for common specification categories
    private readonly Dictionary<string, decimal> _defaultCategoryWeights = new()
    {
        { "performance", 0.30m },
        { "display", 0.25m },
        { "memory", 0.15m },
        { "storage", 0.15m },
        { "connectivity", 0.10m },
        { "general", 0.05m }
    };

    public SpecificationAnalysisEngine(
        ILogger<SpecificationAnalysisEngine> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Analyze and compare specifications between two products
    /// </summary>
    /// <param name="product1">First product</param>
    /// <param name="product2">Second product</param>
    /// <param name="weights">Optional specification weights</param>
    /// <returns>Detailed specification comparison</returns>
    public async Task<SpecificationComparisonDto> AnalyzeSpecificationsAsync(
        ProductDto product1, 
        ProductDto product2, 
        Dictionary<string, decimal>? weights = null)
    {
        try
        {
            _logger.LogInformation("Analyzing specifications for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);

            var spec1 = BuildSpecificationDictionary(product1);
            var spec2 = BuildSpecificationDictionary(product2);
            
            // If product specifications are empty, try to get them from ProductSellerMapping
            if (spec1.Count == 0)
            {
                var mappingSpecs1 = await GetSpecificationsFromMappingAsync(product1.ProductId);
                if (mappingSpecs1 != null && mappingSpecs1.Count > 0)
                {
                    _logger.LogInformation("Using specifications from ProductSellerMapping for product {ProductId}", product1.ProductId);
                    spec1 = mappingSpecs1;
                }
            }
            
            if (spec2.Count == 0)
            {
                var mappingSpecs2 = await GetSpecificationsFromMappingAsync(product2.ProductId);
                if (mappingSpecs2 != null && mappingSpecs2.Count > 0)
                {
                    _logger.LogInformation("Using specifications from ProductSellerMapping for product {ProductId}", product2.ProductId);
                    spec2 = mappingSpecs2;
                }
            }

            var differences = new List<SpecificationDifferenceDto>();
            var matches = new List<SpecificationMatchDto>();
            var categoryScores = new Dictionary<string, CategoryScoreDto>();

            // Get all unique specification keys
            var allKeys = spec1.Keys.Union(spec2.Keys).ToHashSet();

            // Use provided weights or get defaults for the category
            var specWeights = weights ?? await GetDefaultSpecificationWeightsAsync(product1.CategoryId);

            foreach (var key in allKeys)
            {
                var hasSpec1 = spec1.TryGetValue(key, out var value1);
                var hasSpec2 = spec2.TryGetValue(key, out var value2);

                var category = DetermineSpecificationCategory(key);
                var displayName = FormatDisplayName(key);

                if (hasSpec1 && hasSpec2)
                {
                    // Both products have this specification
                    var comparison = CompareSpecificationValues(value1!, value2!, key);
                    
                    if (comparison.ComparisonResult == ComparisonResultType.Equivalent)
                    {
                        matches.Add(new SpecificationMatchDto
                        {
                            SpecificationKey = key,
                            DisplayName = displayName,
                            Category = category,
                            Value = value1!,
                            DisplayValue = FormatValue(value1!)
                        });
                    }
                    else
                    {
                        differences.Add(new SpecificationDifferenceDto
                        {
                            SpecificationKey = key,
                            DisplayName = displayName,
                            Category = category,
                            Product1Value = value1,
                            Product2Value = value2,
                            Product1DisplayValue = FormatValue(value1!),
                            Product2DisplayValue = FormatValue(value2!),
                            ComparisonResult = comparison.ComparisonResult,
                            ImpactScore = comparison.ImpactScore,
                            AnalysisNote = comparison.AnalysisNote
                        });
                    }
                }
                else if (hasSpec1)
                {
                    // Only product1 has this specification
                    differences.Add(new SpecificationDifferenceDto
                    {
                        SpecificationKey = key,
                        DisplayName = displayName,
                        Category = category,
                        Product1Value = value1,
                        Product2Value = null,
                        Product1DisplayValue = FormatValue(value1!),
                        Product2DisplayValue = "Not specified",
                        ComparisonResult = ComparisonResultType.Product1Only,
                        ImpactScore = CalculateImpactScore(key, true),
                        AnalysisNote = "Only available on first product"
                    });
                }
                else if (hasSpec2)
                {
                    // Only product2 has this specification
                    differences.Add(new SpecificationDifferenceDto
                    {
                        SpecificationKey = key,
                        DisplayName = displayName,
                        Category = category,
                        Product1Value = null,
                        Product2Value = value2,
                        Product1DisplayValue = "Not specified",
                        Product2DisplayValue = FormatValue(value2!),
                        ComparisonResult = ComparisonResultType.Product2Only,
                        ImpactScore = CalculateImpactScore(key, true),
                        AnalysisNote = "Only available on second product"
                    });
                }
            }

            // Calculate category scores
            categoryScores = await CalculateCategoryScoresAsync(differences, matches, specWeights);

            _logger.LogInformation("Completed specification analysis for products {Product1} and {Product2}. Found {Differences} differences and {Matches} matches", 
                product1.ProductId, product2.ProductId, differences.Count, matches.Count);

            return new SpecificationComparisonDto
            {
                Differences = differences,
                Matches = matches,
                CategoryScores = categoryScores
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing specifications for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Calculate overall scores for products based on specifications
    /// </summary>
    /// <param name="comparison">Specification comparison result</param>
    /// <returns>Overall scores for both products</returns>
    public async Task<(decimal product1Score, decimal product2Score)> CalculateOverallScoresAsync(
        SpecificationComparisonDto comparison)
    {
        try
        {
            decimal product1Score = 0;
            decimal product2Score = 0;

            foreach (var categoryScore in comparison.CategoryScores.Values)
            {
                product1Score += categoryScore.Product1Score * categoryScore.Weight;
                product2Score += categoryScore.Product2Score * categoryScore.Weight;
            }

            // Normalize scores to 0-1 range
            var maxPossibleScore = comparison.CategoryScores.Values.Sum(cs => cs.Weight);
            if (maxPossibleScore > 0)
            {
                product1Score = product1Score / maxPossibleScore;
                product2Score = product2Score / maxPossibleScore;
            }

            _logger.LogInformation("Calculated overall scores: Product1={Product1Score:F2}, Product2={Product2Score:F2}", 
                product1Score, product2Score);

            return await Task.FromResult((product1Score, product2Score));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating overall scores");
            throw;
        }
    }

    /// <summary>
    /// Get default specification weights for a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Default weights for specifications</returns>
    public async Task<Dictionary<string, decimal>> GetDefaultSpecificationWeightsAsync(Guid categoryId)
    {
        try
        {
            // For now, return default weights. In a real implementation, 
            // this could be retrieved from a database based on category
            _logger.LogInformation("Getting default specification weights for category {CategoryId}", categoryId);

            return await Task.FromResult(new Dictionary<string, decimal>(_defaultCategoryWeights));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default specification weights for category {CategoryId}", categoryId);
            throw;
        }
    }

    /// <summary>
    /// Compare two specification values and determine which is better
    /// </summary>
    private (ComparisonResultType ComparisonResult, decimal ImpactScore, string AnalysisNote) CompareSpecificationValues(
        object value1, object value2, string specificationKey)
    {
        try
        {
            // Handle numeric comparisons
            if (IsNumericValue(value1) && IsNumericValue(value2))
            {
                var num1 = Convert.ToDecimal(value1);
                var num2 = Convert.ToDecimal(value2);

                var isHigherBetter = IsHigherBetter(specificationKey);
                var numericImpactScore = CalculateNumericImpactScore(num1, num2, specificationKey);

                if (Math.Abs(num1 - num2) < 0.01m) // Consider very close values as equivalent
                {
                    return (ComparisonResultType.Equivalent, 0, "Values are essentially equivalent");
                }

                if (isHigherBetter)
                {
                    if (num1 > num2)
                        return (ComparisonResultType.Product1Better, numericImpactScore, $"Higher value is better for {specificationKey}");
                    else
                        return (ComparisonResultType.Product2Better, numericImpactScore, $"Higher value is better for {specificationKey}");
                }
                else
                {
                    if (num1 < num2)
                        return (ComparisonResultType.Product1Better, numericImpactScore, $"Lower value is better for {specificationKey}");
                    else
                        return (ComparisonResultType.Product2Better, numericImpactScore, $"Lower value is better for {specificationKey}");
                }
            }

            // Handle boolean comparisons
            if (value1 is bool bool1 && value2 is bool bool2)
            {
                if (bool1 == bool2)
                    return (ComparisonResultType.Equivalent, 0, "Both products have the same capability");
                
                var boolImpactScore = CalculateImpactScore(specificationKey, true);
                return bool1
                    ? (ComparisonResultType.Product1Better, boolImpactScore, "Feature available on first product only")
                    : (ComparisonResultType.Product2Better, boolImpactScore, "Feature available on second product only");
            }

            // Handle string comparisons
            var str1 = value1?.ToString() ?? "";
            var str2 = value2?.ToString() ?? "";

            if (string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase))
            {
                return (ComparisonResultType.Equivalent, 0, "Values are identical");
            }

            // For non-numeric, non-boolean differences, try to provide meaningful comparison
            var impactScore = CalculateImpactScore(specificationKey, false);
            return (ComparisonResultType.Incomparable, impactScore, "Values differ but cannot be directly compared");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error comparing specification values for {Key}", specificationKey);
            return (ComparisonResultType.Incomparable, 0, "Comparison failed");
        }
    }

    /// <summary>
    /// Calculate category-based scores for both products
    /// </summary>
    private async Task<Dictionary<string, CategoryScoreDto>> CalculateCategoryScoresAsync(
        List<SpecificationDifferenceDto> differences,
        List<SpecificationMatchDto> matches,
        Dictionary<string, decimal> weights)
    {
        var categoryScores = new Dictionary<string, CategoryScoreDto>();

        // Group specifications by category
        var allSpecs = differences.Cast<object>().Concat(matches.Cast<object>()).ToList();
        var categories = new HashSet<string>();

        foreach (var diff in differences)
            categories.Add(diff.Category);
        foreach (var match in matches)
            categories.Add(match.Category);

        foreach (var category in categories)
        {
            var categoryDifferences = differences.Where(d => d.Category == category).ToList();
            var categoryMatches = matches.Where(m => m.Category == category).ToList();

            var totalSpecs = categoryDifferences.Count + categoryMatches.Count;
            if (totalSpecs == 0) continue;

            decimal product1Score = 0;
            decimal product2Score = 0;

            // Score based on differences
            foreach (var diff in categoryDifferences)
            {
                switch (diff.ComparisonResult)
                {
                    case ComparisonResultType.Product1Better:
                        product1Score += diff.ImpactScore ?? 1;
                        break;
                    case ComparisonResultType.Product2Better:
                        product2Score += diff.ImpactScore ?? 1;
                        break;
                    case ComparisonResultType.Product1Only:
                        product1Score += (diff.ImpactScore ?? 1) * 0.7m; // Slight penalty for missing from other product
                        break;
                    case ComparisonResultType.Product2Only:
                        product2Score += (diff.ImpactScore ?? 1) * 0.7m;
                        break;
                    case ComparisonResultType.Equivalent:
                    case ComparisonResultType.Incomparable:
                        product1Score += 0.5m;
                        product2Score += 0.5m;
                        break;
                }
            }

            // Score for matches (both products get equal points)
            var matchScore = categoryMatches.Count * 0.5m;
            product1Score += matchScore;
            product2Score += matchScore;

            // Normalize scores to 0-1 range
            var maxScore = Math.Max(product1Score, product2Score);
            if (maxScore > 0)
            {
                product1Score = product1Score / maxScore;
                product2Score = product2Score / maxScore;
            }

            var weight = GetCategoryWeight(category, weights);
            var analysis = GenerateCategoryAnalysis(category, categoryDifferences, categoryMatches);

            categoryScores[category] = new CategoryScoreDto
            {
                CategoryName = category,
                Product1Score = product1Score,
                Product2Score = product2Score,
                Weight = weight,
                Analysis = analysis
            };
        }

        return await Task.FromResult(categoryScores);
    }

    /// <summary>
    /// Determine the category for a specification key
    /// </summary>
    private string DetermineSpecificationCategory(string key)
    {
        var lowerKey = key.ToLowerInvariant();

        if (lowerKey.Contains("cpu") || lowerKey.Contains("processor") || lowerKey.Contains("gpu") || 
            lowerKey.Contains("graphics") || lowerKey.Contains("performance") || lowerKey.Contains("benchmark"))
            return "performance";

        if (lowerKey.Contains("display") || lowerKey.Contains("screen") || lowerKey.Contains("resolution") || 
            lowerKey.Contains("size") || lowerKey.Contains("inch"))
            return "display";

        if (lowerKey.Contains("memory") || lowerKey.Contains("ram"))
            return "memory";

        if (lowerKey.Contains("storage") || lowerKey.Contains("disk") || lowerKey.Contains("ssd") || 
            lowerKey.Contains("hdd") || lowerKey.Contains("capacity"))
            return "storage";

        if (lowerKey.Contains("wifi") || lowerKey.Contains("bluetooth") || lowerKey.Contains("usb") || 
            lowerKey.Contains("port") || lowerKey.Contains("connectivity"))
            return "connectivity";

        return "general";
    }

    /// <summary>
    /// Format a specification key into a user-friendly display name
    /// </summary>
    private string FormatDisplayName(string key)
    {
        // Convert camelCase and snake_case to Title Case
        var formatted = key.Replace("_", " ").Replace("-", " ");
        var words = formatted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            // Handle camelCase within words
            if (words[i].Length > 1 && char.IsLower(words[i][0]))
            {
                var camelCaseWords = System.Text.RegularExpressions.Regex.Split(words[i], @"(?<!^)(?=[A-Z])");
                words[i] = string.Join(" ", camelCaseWords);
            }
            
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }

        return string.Join(" ", words);
    }

    /// <summary>
    /// Format a specification value for display
    /// </summary>
    private string FormatValue(object value)
    {
        return value switch
        {
            bool b => b ? "Yes" : "No",
            decimal d => d.ToString("N2"),
            double d => d.ToString("N2"),
            float f => f.ToString("N2"),
            int i => i.ToString("N0"),
            long l => l.ToString("N0"),
            _ => value?.ToString() ?? "Unknown"
        };
    }

    /// <summary>
    /// Check if a value is numeric
    /// </summary>
    private bool IsNumericValue(object value)
    {
        return value is decimal || value is double || value is float || value is int || value is long ||
               (value is string str && decimal.TryParse(str, out _));
    }

    /// <summary>
    /// Determine if higher values are better for a specification
    /// </summary>
    private bool IsHigherBetter(string specificationKey)
    {
        var lowerKey = specificationKey.ToLowerInvariant();

        // Higher is better for performance metrics
        if (lowerKey.Contains("speed") || lowerKey.Contains("frequency") || lowerKey.Contains("memory") ||
            lowerKey.Contains("storage") || lowerKey.Contains("capacity") || lowerKey.Contains("resolution") ||
            lowerKey.Contains("performance") || lowerKey.Contains("score") || lowerKey.Contains("fps"))
            return true;

        // Lower is better for negative metrics
        if (lowerKey.Contains("latency") || lowerKey.Contains("delay") || lowerKey.Contains("weight") ||
            lowerKey.Contains("power") || lowerKey.Contains("consumption"))
            return false;

        // Default to higher is better
        return true;
    }

    /// <summary>
    /// Calculate impact score for numeric differences
    /// </summary>
    private decimal CalculateNumericImpactScore(decimal value1, decimal value2, string specificationKey)
    {
        var diff = Math.Abs(value1 - value2);
        var avg = (value1 + value2) / 2;
        
        if (avg == 0) return 0.5m;

        var percentageDiff = diff / avg;
        
        // Scale impact based on the type of specification
        var baseImpact = Math.Min(percentageDiff * 2, 1.0m); // Cap at 1.0
        
        var importance = GetSpecificationImportance(specificationKey);
        return baseImpact * importance;
    }

    /// <summary>
    /// Calculate general impact score for a specification
    /// </summary>
    private decimal CalculateImpactScore(string specificationKey, bool isDifference)
    {
        var importance = GetSpecificationImportance(specificationKey);
        return isDifference ? importance : importance * 0.5m;
    }

    /// <summary>
    /// Get importance weight for a specification
    /// </summary>
    private decimal GetSpecificationImportance(string specificationKey)
    {
        var lowerKey = specificationKey.ToLowerInvariant();

        if (lowerKey.Contains("cpu") || lowerKey.Contains("processor") || lowerKey.Contains("performance"))
            return 1.0m;
        if (lowerKey.Contains("memory") || lowerKey.Contains("ram") || lowerKey.Contains("storage"))
            return 0.9m;
        if (lowerKey.Contains("display") || lowerKey.Contains("screen") || lowerKey.Contains("resolution"))
            return 0.8m;
        if (lowerKey.Contains("gpu") || lowerKey.Contains("graphics"))
            return 0.8m;
        if (lowerKey.Contains("connectivity") || lowerKey.Contains("port"))
            return 0.6m;

        return 0.5m; // Default importance
    }

    /// <summary>
    /// Get weight for a category
    /// </summary>
    private decimal GetCategoryWeight(string category, Dictionary<string, decimal> weights)
    {
        return weights.TryGetValue(category, out var weight) ? weight : 0.1m;
    }

    /// <summary>
    /// Generate analysis text for a category
    /// </summary>
    private string GenerateCategoryAnalysis(string category, 
        List<SpecificationDifferenceDto> differences, 
        List<SpecificationMatchDto> matches)
    {
        var totalSpecs = differences.Count + matches.Count;
        var matchCount = matches.Count;
        var diffCount = differences.Count;

        var product1Advantages = differences.Count(d => 
            d.ComparisonResult == ComparisonResultType.Product1Better || 
            d.ComparisonResult == ComparisonResultType.Product1Only);

        var product2Advantages = differences.Count(d => 
            d.ComparisonResult == ComparisonResultType.Product2Better || 
            d.ComparisonResult == ComparisonResultType.Product2Only);

        return $"Category analysis: {matchCount} matching specifications, {diffCount} differences. " +
               $"Product 1 advantages: {product1Advantages}, Product 2 advantages: {product2Advantages}.";
    }
    
    /// <summary>
    /// Get specifications from ProductSellerMapping if the product itself doesn't have them
    /// </summary>
    /// <param name="productId">The ID of the product to get specifications for</param>
    /// <returns>Dictionary of specifications or null if none found</returns>
    private async Task<Dictionary<string, object>?> GetSpecificationsFromMappingAsync(Guid productId)
    {
        try
        {
            // Get ProductSellerMapping entries for this product with specifications
            var mappings = await _unitOfWork.ProductSellerMappings.FindAsync(
                m => m.CanonicalProductId == productId && 
                     m.LatestSpecifications != null && 
                     m.LatestSpecifications != "");
                
            // Order by quality score and recency
            var bestMapping = mappings
                .OrderByDescending(m => m.SpecificationsQualityScore)
                .ThenByDescending(m => m.SpecificationsLastUpdated)
                .FirstOrDefault();
                
            if (bestMapping == null || string.IsNullOrEmpty(bestMapping.LatestSpecifications))
            {
                return null;
            }
            
            // Parse JSON string to dictionary
            var specifications = JsonSerializer.Deserialize<Dictionary<string, object>>(bestMapping.LatestSpecifications);
            return specifications;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting specifications from ProductSellerMapping for product {ProductId}", productId);
            return null;
        }
    }

    private static Dictionary<string, object> BuildSpecificationDictionary(ProductDto product)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        
        // Add normalized specifications (preferred)
        if (product.NormalizedSpecifications != null && product.NormalizedSpecifications.Count > 0)
        {
            foreach (var kvp in product.NormalizedSpecifications)
            {
                result[kvp.Key] = kvp.Value?.Value ?? string.Empty;
            }
        }
        
        // Add uncategorized specifications as fallback
        if (product.UncategorizedSpecifications != null && product.UncategorizedSpecifications.Count > 0)
        {
            foreach (var kvp in product.UncategorizedSpecifications)
            {
                if (!result.ContainsKey(kvp.Key)) // Don't override normalized specs
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }
}