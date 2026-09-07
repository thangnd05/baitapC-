using Microsoft.AspNetCore.Authorization;

namespace Week1.Rbac.Api.Authorization;

/// <summary>
/// Ten policy = QUYEN ma endpoint can, KHONG phai vai tro nao duoc phep.
///
/// Endpoint chi noi "toi can quyen gi"; anh xa quyen -> vai tro nam duy nhat mot cho
/// trong <see cref="RbacAuthorizationExtensions.AddRbacAuthorization"/>. Doi quy tac phan quyen
/// (vi du cho Staff duoc xoa course) = sua MOT dong o do, khong phai lung sua 6 controller
/// va cung khong the sot mot endpoint.
/// </summary>
public static class AppPolicies
{
    // -- Role-based: quyet dinh chi dua tren vai tro trong token --------------

    /// <summary>Quan tri danh tinh va phan quyen: users, roles, permissions.</summary>
    public const string ManageIdentity = "ManageIdentity";

    /// <summary>Tao/sua danh muc dao tao: programmes, courses.</summary>
    public const string WriteCatalog = "WriteCatalog";

    /// <summary>Xoa danh muc dao tao. Tach khoi <see cref="WriteCatalog"/> vi xoa la thao tac
    /// khong hoan tac duoc, thuong chi cap cao nhat moi duoc lam.</summary>
    public const string DeleteCatalog = "DeleteCatalog";

    /// <summary>Doc/tao ho so sinh vien noi chung - tuc la du lieu ca nhan cua NGUOI KHAC.</summary>
    public const string ManageStudentDirectory = "ManageStudentDirectory";

    /// <summary>Xoa ho so sinh vien.</summary>
    public const string DeleteStudent = "DeleteStudent";

    // -- Resource-based: con phai xet chinh ban ghi dang bi dung toi ----------

    /// <summary>Staff/Admin sua duoc moi ho so, Student chi sua ho so cua chinh minh.</summary>
    public const string CanEditStudent = "CanEditStudent";

    /// <summary>Dieu kien doc mot ho so student cu the.</summary>
    public const string CanReadStudent = "CanReadStudent";
}

/// <summary>
/// Noi khai bao DUY NHAT: quyen nao thuoc ve vai tro nao.
/// </summary>
public static class RbacAuthorizationExtensions
{
    public static IServiceCollection AddRbacAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, StudentOwnerHandler>();

        services.AddAuthorizationBuilder()

            // Role-based
            .AddPolicy(AppPolicies.ManageIdentity, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin))

            .AddPolicy(AppPolicies.WriteCatalog, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Staff, AppRoles.Admin))

            .AddPolicy(AppPolicies.DeleteCatalog, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin))

            .AddPolicy(AppPolicies.ManageStudentDirectory, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Staff, AppRoles.Admin))

            .AddPolicy(AppPolicies.DeleteStudent, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Admin))

            // Resource-based: role check mot minh khong du, phai hoi them "ban ghi id nay
            // co phai cua nguoi goi khong" -> xem StudentOwnerHandler.
            .AddPolicy(AppPolicies.CanEditStudent, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new StudentOwnerRequirement()))

            .AddPolicy(AppPolicies.CanReadStudent, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new StudentOwnerRequirement { ReadOnly = true }));

        return services;
    }
}
