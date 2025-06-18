using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// Request for generating selectors from HTML content
/// </summary>
public class GenerateSelectorsRequest
{
    [Required]
    public string HtmlContent { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = null!;

    public string? SiteName { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request for testing selectors against HTML content
/// </summary>
public class TestSelectorsRequest
{
    [Required]
    public string HtmlContent { get; set; } = null!;

    [Required]
    public SelectorSet Selectors { get; set; } = null!;
}

/// <summary>
/// Request for getting selector improvement suggestions
/// </summary>
public class SelectorImprovementRequest
{
    [Required]
    public string HtmlContent { get; set; } = null!;

    [Required]
    public SelectorSet CurrentSelectors { get; set; } = null!;

    [Required]
    public SelectorTestResult TestResult { get; set; } = null!;
}

/// <summary>
/// Set of CSS selectors for extracting product data
/// </summary>
public class SelectorSet
{
    public string Domain { get; set; } = null!;
    public string[]? ProductNameSelectors { get; set; }
    public string[]? PriceSelectors { get; set; }
    public string[]? ImageSelectors { get; set; }
    public string[]? DescriptionSelectors { get; set; }
    public string[]? ManufacturerSelectors { get; set; }
    public string[]? ModelNumberSelectors { get; set; }
    public string[]? SpecificationSelectors { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of AI selector generation
/// </summary>
public class SelectorGenerationResult
{
    public string Domain { get; set; } = null!;
    public SelectorSet Selectors { get; set; } = null!;
    public double Confidence { get; set; }
    public string? Notes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of testing selectors against HTML content
/// </summary>
public class SelectorTestResult
{
    public string Domain { get; set; } = null!;
    public Dictionary<string, FieldTestResult> Results { get; set; } = new();
    public double OverallScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of testing a specific field selector
/// </summary>
public class FieldTestResult
{
    public string Value { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Selector { get; set; } = string.Empty;
    public string? Error { get; set; }
}

/// <summary>
/// Suggestion for improving a selector
/// </summary>
public class SelectorSuggestion
{
    public string Field { get; set; } = null!;
    public string CurrentSelector { get; set; } = null!;
    public string SuggestedSelector { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Priority { get; set; } = "medium"; // high, medium, low
}

/// <summary>
/// Request to save site configuration
/// </summary>
public class SaveSiteConfigurationRequest
{
    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string SiteName { get; set; } = null!;

    [Required]
    public SelectorSet Selectors { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? TestHtml { get; set; }
}

/// <summary>
/// Site configuration with selectors
/// </summary>
public class SiteConfigurationDto
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = null!;
    public string SiteName { get; set; } = null!;
    public SelectorSet Selectors { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string? TestHtml { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}

/// <summary>
/// Request to get site configurations with filtering
/// </summary>
public class SiteConfigurationFilterRequest
{
    public string? Domain { get; set; }
    public string? SiteName { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Response for HTML analysis with extracted data preview
/// </summary>
public class HtmlAnalysisResult
{
    public string Domain { get; set; } = null!;
    public Dictionary<string, string> ExtractedData { get; set; } = new();
    public List<string> DetectedElements { get; set; } = new();
    public List<string> SuggestedSelectors { get; set; } = new();
    public double AnalysisScore { get; set; }
    public string? Notes { get; set; }
}
