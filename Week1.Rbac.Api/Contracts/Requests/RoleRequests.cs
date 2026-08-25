using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record CreateRoleRequest(
    [Required, MinLength(2), MaxLength(64)] string Name,
    [MaxLength(256)] string? Description);

public sealed record UpdateRoleRequest(
    [Required, MinLength(2), MaxLength(64)] string Name,
    [MaxLength(256)] string? Description);
