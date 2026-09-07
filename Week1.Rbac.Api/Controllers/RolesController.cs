using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[Route("api/roles")]
[Authorize(Policy = AppPolicies.ManageIdentity)]
public sealed class RolesController(IRoleService roles) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<RoleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetAll(CancellationToken ct) =>
        Ok(await roles.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await roles.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roles.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleResponse>> Update(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await roles.UpdateAsync(id, request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await roles.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{roleId:guid}/permissions/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        var result = await roles.AssignPermissionAsync(roleId, permissionId, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{roleId:guid}/permissions/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        var result = await roles.RemovePermissionAsync(roleId, permissionId, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
