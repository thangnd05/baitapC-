using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[Route("api/courses")]
[Authorize]
public sealed class CoursesController(ICourseService courses) : ApiControllerBase
{
    /// <summary>Danh muc hoc phan: moi vai tro da dang nhap deu doc duoc.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<CourseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> GetAll(CancellationToken ct) =>
        Ok(await courses.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(long id, CancellationToken ct)
    {
        var result = await courses.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseResponse>> Create(CreateCourseRequest request, CancellationToken ct)
    {
        var result = await courses.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.CourseId }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseResponse>> Update(long id, UpdateCourseRequest request, CancellationToken ct)
    {
        var result = await courses.UpdateAsync(id, request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await courses.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
