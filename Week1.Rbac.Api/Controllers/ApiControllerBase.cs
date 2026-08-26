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

        // Thong diep co tinh mo ho: khong noi ro email sai hay mat khau sai,
        // vi neu phan biet thi ke tan cong dung chinh API de liet ke email co that.
        ServiceStatus.Unauthorized => Unauthorized(new ProblemDetails
        {
            Title = error ?? "Chưa xác thực",
            Status = StatusCodes.Status401Unauthorized
        }),
        ServiceStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Title = error ?? "Không đủ quyền cho tài nguyên này",
            Status = StatusCodes.Status403Forbidden
        }),

        _ => throw new InvalidOperationException($"Trạng thái không phải lỗi: {status}")
    };
}
