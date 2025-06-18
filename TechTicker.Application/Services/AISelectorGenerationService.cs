using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using TechTicker.Application.Configuration;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for generating CSS selectors using Google Generative AI
/// </summary>
public class AISelectorGenerationService : IAISelectorGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly ProductDiscoveryOptions _options;
    private readonly ILogger<AISelectorGenerationService> _logger;

    public AISelectorGenerationService(
        HttpClient httpClient,
        IOptions<ProductDiscoveryOptions> options,
        ILogger<AISelectorGenerationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Validate Google AI configuration
        if (string.IsNullOrEmpty(_options.GoogleAI.ApiKey))
        {
            throw new InvalidOperationException("Google AI API key is not configured. Please set ProductDiscovery:GoogleAI:ApiKey in your configuration.");
        }

        _httpClient.Timeout = _options.GoogleAI.RequestTimeout;
        _logger.LogInformation("AISelectorGenerationService initialized with model: {ModelName}", _options.GoogleAI.ModelName);
    }

    public async Task<Result<SelectorGenerationResult>> GenerateSelectorsAsync(string htmlContent, string domain)
    {
        try
        {
            _logger.LogInformation("Generating selectors for domain: {Domain}", domain);

            var prompt = BuildSelectorGenerationPrompt(htmlContent, domain);
            var response = await CallGoogleAIAsync(prompt);

            if (response.IsFailure)
            {
                return Result<SelectorGenerationResult>.Failure(response.ErrorMessage!);
            }

            var result = ParseAIResponse(response.Data!, domain);
            return Result<SelectorGenerationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating selectors for domain: {Domain}", domain);
            return Result<SelectorGenerationResult>.Failure(ex);
        }
    }

    public async Task<Result<SelectorTestResult>> TestSelectorsAsync(string htmlContent, SelectorSet selectors)
    {
        try
        {
            _logger.LogInformation("Testing selectors for domain: {Domain}", selectors.Domain);

            var prompt = BuildSelectorTestPrompt(htmlContent, selectors);
            var response = await CallGoogleAIAsync(prompt);

            if (response.IsFailure)
            {
                return Result<SelectorTestResult>.Failure(response.ErrorMessage!);
            }

            var result = ParseTestResponse(response.Data!, selectors);
            return Result<SelectorTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing selectors: {Error}", ex.Message);
            return Result<SelectorTestResult>.Failure(ex);
        }
    }

    public async Task<Result<List<SelectorSuggestion>>> SuggestImprovementsAsync(string htmlContent, SelectorSet currentSelectors, SelectorTestResult testResult)
    {
        try
        {
            _logger.LogInformation("Suggesting selector improvements for domain: {Domain}", currentSelectors.Domain);

            var prompt = BuildImprovementPrompt(htmlContent, currentSelectors, testResult);
            var response = await CallGoogleAIAsync(prompt);

            if (response.IsFailure)
            {
                return Result<List<SelectorSuggestion>>.Failure(response.ErrorMessage!);
            }

            var suggestions = ParseSuggestionResponse(response.Data!);
            return Result<List<SelectorSuggestion>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting improvements: {Error}", ex.Message);
            return Result<List<SelectorSuggestion>>.Failure(ex);
        }
    }

    private async Task<Result<string>> CallGoogleAIAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = _options.GoogleAI.Temperature,
                    maxOutputTokens = _options.GoogleAI.MaxTokens,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.GoogleAI.ModelName}:generateContent?key={_options.GoogleAI.ApiKey}";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google AI API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

                return response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => Result<string>.Failure("Invalid Google AI API key. Please check your configuration."),
                    System.Net.HttpStatusCode.Forbidden => Result<string>.Failure("Google AI API access forbidden. Please check your API key permissions."),
                    System.Net.HttpStatusCode.TooManyRequests => Result<string>.Failure("Google AI API rate limit exceeded. Please try again later."),
                    _ => Result<string>.Failure($"Google AI API error: {response.StatusCode} - {errorContent}")
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<GoogleAIResponse>(responseContent);

            if (aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text == null)
            {
                return Result<string>.Failure("Invalid response from Google AI API");
            }

            var text = aiResponse.Candidates.First().Content!.Parts!.First().Text!;
            return Result<string>.Success(text);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Error calling Google AI API: {ex.Message}");
        }
    }

    private string BuildSelectorGenerationPrompt(string htmlContent, string domain)
    {
        return $@"
You are an expert web scraper. Analyze the following HTML content from {domain} and generate CSS selectors for extracting product information.

HTML Content (truncated if too long):
{TruncateHtml(htmlContent)}

Please provide CSS selectors in JSON format for the following product data:
- productName: The main product title/name
- price: The current price (look for sale price first, then regular price)
- image: The main product image
- description: Product description or key features
- manufacturer: Brand or manufacturer name
- modelNumber: Model/SKU number if available
- specifications: Any technical specifications

Return ONLY a JSON object with this structure:
{{
  ""domain"": ""{domain}"",
  ""selectors"": {{
    ""productName"": [""selector1"", ""selector2""],
    ""price"": [""selector1"", ""selector2""],
    ""image"": [""selector1"", ""selector2""],
    ""description"": [""selector1"", ""selector2""],
    ""manufacturer"": [""selector1"", ""selector2""],
    ""modelNumber"": [""selector1"", ""selector2""],
    ""specifications"": [""selector1"", ""selector2""]
  }},
  ""confidence"": 0.85,
  ""notes"": ""Brief explanation of selector choices""
}}

Provide multiple selector options for each field (in order of preference) to improve reliability.
";
    }

    private string BuildSelectorTestPrompt(string htmlContent, SelectorSet selectors)
    {
        return $@"
Test the following CSS selectors against this HTML content and report what data would be extracted:

HTML Content:
{TruncateHtml(htmlContent)}

Selectors to test:
{JsonSerializer.Serialize(selectors, new JsonSerializerOptions { WriteIndented = true })}

Return ONLY a JSON object with this structure:
{{
  ""results"": {{
    ""productName"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}},
    ""price"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}},
    ""image"": {{""value"": ""extracted url"", ""success"": true, ""selector"": ""used selector""}},
    ""description"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}},
    ""manufacturer"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}},
    ""modelNumber"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}},
    ""specifications"": {{""value"": ""extracted text"", ""success"": true, ""selector"": ""used selector""}}
  }},
  ""overallScore"": 0.85,
  ""issues"": [""list of any problems found""]
}}
";
    }

    private string BuildImprovementPrompt(string htmlContent, SelectorSet currentSelectors, SelectorTestResult testResult)
    {
        return $@"
Based on the test results, suggest improvements to these CSS selectors:

HTML Content:
{TruncateHtml(htmlContent)}

Current Selectors:
{JsonSerializer.Serialize(currentSelectors, new JsonSerializerOptions { WriteIndented = true })}

Test Results:
{JsonSerializer.Serialize(testResult, new JsonSerializerOptions { WriteIndented = true })}

Return ONLY a JSON object with improvement suggestions:
{{
  ""suggestions"": [
    {{
      ""field"": ""productName"",
      ""currentSelector"": ""current selector"",
      ""suggestedSelector"": ""improved selector"",
      ""reason"": ""why this is better"",
      ""priority"": ""high|medium|low""
    }}
  ]
}}
";
    }

    private string TruncateHtml(string html)
    {
        const int maxLength = 8000; // Keep within token limits
        if (html.Length <= maxLength)
            return html;

        return html.Substring(0, maxLength) + "\n... [HTML truncated for length] ...";
    }

    private SelectorGenerationResult ParseAIResponse(string response, string domain)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            
            var result = new SelectorGenerationResult
            {
                Domain = domain,
                Selectors = new SelectorSet
                {
                    Domain = domain,
                    ProductNameSelectors = ParseSelectorArray(jsonResponse, "selectors.productName"),
                    PriceSelectors = ParseSelectorArray(jsonResponse, "selectors.price"),
                    ImageSelectors = ParseSelectorArray(jsonResponse, "selectors.image"),
                    DescriptionSelectors = ParseSelectorArray(jsonResponse, "selectors.description"),
                    ManufacturerSelectors = ParseSelectorArray(jsonResponse, "selectors.manufacturer"),
                    ModelNumberSelectors = ParseSelectorArray(jsonResponse, "selectors.modelNumber"),
                    SpecificationSelectors = ParseSelectorArray(jsonResponse, "selectors.specifications")
                },
                Confidence = ParseDouble(jsonResponse, "confidence"),
                Notes = ParseString(jsonResponse, "notes")
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response, using fallback");
            return new SelectorGenerationResult
            {
                Domain = domain,
                Selectors = new SelectorSet { Domain = domain },
                Confidence = 0.0,
                Notes = $"Failed to parse AI response: {ex.Message}"
            };
        }
    }

    private SelectorTestResult ParseTestResponse(string response, SelectorSet selectors)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            
            return new SelectorTestResult
            {
                Domain = selectors.Domain,
                Results = new Dictionary<string, FieldTestResult>
                {
                    ["productName"] = ParseFieldTestResult(jsonResponse, "results.productName"),
                    ["price"] = ParseFieldTestResult(jsonResponse, "results.price"),
                    ["image"] = ParseFieldTestResult(jsonResponse, "results.image"),
                    ["description"] = ParseFieldTestResult(jsonResponse, "results.description"),
                    ["manufacturer"] = ParseFieldTestResult(jsonResponse, "results.manufacturer"),
                    ["modelNumber"] = ParseFieldTestResult(jsonResponse, "results.modelNumber"),
                    ["specifications"] = ParseFieldTestResult(jsonResponse, "results.specifications")
                },
                OverallScore = ParseDouble(jsonResponse, "overallScore"),
                Issues = ParseStringArray(jsonResponse, "issues")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse test response");
            return new SelectorTestResult
            {
                Domain = selectors.Domain,
                Results = [],
                OverallScore = 0.0,
                Issues = [$"Failed to parse response: {ex.Message}"]
            };
        }
    }

    private List<SelectorSuggestion> ParseSuggestionResponse(string response)
    {
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            var suggestions = new List<SelectorSuggestion>();

            if (jsonResponse.TryGetProperty("suggestions", out var suggestionsArray))
            {
                foreach (var suggestion in suggestionsArray.EnumerateArray())
                {
                    suggestions.Add(new SelectorSuggestion
                    {
                        Field = ParseString(suggestion, "field") ?? "",
                        CurrentSelector = ParseString(suggestion, "currentSelector") ?? "",
                        SuggestedSelector = ParseString(suggestion, "suggestedSelector") ?? "",
                        Reason = ParseString(suggestion, "reason") ?? "",
                        Priority = ParseString(suggestion, "priority") ?? "medium"
                    });
                }
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse suggestion response");
            return [];
        }
    }

    private string[] ParseSelectorArray(JsonElement element, string path)
    {
        try
        {
            var parts = path.Split('.');
            var current = element;
            
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return Array.Empty<string>();
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                return current.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }
        }
        catch { }

        return Array.Empty<string>();
    }

    private string? ParseString(JsonElement element, string path)
    {
        try
        {
            var parts = path.Split('.');
            var current = element;
            
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return null;
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private double ParseDouble(JsonElement element, string path)
    {
        try
        {
            var parts = path.Split('.');
            var current = element;
            
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return 0.0;
            }

            return current.ValueKind == JsonValueKind.Number ? current.GetDouble() : 0.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private List<string> ParseStringArray(JsonElement element, string path)
    {
        try
        {
            var parts = path.Split('.');
            var current = element;
            
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return [];
            }

            if (current.ValueKind == JsonValueKind.Array)
            {
                return current.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch { }

        return [];
    }

    private FieldTestResult ParseFieldTestResult(JsonElement element, string path)
    {
        try
        {
            var parts = path.Split('.');
            var current = element;
            
            foreach (var part in parts)
            {
                if (!current.TryGetProperty(part, out current))
                    return new FieldTestResult();
            }

            return new FieldTestResult
            {
                Value = ParseString(current, "value") ?? "",
                Success = current.TryGetProperty("success", out var successProp) && successProp.GetBoolean(),
                Selector = ParseString(current, "selector") ?? ""
            };
        }
        catch
        {
            return new FieldTestResult();
        }
    }

    private class GoogleAIResponse
    {
        public GoogleAICandidate[]? Candidates { get; set; }
    }

    private class GoogleAICandidate
    {
        public GoogleAIContent? Content { get; set; }
    }

    private class GoogleAIContent
    {
        public GoogleAIPart[]? Parts { get; set; }
    }

    private class GoogleAIPart
    {
        public string? Text { get; set; }
    }
}
