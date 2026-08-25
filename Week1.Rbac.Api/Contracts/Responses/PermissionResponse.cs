namespace Week1.Rbac.Api.Contracts.Responses;

public sealed record PermissionResponse(
    Guid Id,
    string Code,
    string? Description,
    DateTimeOffset CreatedAt);
