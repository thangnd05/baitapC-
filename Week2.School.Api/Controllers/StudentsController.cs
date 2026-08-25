using Microsoft.AspNetCore.Mvc;
using Week2.School.Api.Contracts.Requests;
using Week2.School.Api.Contracts.Responses;
using Week2.School.Api.Services;

namespace Week2.School.Api.Controllers;

[Route("api/students")]
public sealed class StudentsController(IStudentService students) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<StudentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StudentResponse>>> GetAll(CancellationToken ct) =>
        Ok(await students.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentResponse>> GetById(long id, CancellationToken ct)
    {
        var result = await students.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Create(CreateStudentRequest request, CancellationToken ct)
    {
        var result = await students.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.StudentId }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Update(long id, UpdateStudentRequest request, CancellationToken ct)
    {
        var result = await students.UpdateAsync(id, request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await students.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
