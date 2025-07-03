namespace TechTicker.Application.Services.Interfaces;

using TechTicker.Domain.Entities.Canonical;

public interface ISpecificationNormalizer
{
    /// <summary>
    /// Normalizes raw scraped specifications using the canonical template for the specified category.
    /// </summary>
    /// <param name="rawSpecifications">Dictionary of raw key/value pairs as scraped.</param>
    /// <param name="category">Product category (e.g., "Graphics Card").</param>
    /// <returns>
    /// Tuple containing:
    /// 1. Normalized specifications keyed by canonical name.
    /// 2. Uncategorized key/value pairs that couldn't be matched.
    /// </returns>
    (Dictionary<string, NormalizedSpecificationValue> Normalized, Dictionary<string, string> Uncategorized) Normalize(
        Dictionary<string, string> rawSpecifications,
        string category);
} 