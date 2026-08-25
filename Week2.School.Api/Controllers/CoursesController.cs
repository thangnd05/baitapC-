using Microsoft.AspNetCore.Mvc;
using Week2.School.Api.Contracts.Requests;
using Week2.School.Api.Contracts.Responses;
using Week2.School.Api.Services;

namespace Week2.School.Api.Controllers;

[Route("api/courses")]
public sealed class CoursesController(ICourseService courses) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<CourseResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CourseResponse>>> GetAll(CancellationToken ct) =>
        Ok(await courses.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(long id, CancellationToken ct)
    {
        var result = await courses.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseResponse>> Create(CreateCourseRequest request, CancellationToken ct)
    {
        var result = await courses.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.CourseId }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await courses.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
