using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

/// <summary>
/// Doc cau hinh JWT tu configuration. Khoa ky KHONG BAO GIO nam trong appsettings.json;
/// no den tu .env (Jwt__SigningKey) hoac User Secrets (Jwt:SigningKey).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SchoolApi";
    public string Audience { get; set; } = "SchoolApiClients";
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token song ngan - no khong the thu hoi nen phai het han nhanh.</summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Refresh token song dai nhung dung MOT LAN va thu hoi duoc qua bang refresh_token.</summary>
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>HMAC-SHA256 doi khoa toi thieu 32 byte, ngan hon thu vien se nem loi khi ky.</summary>
    public const int MinimumKeyBytes = 32;
}

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, IEnumerable<string> roles);
    RefreshTokenResult CreateRefreshToken(Guid userId);
    string HashRefreshToken(string rawToken);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// <paramref name="RawToken"/> chi ton tai trong RAM va trong response tra ve client.
/// Thu duoc luu xuong database la <paramref name="Entity"/> (chi chua bam).
/// </summary>
public sealed record RefreshTokenResult(string RawToken, RefreshToken Entity);

public sealed class TokenService(JwtOptions options) : ITokenService
{
    public const string StudentIdClaim = "student_id";

    private const int RefreshTokenBytes = 32; // 256 bit entropy

    public AccessTokenResult CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };

        // Claim nay la thu owner policy tra de tra loi "ban ghi nay co phai cua ban khong".
        if (user.StudentId is not null)
        {
            claims.Add(new Claim(StudentIdClaim, user.StudentId.Value.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessTokenResult(token, expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken(Guid userId)
    {
        // Token la gia tri ngau nhien, KHONG phai JWT: no khong mang thong tin gi,
        // chi la chia khoa tra vao bang refresh_token.
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashRefreshToken(raw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays)
        };

        return new RefreshTokenResult(raw, entity);
    }

    /// <summary>
    /// SHA-256 tran la du: token co 256 bit entropy nen khong the do nguoc nhu mat khau,
    /// khong can PBKDF2/salt (va cung khong nen - refresh chay moi 30 phut, phai nhanh).
    /// </summary>
    public string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
