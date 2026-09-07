using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

/// <summary>
/// Cac ma loi DUNG CHUNG khai bao mot lan o day: MVC ap thuoc tinh cap class cho
/// MOI action cua moi controller ke thua, nen tung endpoint chi con phai khai bao
/// nhung gi RIENG cua no (200 + kieu du lieu, 201, 204, 404, 409...).
///
/// 400 -> ValidationProblemDetails do [ApiController] tu tra khi model khong hop le.
/// 401/403 -> pipeline authentication/authorization tra, truoc khi vao action.
/// 500 -> ApiExceptionHandler.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
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
