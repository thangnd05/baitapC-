namespace Week1.Rbac.Api.Models;

/// <summary>
/// Refresh token dung mot lan (rotation). Bang nay chi luu BAM SHA-256 cua token,
/// khong bao gio luu gia tri that: neu database bi doc trom, ke tan cong van khong
/// tai tao duoc token de goi /api/auth/refresh.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 cua token that, dang hex. Token la 32 byte ngau nhien nen khong can salt.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Khac null nghia la token da bi thu hoi - do da dung, do dang xuat, hoac do phat hien replay.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }

    /// <summary>Token moi sinh ra khi xoay vong. Dung de lan vet ca chuoi khi phat hien replay.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}

public static class RevokeReasons
{
    public const string Rotated = "rotated";
    public const string LoggedOut = "logged_out";
    public const string ReplayDetected = "replay_detected";
}
