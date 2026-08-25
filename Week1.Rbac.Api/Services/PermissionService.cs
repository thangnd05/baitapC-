using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public sealed class PermissionService(AppDbContext db) : IPermissionService
{
    private static PermissionResponse ToResponse(Permission permission) =>
        new(permission.Id, permission.Code, permission.Description, permission.CreatedAt);

    public async Task<IReadOnlyList<PermissionResponse>> GetAllAsync(CancellationToken ct)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        return permissions.Select(ToResponse).ToList();
    }

    public async Task<ServiceResult<PermissionResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var permission = await db.Permissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        return permission is null
            ? ServiceResult<PermissionResponse>.NotFound("Permission không tồn tại")
            : ServiceResult<PermissionResponse>.Success(ToResponse(permission));
    }

    public async Task<ServiceResult<PermissionResponse>> CreateAsync(CreatePermissionRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await db.Permissions.AnyAsync(x => x.Code == code, ct))
        {
            return ServiceResult<PermissionResponse>.Conflict($"Permission code đã tồn tại: {code}");
        }

        var permission = new Permission { Code = code, Description = request.Description?.Trim() };
        db.Permissions.Add(permission);
        await db.SaveChangesAsync(ct);

        return ServiceResult<PermissionResponse>.Success(ToResponse(permission));
    }

    public async Task<ServiceResult<PermissionResponse>> UpdateAsync(Guid id, UpdatePermissionRequest request, CancellationToken ct)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (permission is null)
        {
            return ServiceResult<PermissionResponse>.NotFound("Permission không tồn tại");
        }

        var code = request.Code.Trim().ToLowerInvariant();
        if (await db.Permissions.AnyAsync(x => x.Code == code && x.Id != id, ct))
        {
            return ServiceResult<PermissionResponse>.Conflict($"Permission code đã tồn tại: {code}");
        }

        permission.Code = code;
        permission.Description = request.Description?.Trim();
        await db.SaveChangesAsync(ct);

        return ServiceResult<PermissionResponse>.Success(ToResponse(permission));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (permission is null)
        {
            return ServiceResult.NotFound("Permission không tồn tại");
        }

        db.Permissions.Remove(permission);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
