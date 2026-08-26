using Microsoft.AspNetCore.Authorization;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Authorization;

/// <summary>
/// Role check chi tra loi "ban thuoc nhom nao". No KHONG ngan mot sinh vien da dang nhap
/// goi PUT /api/students/{id} voi id cua nguoi khac - do la BOLA
/// (Broken Object Level Authorization, OWASP API Security Top 10 #1).
/// Requirement nay la tang thu hai: "ban duoc lam gi tren BAN GHI NAY".
/// </summary>
public sealed class StudentOwnerRequirement : IAuthorizationRequirement
{
    /// <summary>true = chi doc; false = ghi. Hien tai ca hai dung chung quy tac chu so huu.</summary>
    public bool ReadOnly { get; init; }
}

public sealed class StudentOwnerHandler
    : AuthorizationHandler<StudentOwnerRequirement, long>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentOwnerRequirement requirement,
        long studentId)
    {
        // Staff va Admin duoc thao tac tren moi ho so.
        // Thieu nhanh nay thi chinh Admin cung bi 403.
        if (context.User.IsInRole(AppRoles.Staff) || context.User.IsInRole(AppRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Chu so huu: claim student_id trong token phai trung id tai nguyen dang goi.
        var claim = context.User.FindFirst(TokenService.StudentIdClaim)?.Value;
        if (long.TryParse(claim, out var ownedStudentId) && ownedStudentId == studentId)
        {
            context.Succeed(requirement);
        }

        // Khong goi Succeed nghia la tu choi -> IAuthorizationService tra Succeeded = false.
        return Task.CompletedTask;
    }
}
