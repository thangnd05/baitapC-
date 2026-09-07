using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

/// <summary>
/// Mac dinh la TU CHOI: bao ve ca controller roi moi mo ra tung endpoint,
/// thay vi de mo roi co nho khoa tung endpoint.
/// </summary>
[Route("api/students")]
[Authorize]
public sealed class StudentsController(
    IStudentService students,
    IAuthorizationService authorization) : ApiControllerBase
{
    /// <summary>Danh sach toan bo student - Student KHONG duoc xem.</summary>
    [HttpGet]
    [Authorize(Policy = AppPolicies.ManageStudentDirectory)]
    [ProducesResponseType<IEnumerable<StudentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StudentResponse>>> GetAll(CancellationToken ct) =>
        Ok(await students.GetAllAsync(ct));

    /// <summary>Student chi doc duoc ho so cua chinh minh; Staff/Admin doc duoc tat ca.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentResponse>> GetById(long id, CancellationToken ct)
    {
        var check = await authorization.AuthorizeAsync(User, id, AppPolicies.CanReadStudent);
        if (!check.Succeeded)
        {
            return Forbid();
        }

        var result = await students.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.ManageStudentDirectory)]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Create(CreateStudentRequest request, CancellationToken ct)
    {
        var result = await students.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.StudentId }, result.Value)
            : Failure(result.Status, result.Error);
    }

    /// <summary>
    /// Diem chong BOLA. Role check mot minh khong du: mot Student da dang nhap van co the
    /// goi endpoint nay voi id cua nguoi khac, nen phai hoi owner policy trung tai nguyen id.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<StudentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentResponse>> Update(
        long id, UpdateStudentRequest request, CancellationToken ct)
    {
        var check = await authorization.AuthorizeAsync(User, id, AppPolicies.CanEditStudent);
        if (!check.Succeeded)
        {
            return Forbid(); // 403, khong phai 401 - nguoi goi DA xac thuc, chi la khong du quyen
        }

        var result = await students.UpdateAsync(id, request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AppPolicies.DeleteStudent)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await students.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
