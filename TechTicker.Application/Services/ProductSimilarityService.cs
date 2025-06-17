using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Shared.Utilities;
using System.Text.RegularExpressions;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for product similarity matching using text similarity algorithms
/// </summary>
public class ProductSimilarityService : IProductSimilarityService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductSimilarityService> _logger;

    // Similarity thresholds
    private const decimal ExactMatchThreshold = 0.95m;
    private const decimal HighSimilarityThreshold = 0.8m;
    private const decimal ModerateSimilarityThreshold = 0.6m;
    private const decimal DefaultDuplicateThreshold = 0.85m;

    // Weight factors for different matching methods
    private const decimal NameWeight = 0.4m;
    private const decimal ManufacturerWeight = 0.2m;
    private const decimal ModelWeight = 0.3m;
    private const decimal SpecificationWeight = 0.1m;

    public ProductSimilarityService(
        IProductRepository productRepository,
        ILogger<ProductSimilarityService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<Result<List<SimilarProductResult>>> FindSimilarProductsAsync(ProductExtractionResult productData, int maxResults = 5)
    {
        try
        {
            _logger.LogInformation("Finding similar products for: {ProductName}", productData.ExtractedProductName);

            var allProducts = await _productRepository.GetAllAsync();
            var similarProducts = new List<SimilarProductResult>();

            foreach (var product in allProducts)
            {
                var similarityScore = await CalculateSimilarityScoreAsync(product.ProductId, productData);
                if (similarityScore.IsSuccess && similarityScore.Data >= ModerateSimilarityThreshold)
                {
                    var matchingMethod = DetermineMatchingMethod(similarityScore.Data);
                    var matchingFields = GetMatchingFields(product, productData);

                    similarProducts.Add(new SimilarProductResult
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        SimilarityScore = similarityScore.Data,
                        MatchingMethod = matchingMethod,
                        MatchingFields = matchingFields,
                        MatchMetadata = new Dictionary<string, object>
                        {
                            ["Manufacturer"] = product.Manufacturer ?? "Unknown",
                            ["ModelNumber"] = product.ModelNumber ?? "Unknown",
                            ["Category"] = "Unknown", // Would need to load category
                            ["CalculatedAt"] = DateTimeOffset.UtcNow
                        }
                    });
                }
            }

            // Sort by similarity score (highest first) and take top results
            var topSimilar = similarProducts
                .OrderByDescending(p => p.SimilarityScore)
                .Take(maxResults)
                .ToList();

            _logger.LogInformation("Found {Count} similar products for: {ProductName}", 
                topSimilar.Count, productData.ExtractedProductName);

            return Result<List<SimilarProductResult>>.Success(topSimilar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding similar products for: {ProductName}", productData.ExtractedProductName);
            return Result<List<SimilarProductResult>>.Failure(ex);
        }
    }

    public async Task<Result<decimal>> CalculateSimilarityScoreAsync(Guid productId, ProductExtractionResult candidateData)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return Result<decimal>.Failure("Product not found");
            }

            // Calculate weighted similarity scores for different fields
            var nameScore = CalculateTextSimilarity(
                product.Name ?? "", 
                candidateData.ExtractedProductName ?? "");

            var manufacturerScore = CalculateTextSimilarity(
                product.Manufacturer ?? "", 
                candidateData.ExtractedManufacturer ?? "");

            var modelScore = CalculateTextSimilarity(
                product.ModelNumber ?? "", 
                candidateData.ExtractedModelNumber ?? "");

            var specificationScore = CalculateSpecificationSimilarity(
                DeserializeSpecifications(product.Specifications),
                candidateData.ExtractedSpecifications);

            // Calculate weighted total score
            var totalScore = (nameScore * NameWeight) +
                           (manufacturerScore * ManufacturerWeight) +
                           (modelScore * ModelWeight) +
                           (specificationScore * SpecificationWeight);

            // Apply bonuses for exact matches
            if (manufacturerScore > 0.9m && !string.IsNullOrWhiteSpace(product.Manufacturer))
            {
                totalScore += 0.1m; // Manufacturer match bonus
            }

            if (modelScore > 0.9m && !string.IsNullOrWhiteSpace(product.ModelNumber))
            {
                totalScore += 0.1m; // Model number match bonus
            }

            // Ensure score doesn't exceed 1.0
            totalScore = Math.Min(1.0m, totalScore);

            return Result<decimal>.Success(totalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating similarity score for product {ProductId}", productId);
            return Result<decimal>.Failure(ex);
        }
    }

    public async Task<Result<List<SimilarProductResult>>> FindExactMatchesAsync(ProductExtractionResult productData)
    {
        try
        {
            var similarProducts = await FindSimilarProductsAsync(productData, 10);
            if (similarProducts.IsFailure)
            {
                return Result<List<SimilarProductResult>>.Failure(similarProducts.ErrorMessage!);
            }

            var exactMatches = similarProducts.Data!
                .Where(p => p.SimilarityScore >= ExactMatchThreshold)
                .ToList();

            _logger.LogInformation("Found {Count} exact matches for: {ProductName}", 
                exactMatches.Count, productData.ExtractedProductName);

            return Result<List<SimilarProductResult>>.Success(exactMatches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding exact matches for: {ProductName}", productData.ExtractedProductName);
            return Result<List<SimilarProductResult>>.Failure(ex);
        }
    }

    public async Task<Result<bool>> ValidateSimilarityMatchAsync(Guid productId, ProductExtractionResult candidateData, decimal similarityScore)
    {
        try
        {
            // Recalculate to verify the provided score
            var calculatedScore = await CalculateSimilarityScoreAsync(productId, candidateData);
            if (calculatedScore.IsFailure)
            {
                return Result<bool>.Success(false);
            }

            // Allow small tolerance for calculation differences
            var scoreDifference = Math.Abs(calculatedScore.Data - similarityScore);
            var isValid = scoreDifference <= 0.05m && similarityScore >= ModerateSimilarityThreshold;

            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating similarity match for product {ProductId}", productId);
            return Result<bool>.Failure(ex);
        }
    }

    public async Task<Result<decimal>> GetDuplicateThresholdAsync()
    {
        try
        {
            // For now, return a static threshold
            // In future, this could be dynamic based on category or user settings
            return Result<decimal>.Success(DefaultDuplicateThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting duplicate threshold");
            return Result<decimal>.Failure(ex);
        }
    }

    public async Task<Result> UpdateSimilarityFeedbackAsync(Guid productId, ProductExtractionResult candidateData, bool isActuallySimilar)
    {
        try
        {
            _logger.LogInformation("Received similarity feedback for product {ProductId}: {IsActuallySimilar}", 
                productId, isActuallySimilar);

            // For now, just log the feedback
            // In future implementations, this could be used to:
            // 1. Adjust similarity algorithm weights
            // 2. Train ML models for better similarity detection
            // 3. Store feedback for analysis and improvement

            var calculatedScore = await CalculateSimilarityScoreAsync(productId, candidateData);
            if (calculatedScore.IsSuccess)
            {
                _logger.LogInformation("Feedback: ProductId={ProductId}, CalculatedScore={Score}, ActuallySimilar={IsActuallySimilar}", 
                    productId, calculatedScore.Data, isActuallySimilar);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating similarity feedback");
            return Result.Failure(ex);
        }
    }

    private static decimal CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) && string.IsNullOrWhiteSpace(text2))
            return 1.0m;

        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0.0m;

        // Normalize texts
        var normalized1 = NormalizeText(text1);
        var normalized2 = NormalizeText(text2);

        // Exact match
        if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        // Levenshtein distance similarity
        var levenshteinSimilarity = CalculateLevenshteinSimilarity(normalized1, normalized2);
        
        // Jaccard similarity (word-based)
        var jaccardSimilarity = CalculateJaccardSimilarity(normalized1, normalized2);
        
        // Longest common subsequence similarity
        var lcsSimilarity = CalculateLCSSimilarity(normalized1, normalized2);

        // Return weighted average of different similarity measures
        return (levenshteinSimilarity * 0.4m) + (jaccardSimilarity * 0.4m) + (lcsSimilarity * 0.2m);
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Remove special characters, normalize whitespace, convert to lowercase
        var normalized = Regex.Replace(text, @"[^\w\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized.Trim().ToLowerInvariant();
    }

    private static decimal CalculateLevenshteinSimilarity(string s1, string s2)
    {
        var distance = CalculateLevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        
        if (maxLength == 0)
            return 1.0m;

        return 1.0m - ((decimal)distance / maxLength);
    }

    private static int CalculateLevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (var i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= s1.Length; i++)
        {
            for (var j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    private static decimal CalculateJaccardSimilarity(string s1, string s2)
    {
        var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (!words1.Any() && !words2.Any())
            return 1.0m;

        if (!words1.Any() || !words2.Any())
            return 0.0m;

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return (decimal)intersection / union;
    }

    private static decimal CalculateLCSSimilarity(string s1, string s2)
    {
        var lcsLength = CalculateLCS(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);

        if (maxLength == 0)
            return 1.0m;

        return (decimal)lcsLength / maxLength;
    }

    private static int CalculateLCS(string s1, string s2)
    {
        var dp = new int[s1.Length + 1, s2.Length + 1];

        for (var i = 1; i <= s1.Length; i++)
        {
            for (var j = 1; j <= s2.Length; j++)
            {
                if (s1[i - 1] == s2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        return dp[s1.Length, s2.Length];
    }

    private static decimal CalculateSpecificationSimilarity(Dictionary<string, object>? specs1, Dictionary<string, object>? specs2)
    {
        if (specs1 == null && specs2 == null)
            return 1.0m;

        if (specs1 == null || specs2 == null)
            return 0.0m;

        if (!specs1.Any() && !specs2.Any())
            return 1.0m;

        if (!specs1.Any() || !specs2.Any())
            return 0.0m;

        var allKeys = specs1.Keys.Union(specs2.Keys).ToList();
        var matchingKeys = 0;
        var totalSimilarity = 0.0m;

        foreach (var key in allKeys)
        {
            var hasKey1 = specs1.ContainsKey(key);
            var hasKey2 = specs2.ContainsKey(key);

            if (hasKey1 && hasKey2)
            {
                var value1 = specs1[key]?.ToString() ?? "";
                var value2 = specs2[key]?.ToString() ?? "";
                var valueSimilarity = CalculateTextSimilarity(value1, value2);
                totalSimilarity += valueSimilarity;
                matchingKeys++;
            }
        }

        if (matchingKeys == 0)
            return 0.0m;

        return totalSimilarity / matchingKeys;
    }

    private static string DetermineMatchingMethod(decimal similarityScore)
    {
        return similarityScore switch
        {
            >= ExactMatchThreshold => "EXACT_MATCH",
            >= HighSimilarityThreshold => "HIGH_SIMILARITY",
            >= ModerateSimilarityThreshold => "TEXT_SIMILARITY",
            _ => "LOW_SIMILARITY"
        };
    }

    private static List<string> GetMatchingFields(Domain.Entities.Product product, ProductExtractionResult candidateData)
    {
        var matchingFields = new List<string>();

        // Check name similarity
        var nameScore = CalculateTextSimilarity(product.Name ?? "", candidateData.ExtractedProductName ?? "");
        if (nameScore >= 0.7m)
            matchingFields.Add("Name");

        // Check manufacturer similarity
        var manufacturerScore = CalculateTextSimilarity(product.Manufacturer ?? "", candidateData.ExtractedManufacturer ?? "");
        if (manufacturerScore >= 0.8m)
            matchingFields.Add("Manufacturer");

        // Check model number similarity
        var modelScore = CalculateTextSimilarity(product.ModelNumber ?? "", candidateData.ExtractedModelNumber ?? "");
        if (modelScore >= 0.8m)
            matchingFields.Add("ModelNumber");

        // Check specification similarity
        var specScore = CalculateSpecificationSimilarity(DeserializeSpecifications(product.Specifications), candidateData.ExtractedSpecifications);
        if (specScore >= 0.6m)
            matchingFields.Add("Specifications");

        return matchingFields;
    }

    private static Dictionary<string, object>? DeserializeSpecifications(string? specificationsJson)
    {
        if (string.IsNullOrWhiteSpace(specificationsJson))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(specificationsJson);
        }
        catch
        {
            return null;
        }
    }
}