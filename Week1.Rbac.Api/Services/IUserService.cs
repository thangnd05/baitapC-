using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;

namespace Week1.Rbac.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<ServiceResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
    Task<ServiceResult> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<string>>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct);
}
