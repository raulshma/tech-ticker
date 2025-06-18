using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for analyzing URLs and extracting product data
/// </summary>
public interface IUrlAnalysisService
{
    /// <summary>
    /// Extracts product data from a given URL
    /// </summary>
    Task<Result<ProductExtractionResult>> ExtractProductDataAsync(string url);
    
    /// <summary>
    /// Analyzes if a site is compatible with our scraping methods
    /// </summary>
    Task<Result<SiteCompatibilityResult>> AnalyzeSiteCompatibilityAsync(string url);
    
    /// <summary>
    /// Extracts multiple product URLs from a catalog/category page
    /// </summary>
    Task<Result<List<string>>> ExtractProductUrlsFromPageAsync(string catalogUrl);
    
    /// <summary>
    /// Validates if a URL is a valid product page
    /// </summary>
    Task<Result<bool>> IsValidProductUrlAsync(string url);
    
    /// <summary>
    /// Gets the domain from a URL for compatibility checking
    /// </summary>
    Result<string> GetDomainFromUrl(string url);
}