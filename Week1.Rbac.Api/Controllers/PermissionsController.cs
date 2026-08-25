using Microsoft.AspNetCore.Mvc;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Controllers;

[Route("api/permissions")]
public sealed class PermissionsController(IPermissionService permissions) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<PermissionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetAll(CancellationToken ct) =>
        Ok(await permissions.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionResponse>> GetById(Guid id, CancellationToken ct)
    {
        var result = await permissions.GetByIdAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPost]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionResponse>> Create(CreatePermissionRequest request, CancellationToken ct)
    {
        var result = await permissions.CreateAsync(request, ct);
        return result.Status is ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionResponse>> Update(Guid id, UpdatePermissionRequest request, CancellationToken ct)
    {
        var result = await permissions.UpdateAsync(id, request, ct);
        return result.Status is ServiceStatus.Success
            ? Ok(result.Value)
            : Failure(result.Status, result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await permissions.DeleteAsync(id, ct);
        return result.Status is ServiceStatus.Success
            ? NoContent()
            : Failure(result.Status, result.Error);
    }
}
