using Microsoft.AspNetCore.Mvc;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Common;
using TechTicker.Shared.Controllers;
using TechTicker.Shared.Authorization;
using TechTicker.Shared.Constants;

namespace TechTicker.ApiService.Controllers;

/// <summary>
/// Controller for browser automation profile testing
/// </summary>
[ApiController]
[Route("api/browser-automation/test")]
public class BrowserAutomationTestController : BaseApiController
{
    private readonly IBrowserAutomationTestService _testService;
    private readonly ILogger<BrowserAutomationTestController> _logger;

    public BrowserAutomationTestController(
        IBrowserAutomationTestService testService,
        ILogger<BrowserAutomationTestController> logger)
    {
        _testService = testService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new browser automation test session
    /// </summary>
    /// <param name="request">Test request with profile and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test session information</returns>
    [HttpPost("start")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<BrowserTestSessionDto>>> StartTestSession(
        [FromBody] BrowserAutomationTestRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting browser automation test session for URL: {TestUrl}", request.TestUrl);

            var result = await _testService.StartTestSessionAsync(request, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting browser automation test session");
            return StatusCode(500, ApiResponse<BrowserTestSessionDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Stop a running test session
    /// </summary>
    /// <param name="sessionId">Session ID to stop</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results if available</returns>
    [HttpPost("{sessionId}/stop")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<BrowserAutomationTestResultDto?>>> StopTestSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping browser automation test session: {SessionId}", sessionId);

            var result = await _testService.StopTestSessionAsync(sessionId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping browser automation test session {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<BrowserAutomationTestResultDto?>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get the status of a test session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session status information</returns>
    [HttpGet("{sessionId}/status")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<TestSessionStatusDto>>> GetTestSessionStatus(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testService.GetTestSessionStatusAsync(sessionId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session status {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<TestSessionStatusDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get test session results
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    [HttpGet("{sessionId}/results")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<BrowserAutomationTestResultDto>>> GetTestSessionResults(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testService.GetTestSessionResultsAsync(sessionId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session results {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<BrowserAutomationTestResultDto>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get current screenshot for a test session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Screenshot as base64 string</returns>
    [HttpGet("{sessionId}/screenshot")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<string>>> GetTestSessionScreenshot(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testService.GetTestSessionScreenshotAsync(sessionId, cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session screenshot {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Get test session screenshot as image
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Screenshot as PNG image</returns>
    [HttpGet("{sessionId}/screenshot/image")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<IActionResult> GetTestSessionScreenshotImage(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testService.GetTestSessionScreenshotAsync(sessionId, cancellationToken);
            
            if (!result.IsSuccess || string.IsNullOrEmpty(result.Data))
            {
                return NotFound();
            }

            var imageBytes = Convert.FromBase64String(result.Data);
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session screenshot image {SessionId}", sessionId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Get all active test sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    [HttpGet("sessions")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<List<BrowserTestSessionDto>>>> GetActiveTestSessions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _testService.GetActiveTestSessionsAsync(cancellationToken);
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active test sessions");
            return StatusCode(500, ApiResponse<List<BrowserTestSessionDto>>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Save test results for future reference
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Save request with name and description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved result ID</returns>
    [HttpPost("{sessionId}/save")]
    [RequirePermission(Permissions.ScrapersManageSites)]
    public async Task<ActionResult<ApiResponse<string>>> SaveTestResults(
        string sessionId,
        [FromBody] SaveTestResultsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Saving test results for session {SessionId} with name {Name}", 
                sessionId, request.Name);

            var result = await _testService.SaveTestResultsAsync(
                sessionId, 
                request.Name, 
                request.Description, 
                request.Tags, 
                cancellationToken);
            
            return HandleResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving test results for session {SessionId}", sessionId);
            return StatusCode(500, ApiResponse<string>.FailureResult("Internal server error", 500));
        }
    }

    /// <summary>
    /// Validate a browser automation profile without executing
    /// </summary>
    /// <param name="request">Profile validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [RequirePermission(Permissions.ScrapersViewLogs)]
    public async Task<ActionResult<ApiResponse<ProfileValidationResultDto>>> ValidateProfile(
        [FromBody] ProfileValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation logic
            var validationResult = new ProfileValidationResultDto
            {
                IsValid = true,
                Warnings = new List<string>(),
                Errors = new List<string>()
            };

            // Validate profile structure
            if (request.Profile.Actions != null)
            {
                foreach (var action in request.Profile.Actions.Select((a, i) => new { Action = a, Index = i }))
                {
                    if (string.IsNullOrEmpty(action.Action.ActionType))
                    {
                        validationResult.Errors.Add($"Action {action.Index + 1}: ActionType is required");
                        validationResult.IsValid = false;
                    }

                    if (action.Action.ActionType?.ToLower() == "click" && string.IsNullOrEmpty(action.Action.Selector))
                    {
                        validationResult.Errors.Add($"Action {action.Index + 1}: Selector is required for click actions");
                        validationResult.IsValid = false;
                    }

                    if (action.Action.Repeat.HasValue && action.Action.Repeat <= 0)
                    {
                        validationResult.Warnings.Add($"Action {action.Index + 1}: Repeat count should be greater than 0");
                    }
                }
            }

            // Validate proxy configuration
            if (!string.IsNullOrEmpty(request.Profile.ProxyServer))
            {
                if (!Uri.TryCreate(request.Profile.ProxyServer, UriKind.Absolute, out _))
                {
                    validationResult.Errors.Add("ProxyServer must be a valid URL");
                    validationResult.IsValid = false;
                }
            }

            // Validate timeouts
            if (request.Profile.TimeoutSeconds.HasValue && request.Profile.TimeoutSeconds <= 0)
            {
                validationResult.Errors.Add("TimeoutSeconds must be greater than 0");
                validationResult.IsValid = false;
            }

            return ApiResponse<ProfileValidationResultDto>.SuccessResult(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating browser automation profile");
            return StatusCode(500, ApiResponse<ProfileValidationResultDto>.FailureResult("Internal server error", 500));
        }
    }
}

/// <summary>
/// Request DTO for saving test results
/// </summary>
public class SaveTestResultsRequestDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Request DTO for profile validation
/// </summary>
public class ProfileValidationRequestDto
{
    public BrowserAutomationProfileDto Profile { get; set; } = null!;
}

/// <summary>
/// Result DTO for profile validation
/// </summary>
public class ProfileValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
} 