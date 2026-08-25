using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record CreatePermissionRequest(
    [Required, MinLength(3), MaxLength(64)] string Code,
    [MaxLength(256)] string? Description);

public sealed record UpdatePermissionRequest(
    [Required, MinLength(3), MaxLength(64)] string Code,
    [MaxLength(256)] string? Description);
