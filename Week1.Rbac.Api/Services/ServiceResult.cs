namespace Week1.Rbac.Api.Services;

public enum ServiceStatus
{
    Success,
    NotFound,
    Conflict,

    /// <summary>Chua xac thuc: thieu token, token sai dinh dang hoac het han.</summary>
    Unauthorized,

    /// <summary>Da xac thuc nhung thieu vai tro hoac khong phai chu so huu.</summary>
    Forbidden
}

public sealed record ServiceResult<T>(ServiceStatus Status, T? Value, string? Error)
{
    public static ServiceResult<T> Success(T value) => new(ServiceStatus.Success, value, null);
    public static ServiceResult<T> NotFound(string error) => new(ServiceStatus.NotFound, default, error);
    public static ServiceResult<T> Conflict(string error) => new(ServiceStatus.Conflict, default, error);
    public static ServiceResult<T> Unauthorized(string error) => new(ServiceStatus.Unauthorized, default, error);
    public static ServiceResult<T> Forbidden(string error) => new(ServiceStatus.Forbidden, default, error);
}

public sealed record ServiceResult(ServiceStatus Status, string? Error)
{
    public static ServiceResult Success() => new(ServiceStatus.Success, null);
    public static ServiceResult NotFound(string error) => new(ServiceStatus.NotFound, error);
    public static ServiceResult Conflict(string error) => new(ServiceStatus.Conflict, error);
    public static ServiceResult Unauthorized(string error) => new(ServiceStatus.Unauthorized, error);
    public static ServiceResult Forbidden(string error) => new(ServiceStatus.Forbidden, error);
}
