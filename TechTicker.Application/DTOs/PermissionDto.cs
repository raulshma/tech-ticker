using System.ComponentModel.DataAnnotations;

namespace TechTicker.Application.DTOs;

/// <summary>
/// DTO for Permission responses
/// </summary>
public class PermissionDto
{
    public Guid PermissionId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new permission
/// </summary>
public class CreatePermissionDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = null!;
}

/// <summary>
/// DTO for updating an existing permission
/// </summary>
public class UpdatePermissionDto
{
    [MaxLength(255)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }
}

/// <summary>
/// DTO for role-permission assignment
/// </summary>
public class RolePermissionDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = null!;
    public DateTimeOffset AssignedAt { get; set; }
}
