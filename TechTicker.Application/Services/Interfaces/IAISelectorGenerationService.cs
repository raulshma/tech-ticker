using TechTicker.Application.DTOs;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services.Interfaces;

/// <summary>
/// Service for generating CSS selectors using AI
/// </summary>
public interface IAISelectorGenerationService
{
    /// <summary>
    /// Generates CSS selectors for a given HTML content and domain using AI
    /// </summary>
    Task<Result<SelectorGenerationResult>> GenerateSelectorsAsync(string htmlContent, string domain);

    /// <summary>
    /// Tests existing selectors against HTML content to validate their effectiveness
    /// </summary>
    Task<Result<SelectorTestResult>> TestSelectorsAsync(string htmlContent, SelectorSet selectors);

    /// <summary>
    /// Suggests improvements to existing selectors based on test results
    /// </summary>
    Task<Result<List<SelectorSuggestion>>> SuggestImprovementsAsync(string htmlContent, SelectorSet currentSelectors, SelectorTestResult testResult);
}
