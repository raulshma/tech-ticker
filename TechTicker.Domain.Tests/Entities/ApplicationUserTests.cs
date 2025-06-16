using TechTicker.Domain.Entities;

namespace TechTicker.Domain.Tests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
        user.AlertRules.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ApplicationUser_WithValidData_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var user = new ApplicationUser
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            UserName = "johndoe@example.com",
            Email = "johndoe@example.com",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Assert
        user.Id.Should().Be(userId);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.UserName.Should().Be("johndoe@example.com");
        user.Email.Should().Be("johndoe@example.com");
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(createdAt);
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("John", "Doe", "John Doe")]
    [InlineData("Alice", "Smith", "Alice Smith")]
    [InlineData("Bob", "", "Bob")]
    [InlineData("", "Johnson", "Johnson")]
    [InlineData("", "", "")]
    [InlineData(null, "Brown", "Brown")]
    [InlineData("Charlie", null, "Charlie")]
    [InlineData(null, null, "")]
    public void FullName_WithVariousNameCombinations_ShouldReturnCorrectFullName(
        string? firstName, string? lastName, string expectedFullName)
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be(expectedFullName);
    }

    [Fact]
    public void ApplicationUser_AlertRulesCollection_ShouldAllowAddingAlertRules()
    {
        // Arrange
        var user = new ApplicationUser();
        var alertRule1 = new AlertRule { AlertRuleId = Guid.NewGuid(), UserId = user.Id };
        var alertRule2 = new AlertRule { AlertRuleId = Guid.NewGuid(), UserId = user.Id };

        // Act
        user.AlertRules.Add(alertRule1);
        user.AlertRules.Add(alertRule2);

        // Assert
        user.AlertRules.Should().HaveCount(2);
        user.AlertRules.Should().Contain(alertRule1);
        user.AlertRules.Should().Contain(alertRule2);
    }

    [Fact]
    public void ApplicationUser_IsActive_ShouldDefaultToTrue()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_CanBeDeactivated_ShouldAllowSettingInactive()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ApplicationUser_InheritsFromIdentityUser_ShouldHaveIdentityProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+1234567890",
            PhoneNumberConfirmed = true
        };

        // Assert
        user.UserName.Should().Be("testuser");
        user.Email.Should().Be("test@example.com");
        user.EmailConfirmed.Should().BeTrue();
        user.PhoneNumber.Should().Be("+1234567890");
        user.PhoneNumberConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_AlertRulesCollection_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.AlertRules.Should().NotBeNull();
        user.AlertRules.Should().BeOfType<List<AlertRule>>();
        user.AlertRules.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationUser_CreatedAndUpdatedAt_ShouldTrackTimestamps()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var user = new ApplicationUser
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(updatedAt);
        user.UpdatedAt.Should().BeAfter(user.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FullName_WithWhitespaceNames_ShouldTrimCorrectly(string whitespace)
    {
        // Arrange
        var user = new ApplicationUser
        {
            FirstName = whitespace,
            LastName = whitespace
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("");
    }

    [Fact]
    public void ApplicationUser_WithLongNames_ShouldHandleMaxLength()
    {
        // Arrange
        var longName = new string('A', 100); // Max length is 100

        // Act
        var user = new ApplicationUser
        {
            FirstName = longName,
            LastName = longName
        };

        // Assert
        user.FirstName.Should().Be(longName);
        user.LastName.Should().Be(longName);
        user.FirstName.Length.Should().Be(100);
        user.LastName.Length.Should().Be(100);
    }

    [Fact]
    public void ApplicationUser_WithSetId_ShouldMaintainId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = new ApplicationUser { Id = userId };

        // Assert
        user.Id.Should().Be(userId);
    }
}