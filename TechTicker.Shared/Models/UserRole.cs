using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Shared.Models
{
    /// <summary>
    /// Represents the many-to-many relationship between users and roles
    /// </summary>
    public class UserRole
    {
        [Key]
        public Guid UserRoleId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;
    }
}
