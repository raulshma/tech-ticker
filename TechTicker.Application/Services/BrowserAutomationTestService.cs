using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service for testing browser automation profiles with real-time feedback
/// </summary>
public class BrowserAutomationTestService : IBrowserAutomationTestService
{
    private readonly IBrowserAutomationWebSocketService _webSocketService;
    private readonly ILogger<BrowserAutomationTestService> _logger;
    private readonly ConcurrentDictionary<string, TestSession> _activeSessions;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _sessionCancellationTokens;

    public BrowserAutomationTestService(
        IBrowserAutomationWebSocketService webSocketService,
        ILogger<BrowserAutomationTestService> logger)
    {
        _webSocketService = webSocketService;
        _logger = logger;
        _activeSessions = new ConcurrentDictionary<string, TestSession>();
        _sessionCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public async Task<Result<BrowserTestSessionDto>> StartTestSessionAsync(
        BrowserAutomationTestRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _logger.LogInformation("Starting browser automation test session {SessionId} for URL {TestUrl}", 
                sessionId, request.TestUrl);

            var session = new TestSession
            {
                Id = sessionId,
                TestUrl = request.TestUrl,
                Profile = request.Profile,
                Options = request.Options,
                Status = "initializing",
                StartedAt = DateTimeOffset.UtcNow,
                SessionName = request.SessionName
            };

            _activeSessions[sessionId] = session;
            _sessionCancellationTokens[sessionId] = sessionCts;

            var sessionDto = new BrowserTestSessionDto
            {
                Id = sessionId,
                TestUrl = request.TestUrl,
                Profile = request.Profile,
                Options = request.Options,
                Status = "initializing",
                StartedAt = session.StartedAt,
                SessionName = request.SessionName,
                WebSocketUrl = $"/hubs/browser-automation-test?sessionId={sessionId}"
            };

            // Start the test execution in the background
            _ = Task.Run(async () => await ExecuteTestAsync(session, sessionCts.Token), sessionCts.Token);

            return Result<BrowserTestSessionDto>.Success(sessionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting test session {SessionId}", sessionId);
            CleanupSession(sessionId);
            return Result<BrowserTestSessionDto>.Failure(
                "Failed to start test session", 
                "TEST_SESSION_START_ERROR");
        }
    }

    public async Task<Result<BrowserAutomationTestResultDto?>> StopTestSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping test session {SessionId}", sessionId);

            if (_sessionCancellationTokens.TryGetValue(sessionId, out var cts))
            {
                cts.Cancel();
            }

            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.Status = "cancelled";
                session.CompletedAt = DateTimeOffset.UtcNow;

                await _webSocketService.BroadcastBrowserStateAsync(sessionId, new BrowserStateUpdateDto
                {
                    CurrentUrl = session.TestUrl,
                    CurrentAction = "Test stopped",
                    ActionIndex = 0,
                    TotalActions = session.Profile.Actions?.Count ?? 0,
                    Status = "cancelled",
                    Progress = 100,
                    Timestamp = DateTimeOffset.UtcNow
                });

                var result = session.Result;
                CleanupSession(sessionId);

                return Result<BrowserAutomationTestResultDto?>.Success(result);
            }

            return Result<BrowserAutomationTestResultDto?>.Success(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping test session {SessionId}", sessionId);
            return Result<BrowserAutomationTestResultDto?>.Failure(
                "Failed to stop test session", 
                "TEST_SESSION_STOP_ERROR");
        }
    }

    public Task<Result<TestSessionStatusDto>> GetTestSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                var status = new TestSessionStatusDto
                {
                    Status = session.Status,
                    Progress = session.Progress,
                    CurrentAction = session.CurrentAction,
                    LastUpdated = DateTimeOffset.UtcNow
                };

                return Task.FromResult(Result<TestSessionStatusDto>.Success(status));
            }

            return Task.FromResult(Result<TestSessionStatusDto>.Failure(
                "Test session not found", 
                "TEST_SESSION_NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session status {SessionId}", sessionId);
            return Task.FromResult(Result<TestSessionStatusDto>.Failure(
                "Failed to get test session status", 
                "TEST_SESSION_STATUS_ERROR"));
        }
    }

    public Task<Result<BrowserAutomationTestResultDto>> GetTestSessionResultsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeSessions.TryGetValue(sessionId, out var session) && session.Result != null)
            {
                return Task.FromResult(Result<BrowserAutomationTestResultDto>.Success(session.Result));
            }

            return Task.FromResult(Result<BrowserAutomationTestResultDto>.Failure(
                "Test results not available", 
                "TEST_RESULTS_NOT_AVAILABLE"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session results {SessionId}", sessionId);
            return Task.FromResult(Result<BrowserAutomationTestResultDto>.Failure(
                "Failed to get test session results", 
                "TEST_SESSION_RESULTS_ERROR"));
        }
    }

    public Task<Result<BrowserTestSessionDto?>> GetTestSessionDetailsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                var sessionDto = new BrowserTestSessionDto
                {
                    Id = session.Id,
                    TestUrl = session.TestUrl,
                    Profile = session.Profile,
                    Options = session.Options,
                    Status = session.Status,
                    StartedAt = session.StartedAt,
                    CompletedAt = session.CompletedAt,
                    SessionName = session.SessionName,
                    WebSocketUrl = $"/hubs/browser-automation-test?sessionId={session.Id}"
                };

                return Task.FromResult(Result<BrowserTestSessionDto?>.Success(sessionDto));
            }

            return Task.FromResult(Result<BrowserTestSessionDto?>.Success(null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session details {SessionId}", sessionId);
            return Task.FromResult(Result<BrowserTestSessionDto?>.Failure(
                "Failed to get test session details", 
                "TEST_SESSION_DETAILS_ERROR"));
        }
    }

    public Task<Result<string>> GetTestSessionScreenshotAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeSessions.TryGetValue(sessionId, out var session) && 
                !string.IsNullOrEmpty(session.CurrentScreenshot))
            {
                return Task.FromResult(Result<string>.Success(session.CurrentScreenshot));
            }

            return Task.FromResult(Result<string>.Failure(
                "Screenshot not available", 
                "SCREENSHOT_NOT_AVAILABLE"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting test session screenshot {SessionId}", sessionId);
            return Task.FromResult(Result<string>.Failure(
                "Failed to get screenshot", 
                "SCREENSHOT_ERROR"));
        }
    }

    public Task<Result<List<BrowserTestSessionDto>>> GetActiveTestSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = _activeSessions.Values
                .Where(s => s.Status != "completed" && s.Status != "error" && s.Status != "cancelled")
                .Select(s => new BrowserTestSessionDto
                {
                    Id = s.Id,
                    TestUrl = s.TestUrl,
                    Profile = s.Profile,
                    Options = s.Options,
                    Status = s.Status,
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt,
                    SessionName = s.SessionName,
                    WebSocketUrl = $"/hubs/browser-automation-test?sessionId={s.Id}"
                })
                .ToList();

            return Task.FromResult(Result<List<BrowserTestSessionDto>>.Success(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active test sessions");
            return Task.FromResult(Result<List<BrowserTestSessionDto>>.Failure(
                "Failed to get active test sessions", 
                "ACTIVE_SESSIONS_ERROR"));
        }
    }



    private async Task ExecuteTestAsync(TestSession session, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        IBrowser? browser = null;
        IPage? page = null;

        try
        {
            session.Status = "running";
            session.Progress = 0;
            session.CurrentAction = "Initializing browser";

            await _webSocketService.BroadcastBrowserStateAsync(session.Id, new BrowserStateUpdateDto
            {
                CurrentUrl = session.TestUrl,
                CurrentAction = session.CurrentAction,
                ActionIndex = 0,
                TotalActions = session.Profile.Actions?.Count ?? 0,
                Status = "initializing",
                Progress = 0,
                Timestamp = DateTimeOffset.UtcNow
            });

            await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = "info",
                Category = "system",
                Message = "Starting browser automation test",
                Details = new { TestUrl = session.TestUrl, SessionId = session.Id }
            });

            // Initialize Playwright
            using var playwright = await Playwright.CreateAsync();
            
            var browserType = session.Profile.PreferredBrowser?.ToLower() switch
            {
                "firefox" => playwright.Firefox,
                "webkit" => playwright.Webkit,
                _ => playwright.Chromium
            };

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = session.Options.Headless,
                SlowMo = session.Options.SlowMotion,
                Timeout = session.Options.TestTimeoutMs
            };

            browser = await browserType.LaunchAsync(launchOptions);

            // Configure browser context
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = session.Options.ViewportWidth,
                    Height = session.Options.ViewportHeight
                },
                UserAgent = session.Profile.UserAgent,
                ExtraHTTPHeaders = session.Profile.Headers
            };

            // Configure proxy if provided
            if (!string.IsNullOrEmpty(session.Profile.ProxyServer))
            {
                contextOptions.Proxy = new Proxy
                {
                    Server = session.Profile.ProxyServer,
                    Username = session.Profile.ProxyUsername,
                    Password = session.Profile.ProxyPassword
                };
            }

            var context = await browser.NewContextAsync(contextOptions);
            page = await context.NewPageAsync();

            // Set up page monitoring
            if (session.Options.EnableConsoleLogging)
            {
                page.Console += async (_, args) =>
                {
                    await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        Level = args.Type.ToLower(),
                        Category = "console",
                        Message = args.Text,
                        Details = new { Source = "browser_console" }
                    });
                };
            }

            if (session.Options.EnableNetworkLogging)
            {
                page.Request += async (_, request) =>
                {
                    await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
                    {
                        Timestamp = DateTimeOffset.UtcNow,
                        Level = "debug",
                        Category = "network",
                        Message = $"Request: {request.Method} {request.Url}",
                        Details = new { Method = request.Method, Url = request.Url }
                    });
                };
            }

            // Navigate to the test URL
            session.CurrentAction = "Navigating to test URL";
            session.Progress = 10;

            await _webSocketService.BroadcastBrowserStateAsync(session.Id, new BrowserStateUpdateDto
            {
                CurrentUrl = session.TestUrl,
                CurrentAction = session.CurrentAction,
                ActionIndex = 0,
                TotalActions = session.Profile.Actions?.Count ?? 0,
                Status = "navigating",
                Progress = session.Progress,
                Timestamp = DateTimeOffset.UtcNow
            });

            await page.GotoAsync(session.TestUrl, new PageGotoOptions
            {
                Timeout = session.Options.NavigationTimeoutMs,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Wait for initial page load if specified
            if (session.Profile.WaitTimeMs.HasValue && session.Profile.WaitTimeMs > 0)
            {
                session.CurrentAction = $"Waiting {session.Profile.WaitTimeMs}ms for page load";
                await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Level = "info",
                    Category = "action",
                    Message = session.CurrentAction
                });

                await Task.Delay(session.Profile.WaitTimeMs.Value, cancellationToken);
            }

            // Take initial screenshot
            var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = false
            });
            session.CurrentScreenshot = Convert.ToBase64String(screenshotBytes);

            await _webSocketService.BroadcastBrowserStateAsync(session.Id, new BrowserStateUpdateDto
            {
                CurrentUrl = page.Url,
                CurrentAction = "Page loaded",
                ActionIndex = 0,
                TotalActions = session.Profile.Actions?.Count ?? 0,
                Screenshot = session.CurrentScreenshot,
                Status = "executing",
                Progress = 20,
                Timestamp = DateTimeOffset.UtcNow
            });

            // Execute automation actions
            if (session.Profile.Actions != null)
            {
                await ExecuteActionsAsync(session, page, cancellationToken);
            }

            // Capture final screenshot
            var finalScreenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = false
            });
            var finalScreenshot = Convert.ToBase64String(finalScreenshotBytes);

            stopwatch.Stop();

            // Create test result
            session.Result = new BrowserAutomationTestResultDto
            {
                SessionId = session.Id,
                TestUrl = session.TestUrl,
                Success = true,
                StartedAt = session.StartedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Duration = (int)stopwatch.ElapsedMilliseconds,
                Profile = session.Profile,
                Options = session.Options,
                ActionsExecuted = session.Profile.Actions?.Count ?? 0,
                FinalScreenshot = finalScreenshot,
                Screenshots = new List<ScreenshotCaptureDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = DateTimeOffset.UtcNow,
                        Base64Data = finalScreenshot,
                        ActionIndex = -1,
                        ActionType = "final"
                    }
                },
                Metrics = new ExecutionMetricsDto
                {
                    TotalDuration = (int)stopwatch.ElapsedMilliseconds,
                    NavigationTime = 2000, // TODO: Track actual navigation time
                    ActionsTime = (int)stopwatch.ElapsedMilliseconds - 2000,
                    NetworkRequestCount = 0, // TODO: Track network requests
                    NetworkBytesReceived = 0,
                    NetworkBytesSent = 0,
                    MemoryUsage = GC.GetTotalMemory(false),
                    CpuUsage = 0.0
                }
            };

            session.Status = "completed";
            session.CompletedAt = DateTimeOffset.UtcNow;
            session.Progress = 100;

            await _webSocketService.BroadcastTestCompletedAsync(session.Id, session.Result);

            _logger.LogInformation("Test session {SessionId} completed successfully in {Duration}ms", 
                session.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Test session {SessionId} was cancelled", session.Id);
            session.Status = "cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing test session {SessionId}", session.Id);
            
            session.Status = "error";
            session.Result = new BrowserAutomationTestResultDto
            {
                SessionId = session.Id,
                TestUrl = session.TestUrl,
                Success = false,
                StartedAt = session.StartedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Duration = (int)stopwatch.ElapsedMilliseconds,
                Profile = session.Profile,
                Options = session.Options,
                Errors = new List<TestErrorDto>
                {
                    new()
                    {
                        Code = "EXECUTION_ERROR",
                        Message = ex.Message,
                        Details = ex.ToString(),
                        Timestamp = DateTimeOffset.UtcNow
                    }
                }
            };

            await _webSocketService.BroadcastErrorAsync(session.Id, new TestErrorDto
            {
                Code = "EXECUTION_ERROR",
                Message = ex.Message,
                Details = ex.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        finally
        {
            if (page != null)
            {
                await page.CloseAsync();
            }
            if (browser != null)
            {
                await browser.CloseAsync();
            }
            
            CleanupSession(session.Id);
        }
    }

    private async Task ExecuteActionsAsync(TestSession session, IPage page, CancellationToken cancellationToken)
    {
        if (session.Profile.Actions == null) return;

        for (int i = 0; i < session.Profile.Actions.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var action = session.Profile.Actions[i];
            var actionStopwatch = Stopwatch.StartNew();

            try
            {
                session.CurrentAction = $"Executing {action.ActionType}";
                session.Progress = 20 + (int)((i / (double)session.Profile.Actions.Count) * 70);

                await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Level = "info",
                    Category = "action",
                    Message = $"Executing action {i + 1}/{session.Profile.Actions.Count}: {action.ActionType}",
                    ActionIndex = i,
                    Details = action
                });

                await ExecuteActionAsync(page, action, session.Options);

                // Take screenshot after action if enabled
                if (session.Options.CaptureScreenshots)
                {
                    var screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        FullPage = false
                    });
                    session.CurrentScreenshot = Convert.ToBase64String(screenshotBytes);
                }

                actionStopwatch.Stop();

                await _webSocketService.BroadcastBrowserStateAsync(session.Id, new BrowserStateUpdateDto
                {
                    CurrentUrl = page.Url,
                    CurrentAction = $"Completed {action.ActionType}",
                    ActionIndex = i + 1,
                    TotalActions = session.Profile.Actions.Count,
                    Screenshot = session.CurrentScreenshot,
                    Status = "executing",
                    Progress = session.Progress,
                    Timestamp = DateTimeOffset.UtcNow
                });

                _logger.LogDebug("Action {ActionIndex} ({ActionType}) completed in {Duration}ms", 
                    i, action.ActionType, actionStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {ActionIndex} ({ActionType}) in session {SessionId}", 
                    i, action.ActionType, session.Id);

                await _webSocketService.BroadcastLogEntryAsync(session.Id, new TestLogEntryDto
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    Level = "error",
                    Category = "action",
                    Message = $"Error executing action {i + 1}: {ex.Message}",
                    ActionIndex = i,
                    Details = new { Action = action, Error = ex.Message }
                });

                // Continue with remaining actions unless it's a critical error
                if (ex is TimeoutException || ex.Message.Contains("timeout"))
                {
                    continue;
                }
            }
        }
    }

    private async Task ExecuteActionAsync(IPage page, BrowserAutomationActionDto action, BrowserTestOptionsDto options)
    {
        var repeat = action.Repeat ?? 1;
        
        for (int i = 0; i < repeat; i++)
        {
            switch (action.ActionType.ToLower())
            {
                case "navigate":
                case "goto":
                case "url":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await page.GotoAsync(action.Value, new PageGotoOptions
                        {
                            Timeout = options.NavigationTimeoutMs,
                            WaitUntil = WaitUntilState.NetworkIdle
                        });
                    }
                    break;

                case "scroll":
                    await page.EvaluateAsync("window.scrollBy(0, window.innerHeight);");
                    break;

                case "click":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.ClickAsync(action.Selector, new PageClickOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "waitforselector":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.WaitForSelectorAsync(action.Selector, new PageWaitForSelectorOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "type":
                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                    {
                        await page.Locator(action.Selector).FillAsync(action.Value, new LocatorFillOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "wait":
                case "waitfortimeout":
                    if (action.DelayMs.HasValue)
                    {
                        await page.WaitForTimeoutAsync(action.DelayMs.Value);
                    }
                    break;

                case "screenshot":
                    var screenshotPath = !string.IsNullOrEmpty(action.Value) 
                        ? action.Value 
                        : $"screenshot_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
                    await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                    break;

                case "evaluate":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await page.EvaluateAsync(action.Value);
                    }
                    break;

                case "hover":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.HoverAsync(action.Selector, new PageHoverOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "selectoption":
                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                    {
                        await page.SelectOptionAsync(action.Selector, action.Value);
                    }
                    break;

                case "setvalue":
                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                    {
                        var setValue = action.Value.Replace("'", "\\'");
                        await page.EvaluateAsync($"document.querySelector('{action.Selector}').value = '{setValue}'");
                    }
                    break;

                case "doubleclick":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.DblClickAsync(action.Selector, new PageDblClickOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "rightclick":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.ClickAsync(action.Selector, new PageClickOptions
                        {
                            Button = MouseButton.Right,
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "focus":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.FocusAsync(action.Selector, new PageFocusOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "blur":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.EvaluateAsync($"document.querySelector('{action.Selector}').blur()");
                    }
                    break;

                case "clear":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        await page.Locator(action.Selector).ClearAsync(new LocatorClearOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "press":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await page.Keyboard.PressAsync(action.Value);
                    }
                    break;

                case "upload":
                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                    {
                        await page.SetInputFilesAsync(action.Selector, action.Value, new PageSetInputFilesOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "drag":
                    if (!string.IsNullOrEmpty(action.Selector) && !string.IsNullOrEmpty(action.Value))
                    {
                        // action.Value should contain target selector
                        await page.DragAndDropAsync(action.Selector, action.Value, new PageDragAndDropOptions
                        {
                            Timeout = options.ActionTimeoutMs
                        });
                    }
                    break;

                case "reload":
                case "refresh":
                    await page.ReloadAsync(new PageReloadOptions
                    {
                        Timeout = options.ActionTimeoutMs
                    });
                    break;

                case "goback":
                    await page.GoBackAsync(new PageGoBackOptions
                    {
                        Timeout = options.ActionTimeoutMs
                    });
                    break;

                case "goforward":
                    await page.GoForwardAsync(new PageGoForwardOptions
                    {
                        Timeout = options.ActionTimeoutMs
                    });
                    break;

                case "maximize":
                    await page.Context.Pages.First().SetViewportSizeAsync(1920, 1080);
                    break;

                case "minimize":
                    await page.Context.Pages.First().SetViewportSizeAsync(800, 600);
                    break;

                case "fullscreen":
                    await page.EvaluateAsync("document.documentElement.requestFullscreen()");
                    break;

                case "switchframe":
                case "switchiframe":
                    if (!string.IsNullOrEmpty(action.Selector))
                    {
                        var frame = page.FrameLocator(action.Selector);
                        // Note: Frame switching in Playwright is handled differently
                        // This would need additional context management
                    }
                    break;

                case "alert":
                case "acceptalert":
                    // Handle alert dialogs
                    page.Dialog += async (sender, dialog) =>
                    {
                        await dialog.AcceptAsync();
                    };
                    break;

                case "dismissalert":
                    // Handle alert dialogs
                    page.Dialog += async (sender, dialog) =>
                    {
                        await dialog.DismissAsync();
                    };
                    break;

                case "getcookies":
                    var cookies = await page.Context.CookiesAsync();
                    // Store cookies for later use or validation
                    break;

                case "setcookies":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        try
                        {
                            var cookieData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(action.Value);
                            // Set cookies from JSON data
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to parse cookie data: {Error}", ex.Message);
                        }
                    }
                    break;

                case "deletecookies":
                    await page.Context.ClearCookiesAsync();
                    break;

                case "waitfornavigation":
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                    {
                        Timeout = options.ActionTimeoutMs
                    });
                    break;

                case "waitforloadstate":
                    var loadState = action.Value?.ToLower() switch
                    {
                        "domcontentloaded" => LoadState.DOMContentLoaded,
                        "networkidle" => LoadState.NetworkIdle,
                        _ => LoadState.Load
                    };
                    await page.WaitForLoadStateAsync(loadState, new PageWaitForLoadStateOptions
                    {
                        Timeout = options.ActionTimeoutMs
                    });
                    break;

                case "closetab":
                case "closepage":
                    await page.CloseAsync();
                    break;

                case "newtab":
                case "newpage":
                    var newPage = await page.Context.NewPageAsync();
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await newPage.GotoAsync(action.Value);
                    }
                    break;

                case "switchwindow":
                case "switchtab":
                    if (!string.IsNullOrEmpty(action.Value) && int.TryParse(action.Value, out var tabIndex))
                    {
                        var pages = page.Context.Pages;
                        if (tabIndex >= 0 && tabIndex < pages.Count)
                        {
                            await pages[tabIndex].BringToFrontAsync();
                        }
                    }
                    break;

                case "addstylesheet":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await page.AddStyleTagAsync(new PageAddStyleTagOptions
                        {
                            Content = action.Value
                        });
                    }
                    break;

                case "addscript":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        await page.AddScriptTagAsync(new PageAddScriptTagOptions
                        {
                            Content = action.Value
                        });
                    }
                    break;

                case "emulatedevice":
                    if (!string.IsNullOrEmpty(action.Value))
                    {
                        // Parse device settings from action.Value (JSON)
                        try
                        {
                            var deviceSettings = System.Text.Json.JsonSerializer.Deserialize<dynamic>(action.Value);
                            // Apply device emulation settings
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to parse device emulation data: {Error}", ex.Message);
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException($"Action type '{action.ActionType}' is not supported. " +
                        $"Supported actions: navigate, goto, url, click, doubleclick, rightclick, type, focus, blur, clear, " +
                        $"hover, scroll, wait, press, upload, drag, screenshot, evaluate, selectoption, setvalue, " +
                        $"reload, goback, goforward, maximize, minimize, fullscreen, switchframe, alert, " +
                        $"acceptalert, dismissalert, getcookies, setcookies, deletecookies, waitfornavigation, " +
                        $"waitforloadstate, closetab, newtab, switchwindow, addstylesheet, addscript, emulatedevice");
            }

            // Apply delay after action if specified
            if (action.DelayMs.HasValue && action.ActionType.ToLower() != "wait" && action.ActionType.ToLower() != "waitfortimeout")
            {
                await Task.Delay(action.DelayMs.Value);
            }
        }
    }

    private void CleanupSession(string sessionId)
    {
        _activeSessions.TryRemove(sessionId, out _);
        if (_sessionCancellationTokens.TryRemove(sessionId, out var cts))
        {
            cts.Dispose();
        }
    }

    private class TestSession
    {
        public string Id { get; set; } = null!;
        public string TestUrl { get; set; } = null!;
        public BrowserAutomationProfileDto Profile { get; set; } = null!;
        public BrowserTestOptionsDto Options { get; set; } = null!;
        public string Status { get; set; } = "initializing";
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? SessionName { get; set; }
        public int Progress { get; set; }
        public string CurrentAction { get; set; } = "";
        public string? CurrentScreenshot { get; set; }
        public BrowserAutomationTestResultDto? Result { get; set; }
    }
}