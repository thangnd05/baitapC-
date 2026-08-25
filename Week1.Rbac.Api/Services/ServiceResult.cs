namespace Week1.Rbac.Api.Services;

public enum ServiceStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed record ServiceResult<T>(ServiceStatus Status, T? Value, string? Error)
{
    public static ServiceResult<T> Success(T value) => new(ServiceStatus.Success, value, null);
    public static ServiceResult<T> NotFound(string error) => new(ServiceStatus.NotFound, default, error);
    public static ServiceResult<T> Conflict(string error) => new(ServiceStatus.Conflict, default, error);
}

public sealed record ServiceResult(ServiceStatus Status, string? Error)
{
    public static ServiceResult Success() => new(ServiceStatus.Success, null);
    public static ServiceResult NotFound(string error) => new(ServiceStatus.NotFound, error);
    public static ServiceResult Conflict(string error) => new(ServiceStatus.Conflict, error);
}
