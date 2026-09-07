namespace Week1.Rbac.Api.Authorization;

/// <summary>
/// Ten vai tro. Phai khop CHINH XAC gia tri cot roles.name trong DB -
/// so sanh role la case-sensitive.
///
/// LUU Y: controller KHONG dung truc tiep hang so nay nua. Hang so o day chi phuc vu
/// hai cho: seed DB (<see cref="Data.IdentitySeeder"/>) va noi KHAI BAO policy
/// (<see cref="RbacAuthorizationExtensions.AddRbacAuthorization"/>).
/// Endpoint khai bao QUYEN no can qua <see cref="AppPolicies"/>, khong phai vai tro.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Staff, Student];
}
