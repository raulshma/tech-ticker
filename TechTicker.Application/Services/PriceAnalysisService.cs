using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for analyzing price differences and value propositions
/// </summary>
public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly ILogger<PriceAnalysisService> _logger;

    public PriceAnalysisService(ILogger<PriceAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyze price differences and value proposition between two products
    /// </summary>
    /// <param name="product1">First product with pricing</param>
    /// <param name="product2">Second product with pricing</param>
    /// <returns>Comprehensive price analysis</returns>
    public async Task<PriceAnalysisDto> AnalyzePricesAsync(
        ProductWithCurrentPricesDto product1, 
        ProductWithCurrentPricesDto product2)
    {
        try
        {
            _logger.LogInformation("Analyzing prices for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);

            // Get price summaries
            var summary = await CreatePriceComparisonSummaryAsync(product1, product2);

            // Compare prices across sellers
            var sellerComparisons = await CreateSellerComparisonsAsync(product1, product2);

            // Calculate value analysis (will be enhanced when specification scores are available)
            var valueAnalysis = await CreateValueAnalysisAsync(product1, product2);

            var priceAnalysis = new PriceAnalysisDto
            {
                Summary = summary,
                SellerComparisons = sellerComparisons,
                ValueAnalysis = valueAnalysis
            };

            _logger.LogInformation("Completed price analysis for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);

            return priceAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing prices for products {Product1} and {Product2}", 
                product1.ProductId, product2.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Calculate value score considering price and specifications
    /// </summary>
    /// <param name="product">Product with pricing</param>
    /// <param name="specificationScore">Product's specification score</param>
    /// <returns>Value score</returns>
    public async Task<decimal> CalculateValueScoreAsync(
        ProductWithCurrentPricesDto product, 
        decimal specificationScore)
    {
        try
        {
            _logger.LogInformation("Calculating value score for product {ProductId} with spec score {SpecScore}", 
                product.ProductId, specificationScore);

            if (product.LowestCurrentPrice == null || product.LowestCurrentPrice <= 0)
            {
                _logger.LogWarning("No valid pricing found for product {ProductId}", product.ProductId);
                return 0m;
            }

            var price = product.LowestCurrentPrice.Value;

            // Normalize specification score to 0-1 range if it's not already
            var normalizedSpecScore = specificationScore > 1 ? specificationScore / 100m : specificationScore;

            // Calculate value as performance per dollar
            // Higher spec score and lower price = better value
            var baseValueScore = normalizedSpecScore / (decimal)Math.Log10((double)price + 1);

            // Apply availability bonus
            var availabilityMultiplier = CalculateAvailabilityMultiplier(product);
            var valueScore = baseValueScore * availabilityMultiplier;

            // Scale to 0-1 range
            valueScore = Math.Min(valueScore, 1m);

            _logger.LogInformation("Calculated value score {ValueScore:F2} for product {ProductId}", 
                valueScore, product.ProductId);

            return await Task.FromResult(valueScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating value score for product {ProductId}", product.ProductId);
            throw;
        }
    }

    /// <summary>
    /// Create price comparison summary
    /// </summary>
    private async Task<PriceComparisonSummaryDto> CreatePriceComparisonSummaryAsync(
        ProductWithCurrentPricesDto product1,
        ProductWithCurrentPricesDto product2)
    {
        var product1LowestPrice = product1.LowestCurrentPrice ?? decimal.MaxValue;
        var product2LowestPrice = product2.LowestCurrentPrice ?? decimal.MaxValue;

        var priceDifference = Math.Abs(product1LowestPrice - product2LowestPrice);
        var lowerPrice = Math.Min(product1LowestPrice, product2LowestPrice);
        var pricePercentageDiff = lowerPrice > 0 ? (priceDifference / lowerPrice) * 100 : 0;

        var lowerPricedProduct = product1LowestPrice <= product2LowestPrice
            ? "Product1"
            : "Product2";

        // Handle cases where products have no pricing
        if (product1LowestPrice == decimal.MaxValue && product2LowestPrice == decimal.MaxValue)
        {
            product1LowestPrice = 0;
            product2LowestPrice = 0;
            priceDifference = 0;
            pricePercentageDiff = 0;
            lowerPricedProduct = "Neither product has pricing available";
        }
        else if (product1LowestPrice == decimal.MaxValue)
        {
            product1LowestPrice = 0;
            lowerPricedProduct = "Product2";
        }
        else if (product2LowestPrice == decimal.MaxValue)
        {
            product2LowestPrice = 0;
            lowerPricedProduct = "Product1";
        }

        return await Task.FromResult(new PriceComparisonSummaryDto
        {
            Product1LowestPrice = product1LowestPrice,
            Product2LowestPrice = product2LowestPrice,
            PriceDifference = priceDifference,
            PriceDifferencePercentage = pricePercentageDiff,
            LowerPricedProduct = lowerPricedProduct,
            Product1SellerCount = product1.CurrentPrices?.Count() ?? 0,
            Product2SellerCount = product2.CurrentPrices?.Count() ?? 0
        });
    }

    /// <summary>
    /// Create seller-by-seller price comparisons
    /// </summary>
    private async Task<IEnumerable<SellerPriceComparisonDto>> CreateSellerComparisonsAsync(
        ProductWithCurrentPricesDto product1,
        ProductWithCurrentPricesDto product2)
    {
        var sellerComparisons = new List<SellerPriceComparisonDto>();

        // Get all unique sellers from both products, handling null/empty/duplicate seller names
        var product1Prices = product1.CurrentPrices?
            .Where(p => !string.IsNullOrWhiteSpace(p.SellerName))
            .GroupBy(p => p.SellerName)
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<string, CurrentPriceDto>();
        var product2Prices = product2.CurrentPrices?
            .Where(p => !string.IsNullOrWhiteSpace(p.SellerName))
            .GroupBy(p => p.SellerName)
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<string, CurrentPriceDto>();

        var allSellers = product1Prices.Keys.Union(product2Prices.Keys).ToList();

        foreach (var seller in allSellers)
        {
            var hasProduct1 = product1Prices.TryGetValue(seller, out var product1Price);
            var hasProduct2 = product2Prices.TryGetValue(seller, out var product2Price);

            var comparison = new SellerPriceComparisonDto
            {
                SellerName = seller,
                Product1Price = hasProduct1 ? product1Price!.Price : null,
                Product2Price = hasProduct2 ? product2Price!.Price : null,
                Product1StockStatus = hasProduct1 ? product1Price!.StockStatus : null,
                Product2StockStatus = hasProduct2 ? product2Price!.StockStatus : null
            };

            // Calculate price difference if both products are available from this seller
            if (hasProduct1 && hasProduct2)
            {
                comparison.PriceDifference = Math.Abs(product1Price!.Price - product2Price!.Price);
            }

            // Determine availability advantage
            comparison.AvailabilityAdvantage = DetermineAvailabilityAdvantage(
                hasProduct1 ? product1Price!.StockStatus : null,
                hasProduct2 ? product2Price!.StockStatus : null);

            sellerComparisons.Add(comparison);
        }

        return await Task.FromResult(sellerComparisons.OrderBy(sc => sc.SellerName));
    }

    /// <summary>
    /// Create value analysis comparing price vs expected performance
    /// </summary>
    private async Task<ValueAnalysisDto> CreateValueAnalysisAsync(
        ProductWithCurrentPricesDto product1,
        ProductWithCurrentPricesDto product2)
    {
        // For now, create a basic value analysis based on price alone
        // This will be enhanced when specification scores are available
        
        var product1Price = product1.LowestCurrentPrice ?? decimal.MaxValue;
        var product2Price = product2.LowestCurrentPrice ?? decimal.MaxValue;

        // Basic value scoring (will be improved with specification integration)
        var product1ValueScore = CalculateBasicValueScore(product1) / 100m; // Convert to 0-1 range
        var product2ValueScore = CalculateBasicValueScore(product2) / 100m; // Convert to 0-1 range

        var betterValueProduct = product1ValueScore >= product2ValueScore
            ? "Product1"
            : "Product2";

        var analysisReason = GenerateValueAnalysisReason(
            product1, product2, product1Price, product2Price, 
            product1ValueScore, product2ValueScore);

        return await Task.FromResult(new ValueAnalysisDto
        {
            Product1ValueScore = product1ValueScore,
            Product2ValueScore = product2ValueScore,
            BetterValueProduct = betterValueProduct,
            ValueAnalysisReason = analysisReason
        });
    }

    /// <summary>
    /// Calculate basic value score based on price and availability
    /// </summary>
    private decimal CalculateBasicValueScore(ProductWithCurrentPricesDto product)
    {
        if (product.LowestCurrentPrice == null || product.LowestCurrentPrice <= 0)
            return 0m;

        var baseScore = 50m; // Neutral score
        
        // Adjust for availability
        var availabilityMultiplier = CalculateAvailabilityMultiplier(product);
        baseScore *= availabilityMultiplier;

        // Adjust for number of sellers (more sellers = better availability)
        var sellerBonus = Math.Min(product.AvailableSellersCount * 2m, 10m);
        baseScore += sellerBonus;

        return Math.Min(baseScore, 100m);
    }

    /// <summary>
    /// Calculate availability multiplier based on stock status and seller count
    /// </summary>
    private decimal CalculateAvailabilityMultiplier(ProductWithCurrentPricesDto product)
    {
        if (product.CurrentPrices == null || !product.CurrentPrices.Any())
            return 0.5m;

        var inStockCount = product.CurrentPrices.Count(p => IsInStock(p.StockStatus));

        var totalSellers = product.CurrentPrices.Count();
        var availabilityRatio = totalSellers > 0 ? (decimal)inStockCount / totalSellers : 0m;

        // Base multiplier between 0.7 and 1.2
        return 0.7m + (availabilityRatio * 0.5m);
    }

    /// <summary>
    /// Determine which product has availability advantage for a seller
    /// </summary>
    private string? DetermineAvailabilityAdvantage(string? product1Stock, string? product2Stock)
    {
        var product1Available = IsInStock(product1Stock);
        var product2Available = IsInStock(product2Stock);

        if (product1Available && !product2Available)
            return "Product1";
        if (product2Available && !product1Available)
            return "Product2";
        if (product1Available && product2Available)
            return "Both available";
        
        return "Neither available";
    }

    /// <summary>
    /// Check if a stock status indicates the product is in stock
    /// </summary>
    private bool IsInStock(string? stockStatus)
    {
        if (string.IsNullOrEmpty(stockStatus))
            return false;

        var status = stockStatus.ToLowerInvariant();
        
        // Check for negative indicators first
        if (status.Contains("out of stock") || 
            status.Contains("out of stock") ||
            status.Contains("unavailable") ||
            status.Contains("discontinued") ||
            status.Contains("backorder"))
            return false;
        
        // Check for positive indicators
        return status.Contains("in stock") || 
               status.Contains("available") || 
               status.Contains("ready") ||
               status.Contains("in stock");
    }

    /// <summary>
    /// Generate value analysis reasoning text
    /// </summary>
    private string GenerateValueAnalysisReason(
        ProductWithCurrentPricesDto product1,
        ProductWithCurrentPricesDto product2,
        decimal product1Price,
        decimal product2Price,
        decimal product1ValueScore,
        decimal product2ValueScore)
    {
        var priceDiff = Math.Abs(product1Price - product2Price);
        var scoreDiff = Math.Abs(product1ValueScore - product2ValueScore);

        if (product1Price == decimal.MaxValue && product2Price == decimal.MaxValue)
        {
            return "Neither product has pricing information available for value comparison.";
        }

        if (product1Price == decimal.MaxValue)
        {
            return $"Product 2 has better value as Product 1 has no pricing available. Product 2 is priced at {product2Price:C}.";
        }

        if (product2Price == decimal.MaxValue)
        {
            return $"Product 1 has better value as Product 2 has no pricing available. Product 1 is priced at {product1Price:C}.";
        }

        if (scoreDiff < 5m)
        {
            return $"Both products offer similar value. Price difference is {priceDiff:C} ({Math.Abs(product1Price - product2Price) / Math.Min(product1Price, product2Price) * 100:F1}%).";
        }

        var betterProduct = product1ValueScore > product2ValueScore ? "Product 1" : "Product 2";
        var betterPrice = product1Price < product2Price ? product1Price : product2Price;
        var higherPrice = product1Price > product2Price ? product1Price : product2Price;

        return $"{betterProduct} offers better value with superior price-to-availability ratio. " +
               $"Price difference is {priceDiff:C} between {betterPrice:C} and {higherPrice:C}.";
    }
}