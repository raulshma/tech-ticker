using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechTicker.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between ASP.NET Core Identity roles and permissions
/// </summary>
public class RolePermission
{
    [Key]
    public Guid RolePermissionId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid PermissionId { get; set; }

    [Required]
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RoleId))]
    public IdentityRole<Guid> Role { get; set; } = null!;

    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = null!;
}
