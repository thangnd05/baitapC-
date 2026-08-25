using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;

namespace Week1.Rbac.Api.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<PermissionResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<PermissionResponse>> CreateAsync(CreatePermissionRequest request, CancellationToken ct);
    Task<ServiceResult<PermissionResponse>> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
}
