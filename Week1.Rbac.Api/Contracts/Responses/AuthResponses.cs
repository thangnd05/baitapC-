namespace Week1.Rbac.Api.Contracts.Responses;

/// <summary>
/// Tra ve ca cap token. Access token dung cho moi request; refresh token chi dung
/// mot lan duy nhat de doi lay cap moi khi access token het han.
/// </summary>
public sealed record TokenPairResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    IReadOnlyCollection<string> Roles);

/// <summary>Doc lai chinh token dang cam - dung de chung minh claim role/student_id co that.</summary>
public sealed record MeResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    long? StudentId,
    IReadOnlyCollection<string> Roles);
