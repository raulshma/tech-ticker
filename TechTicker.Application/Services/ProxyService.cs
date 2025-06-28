using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Common;
using TechTicker.Shared.Utilities;

namespace TechTicker.Application.Services;

/// <summary>
/// Service implementation for proxy configuration management
/// </summary>
public class ProxyService : IProxyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProxyService> _logger;
    private readonly HttpClient _httpClient;

    // Regex patterns for parsing proxy strings
    private static readonly Regex ProxyRegex = new(
        @"^(?<type>https?|socks[45]?)://(?:(?<username>[^:]+):(?<password>[^@]+)@)?(?<host>[^:]+):(?<port>\d+)/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SimpleProxyRegex = new(
        @"^(?<host>[^:]+):(?<port>\d+)(?::(?<username>[^:]+):(?<password>.+))?$",
        RegexOptions.Compiled);

    public ProxyService(IUnitOfWork unitOfWork, ILogger<ProxyService> logger, HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<Result<IEnumerable<ProxyConfigurationDto>>> GetAllProxiesAsync()
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetAllAsync();
            var proxyDtos = proxies.Select(MapToDto).ToList();
            return Result<IEnumerable<ProxyConfigurationDto>>.Success(proxyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all proxy configurations");
            return Result<IEnumerable<ProxyConfigurationDto>>.Failure("Failed to retrieve proxy configurations");
        }
    }

    public async Task<Result<ProxyConfigurationDto>> GetProxyByIdAsync(Guid id)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<ProxyConfigurationDto>.Failure("Proxy configuration not found");
            }

            return Result<ProxyConfigurationDto>.Success(MapToDto(proxy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving proxy configuration {ProxyId}", id);
            return Result<ProxyConfigurationDto>.Failure("Failed to retrieve proxy configuration");
        }
    }

    public async Task<Result<IEnumerable<ProxyConfigurationDto>>> GetActiveProxiesAsync()
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetActiveProxiesAsync();
            var proxyDtos = proxies.Select(MapToDto).ToList();
            return Result<IEnumerable<ProxyConfigurationDto>>.Success(proxyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active proxy configurations");
            return Result<IEnumerable<ProxyConfigurationDto>>.Failure("Failed to retrieve active proxy configurations");
        }
    }

    public async Task<Result<IEnumerable<ProxyConfigurationDto>>> GetHealthyProxiesAsync()
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetHealthyProxiesAsync();
            var proxyDtos = proxies.Select(MapToDto).ToList();
            return Result<IEnumerable<ProxyConfigurationDto>>.Success(proxyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving healthy proxy configurations");
            return Result<IEnumerable<ProxyConfigurationDto>>.Failure("Failed to retrieve healthy proxy configurations");
        }
    }

    public async Task<Result<IEnumerable<ProxyConfigurationDto>>> GetProxiesByTypeAsync(string proxyType)
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetProxiesByTypeAsync(proxyType);
            var proxyDtos = proxies.Select(MapToDto).ToList();
            return Result<IEnumerable<ProxyConfigurationDto>>.Success(proxyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving proxy configurations by type {ProxyType}", proxyType);
            return Result<IEnumerable<ProxyConfigurationDto>>.Failure("Failed to retrieve proxy configurations by type");
        }
    }

    public async Task<Result<ProxyConfigurationDto>> CreateProxyAsync(CreateProxyConfigurationDto createDto)
    {
        try
        {
            // Check if proxy already exists
            var existingProxy = await _unitOfWork.ProxyConfigurations.GetByHostAndPortAsync(createDto.Host, createDto.Port);
            if (existingProxy != null)
            {
                return Result<ProxyConfigurationDto>.Failure("A proxy with this host and port already exists");
            }

            var proxy = new ProxyConfiguration
            {
                ProxyConfigurationId = Guid.NewGuid(),
                Host = createDto.Host,
                Port = createDto.Port,
                ProxyType = createDto.ProxyType.ToUpper(),
                Username = createDto.Username,
                Password = createDto.Password, // TODO: Encrypt password
                Description = createDto.Description,
                TimeoutSeconds = createDto.TimeoutSeconds,
                MaxRetries = createDto.MaxRetries,
                IsActive = createDto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.ProxyConfigurations.AddAsync(proxy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created new proxy configuration {ProxyId}: {Host}:{Port}", 
                proxy.ProxyConfigurationId, proxy.Host, proxy.Port);

            return Result<ProxyConfigurationDto>.Success(MapToDto(proxy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating proxy configuration");
            return Result<ProxyConfigurationDto>.Failure("Failed to create proxy configuration");
        }
    }

    public async Task<Result<ProxyConfigurationDto>> UpdateProxyAsync(Guid id, UpdateProxyConfigurationDto updateDto)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<ProxyConfigurationDto>.Failure("Proxy configuration not found");
            }

            // Check if another proxy with same host/port exists (excluding current one)
            var existingProxy = await _unitOfWork.ProxyConfigurations.GetByHostAndPortAsync(updateDto.Host, updateDto.Port);
            if (existingProxy != null && existingProxy.ProxyConfigurationId != id)
            {
                return Result<ProxyConfigurationDto>.Failure("Another proxy with this host and port already exists");
            }

            proxy.Host = updateDto.Host;
            proxy.Port = updateDto.Port;
            proxy.ProxyType = updateDto.ProxyType.ToUpper();
            proxy.Username = updateDto.Username;
            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                proxy.Password = updateDto.Password; // TODO: Encrypt password
            }
            proxy.Description = updateDto.Description;
            proxy.TimeoutSeconds = updateDto.TimeoutSeconds;
            proxy.MaxRetries = updateDto.MaxRetries;
            proxy.IsActive = updateDto.IsActive;
            proxy.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.ProxyConfigurations.Update(proxy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated proxy configuration {ProxyId}: {Host}:{Port}", 
                proxy.ProxyConfigurationId, proxy.Host, proxy.Port);

            return Result<ProxyConfigurationDto>.Success(MapToDto(proxy));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating proxy configuration {ProxyId}", id);
            return Result<ProxyConfigurationDto>.Failure("Failed to update proxy configuration");
        }
    }

    public async Task<Result<bool>> DeleteProxyAsync(Guid id)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<bool>.Failure("Proxy configuration not found");
            }

            _unitOfWork.ProxyConfigurations.Remove(proxy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deleted proxy configuration {ProxyId}: {Host}:{Port}", 
                proxy.ProxyConfigurationId, proxy.Host, proxy.Port);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting proxy configuration {ProxyId}", id);
            return Result<bool>.Failure("Failed to delete proxy configuration");
        }
    }

    public async Task<Result<ProxyTestResultDto>> TestProxyAsync(Guid id, string? testUrl = null, int timeoutSeconds = 30)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<ProxyTestResultDto>.Failure("Proxy configuration not found");
            }

            var result = string.IsNullOrEmpty(testUrl)
                ? await TestProxyWithFallbacks(proxy, timeoutSeconds)
                : await TestProxyConnection(proxy, testUrl, timeoutSeconds);

            // Update proxy health status
            proxy.UpdateHealthStatus(result.IsHealthy, result.ErrorMessage, result.ErrorCode);
            _unitOfWork.ProxyConfigurations.Update(proxy);
            await _unitOfWork.SaveChangesAsync();

            return Result<ProxyTestResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing proxy configuration {ProxyId}", id);
            return Result<ProxyTestResultDto>.Failure("Failed to test proxy configuration");
        }
    }

    public async Task<Result<IEnumerable<ProxyTestResultDto>>> BulkTestProxiesAsync(BulkProxyTestDto testDto)
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetByIdsAsync(testDto.ProxyIds);
            var results = new List<ProxyTestResultDto>();
            var healthUpdates = new List<(Guid ProxyId, bool IsHealthy, string? ErrorMessage, string? ErrorCode)>();

            foreach (var proxy in proxies)
            {
                var result = await TestProxyConnection(proxy, testDto.TestUrl ?? "http://httpbin.org/ip", testDto.TimeoutSeconds);
                results.Add(result);
                healthUpdates.Add((proxy.ProxyConfigurationId, result.IsHealthy, result.ErrorMessage, result.ErrorCode));
            }

            // Bulk update health status
            await _unitOfWork.ProxyConfigurations.BulkUpdateHealthStatusAsync(healthUpdates);
            await _unitOfWork.SaveChangesAsync();

            return Result<IEnumerable<ProxyTestResultDto>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk testing proxy configurations");
            return Result<IEnumerable<ProxyTestResultDto>>.Failure("Failed to test proxy configurations");
        }
    }

    public async Task<Result<ProxyTestResultDto>> TestProxyConfigurationAsync(CreateProxyConfigurationDto proxyDto, string? testUrl = null, int timeoutSeconds = 30)
    {
        try
        {
            // Create a temporary proxy configuration object for testing
            var tempProxy = new ProxyConfiguration
            {
                ProxyConfigurationId = Guid.NewGuid(), // Temporary ID for testing
                Host = proxyDto.Host,
                Port = proxyDto.Port,
                ProxyType = proxyDto.ProxyType.ToUpper(),
                Username = proxyDto.Username,
                Password = proxyDto.Password,
                TimeoutSeconds = proxyDto.TimeoutSeconds,
                MaxRetries = proxyDto.MaxRetries,
                IsActive = proxyDto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var result = string.IsNullOrEmpty(testUrl)
                ? await TestProxyWithFallbacks(tempProxy, timeoutSeconds)
                : await TestProxyConnection(tempProxy, testUrl, timeoutSeconds);

            return Result<ProxyTestResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing proxy configuration {Host}:{Port}", proxyDto.Host, proxyDto.Port);
            return Result<ProxyTestResultDto>.Failure($"Failed to test proxy configuration: {ex.Message}");
        }
    }

    public async Task<Result<ProxyStatsDto>> GetProxyStatsAsync()
    {
        try
        {
            var (total, active, healthy, averageSuccessRate) = await _unitOfWork.ProxyConfigurations.GetProxyStatsAsync();
            var allProxies = await _unitOfWork.ProxyConfigurations.GetAllAsync();

            var stats = new ProxyStatsDto
            {
                TotalProxies = total,
                ActiveProxies = active,
                HealthyProxies = healthy,
                AverageSuccessRate = averageSuccessRate,
                ProxiesWithErrors = allProxies.Count(p => !string.IsNullOrEmpty(p.LastErrorMessage)),
                ProxiesByType = allProxies.GroupBy(p => p.ProxyType)
                                         .ToDictionary(g => g.Key, g => g.Count()),
                ProxiesByStatus = allProxies.GroupBy(p => p.StatusDescription)
                                           .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<ProxyStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving proxy statistics");
            return Result<ProxyStatsDto>.Failure("Failed to retrieve proxy statistics");
        }
    }

    public Result<ProxyImportItemDto> ParseProxyString(string proxyString, string defaultProxyType = "HTTP")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(proxyString))
            {
                return Result<ProxyImportItemDto>.Failure("Proxy string cannot be empty");
            }

            proxyString = proxyString.Trim();

            // Try full URL format first (http://user:pass@host:port)
            var match = ProxyRegex.Match(proxyString);
            if (match.Success)
            {
                return Result<ProxyImportItemDto>.Success(new ProxyImportItemDto
                {
                    Host = match.Groups["host"].Value,
                    Port = int.Parse(match.Groups["port"].Value),
                    ProxyType = match.Groups["type"].Value.ToUpper(),
                    Username = match.Groups["username"].Success ? match.Groups["username"].Value : null,
                    Password = match.Groups["password"].Success ? match.Groups["password"].Value : null
                });
            }

            // Try simple format (host:port or host:port:user:pass)
            match = SimpleProxyRegex.Match(proxyString);
            if (match.Success)
            {
                return Result<ProxyImportItemDto>.Success(new ProxyImportItemDto
                {
                    Host = match.Groups["host"].Value,
                    Port = int.Parse(match.Groups["port"].Value),
                    ProxyType = defaultProxyType.ToUpper(), // Use provided default type
                    Username = match.Groups["username"].Success ? match.Groups["username"].Value : null,
                    Password = match.Groups["password"].Success ? match.Groups["password"].Value : null
                });
            }

            return Result<ProxyImportItemDto>.Failure($"Invalid proxy format: {proxyString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing proxy string: {ProxyString}", proxyString);
            return Result<ProxyImportItemDto>.Failure($"Failed to parse proxy string: {ex.Message}");
        }
    }

    public Result<IEnumerable<ProxyImportItemDto>> ParseProxyText(string proxyText, string defaultProxyType = "HTTP")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(proxyText))
            {
                return Result<IEnumerable<ProxyImportItemDto>>.Failure("Proxy text cannot be empty");
            }

            var lines = proxyText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var proxies = new List<ProxyImportItemDto>();
            var errors = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                var parseResult = ParseProxyString(trimmedLine, defaultProxyType);
                if (parseResult.IsSuccess)
                {
                    proxies.Add(parseResult.Data!);
                }
                else
                {
                    errors.Add($"Line '{trimmedLine}': {parseResult.ErrorMessage}");
                }
            }

            if (errors.Count > 0)
            {
                return Result<IEnumerable<ProxyImportItemDto>>.Failure($"Parsing errors: {string.Join("; ", errors)}");
            }

            return Result<IEnumerable<ProxyImportItemDto>>.Success(proxies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing proxy text");
            return Result<IEnumerable<ProxyImportItemDto>>.Failure($"Failed to parse proxy text: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SetProxyActiveStatusAsync(Guid id, bool isActive)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<bool>.Failure("Proxy configuration not found");
            }

            proxy.IsActive = isActive;
            proxy.UpdatedAt = DateTimeOffset.UtcNow;

            _unitOfWork.ProxyConfigurations.Update(proxy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated proxy {ProxyId} active status to {IsActive}", id, isActive);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating proxy active status {ProxyId}", id);
            return Result<bool>.Failure("Failed to update proxy active status");
        }
    }

    public async Task<Result<bool>> BulkSetProxyActiveStatusAsync(IEnumerable<Guid> ids, bool isActive)
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetByIdsAsync(ids);
            foreach (var proxy in proxies)
            {
                proxy.IsActive = isActive;
                proxy.UpdatedAt = DateTimeOffset.UtcNow;
            }

            _unitOfWork.ProxyConfigurations.UpdateRange(proxies);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bulk updated {Count} proxies active status to {IsActive}", proxies.Count(), isActive);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating proxy active status");
            return Result<bool>.Failure("Failed to bulk update proxy active status");
        }
    }

    public async Task<Result<IEnumerable<ProxyConfigurationDto>>> GetProxiesForHealthCheckAsync(TimeSpan maxAge)
    {
        try
        {
            var proxies = await _unitOfWork.ProxyConfigurations.GetProxiesForHealthCheckAsync(maxAge);
            var proxyDtos = proxies.Select(MapToDto).ToList();
            return Result<IEnumerable<ProxyConfigurationDto>>.Success(proxyDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving proxies for health check");
            return Result<IEnumerable<ProxyConfigurationDto>>.Failure("Failed to retrieve proxies for health check");
        }
    }

    public async Task<Result<bool>> UpdateProxyHealthAsync(Guid id, bool isHealthy, string? errorMessage = null, string? errorCode = null)
    {
        try
        {
            var proxy = await _unitOfWork.ProxyConfigurations.GetByIdAsync(id);
            if (proxy == null)
            {
                return Result<bool>.Failure("Proxy configuration not found");
            }

            proxy.UpdateHealthStatus(isHealthy, errorMessage, errorCode);
            _unitOfWork.ProxyConfigurations.Update(proxy);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating proxy health status {ProxyId}", id);
            return Result<bool>.Failure("Failed to update proxy health status");
        }
    }

    public async Task<Result<bool>> UpdateProxyUsageAsync(ProxyUsageUpdateDto usageDto)
    {
        try
        {
            await _unitOfWork.ProxyConfigurations.UpdateProxyStatsAsync(
                usageDto.ProxyConfigurationId,
                usageDto.Success,
                usageDto.ErrorMessage,
                usageDto.ErrorCode);

            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating proxy usage {ProxyId}", usageDto.ProxyConfigurationId);
            return Result<bool>.Failure("Failed to update proxy usage");
        }
    }

    public async Task<Result<BulkProxyImportValidationDto>> ValidateProxyImportAsync(BulkProxyImportDto importDto)
    {
        try
        {
            var validation = new BulkProxyImportValidationDto
            {
                TotalProxies = importDto.Proxies.Count
            };

            foreach (var proxyItem in importDto.Proxies)
            {
                // Validate proxy format
                if (string.IsNullOrWhiteSpace(proxyItem.Host) || proxyItem.Port <= 0 || proxyItem.Port > 65535)
                {
                    proxyItem.IsValid = false;
                    proxyItem.ValidationErrors.Add("Invalid host or port");
                    validation.InvalidProxies++;
                    continue;
                }

                // Check if proxy already exists
                var existingProxy = await _unitOfWork.ProxyConfigurations.GetByHostAndPortAsync(proxyItem.Host, proxyItem.Port);
                if (existingProxy != null)
                {
                    proxyItem.AlreadyExists = true;
                    validation.DuplicateProxies++;
                    if (!importDto.OverwriteExisting)
                    {
                        proxyItem.IsValid = false;
                        proxyItem.ValidationErrors.Add("Proxy already exists");
                        validation.InvalidProxies++;
                        continue;
                    }
                }

                validation.ValidProxies++;
                validation.ProxiesToImport.Add(proxyItem);
            }

            return Result<BulkProxyImportValidationDto>.Success(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating proxy import");
            return Result<BulkProxyImportValidationDto>.Failure("Failed to validate proxy import");
        }
    }

    public async Task<Result<BulkProxyImportResultDto>> BulkImportProxiesAsync(BulkProxyImportDto importDto)
    {
        try
        {
            var result = new BulkProxyImportResultDto
            {
                TotalProcessed = importDto.Proxies.Count
            };

            // Validate first
            var validationResult = await ValidateProxyImportAsync(importDto);
            if (!validationResult.IsSuccess)
            {
                return Result<BulkProxyImportResultDto>.Failure(validationResult.ErrorMessage!);
            }

            var validation = validationResult.Data!;

            foreach (var proxyItem in validation.ProxiesToImport.Where(p => p.IsValid))
            {
                try
                {
                    ProxyConfiguration? proxy = null;

                    if (proxyItem.AlreadyExists && importDto.OverwriteExisting)
                    {
                        // Update existing proxy
                        proxy = await _unitOfWork.ProxyConfigurations.GetByHostAndPortAsync(proxyItem.Host, proxyItem.Port);
                        if (proxy != null)
                        {
                            proxy.ProxyType = proxyItem.ProxyType.ToUpper();
                            proxy.Username = proxyItem.Username;
                            proxy.Password = proxyItem.Password;
                            proxy.Description = proxyItem.Description;
                            proxy.TimeoutSeconds = proxyItem.TimeoutSeconds;
                            proxy.MaxRetries = proxyItem.MaxRetries;
                            proxy.UpdatedAt = DateTimeOffset.UtcNow;
                            _unitOfWork.ProxyConfigurations.Update(proxy);
                        }
                    }
                    else
                    {
                        // Create new proxy
                        proxy = new ProxyConfiguration
                        {
                            ProxyConfigurationId = Guid.NewGuid(),
                            Host = proxyItem.Host,
                            Port = proxyItem.Port,
                            ProxyType = proxyItem.ProxyType.ToUpper(),
                            Username = proxyItem.Username,
                            Password = proxyItem.Password,
                            Description = proxyItem.Description,
                            TimeoutSeconds = proxyItem.TimeoutSeconds,
                            MaxRetries = proxyItem.MaxRetries,
                            IsActive = true,
                            CreatedAt = DateTimeOffset.UtcNow,
                            UpdatedAt = DateTimeOffset.UtcNow
                        };
                        await _unitOfWork.ProxyConfigurations.AddAsync(proxy);
                    }

                    if (proxy != null)
                    {
                        result.ImportedProxies.Add(MapToDto(proxy));
                        result.SuccessfulImports++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add($"Failed to import {proxyItem.Host}:{proxyItem.Port}: {ex.Message}");
                    _logger.LogError(ex, "Error importing proxy {Host}:{Port}", proxyItem.Host, proxyItem.Port);
                }
            }

            result.SkippedDuplicates = validation.DuplicateProxies - (importDto.OverwriteExisting ? 0 : validation.DuplicateProxies);

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bulk imported {SuccessCount} proxies, {FailCount} failed, {SkipCount} skipped",
                result.SuccessfulImports, result.FailedImports, result.SkippedDuplicates);

            return Result<BulkProxyImportResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk importing proxies");
            return Result<BulkProxyImportResultDto>.Failure("Failed to bulk import proxies");
        }
    }

    private async Task<ProxyTestResultDto> TestProxyConnection(ProxyConfiguration proxy, string testUrl, int timeoutSeconds)
    {
        var result = new ProxyTestResultDto
        {
            ProxyConfigurationId = proxy.ProxyConfigurationId,
            Host = proxy.Host,
            Port = proxy.Port,
            ProxyType = proxy.ProxyType,
            TestedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var startTime = DateTimeOffset.UtcNow;

            // Create HttpClientHandler with proxy configuration
            var handler = new HttpClientHandler();

            // Configure proxy based on type
            if (proxy.ProxyType.Equals("SOCKS5", StringComparison.OrdinalIgnoreCase))
            {
                // .NET 6+ native SOCKS5 support
                var socksProxy = new WebProxy($"socks5://{proxy.Host}:{proxy.Port}");

                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    socksProxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
                }

                handler.Proxy = socksProxy;
                handler.UseProxy = true;
            }
            else if (proxy.ProxyType.Equals("SOCKS4", StringComparison.OrdinalIgnoreCase))
            {
                // .NET 6+ native SOCKS4 support
                var socksProxy = new WebProxy($"socks4://{proxy.Host}:{proxy.Port}");

                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    socksProxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
                }

                handler.Proxy = socksProxy;
                handler.UseProxy = true;
            }
            else
            {
                // For HTTP/HTTPS proxies, create the proxy URI correctly
                // Use the full URI format to avoid trailing slash issues
                var proxyUri = new Uri($"http://{proxy.Host}:{proxy.Port}");
                var webProxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxy.Username))
                {
                    webProxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
                }

                handler.Proxy = webProxy;
                handler.UseProxy = true;
            }

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Add headers to make the request look more like a real browser
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var response = await client.GetAsync(testUrl);
            var endTime = DateTimeOffset.UtcNow;

            result.IsHealthy = response.IsSuccessStatusCode;
            result.ResponseTimeMs = (int)(endTime - startTime).TotalMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                result.ErrorCode = "HTTP_ERROR";
            }
            else
            {
                // Try to read a bit of the response to ensure the proxy is working correctly
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    result.ErrorMessage = "Proxy returned empty response";
                    result.ErrorCode = "EMPTY_RESPONSE";
                    result.IsHealthy = false;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            result.IsHealthy = false;
            result.ErrorMessage = $"HTTP request failed: {ex.Message}";
            result.ErrorCode = "HTTP_REQUEST_ERROR";
            result.ResponseTimeMs = timeoutSeconds * 1000;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            result.IsHealthy = false;
            result.ErrorMessage = "Request timed out";
            result.ErrorCode = "TIMEOUT";
            result.ResponseTimeMs = timeoutSeconds * 1000;
        }
        catch (TaskCanceledException)
        {
            result.IsHealthy = false;
            result.ErrorMessage = "Request was cancelled or timed out";
            result.ErrorCode = "CANCELLED";
            result.ResponseTimeMs = timeoutSeconds * 1000;
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.ErrorMessage = ex.Message;
            result.ErrorCode = ex.GetType().Name;
            result.ResponseTimeMs = timeoutSeconds * 1000;
        }

        return result;
    }

    /// <summary>
    /// Test proxy with multiple fallback URLs for better reliability
    /// </summary>
    private async Task<ProxyTestResultDto> TestProxyWithFallbacks(ProxyConfiguration proxy, int timeoutSeconds)
    {
        var testUrls = new[]
        {
            "http://httpbin.org/ip",
            "http://icanhazip.com",
            "http://ipinfo.io/ip",
            "http://api.ipify.org"
        };

        ProxyTestResultDto? lastResult = null;

        foreach (var testUrl in testUrls)
        {
            try
            {
                var result = await TestProxyConnection(proxy, testUrl, timeoutSeconds);
                if (result.IsHealthy)
                {
                    return result; // Return first successful test
                }
                lastResult = result;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to test proxy {ProxyHost}:{ProxyPort} with URL {TestUrl}",
                    proxy.Host, proxy.Port, testUrl);

                lastResult = new ProxyTestResultDto
                {
                    ProxyConfigurationId = proxy.ProxyConfigurationId,
                    Host = proxy.Host,
                    Port = proxy.Port,
                    ProxyType = proxy.ProxyType,
                    TestedAt = DateTimeOffset.UtcNow,
                    IsHealthy = false,
                    ErrorMessage = ex.Message,
                    ErrorCode = ex.GetType().Name,
                    ResponseTimeMs = timeoutSeconds * 1000
                };
            }
        }

        // If all tests failed, return the last result
        return lastResult ?? new ProxyTestResultDto
        {
            ProxyConfigurationId = proxy.ProxyConfigurationId,
            Host = proxy.Host,
            Port = proxy.Port,
            ProxyType = proxy.ProxyType,
            TestedAt = DateTimeOffset.UtcNow,
            IsHealthy = false,
            ErrorMessage = "All test URLs failed",
            ErrorCode = "ALL_TESTS_FAILED",
            ResponseTimeMs = timeoutSeconds * 1000
        };
    }

    private static ProxyConfigurationDto MapToDto(ProxyConfiguration proxy)
    {
        return new ProxyConfigurationDto
        {
            ProxyConfigurationId = proxy.ProxyConfigurationId,
            Host = proxy.Host,
            Port = proxy.Port,
            ProxyType = proxy.ProxyType,
            Username = proxy.Username,
            HasPassword = !string.IsNullOrEmpty(proxy.Password),
            Description = proxy.Description,
            IsActive = proxy.IsActive,
            IsHealthy = proxy.IsHealthy,
            LastTestedAt = proxy.LastTestedAt,
            LastUsedAt = proxy.LastUsedAt,
            SuccessRate = proxy.SuccessRate,
            TotalRequests = proxy.TotalRequests,
            SuccessfulRequests = proxy.SuccessfulRequests,
            FailedRequests = proxy.FailedRequests,
            ConsecutiveFailures = proxy.ConsecutiveFailures,
            TimeoutSeconds = proxy.TimeoutSeconds,
            MaxRetries = proxy.MaxRetries,
            LastErrorMessage = proxy.LastErrorMessage,
            LastErrorCode = proxy.LastErrorCode,
            CreatedAt = proxy.CreatedAt,
            UpdatedAt = proxy.UpdatedAt,
            DisplayName = proxy.DisplayName,
            RequiresAuthentication = proxy.RequiresAuthentication,
            IsReliable = proxy.IsReliable,
            StatusDescription = proxy.StatusDescription
        };
    }
}
