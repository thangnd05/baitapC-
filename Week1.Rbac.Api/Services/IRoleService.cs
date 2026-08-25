using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;

namespace Week1.Rbac.Api.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<RoleResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<RoleResponse>> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<ServiceResult<RoleResponse>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
    Task<ServiceResult> AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct);
    Task<ServiceResult> RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct);
}
