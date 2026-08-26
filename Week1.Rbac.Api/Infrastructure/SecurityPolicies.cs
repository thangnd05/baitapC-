namespace Week1.Rbac.Api.Infrastructure;

public static class CorsPolicies
{
    /// <summary>
    /// Allowlist origin cu the. KHONG dung AllowAnyOrigin.
    /// Luu y: CORS la chinh sach cua TRINH DUYET - no khong chan duoc curl hay Postman,
    /// nen no khong bao gio thay duoc authorization.
    /// </summary>
    public const string SpaAllowlist = "SpaAllowlist";
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}

/// <summary>
/// Security headers co ban cho moi response.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        return next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
