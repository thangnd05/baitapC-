using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password);

public sealed record RefreshRequest(
    [Required, MaxLength(128)] string RefreshToken);

public sealed record LogoutRequest(
    [Required, MaxLength(128)] string RefreshToken);
