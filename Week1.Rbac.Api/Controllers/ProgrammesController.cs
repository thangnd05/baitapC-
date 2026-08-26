using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[Route("api/programmes")]
[Authorize]
public sealed class ProgrammesController(IProgrammeService programmes) : ApiControllerBase
{
    /// <summary>Danh muc chuong trinh dao tao: moi vai tro da dang nhap deu doc duoc.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<ProgrammeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProgrammeResponse>>> GetAll(CancellationToken ct) =>
        Ok(await programmes.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProgrammeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgrammeResponse>> GetById(long id, CancellationToken ct)
    {
        var result = await programmes.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [ProducesResponseType<ProgrammeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgrammeResponse>> Create(CreateProgrammeRequest request, CancellationToken ct)
    {
        var result = await programmes.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.ProgrammeId }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [ProducesResponseType<ProgrammeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgrammeResponse>> Update(long id, UpdateProgrammeRequest request, CancellationToken ct)
    {
        var result = await programmes.UpdateAsync(id, request, ct);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await programmes.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }

    /// <summary>Liet ke ho so sinh vien theo chuong trinh - du lieu ca nhan, chi Staff/Admin.</summary>
    [HttpGet("{id:long}/students")]
    [Authorize(Roles = AppRoles.StaffOrAdmin)]
    [ProducesResponseType<IEnumerable<StudentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<StudentResponse>>> GetStudents(long id, CancellationToken ct)
    {
        var result = await programmes.GetStudentsAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }
}
