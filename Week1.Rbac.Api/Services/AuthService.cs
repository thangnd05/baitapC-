using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<TokenPairResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<ServiceResult<TokenPairResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task<ServiceResult> LogoutAsync(LogoutRequest request, CancellationToken ct);
}

public sealed class AuthService(
    AppDbContext db,
    IPasswordService passwords,
    ITokenService tokens,
    ILogger<AuthService> logger) : IAuthService
{
    /// <summary>
    /// Mot thong diep duy nhat cho ca "email khong ton tai" lan "mat khau sai".
    /// Phan biet hai truong hop = bien API thanh cong cu liet ke email co that.
    /// </summary>
    private const string InvalidCredentials = "Email hoặc mật khẩu không đúng";

    /// <summary>Cung mot thong diep cho moi ly do refresh that bai: khong tiet lo token do sai o dau.</summary>
    private const string InvalidRefreshToken = "Refresh token không hợp lệ hoặc đã hết hạn";

    public async Task<ServiceResult<TokenPairResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == email, ct);

        // Tai khoan bi khoa cung tra ve dung thong diep chung o tren.
        if (user is null || !user.IsActive || !passwords.Verify(user, request.Password))
        {
            return ServiceResult<TokenPairResponse>.Unauthorized(InvalidCredentials);
        }

        var pair = await IssuePairAsync(user, previous: null, ct);
        return ServiceResult<TokenPairResponse>.Success(pair);
    }

    public async Task<ServiceResult<TokenPairResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        var stored = await db.RefreshTokens
            .Include(x => x.User).ThenInclude(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (stored is null)
        {
            return ServiceResult<TokenPairResponse>.Unauthorized(InvalidRefreshToken);
        }

        // PHAT HIEN REPLAY: token nay da bi thu hoi roi ma van co nguoi dung.
        // Hoac client giu lai token cu, hoac token da bi danh cap. Khong phan biet duoc,
        // nen xu ly theo huong an toan nhat: huy TOAN BO phien cua user, bat dang nhap lai.
        if (stored.RevokedAt is not null)
        {
            await RevokeAllActiveAsync(stored.UserId, RevokeReasons.ReplayDetected, now, ct);

            logger.LogWarning(
                "Phat hien replay refresh token cua user {UserId}. Da thu hoi toan bo phien.",
                stored.UserId);

            return ServiceResult<TokenPairResponse>.Unauthorized(InvalidRefreshToken);
        }

        if (stored.ExpiresAt <= now || !stored.User.IsActive)
        {
            return ServiceResult<TokenPairResponse>.Unauthorized(InvalidRefreshToken);
        }

        // ROTATION: token cu bi thu hoi ngay, client nhan token moi.
        var pair = await IssuePairAsync(stored.User, previous: stored, ct);
        return ServiceResult<TokenPairResponse>.Success(pair);
    }

    public async Task<ServiceResult> LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        var stored = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);

        // Dang xuat luon tra 204, ke ca khi token khong ton tai hoac da thu hoi:
        // client khong can biet, va tra loi khac nhau se ro ri thong tin.
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = now;
            stored.RevokedReason = RevokeReasons.LoggedOut;
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult.Success();
    }

    /// <summary>Phat cap token moi; neu co <paramref name="previous"/> thi thu hoi va noi chuoi.</summary>
    private async Task<TokenPairResponse> IssuePairAsync(User user, RefreshToken? previous, CancellationToken ct)
    {
        var roles = user.UserRoles
            .Select(x => x.Role.Name)
            .OrderBy(x => x)
            .ToArray();

        var access = tokens.CreateAccessToken(user, roles);
        var refresh = tokens.CreateRefreshToken(user.Id);

        db.RefreshTokens.Add(refresh.Entity);

        if (previous is not null)
        {
            previous.RevokedAt = DateTimeOffset.UtcNow;
            previous.RevokedReason = RevokeReasons.Rotated;
            previous.ReplacedByTokenId = refresh.Entity.Id;
        }

        await db.SaveChangesAsync(ct);

        return new TokenPairResponse(
            access.Token,
            "Bearer",
            access.ExpiresAt,
            refresh.RawToken,
            refresh.Entity.ExpiresAt,
            roles);
    }

    private async Task RevokeAllActiveAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.RevokedAt, now)
                      .SetProperty(x => x.RevokedReason, reason),
                ct);
    }
}
