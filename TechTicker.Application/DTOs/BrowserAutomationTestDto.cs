namespace TechTicker.Application.DTOs;

/// <summary>
/// Request to start a browser automation test session
/// </summary>
public class BrowserAutomationTestRequestDto
{
    public string TestUrl { get; set; } = null!;
    public BrowserAutomationProfileDto Profile { get; set; } = null!;
    public BrowserTestOptionsDto Options { get; set; } = new();
    public bool SaveResults { get; set; } = false;
    public string? SessionName { get; set; }
}

/// <summary>
/// Browser automation profile for testing
/// </summary>
public class BrowserAutomationProfileDto
{
    public string? PreferredBrowser { get; set; }
    public int? WaitTimeMs { get; set; }
    public List<BrowserAutomationActionDto>? Actions { get; set; }
    public int? TimeoutSeconds { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? ProxyServer { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
}

/// <summary>
/// Browser automation action for testing
/// </summary>
public class BrowserAutomationActionDto
{
    public string ActionType { get; set; } = null!;
    public string? Selector { get; set; }
    public int? Repeat { get; set; }
    public int? DelayMs { get; set; }
    public string? Value { get; set; }
}

/// <summary>
/// Browser test configuration options
/// </summary>
public class BrowserTestOptionsDto
{
    // Browser configuration
    public bool RecordVideo { get; set; } = false;
    public bool CaptureScreenshots { get; set; } = true;
    public int SlowMotion { get; set; } = 0; // milliseconds between actions
    public bool Headless { get; set; } = false; // Set to false for testing visibility

    // Logging options
    public bool EnableNetworkLogging { get; set; } = true;
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnablePerformanceLogging { get; set; } = true;

    // Viewport settings
    public int ViewportWidth { get; set; } = 1920;
    public int ViewportHeight { get; set; } = 1080;
    public string DeviceEmulation { get; set; } = "desktop";

    // Timeout settings
    public int TestTimeoutMs { get; set; } = 60000;
    public int ActionTimeoutMs { get; set; } = 30000;
    public int NavigationTimeoutMs { get; set; } = 30000;
}

/// <summary>
/// Browser automation test result
/// </summary>
public class BrowserAutomationTestResultDto
{
    public string SessionId { get; set; } = null!;
    public bool Success { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Duration { get; set; } // milliseconds

    // Execution details
    public int ActionsExecuted { get; set; }
    public List<ActionExecutionResultDto> ActionResults { get; set; } = new();

    // Captured data
    public string? FinalScreenshot { get; set; }
    public string? VideoRecording { get; set; }
    public List<ScreenshotCaptureDto> Screenshots { get; set; } = new();

    // Performance metrics
    public ExecutionMetricsDto? Metrics { get; set; }
    public List<NetworkRequestDto> NetworkRequests { get; set; } = new();
    public List<ConsoleMessageDto> ConsoleMessages { get; set; } = new();

    // Error information
    public List<TestErrorDto> Errors { get; set; } = new();
    public List<TestWarningDto> Warnings { get; set; } = new();

    // Extracted data (if applicable)
    public Dictionary<string, object>? ExtractedData { get; set; }
}

/// <summary>
/// Action execution result
/// </summary>
public class ActionExecutionResultDto
{
    public int ActionIndex { get; set; }
    public string ActionType { get; set; } = null!;
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public bool Success { get; set; }
    public int Duration { get; set; } // milliseconds
    public string? Screenshot { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Screenshot capture details
/// </summary>
public class ScreenshotCaptureDto
{
    public string Id { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string Base64Data { get; set; } = null!;
    public int ActionIndex { get; set; }
    public string? ActionType { get; set; }
}

/// <summary>
/// Execution performance metrics
/// </summary>
public class ExecutionMetricsDto
{
    public int TotalDuration { get; set; } // milliseconds
    public int NavigationTime { get; set; } // milliseconds
    public int ActionsTime { get; set; } // milliseconds
    public long MemoryUsage { get; set; } // bytes
    public double CpuUsage { get; set; } // percentage
    public int NetworkRequestCount { get; set; }
    public long NetworkBytesReceived { get; set; }
    public long NetworkBytesSent { get; set; }
}

/// <summary>
/// Network request details
/// </summary>
public class NetworkRequestDto
{
    public string Url { get; set; } = null!;
    public string Method { get; set; } = null!;
    public int StatusCode { get; set; }
    public string? StatusText { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public int Duration { get; set; } // milliseconds
    public long Size { get; set; } // bytes
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Console message details
/// </summary>
public class ConsoleMessageDto
{
    public string Level { get; set; } = null!; // log, warn, error, debug
    public string Message { get; set; } = null!;
    public string? Source { get; set; }
    public int? LineNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Test error details
/// </summary>
public class TestErrorDto
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public int? ActionIndex { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Test warning details
/// </summary>
public class TestWarningDto
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public int? ActionIndex { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Browser state update for real-time communication
/// </summary>
public class BrowserStateUpdateDto
{
    public string CurrentUrl { get; set; } = null!;
    public string CurrentAction { get; set; } = null!;
    public int ActionIndex { get; set; }
    public int TotalActions { get; set; }
    public string? Screenshot { get; set; }
    public string Status { get; set; } = null!; // navigating, executing, waiting, completed, error
    public int Progress { get; set; } // 0-100
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Test log entry for real-time logging
/// </summary>
public class TestLogEntryDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = null!; // info, warn, error, debug
    public string Category { get; set; } = null!; // browser, network, console, action, system
    public string Message { get; set; } = null!;
    public object? Details { get; set; }
    public int? ActionIndex { get; set; }
}

/// <summary>
/// Test session information
/// </summary>
public class BrowserTestSessionDto
{
    public string Id { get; set; } = null!;
    public string TestUrl { get; set; } = null!;
    public BrowserAutomationProfileDto Profile { get; set; } = null!;
    public BrowserTestOptionsDto Options { get; set; } = null!;
    public string Status { get; set; } = null!; // initializing, running, completed, error, cancelled
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? SessionName { get; set; }
    public string WebSocketUrl { get; set; } = null!;
}

/// <summary>
/// Test session status response
/// </summary>
public class TestSessionStatusDto
{
    public string Status { get; set; } = null!;
    public int Progress { get; set; }
    public string CurrentAction { get; set; } = null!;
    public DateTimeOffset LastUpdated { get; set; }
} 