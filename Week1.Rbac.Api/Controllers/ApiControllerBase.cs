using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult Failure(ServiceStatus status, string? error) => status switch
    {
        ServiceStatus.NotFound => NotFound(new ProblemDetails
        {
            Title = error ?? "Không tìm thấy",
            Status = StatusCodes.Status404NotFound
        }),
        ServiceStatus.Conflict => Conflict(new ProblemDetails
        {
            Title = error ?? "Dữ liệu bị trùng",
            Status = StatusCodes.Status409Conflict
        }),
        _ => throw new InvalidOperationException($"Trạng thái không phải lỗi: {status}")
    };
}
