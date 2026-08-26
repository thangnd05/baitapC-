namespace Week1.Rbac.Api.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Hồ sơ student mà tài khoản này sở hữu. Null với Admin/Staff.
    /// Đây là dữ liệu mà owner policy tra để trả lời "bản ghi này có phải của bạn không".
    /// </summary>
    public long? StudentId { get; set; }
    public Student? Student { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
