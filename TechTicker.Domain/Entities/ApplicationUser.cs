using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}".Trim();
}
