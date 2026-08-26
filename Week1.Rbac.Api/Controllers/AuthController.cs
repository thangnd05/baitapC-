using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Infrastructure;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[Route("api/auth")]
[Authorize]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Endpoint cong khai. Co rate limit rieng de chan do mat khau tu dong.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenPairResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    /// <summary>
    /// Doi refresh token lay cap token moi. Token cu bi thu hoi ngay trong cung giao dich (rotation).
    /// Dung lai mot token da thu hoi bi coi la replay va huy toan bo phien cua tai khoan do.
    /// Cong khai vi client goi endpoint nay DUNG LUC access token da het han.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<TokenPairResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenPairResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var result = await auth.RefreshAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    /// <summary>
    /// Thu hoi refresh token. Luon tra 204 ke ca khi token khong ton tai -
    /// tra loi khac nhau se cho biet token nao co that.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>Doc lai chinh token dang cam: chung minh claim role va student_id co that.</summary>
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var studentIdClaim = User.FindFirstValue(TokenService.StudentIdClaim);

        return Ok(new MeResponse(
            Guid.TryParse(userId, out var id) ? id : Guid.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            long.TryParse(studentIdClaim, out var studentId) ? studentId : null,
            User.FindAll(ClaimTypes.Role).Select(x => x.Value).OrderBy(x => x).ToArray()));
    }
}
