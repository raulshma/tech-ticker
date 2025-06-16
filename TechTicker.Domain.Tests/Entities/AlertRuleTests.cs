using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class AlertRuleTests
{
    [Fact]
    public void AlertRule_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var alertRule = new AlertRule();

        // Assert
        alertRule.IsActive.Should().BeTrue();
        alertRule.NotificationFrequencyMinutes.Should().Be(1440); // 24 hours
    }

    [Theory]
    [InlineData("PRICE_BELOW", "100.50", "Price below $100.50")]
    [InlineData("PERCENT_DROP_FROM_LAST", "15.5", "15.5% drop from last price")]
    [InlineData("BACK_IN_STOCK", null, "Back in stock")]
    [InlineData("UNKNOWN_TYPE", null, "Unknown condition")]
    public void RuleDescription_VariousConditionTypes_ReturnsCorrectDescription(
        string conditionType, string? valueStr, string expectedDescription)
    {
        // Arrange
        var value = decimal.TryParse(valueStr, out var parsedValue) ? parsedValue : (decimal?)null;
        var alertRule = new AlertRule
        {
            ConditionType = conditionType,
            ThresholdValue = conditionType == "PRICE_BELOW" ? value : null,
            PercentageValue = conditionType == "PERCENT_DROP_FROM_LAST" ? value : null
        };

        // Act
        var description = alertRule.RuleDescription;

        // Assert
        description.Should().Be(expectedDescription);
    }

    [Fact]
    public void AlertRule_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var alertRule = new AlertRule
        {
            UserId = userId,
            CanonicalProductId = productId,
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 99.99m,
            SpecificSellerName = "Amazon",
            NotificationFrequencyMinutes = 720, // 12 hours
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        alertRule.UserId.Should().Be(userId);
        alertRule.CanonicalProductId.Should().Be(productId);
        alertRule.ConditionType.Should().Be("PRICE_BELOW");
        alertRule.ThresholdValue.Should().Be(99.99m);
        alertRule.SpecificSellerName.Should().Be("Amazon");
        alertRule.NotificationFrequencyMinutes.Should().Be(720);
        alertRule.CreatedAt.Should().Be(createdAt);
        alertRule.UpdatedAt.Should().Be(createdAt);
        alertRule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AlertRule_PriceBelowCondition_ShouldHaveThresholdValue()
    {
        // Arrange & Act
        var alertRule = new AlertRule
        {
            ConditionType = "PRICE_BELOW",
            ThresholdValue = 150.00m
        };

        // Assert
        alertRule.ConditionType.Should().Be("PRICE_BELOW");
        alertRule.ThresholdValue.Should().Be(150.00m);
        alertRule.PercentageValue.Should().BeNull();
    }

    [Fact]
    public void AlertRule_PercentageDropCondition_ShouldHavePercentageValue()
    {
        // Arrange & Act
        var alertRule = new AlertRule
        {
            ConditionType = "PERCENT_DROP_FROM_LAST",
            PercentageValue = 25.5m
        };

        // Assert
        alertRule.ConditionType.Should().Be("PERCENT_DROP_FROM_LAST");
        alertRule.PercentageValue.Should().Be(25.5m);
        alertRule.ThresholdValue.Should().BeNull();
    }

    [Fact]
    public void AlertRule_BackInStockCondition_ShouldNotRequireValues()
    {
        // Arrange & Act
        var alertRule = new AlertRule
        {
            ConditionType = "BACK_IN_STOCK"
        };

        // Assert
        alertRule.ConditionType.Should().Be("BACK_IN_STOCK");
        alertRule.ThresholdValue.Should().BeNull();
        alertRule.PercentageValue.Should().BeNull();
    }

    [Fact]
    public void AlertRule_WithLastNotifiedAt_ShouldTrackNotificationTime()
    {
        // Arrange
        var notificationTime = DateTimeOffset.UtcNow.AddHours(-2);
        var alertRule = new AlertRule();

        // Act
        alertRule.LastNotifiedAt = notificationTime;

        // Assert
        alertRule.LastNotifiedAt.Should().Be(notificationTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AlertRule_WithInvalidConditionType_ShouldReturnUnknownCondition(string conditionType)
    {
        // Arrange
        var alertRule = new AlertRule { ConditionType = conditionType };

        // Act
        var description = alertRule.RuleDescription;

        // Assert
        description.Should().Be("Unknown condition");
    }

    [Fact]
    public void AlertRule_WithGuids_ShouldSetIdsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var alertRule = new AlertRule
        {
            UserId = userId,
            CanonicalProductId = productId
        };

        // Assert
        alertRule.UserId.Should().Be(userId);
        alertRule.CanonicalProductId.Should().Be(productId);
    }
}