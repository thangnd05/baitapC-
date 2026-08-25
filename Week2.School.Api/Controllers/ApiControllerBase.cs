using Microsoft.AspNetCore.Mvc;
using Week2.School.Api.Services;

namespace Week2.School.Api.Controllers;

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
            Title = error ?? "Vi phạm quy tắc nghiệp vụ",
            Status = StatusCodes.Status409Conflict
        }),
        _ => throw new InvalidOperationException($"Trạng thái không phải lỗi: {status}")
    };
}
