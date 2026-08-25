using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record CreateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(2), MaxLength(128)] string DisplayName,
    [Required, MinLength(8), MaxLength(128)] string Password);

public sealed record UpdateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(2), MaxLength(128)] string DisplayName,
    bool IsActive);
