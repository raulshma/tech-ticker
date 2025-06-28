using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechTicker.Application.Configuration;
using TechTicker.Application.Messages;
using TechTicker.Application.Services;
using TechTicker.Application.Services.Interfaces;
using TechTicker.DataAccess.Repositories.Interfaces;
using TechTicker.Domain.Entities;
using TechTicker.Shared.Constants;

namespace TechTicker.Application.Tests.Services;

public class AlertProcessingServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessagePublisher> _mockMessagePublisher;
    private readonly Mock<IAlertPerformanceMonitoringService> _mockPerformanceMonitoring;
    private readonly Mock<ILogger<AlertProcessingService>> _mockLogger;
    private readonly MessagingConfiguration _messagingConfig;
    private readonly AlertProcessingService _alertProcessingService;

    public AlertProcessingServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMessagePublisher = new Mock<IMessagePublisher>();
        _mockPerformanceMonitoring = new Mock<IAlertPerformanceMonitoringService>();
        _mockLogger = new Mock<ILogger<AlertProcessingService>>();
        
        _messagingConfig = new MessagingConfiguration
        {
            AlertsExchange = "alerts",
            AlertTriggeredRoutingKey = "alert.triggered"
        };

        var options = Options.Create(_messagingConfig);
        _alertProcessingService = new AlertProcessingService(
            _mockUnitOfWork.Object,
            _mockMessagePublisher.Object,
            _mockPerformanceMonitoring.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithNoPriceAlerts_ShouldNotTriggerAnyAlerts()
    {
        // Arrange
        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            Price = 299.99m
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule>());

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<AlertTriggeredEvent>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithPriceBelowAlert_ShouldTriggerAlert()
    {
        // Arrange
        var fixedProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixedAlertRuleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var fixedUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = fixedProductId,
            SellerName = "Amazon",
            Price = 250.00m,
            StockStatus = StockStatus.InStock,
            SourceUrl = "https://amazon.com/product/123"
        };

        var alertRule = new AlertRule
        {
            AlertRuleId = fixedAlertRuleId,
            UserId = fixedUserId,
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 300.00m,
            NotificationFrequencyMinutes = 1,
            LastNotifiedAt = null,
            IsActive = true,
            AlertType = "RECURRING",
            User = new ApplicationUser { Email = "test@example.com", FirstName = "John", Id = fixedUserId },
            Product = new Product { Name = "Test Product", ProductId = fixedProductId }
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule> { alertRule });

        _mockUnitOfWork.Setup(x => x.PriceHistory.GetLastPriceAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync((TechTicker.Domain.Entities.PriceHistory?)null);

        _mockUnitOfWork.Setup(x => x.AlertHistories.AddAsync(It.IsAny<AlertHistory>()))
            .ReturnsAsync((AlertHistory ah) => ah);
        _mockUnitOfWork.Setup(x => x.AlertRules.Update(It.IsAny<AlertRule>()));
        _mockMessagePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<AlertTriggeredEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<object, string, string>((evt, exch, key) =>
            {
                // Diagnostic output for debugging
                var e = evt as AlertTriggeredEvent;
                Console.WriteLine($"PublishAsync called with AlertRuleId={e?.AlertRuleId}, Exchange={exch}, RoutingKey={key}");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.Is<AlertTriggeredEvent>(e => e.AlertRuleId == alertRule.AlertRuleId),
            _messagingConfig.AlertsExchange,
            _messagingConfig.AlertTriggeredRoutingKey), Times.AtLeastOnce);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithPriceAboveThreshold_ShouldNotTriggerAlert()
    {
        // Arrange
        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            Price = 350.00m
        };

        var alertRule = new AlertRule
        {
            AlertRuleId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 300.00m
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule> { alertRule });

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<AlertTriggeredEvent>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithPercentageDropAlert_ShouldTriggerWhenDropExceeded()
    {
        // Arrange
        var fixedProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixedAlertRuleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var fixedUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = fixedProductId,
            SellerName = "Amazon",
            Price = 200.00m,
            StockStatus = StockStatus.InStock,
            SourceUrl = "https://amazon.com/product/123"
        };

        var alertRule = new AlertRule
        {
            AlertRuleId = fixedAlertRuleId,
            UserId = fixedUserId,
            ConditionType = "PERCENT_DROP_FROM_LAST",
            PercentageValue = 15.0m,
            NotificationFrequencyMinutes = 1,
            LastNotifiedAt = null,
            IsActive = true,
            AlertType = "RECURRING",
            User = new ApplicationUser { Email = "test@example.com", FirstName = "John", Id = fixedUserId },
            Product = new Product { Name = "Test Product", ProductId = fixedProductId }
        };

        var lastPrice = new PriceHistory
        {
            Price = 250.00m // 20% drop from 250 to 200
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule> { alertRule });

        _mockUnitOfWork.Setup(x => x.PriceHistory.GetLastPriceAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(lastPrice);

        _mockUnitOfWork.Setup(x => x.PriceHistory.GetLastStockStatusAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync((string?)null);

        _mockUnitOfWork.Setup(x => x.AlertHistories.AddAsync(It.IsAny<AlertHistory>()))
            .ReturnsAsync((AlertHistory ah) => ah);
        _mockUnitOfWork.Setup(x => x.AlertRules.Update(It.IsAny<AlertRule>()));
        _mockMessagePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<AlertTriggeredEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<object, string, string>((evt, exch, key) =>
            {
                var e = evt as AlertTriggeredEvent;
                Console.WriteLine($"PublishAsync called with AlertRuleId={e?.AlertRuleId}, Exchange={exch}, RoutingKey={key}");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.Is<AlertTriggeredEvent>(e => e.AlertRuleId == alertRule.AlertRuleId),
            _messagingConfig.AlertsExchange,
            _messagingConfig.AlertTriggeredRoutingKey), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithBackInStockAlert_ShouldTriggerWhenInStock()
    {
        // Arrange
        var fixedProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fixedAlertRuleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var fixedUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = fixedProductId,
            SellerName = "Amazon",
            Price = 299.99m,
            StockStatus = StockStatus.InStock,
            SourceUrl = "https://amazon.com/product/123"
        };

        var alertRule = new AlertRule
        {
            AlertRuleId = fixedAlertRuleId,
            UserId = fixedUserId,
            ConditionType = "BACK_IN_STOCK",
            IsActive = true,
            AlertType = "RECURRING",
            User = new ApplicationUser { Email = "test@example.com", FirstName = "John", Id = fixedUserId },
            Product = new Product { Name = "Test Product", ProductId = fixedProductId }
        };

        _mockUnitOfWork.Setup(x => x.PriceHistory.GetLastStockStatusAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(StockStatus.OutOfStock);

        _mockUnitOfWork.Setup(x => x.PriceHistory.GetLastPriceAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync((TechTicker.Domain.Entities.PriceHistory?)null);

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule> { alertRule });

        _mockUnitOfWork.Setup(x => x.AlertHistories.AddAsync(It.IsAny<AlertHistory>()))
            .ReturnsAsync((AlertHistory ah) => ah);
        _mockUnitOfWork.Setup(x => x.AlertRules.Update(It.IsAny<AlertRule>()));
        _mockMessagePublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<AlertTriggeredEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<object, string, string>((evt, exch, key) =>
            {
                var e = evt as AlertTriggeredEvent;
                Console.WriteLine($"PublishAsync called with AlertRuleId={e?.AlertRuleId}, Exchange={exch}, RoutingKey={key}");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.Is<AlertTriggeredEvent>(e => e.AlertRuleId == alertRule.AlertRuleId),
            _messagingConfig.AlertsExchange,
            _messagingConfig.AlertTriggeredRoutingKey), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithRecentNotification_ShouldSkipAlert()
    {
        // Arrange
        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            Price = 250.00m
        };

        var alertRule = new AlertRule
        {
            AlertRuleId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 300.00m,
            NotificationFrequencyMinutes = 60,
            LastNotifiedAt = DateTimeOffset.UtcNow.AddMinutes(-30) // 30 minutes ago, within frequency limit
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            pricePoint.CanonicalProductId, pricePoint.SellerName))
            .ReturnsAsync(new List<AlertRule> { alertRule });

        // Act
        await _alertProcessingService.ProcessPricePointAsync(pricePoint);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            It.IsAny<AlertTriggeredEvent>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPricePointAsync_WithException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var pricePoint = new PricePointRecordedEvent
        {
            CanonicalProductId = Guid.NewGuid(),
            SellerName = "Amazon",
            Price = 299.99m
        };

        _mockUnitOfWork.Setup(x => x.AlertRules.GetActiveAlertsForProductAsync(
            It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _alertProcessingService.ProcessPricePointAsync(pricePoint));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing price point")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithValidAlert_ShouldPublishMessage()
    {
        // Arrange
        var alertEvent = new AlertTriggeredEvent
        {
            AlertRuleId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid(),
            UserEmail = "test@example.com",
            ProductName = "Test Product"
        };

        // Act
        await _alertProcessingService.SendAlertNotificationAsync(alertEvent);

        // Assert
        _mockMessagePublisher.Verify(x => x.PublishAsync(
            alertEvent,
            _messagingConfig.AlertsExchange,
            _messagingConfig.AlertTriggeredRoutingKey), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithException_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var alertEvent = new AlertTriggeredEvent
        {
            AlertRuleId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CanonicalProductId = Guid.NewGuid()
        };

        _mockMessagePublisher.Setup(x => x.PublishAsync(
            It.IsAny<AlertTriggeredEvent>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ThrowsAsync(new Exception("Message queue error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _alertProcessingService.SendAlertNotificationAsync(alertEvent));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error sending alert notification")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
