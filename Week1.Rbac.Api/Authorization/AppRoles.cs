namespace Week1.Rbac.Api.Authorization;

/// <summary>
/// Ten vai tro dung trong [Authorize(Roles = ...)]. Phai khop CHINH XAC gia tri
/// cot roles.name trong DB - so sanh role la case-sensitive.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Student = "Student";

    public const string StaffOrAdmin = $"{Staff},{Admin}";

    public static readonly IReadOnlyList<string> All = [Admin, Staff, Student];
}

public static class AppPolicies
{
    /// <summary>Resource-based: Staff/Admin sua duoc moi ho so, Student chi sua ho so cua chinh minh.</summary>
    public const string CanEditStudent = "CanEditStudent";

    /// <summary>Resource-based: dieu kien doc mot ho so student cu the.</summary>
    public const string CanReadStudent = "CanReadStudent";
}
