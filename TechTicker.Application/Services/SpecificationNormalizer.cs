using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Domain.Entities.Canonical;
using TechTicker.Shared.Utilities.Html;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TechTicker.Application.Services;

/// <summary>
/// Transforms raw scraped specification key/value pairs into normalized canonical specifications.
/// </summary>
public class SpecificationNormalizer : ISpecificationNormalizer
{
    private readonly ILogger<SpecificationNormalizer> _logger;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, CanonicalSpecificationTemplate> _templateCache = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    static SpecificationNormalizer()
    {
        // Initialize static JSON options with the enum converter
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        options.Converters.Add(new JsonStringEnumConverter());
        _staticJsonOptions = options;
    }

    private static readonly JsonSerializerOptions _staticJsonOptions;

    // Location of canonical template JSON files, relative to the executing assembly.
    private static readonly string TemplateRoot = Path.Combine(AppContext.BaseDirectory, "CanonicalSpecs");

    public SpecificationNormalizer(ILogger<SpecificationNormalizer> logger, IMemoryCache? cache = null)
    {
        _logger = logger;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
    }

    /// <inheritdoc />
    public (Dictionary<string, NormalizedSpecificationValue> Normalized, Dictionary<string, string> Uncategorized) Normalize(
        Dictionary<string, string> rawSpecifications,
        string category)
    {
        var normalized = new Dictionary<string, NormalizedSpecificationValue>(StringComparer.OrdinalIgnoreCase);
        var uncategorized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (rawSpecifications == null || rawSpecifications.Count == 0)
        {
            _logger.LogDebug("Normalize called with null or empty rawSpecifications");
            return (normalized, uncategorized);
        }

        _logger.LogInformation("Starting normalization of {SpecCount} specifications for category '{Category}'", 
            rawSpecifications.Count, category);

        var template = LoadTemplate(category);
        if (template == null)
        {
            _logger.LogWarning("No canonical template found for category '{Category}'. Storing all {SpecCount} specs as uncategorized.", 
                category, rawSpecifications.Count);
            foreach (var kvp in rawSpecifications)
            {
                uncategorized[kvp.Key] = kvp.Value;
            }
            return (normalized, uncategorized);
        }

        _logger.LogDebug("Using template with {PropertyCount} canonical properties for category '{Category}'", 
            template.Properties?.Count ?? 0, category);

        int matchedCount = 0;
        foreach (var (rawKey, rawValue) in rawSpecifications)
        {
            var match = MatchCanonicalProperty(template, rawKey);
            if (match == null)
            {
                _logger.LogDebug("No match found for specification key '{RawKey}'", rawKey);
                uncategorized[rawKey] = rawValue;
                continue;
            }

            var value = CreateNormalizedValue(match, rawValue, rawKey);
            normalized[match.CanonicalName] = value;
            matchedCount++;
            
            _logger.LogDebug("Matched '{RawKey}' -> '{CanonicalName}' with confidence {Confidence:F2}", 
                rawKey, match.CanonicalName, value.Confidence);
        }

        _logger.LogInformation("Normalization complete: {MatchedCount}/{TotalCount} specifications matched, {NormalizedCount} normalized, {UncategorizedCount} uncategorized",
            matchedCount, rawSpecifications.Count, normalized.Count, uncategorized.Count);

        return (normalized, uncategorized);
    }

    private CanonicalSpecificationTemplate? LoadTemplate(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) 
        {
            _logger.LogWarning("LoadTemplate called with null or empty category");
            return null;
        }
        
        var slug = Slugify(category);
        _logger.LogDebug("Loading template for category '{Category}' with slug '{Slug}'", category, slug);

        // Try memory cache first
        if (_templateCache.TryGetValue(slug, out var cached))
        {
            _logger.LogDebug("Template found in cache for slug '{Slug}'", slug);
            return cached;
        }

        var filePath = Path.Combine(TemplateRoot, $"{slug}.json");
        _logger.LogDebug("Looking for template file at '{FilePath}'", filePath);
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Template file not found at '{FilePath}'. Available files: {AvailableFiles}", 
                filePath, 
                Directory.Exists(TemplateRoot) ? string.Join(", ", Directory.GetFiles(TemplateRoot, "*.json").Select(Path.GetFileName)) : "Directory not found");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var template = JsonSerializer.Deserialize<CanonicalSpecificationTemplate>(json, _staticJsonOptions);
            if (template != null)
            {
                _templateCache[slug] = template;
                _logger.LogInformation("Successfully loaded template for category '{Category}' with {PropertyCount} properties", 
                    category, template.Properties?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("Template deserialization returned null for file '{FilePath}'", filePath);
            }
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load canonical template '{FilePath}'.", filePath);
            return null;
        }
    }

    private static CanonicalProperty? MatchCanonicalProperty(CanonicalSpecificationTemplate template, string rawKey)
    {
        if (template?.Properties == null) return null;

        var cleanedKey = rawKey.Trim().ToLowerInvariant();
        foreach (var prop in template.Properties)
        {
            // Check canonical name, display name, and aliases
            if (prop.CanonicalName.Equals(cleanedKey, StringComparison.OrdinalIgnoreCase) ||
                prop.DisplayName.Equals(cleanedKey, StringComparison.OrdinalIgnoreCase) ||
                prop.Aliases.Any(a => a.Equals(cleanedKey, StringComparison.OrdinalIgnoreCase)))
            {
                return prop;
            }
        }
        return null;
    }

    private NormalizedSpecificationValue CreateNormalizedValue(CanonicalProperty property, string rawValue, string rawKey)
    {
        var (parsedValue, unit, confidence) = ParseAndConvert(property, rawValue);

        return new NormalizedSpecificationValue
        {
            CanonicalName = property.CanonicalName,
            RawValue = rawValue,
            Value = parsedValue,
            Unit = unit ?? property.Unit,
            DataType = property.DataType,
            Confidence = confidence
        };
    }

    private static (object? value, string? unit, double confidence) ParseAndConvert(CanonicalProperty property, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return (null, property.Unit, 0.0);

        string trimmed = rawValue.Trim();
        string lower = trimmed.ToLowerInvariant();
        double num;
        Match m;

        switch (property.DataType)
        {
            case SpecificationType.Memory:
                m = Regex.Match(lower, @"(?<num>\d+(?:\.\d+)?)\s*(?<unit>gb|gib|mb|mib|kb|kib)", RegexOptions.IgnoreCase);
                if (m.Success && double.TryParse(m.Groups["num"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                {
                    string unit = m.Groups["unit"].Value.ToLowerInvariant();
                    double gb = unit switch
                    {
                        "gb" or "gib" => num,
                        "mb" or "mib" => num / 1024,
                        "kb" or "kib" => num / (1024 * 1024),
                        _ => num
                    };
                    return (gb, "GB", 0.95);
                }
                break;
            case SpecificationType.Clock:
            case SpecificationType.Speed:
                m = Regex.Match(lower, @"(?<num>\d+(?:\.\d+)?)\s*(?<unit>ghz|mhz)", RegexOptions.IgnoreCase);
                if (m.Success && double.TryParse(m.Groups["num"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                {
                    string unit = m.Groups["unit"].Value.ToLowerInvariant();
                    double mhz = unit == "ghz" ? num * 1000 : num;
                    return (mhz, "MHz", 0.95);
                }
                break;
            case SpecificationType.Power:
                m = Regex.Match(lower, @"(?<num>\d+(?:\.\d+)?)\s*w", RegexOptions.IgnoreCase);
                if (m.Success && double.TryParse(m.Groups["num"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                {
                    return (num, "W", 0.95);
                }
                break;
            case SpecificationType.Count:
            case SpecificationType.Numeric:
                if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                {
                    return (num, property.Unit, 0.9);
                }
                break;
            default:
                // treat as text
                break;
        }

        // Default fallback when parsing fails
        return (trimmed, property.Unit, 0.7);
    }

    private static string Slugify(string text) =>
        text.Trim().ToLowerInvariant().Replace(" ", "_");
} 