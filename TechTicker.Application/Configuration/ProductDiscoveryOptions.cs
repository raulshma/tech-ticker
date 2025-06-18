using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.Configuration;

/// <summary>
/// Configuration options for Product Discovery feature
/// </summary>
public class ProductDiscoveryOptions
{
    public const string SectionName = "ProductDiscovery";

    /// <summary>
    /// Maximum number of concurrent URL analysis operations
    /// </summary>
    [Range(1, 20, ErrorMessage = "MaxConcurrentAnalysis must be between 1 and 20")]
    public int MaxConcurrentAnalysis { get; set; } = 5;

    /// <summary>
    /// Default confidence threshold for category prediction
    /// </summary>
    [Range(0.1, 1.0, ErrorMessage = "DefaultCategoryConfidenceThreshold must be between 0.1 and 1.0")]
    public decimal DefaultCategoryConfidenceThreshold { get; set; } = 0.7m;

    /// <summary>
    /// Threshold for determining product similarity
    /// </summary>
    [Range(0.1, 1.0, ErrorMessage = "SimilarityScoreThreshold must be between 0.1 and 1.0")]
    public decimal SimilarityScoreThreshold { get; set; } = 0.8m;

    /// <summary>
    /// Batch size for bulk URL analysis operations
    /// </summary>
    [Range(10, 1000, ErrorMessage = "BulkAnalysisBatchSize must be between 10 and 1000")]
    public int BulkAnalysisBatchSize { get; set; } = 100;

    /// <summary>
    /// Confidence threshold for automatic approval of candidates
    /// </summary>
    [Range(0.8, 1.0, ErrorMessage = "AutoApprovalThreshold must be between 0.8 and 1.0")]
    public decimal AutoApprovalThreshold { get; set; } = 0.95m;

    /// <summary>
    /// List of supported e-commerce sites for product extraction
    /// </summary>
    public List<string> SupportedSites { get; set; } = new()
    {
        "amazon.com",
        "newegg.com",
        "bestbuy.com"
    };

    /// <summary>
    /// Category prediction configuration
    /// </summary>
    public CategoryPredictionOptions CategoryPrediction { get; set; } = new();

    /// <summary>
    /// URL analysis configuration
    /// </summary>
    public UrlAnalysisOptions UrlAnalysis { get; set; } = new();

    /// <summary>
    /// Google AI configuration for selector generation
    /// </summary>
    public GoogleAIOptions GoogleAI { get; set; } = new();
}

/// <summary>
/// Configuration for category prediction functionality
/// </summary>
public class CategoryPredictionOptions
{
    /// <summary>
    /// Whether to use ML classifier for category prediction
    /// </summary>
    public bool UseMLClassifier { get; set; } = false;

    /// <summary>
    /// Whether to fallback to rule engine if ML classifier fails
    /// </summary>
    public bool FallbackToRuleEngine { get; set; } = true;

    /// <summary>
    /// Path to the ML model file
    /// </summary>
    public string ModelPath { get; set; } = "/models/category-classifier.onnx";
}

/// <summary>
/// Configuration for URL analysis functionality
/// </summary>
public class UrlAnalysisOptions
{
    /// <summary>
    /// HTTP request timeout for URL analysis
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    [Required(ErrorMessage = "UserAgent is required")]
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Configuration for Google AI integration
/// </summary>
public class GoogleAIOptions
{
    /// <summary>
    /// Google AI API key
    /// </summary>
    [Required(ErrorMessage = "Google AI API key is required")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use for selector generation
    /// </summary>
    public string ModelName { get; set; } = "gemini-2.5-flash";

    /// <summary>
    /// Maximum tokens for AI response
    /// </summary>
    [Range(100, 65536, ErrorMessage = "MaxTokens must be between 100 and 65536")]
    public int MaxTokens { get; set; } = 65536;

    /// <summary>
    /// Temperature for AI response creativity
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Request timeout for AI API calls
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
