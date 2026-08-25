namespace Week1.Rbac.Api.Contracts.Responses;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Permissions);
