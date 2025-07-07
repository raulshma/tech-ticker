using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TechTicker.Application.Configuration;
using TechTicker.Application.DTOs;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Utilities;
using Xunit;

namespace TechTicker.Application.Tests.Services;

public class AlertTestingServiceTests : IDisposable
{
    private readonly TechTickerDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AlertTestingService>> _loggerMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly Mock<IOptions<MessagingConfiguration>> _messagingConfigMock;
    private readonly AlertTestingService _service;

    public AlertTestingServiceTests()
    {
        var options = new DbContextOptionsBuilder<TechTickerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TechTickerDbContext(options);
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AlertTestingService>>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _messagingConfigMock = new Mock<IOptions<MessagingConfiguration>>();
        
        // Setup messaging configuration with default values
        _messagingConfigMock.Setup(x => x.Value).Returns(new MessagingConfiguration());

        _service = new AlertTestingService(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _messagePublisherMock.Object,
            _messagingConfigMock.Object);
    }

    [Fact]
    public async Task TestAlertRuleAsync_ShouldReturnValid_WhenRuleIsCorrect()
    {
        // Arrange
        var alertRuleId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var alertRule = new AlertRule
        {
            AlertRuleId = alertRuleId,
            CanonicalProductId = productId,
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 100.00m,

            IsActive = true
        };

        var testPricePoint = new TestPricePointDto
        {
            Price = 95.00m,
            SellerName = "Test Seller",
            StockStatus = "IN_STOCK",
            SourceUrl = "https://test.com",
            Timestamp = DateTimeOffset.UtcNow
        };

        _unitOfWorkMock.Setup(u => u.AlertRules.GetByIdAsync(alertRuleId))
            .ReturnsAsync(alertRule);

        // Act
        var result = await _service.TestAlertRuleAsync(alertRuleId, testPricePoint);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(alertRuleId, result.Data.AlertRuleId);
        Assert.True(result.Data.WouldTrigger);
    }

    [Fact]
    public async Task TestAlertRuleAsync_ShouldReturnFailure_WhenAlertRuleNotFound()
    {
        // Arrange
        var alertRuleId = Guid.NewGuid();
        var testPricePoint = new TestPricePointDto
        {
            Price = 95.00m,
            SellerName = "Test Seller"
        };

        _unitOfWorkMock.Setup(u => u.AlertRules.GetByIdAsync(alertRuleId))
            .ReturnsAsync((AlertRule?)null);

        // Act
        var result = await _service.TestAlertRuleAsync(alertRuleId, testPricePoint);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ALERT_NOT_FOUND", result.ErrorCode);
    }





















    public void Dispose()
    {
        _context.Dispose();
    }
} 