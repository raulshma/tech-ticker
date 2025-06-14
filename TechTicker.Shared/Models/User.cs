using System.ComponentModel.DataAnnotations;

namespace TechTicker.Shared.Models
{
    /// <summary>
    /// Represents a user in the system
    /// </summary>
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        public DateTimeOffset? EmailConfirmedAt { get; set; }

        public DateTimeOffset? LastLoginAt { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
