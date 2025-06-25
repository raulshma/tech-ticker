using System.ComponentModel.DataAnnotations;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Represents a permission in the RBAC system
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
    [MaxLength(50)]
    public string Category { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    [Required]
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
