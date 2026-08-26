using Microsoft.AspNetCore.Identity;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}

/// <summary>
/// Bọc PasswordHasher của ASP.NET Core (PBKDF2 + salt riêng cho từng mật khẩu).
/// Không bao giờ lưu mật khẩu dạng rõ, không dùng MD5/SHA-256 trần.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) =>
        _hasher.HashPassword(user, password);

    public bool Verify(User user, string password) =>
        _hasher.VerifyHashedPassword(user, user.PasswordHash, password)
            != PasswordVerificationResult.Failed;
}
