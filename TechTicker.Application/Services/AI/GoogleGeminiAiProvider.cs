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
            // For Google AI, we know the available models
            var models = new List<string>
            {
                "gemini-2.0-flash-exp",
                "gemini-1.5-pro",
                "gemini-1.5-flash",
                "gemini-1.5-flash-8b"
            };

            return Result<AiProviderModelsDto>.Success(new AiProviderModelsDto
            {
                Models = models,
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Google Gemini models");
            return Result<AiProviderModelsDto>.Failure($"Failed to fetch models: {ex.Message}");
        }
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

    private string BuildPrompt(string instructions, string? context)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("You are an expert in browser automation and web scraping. Your task is to generate a sequence of browser automation actions based on user instructions.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Available action types:");
        promptBuilder.AppendLine("- scroll: Scroll down by one viewport");
        promptBuilder.AppendLine("- click: Click on an element by CSS selector");
        promptBuilder.AppendLine("- waitForSelector: Wait for an element to appear");
        promptBuilder.AppendLine("- type: Type text into an input field");
        promptBuilder.AppendLine("- wait: Wait for a specified time (in milliseconds)");
        promptBuilder.AppendLine("- screenshot: Take a screenshot of the page");
        promptBuilder.AppendLine("- evaluate: Execute custom JavaScript code");
        promptBuilder.AppendLine("- hover: Hover over an element");
        promptBuilder.AppendLine("- selectOption: Select an option from a dropdown");
        promptBuilder.AppendLine("- setValue: Set input value using JavaScript");
        promptBuilder.AppendLine();
        
        if (!string.IsNullOrEmpty(context))
        {
            promptBuilder.AppendLine($"Context: {context}");
            promptBuilder.AppendLine();
        }
        
        promptBuilder.AppendLine($"User Instructions: {instructions}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate an array of browser actions in the specified JSON format. Each action should have an actionType and relevant properties. Include a brief explanation for the overall sequence.");
        
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
} 