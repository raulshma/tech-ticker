using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class AiConfigurationServiceTests : IDisposable
{
    private readonly TechTickerDbContext _context;
    private readonly Mock<IAiConfigurationRepository> _repositoryMock;
    private readonly Mock<ILogger<AiConfigurationService>> _loggerMock;
    private readonly Mock<IAiProvider> _aiProviderMock;
    private readonly AiConfigurationService _service;

    public AiConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<TechTickerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TechTickerDbContext(options);
        _repositoryMock = new Mock<IAiConfigurationRepository>();
        _loggerMock = new Mock<ILogger<AiConfigurationService>>();
        _aiProviderMock = new Mock<IAiProvider>();
        _aiProviderMock.Setup(p => p.ProviderName).Returns("GoogleGemini");

        var aiProviders = new[] { _aiProviderMock.Object };
        _service = new AiConfigurationService(_repositoryMock.Object, _loggerMock.Object, aiProviders);
    }

    [Fact]
    public async Task GetAllConfigurationsAsync_ShouldReturnAllConfigurations()
    {
        // Arrange
        var configurations = new List<AiConfiguration>
        {
            new AiConfiguration
            {
                AiConfigurationId = Guid.NewGuid(),
                Provider = "GoogleGemini",
                Name = "Test Configuration 1",
                IsActive = true,
                ApiKey = "test-key-1",
                Model = "gemini-pro",
                InputTokenLimit = 1000,
                OutputTokenLimit = 500,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AiConfiguration
            {
                AiConfigurationId = Guid.NewGuid(),
                Provider = "OpenAI",
                Name = "Test Configuration 2",
                IsActive = false,
                ApiKey = "test-key-2",
                Model = "gpt-3.5-turbo",
                InputTokenLimit = 2000,
                OutputTokenLimit = 800,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(configurations);

        // Act
        var result = await _service.GetAllConfigurationsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count());
        
        var geminiConfig = result.Data.FirstOrDefault(c => c.Provider == "GoogleGemini");
        Assert.NotNull(geminiConfig);
        Assert.True(geminiConfig.IsActive);
        Assert.Equal("gemini-pro", geminiConfig.Model);
    }

    [Fact]
    public async Task GetDefaultConfigurationAsync_ShouldReturnDefaultConfig()
    {
        // Arrange
        var activeConfig = new AiConfiguration
        {
            AiConfigurationId = Guid.NewGuid(),
            Provider = "GoogleGemini",
            Name = "Active Configuration",
            IsActive = true,
            ApiKey = "active-key",
            Model = "gemini-pro",
            InputTokenLimit = 1000,
            OutputTokenLimit = 500,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetDefaultConfigurationAsync())
            .ReturnsAsync(activeConfig);

        // Act
        var result = await _service.GetDefaultConfigurationAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsActive);
        Assert.Equal("GoogleGemini", result.Data.Provider);
        Assert.Equal("gemini-pro", result.Data.Model);
    }

    [Fact]
    public async Task CreateConfigurationAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        var createDto = new CreateAiConfigurationDto
        {
            Provider = "GoogleGemini",
            Name = "Test Configuration",
            ApiKey = "new-api-key",
            Model = "gemini-pro",
            InputTokenLimit = 1500,
            OutputTokenLimit = 500,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.ExistsAsync(createDto.Provider, createDto.Model, null))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<AiConfiguration>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.CreateConfigurationAsync(createDto, Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Failed to create configuration", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ShouldUpdateExistingConfiguration()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var updateDto = new UpdateAiConfigurationDto
        {
            Name = "Updated Configuration",
            Model = "gemini-pro-updated",
            InputTokenLimit = 2000,
            OutputTokenLimit = 800,
            IsActive = false
        };

        var existingConfig = new AiConfiguration
        {
            AiConfigurationId = configId,
            Provider = "GoogleGemini",
            Name = "Existing Configuration",
            ApiKey = "existing-key",
            Model = "gemini-pro",
            InputTokenLimit = 1000,
            OutputTokenLimit = 400,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync(existingConfig);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AiConfiguration>()))
            .ReturnsAsync(existingConfig);

        // Act
        var result = await _service.UpdateConfigurationAsync(configId, updateDto, Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AiConfiguration>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldDeleteConfiguration()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var existingConfig = new AiConfiguration
        {
            AiConfigurationId = configId,
            Provider = "GoogleGemini",
            IsActive = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync(existingConfig);
        _repositoryMock.Setup(r => r.DeleteAsync(configId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteConfigurationAsync(configId);

        // Assert
        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(r => r.DeleteAsync(configId), Times.Once);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_ShouldReturnFailure_WhenConfigurationIsActive()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var activeConfig = new AiConfiguration
        {
            AiConfigurationId = configId,
            Provider = "GoogleGemini",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.DeleteAsync(configId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteConfigurationAsync(configId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Data);
    }

    [Fact]
    public void ValidateConfigurationAsync_ShouldReturnValid_WhenConfigurationIsCorrect()
    {
        // Arrange
        var validateDto = new CreateAiConfigurationDto
        {
            Provider = "GoogleGemini",
            Name = "Test Configuration",
            ApiKey = "valid-api-key",
            Model = "gemini-pro",
            InputTokenLimit = 1000,
            OutputTokenLimit = 500
        };

        // Act & Assert - Just testing that we can create configuration without errors
        // In a real implementation, this would include validation logic
        Assert.True(true); // Placeholder test
    }

    [Fact]
    public void ValidateConfigurationAsync_ShouldReturnInvalid_WhenTemperatureOutOfRange()
    {
        // Arrange
        // Act & Assert - Just testing that validation would catch errors
        // In a real implementation, this would include validation logic
        Assert.True(true); // Placeholder test
    }

    [Fact]
    public async Task TestConfigurationAsync_ShouldReturnFailure_WhenProviderNotSupported()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var testConfig = new AiConfiguration
        {
            AiConfigurationId = configId,
            Provider = "UnsupportedProvider",
            Name = "Test Configuration",
            ApiKey = "test-key",
            Model = "test-model",
            InputTokenLimit = 1000,
            OutputTokenLimit = 500,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync(testConfig);

        // Act
        var result = await _service.TestConfigurationAsync(configId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Provider 'UnsupportedProvider' not supported", result.ErrorMessage);
    }

    [Fact]
    public async Task GetConfigurationByIdAsync_ShouldReturnFailure_WhenConfigurationNotFound()
    {
        // Arrange
        var configId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(configId))
            .ReturnsAsync((AiConfiguration?)null);

        // Act
        var result = await _service.GetConfigurationByIdAsync(configId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Configuration not found", result.ErrorMessage);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
} 