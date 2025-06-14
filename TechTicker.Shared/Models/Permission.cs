using System.ComponentModel.DataAnnotations;

namespace TechTicker.Shared.Models
{
    /// <summary>
    /// Represents a permission in the system
    /// </summary>
    public class Permission
    {
        [Key]
        public Guid PermissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
