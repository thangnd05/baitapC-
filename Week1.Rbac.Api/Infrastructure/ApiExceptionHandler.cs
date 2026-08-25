using Microsoft.AspNetCore.Diagnostics;

namespace Week1.Rbac.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Lỗi không lường trước tại {Path}", context.Request.Path);

        await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Lỗi không lường trước ở phía server",
                detail: exception.Message,
                instance: context.Request.Path)
            .ExecuteAsync(context);

        return true;
    }
}
