using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.AI;

public class GoogleGeminiAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleGeminiAiProvider> _logger;

    public string ProviderName => "Google";

    public GoogleGeminiAiProvider(HttpClient httpClient, ILogger<GoogleGeminiAiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<BrowserActionGenerationResponseDto>> GenerateBrowserActionsAsync(
        string instructions, 
        string? context, 
        AiConfigurationDto configuration)
    {
        try
        {
            var prompt = BuildPrompt(instructions, context);
            var requestBody = BuildGeminiRequest(prompt, configuration);
            
            var response = await CallGeminiApiAsync(requestBody, configuration);
            if (!response.IsSuccess)
            {
                return Result<BrowserActionGenerationResponseDto>.Failure(response.ErrorMessage ?? "Failed to generate actions");
            }

            return Result<BrowserActionGenerationResponseDto>.Success(response.Data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating browser actions with Google Gemini");
            return Result<BrowserActionGenerationResponseDto>.Failure($"Generation failed: {ex.Message}");
        }
    }

    public async Task<Result<AiProviderModelsDto>> GetAvailableModelsAsync(string? baseUrl, string apiKey)
    {
        try
        {
            var effectiveBaseUrl = !string.IsNullOrEmpty(baseUrl) 
                ? baseUrl.TrimEnd('/') 
                : "https://generativelanguage.googleapis.com";

            // Try to fetch models from the API
            var models = await FetchModelsFromApiAsync(effectiveBaseUrl, apiKey);
            
            if (models.Any())
            {
                _logger.LogInformation("Successfully fetched {Count} models from {BaseUrl}", models.Count, effectiveBaseUrl);
                return Result<AiProviderModelsDto>.Success(new AiProviderModelsDto
                {
                    Models = models,
                    Success = true
                });
            }

            // Fallback to hardcoded models if API fetch fails
            _logger.LogWarning("Failed to fetch models from API, using fallback models");
            var fallbackModels = GetFallbackModels(effectiveBaseUrl);
            
            return Result<AiProviderModelsDto>.Success(new AiProviderModelsDto
            {
                Models = fallbackModels,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from {BaseUrl}", baseUrl);
            
            // Return fallback models even on exception
            var fallbackModels = GetFallbackModels(baseUrl);
            return Result<AiProviderModelsDto>.Success(new AiProviderModelsDto
            {
                Models = fallbackModels,
                Success = true
            });
        }
    }

    private async Task<List<string>> FetchModelsFromApiAsync(string baseUrl, string apiKey)
    {
        var models = new List<string>();
        
        try
        {
            // Google AI API models endpoint
            if (baseUrl.Contains("generativelanguage.googleapis.com"))
            {
                var url = $"{baseUrl}/v1beta/models?key={apiKey}";
                
                _logger.LogDebug("Fetching models from Google AI API: {Url}", url.Replace(apiKey, "***"));
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var modelsResponse = JsonSerializer.Deserialize<GoogleModelsResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (modelsResponse?.Models != null)
                    {
                        models.AddRange(modelsResponse.Models
                            .Where(m => m.Name != null && m.Name.Contains("generateContent"))
                            .Select(m => ExtractModelName(m.Name!))
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct());
                    }
                }
            }
            // OpenAI Compatible API
            else if (baseUrl.Contains("openai") || baseUrl.Contains("api"))
            {
                var url = $"{baseUrl}/v1/models";
                
                _logger.LogDebug("Fetching models from OpenAI compatible API: {Url}", url);
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    
                    if (modelsResponse?.Data != null)
                    {
                        models.AddRange(modelsResponse.Data
                            .Where(m => !string.IsNullOrEmpty(m.Id))
                            .Select(m => m.Id!)
                            .Where(id => id.Contains("gemini") || id.Contains("gpt") || id.Contains("claude"))
                            .Distinct());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models from API endpoint: {BaseUrl}", baseUrl);
        }
        
        return models;
    }

    private List<string> GetFallbackModels(string? baseUrl)
    {
        // Return appropriate fallback models based on the base URL
        if (!string.IsNullOrEmpty(baseUrl))
        {
            if (baseUrl.Contains("openai"))
            {
                return new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo" };
            }
            if (baseUrl.Contains("anthropic"))
            {
                return new List<string> { "claude-3-5-sonnet-20241022", "claude-3-5-haiku-20241022", "claude-3-opus-20240229" };
            }
        }
        
        // Default Google Gemini models
        return new List<string>
        {
            "gemini-2.5-pro",
            "gemini-2.5-flash",
            "gemini-2.0-flash-exp",
            "gemini-1.5-pro",
            "gemini-1.5-flash",
            "gemini-1.5-flash-8b"
        };
    }

    private string ExtractModelName(string fullName)
    {
        // Extract model name from Google AI API format: "models/gemini-1.5-pro"
        var parts = fullName.Split('/');
        return parts.Length > 1 ? parts[^1] : fullName;
    }

    public async Task<Result<bool>> TestConnectionAsync(AiConfigurationDto configuration)
    {
        try
        {
            var testPrompt = "Hello, this is a test message. Please respond with 'Connection successful'.";
            var requestBody = BuildGeminiRequest(testPrompt, configuration, isTest: true);
            
            var response = await CallGeminiApiAsync(requestBody, configuration);
            return Result<bool>.Success(response.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Google Gemini connection");
            return Result<bool>.Failure($"Connection test failed: {ex.Message}");
        }
    }

    public async Task<Result<GenericAiResponseDto>> GenerateGenericResponseAsync(
        string inputText,
        string? systemPrompt,
        string? context,
        string? jsonSchema,
        double? temperature,
        int? maxTokens,
        AiConfigurationDto configuration)
    {
        try
        {
            var prompt = BuildGenericPrompt(inputText, systemPrompt, context);
            var requestBody = BuildGenericGeminiRequest(prompt, jsonSchema, temperature, maxTokens, configuration);
            
            var response = await CallGenericGeminiApiAsync(requestBody, configuration, !string.IsNullOrEmpty(jsonSchema));
            if (!response.IsSuccess)
            {
                return Result<GenericAiResponseDto>.Failure(response.ErrorMessage ?? "Failed to generate response");
            }

            return Result<GenericAiResponseDto>.Success(response.Data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating generic response with Google Gemini");
            return Result<GenericAiResponseDto>.Failure($"Generation failed: {ex.Message}");
        }
    }

    private string BuildPrompt(string instructions, string? context)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("# Browser Automation Expert");
        promptBuilder.AppendLine("You are a specialized browser automation expert for web scraping and data extraction. Your task is to generate precise, efficient browser automation sequences.");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("## Available Actions:");
        promptBuilder.AppendLine("1. **scroll** - Scroll down by one viewport (no selector needed)");
        promptBuilder.AppendLine("   - Use repeat for multiple scrolls");
        promptBuilder.AppendLine("   - Add delayMs between scrolls for content loading");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("2. **click** - Click on an element (requires selector)");
        promptBuilder.AppendLine("   - Use specific CSS selectors: .class, #id, [attribute], button[type='submit']");
        promptBuilder.AppendLine("   - Common selectors: 'button.load-more', '.pagination-next', '[data-testid=\"load-more\"]'");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("3. **waitForSelector** - Wait for element to appear (requires selector)");
        promptBuilder.AppendLine("   - Use before clicking dynamic content");
        promptBuilder.AppendLine("   - Essential for SPA applications and lazy loading");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("4. **type** - Type text into input field (requires selector and value)");
        promptBuilder.AppendLine("   - Use for search boxes, forms: 'input[name=\"search\"]', '#search-input'");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("5. **wait** - Wait for specified time in milliseconds");
        promptBuilder.AppendLine("   - Use delayMs property (1000ms = 1 second)");
        promptBuilder.AppendLine("   - Only when necessary for page loading");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("6. **screenshot** - Capture page screenshot");
        promptBuilder.AppendLine("   - Use at key moments or final state");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("7. **hover** - Hover over element (requires selector)");
        promptBuilder.AppendLine("   - Triggers dropdown menus or tooltips");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("8. **selectOption** - Select dropdown option (requires selector and value)");
        promptBuilder.AppendLine("   - Value is the option text or value attribute");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("9. **setValue** - Set input value via JavaScript (requires selector and value)");
        promptBuilder.AppendLine("   - Use for complex inputs that don't respond to 'type'");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("10. **evaluate** - Execute custom JavaScript (requires value with JS code)");
        promptBuilder.AppendLine("    - Use for complex interactions not covered by other actions");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("## Best Practices:");
        promptBuilder.AppendLine("- Always wait for elements before interacting (waitForSelector)");
        promptBuilder.AppendLine("- Use specific CSS selectors (avoid generic ones like 'div', 'span')");
        promptBuilder.AppendLine("- Add delays between actions for content loading (500-2000ms)");
        promptBuilder.AppendLine("- Scroll gradually to trigger lazy loading");
        promptBuilder.AppendLine("- Take screenshots to verify final state");
        promptBuilder.AppendLine("- Use repeat sparingly and only when necessary");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("## Common E-commerce Patterns:");
        promptBuilder.AppendLine("- **Load More**: scroll → waitForSelector('.load-more') → click('.load-more') → wait(2000)");
        promptBuilder.AppendLine("- **Infinite Scroll**: scroll with repeat=5-10, delayMs=1500");
        promptBuilder.AppendLine("- **Price Extraction**: waitForSelector('.price') → screenshot");
        promptBuilder.AppendLine("- **Product Details**: click('.product-link') → waitForSelector('.product-details') → screenshot");
        promptBuilder.AppendLine();
        
        if (!string.IsNullOrEmpty(context))
        {
            promptBuilder.AppendLine($"## Context: {context}");
            promptBuilder.AppendLine();
        }
        
        promptBuilder.AppendLine($"## Task: {instructions}");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("Generate a precise sequence of browser actions. Each action must include:");
        promptBuilder.AppendLine("- actionType (required)");
        promptBuilder.AppendLine("- selector (when targeting elements)");
        promptBuilder.AppendLine("- value (for type, selectOption, setValue, evaluate actions)");
        promptBuilder.AppendLine("- repeat (default: 1, use only when repeating makes sense)");
        promptBuilder.AppendLine("- delayMs (wait time after action, use 500-2000ms for loading)");
        promptBuilder.AppendLine("- description (brief explanation of what this action does)");
        promptBuilder.AppendLine();
        
        promptBuilder.AppendLine("Provide a clear explanation of the overall automation strategy.");
        
        return promptBuilder.ToString();
    }

    private object BuildGeminiRequest(string prompt, AiConfigurationDto configuration, bool isTest = false)
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                actions = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            actionType = new { type = "string" },
                            selector = new { type = "string" },
                            repeat = new { type = "integer" },
                            delayMs = new { type = "integer" },
                            value = new { type = "string" },
                            description = new { type = "string" }
                        },
                        required = new[] { "actionType" }
                    }
                },
                explanation = new { type = "string" }
            },
            required = new[] { "actions", "explanation" }
        };

        if (isTest)
        {
            // Simple test request without structured output
            return new
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
                    maxOutputTokens = 100,
                    temperature = 0.1
                }
            };
        }

        return new
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
                maxOutputTokens = configuration.OutputTokenLimit ?? 8192,
                temperature = 0.1,
                responseMimeType = "application/json",
                responseSchema = schema
            }
        };
    }

    private async Task<Result<BrowserActionGenerationResponseDto>> CallGeminiApiAsync(
        object requestBody, 
        AiConfigurationDto configuration)
    {
        try
        {
            var baseUrl = !string.IsNullOrEmpty(configuration.OpenApiCompatibleUrl)
                ? configuration.OpenApiCompatibleUrl.TrimEnd('/')
                : "https://generativelanguage.googleapis.com";
            
            var apiKey = configuration.DecryptedApiKey ?? throw new InvalidOperationException("API key not available");
            var url = $"{baseUrl}/v1beta/models/{configuration.Model}:generateContent?key={apiKey}";
            
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Sending request to Google Gemini API: {Url}", url.Replace(apiKey, "***"));
            
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<BrowserActionGenerationResponseDto>.Failure($"API error: {response.StatusCode}");
            }
            
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            if (geminiResponse?.Candidates?.Any() != true)
            {
                return Result<BrowserActionGenerationResponseDto>.Failure("No response generated");
            }
            
            var candidate = geminiResponse.Candidates.First();
            var responseText = candidate.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrEmpty(responseText))
            {
                return Result<BrowserActionGenerationResponseDto>.Failure("Empty response from AI");
            }
            
            // Parse the structured JSON response
            var structuredResponse = JsonSerializer.Deserialize<StructuredResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            if (structuredResponse == null)
            {
                return Result<BrowserActionGenerationResponseDto>.Failure("Failed to parse structured response");
            }
            
            var result = new BrowserActionGenerationResponseDto
            {
                Actions = structuredResponse.Actions.Select(a => new GeneratedBrowserAction
                {
                    ActionType = a.ActionType,
                    Selector = a.Selector,
                    Repeat = a.Repeat,
                    DelayMs = a.DelayMs,
                    Value = a.Value,
                    Description = a.Description
                }).ToList(),
                Explanation = structuredResponse.Explanation,
                TokensUsed = geminiResponse.UsageMetadata?.TotalTokenCount ?? 0,
                Model = configuration.Model,
                Success = true
            };
            
            return Result<BrowserActionGenerationResponseDto>.Success(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Google Gemini API response");
            return Result<BrowserActionGenerationResponseDto>.Failure($"Failed to parse response: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Gemini API");
            return Result<BrowserActionGenerationResponseDto>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Google Gemini API");
            return Result<BrowserActionGenerationResponseDto>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private string BuildGenericPrompt(string inputText, string? systemPrompt, string? context)
    {
        var promptBuilder = new StringBuilder();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            promptBuilder.AppendLine($"System: {systemPrompt}");
            promptBuilder.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(context))
        {
            promptBuilder.AppendLine($"Context: {context}");
            promptBuilder.AppendLine();
        }
        
        promptBuilder.AppendLine($"User: {inputText}");
        
        return promptBuilder.ToString();
    }

    private object BuildGenericGeminiRequest(string prompt, string? jsonSchema, double? temperature, int? maxTokens, AiConfigurationDto configuration)
    {
        var generationConfig = new Dictionary<string, object>
        {
            ["maxOutputTokens"] = maxTokens ?? configuration.OutputTokenLimit ?? 8192,
            ["temperature"] = temperature ?? 0.7
        };

        // If JSON schema is provided, use structured output
        if (!string.IsNullOrEmpty(jsonSchema))
        {
            try
            {
                var schemaObj = JsonSerializer.Deserialize<object>(jsonSchema);
                if (schemaObj != null)
                {
                    generationConfig["responseMimeType"] = "application/json";
                    generationConfig["responseSchema"] = schemaObj;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON schema provided, falling back to plain text response");
            }
        }

        return new
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
            generationConfig = generationConfig
        };
    }

    private async Task<Result<GenericAiResponseDto>> CallGenericGeminiApiAsync(
        object requestBody, 
        AiConfigurationDto configuration,
        bool isStructuredOutput = false)
    {
        try
        {
            var baseUrl = !string.IsNullOrEmpty(configuration.OpenApiCompatibleUrl)
                ? configuration.OpenApiCompatibleUrl.TrimEnd('/')
                : "https://generativelanguage.googleapis.com";
            
            var apiKey = configuration.DecryptedApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("API key not available");
            }
            var url = $"{baseUrl}/v1beta/models/{configuration.Model}:generateContent?key={apiKey}";
            
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Sending generic request to Google Gemini API: {Url}", url.Replace(apiKey, "***"));
            
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<GenericAiResponseDto>.Failure($"API error: {response.StatusCode}");
            }
            
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            if (geminiResponse?.Candidates?.Any() != true)
            {
                return Result<GenericAiResponseDto>.Failure("No response generated");
            }
            
            var candidate = geminiResponse.Candidates.First();
            var responseText = candidate.Content?.Parts?.FirstOrDefault()?.Text;
            
            if (string.IsNullOrEmpty(responseText))
            {
                return Result<GenericAiResponseDto>.Failure("Empty response from AI");
            }
            
            var result = new GenericAiResponseDto
            {
                Response = responseText,
                TokensUsed = geminiResponse.UsageMetadata?.TotalTokenCount ?? 0,
                Model = configuration.Model,
                Success = true,
                IsStructuredOutput = isStructuredOutput,
                GeneratedAt = DateTime.UtcNow
            };
            
            return Result<GenericAiResponseDto>.Success(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Google Gemini API response");
            return Result<GenericAiResponseDto>.Failure($"Failed to parse response: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Gemini API");
            return Result<GenericAiResponseDto>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Google Gemini API");
            return Result<GenericAiResponseDto>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Response models for Google Gemini API
    private class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
        public UsageMetadata? UsageMetadata { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
        public string? FinishReason { get; set; }
    }

    private class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }

    private class UsageMetadata
    {
        public int PromptTokenCount { get; set; }
        public int CandidatesTokenCount { get; set; }
        public int TotalTokenCount { get; set; }
    }

    private class StructuredResponse
    {
        public List<StructuredAction> Actions { get; set; } = new();
        public string Explanation { get; set; } = "";
    }

    private class StructuredAction
    {
        public string ActionType { get; set; } = "";
        public string? Selector { get; set; }
        public int? Repeat { get; set; }
        public int? DelayMs { get; set; }   
        public string? Value { get; set; }
        public string? Description { get; set; }
    }

    private class GoogleModelsResponse
    {
        public List<GoogleModel>? Models { get; set; }
    }

    private class GoogleModel
    {
        public string? Name { get; set; }
        public string? BaseModelId { get; set; }
        public string? Version { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int? InputTokenLimit { get; set; }
        public int? OutputTokenLimit { get; set; }
        public List<string>? SupportedGenerationMethods { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public int? TopK { get; set; }
    }

    private class OpenAiModelsResponse
    {
        public string? Object { get; set; }
        public List<OpenAiModel>? Data { get; set; }
    }

    private class OpenAiModel
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long? Created { get; set; }
        public string? OwnedBy { get; set; }
    }
} 