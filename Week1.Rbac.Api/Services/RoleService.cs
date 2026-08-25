using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public sealed class RoleService(AppDbContext db) : IRoleService
{
    private static RoleResponse ToResponse(Role role) => new(
        role.Id,
        role.Name,
        role.Description,
        role.CreatedAt,
        role.RolePermissions.Select(x => x.Permission.Code).OrderBy(x => x).ToArray());

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return roles.Select(ToResponse).ToList();
    }

    public async Task<ServiceResult<RoleResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return role is null
            ? ServiceResult<RoleResponse>.NotFound("Role không tồn tại")
            : ServiceResult<RoleResponse>.Success(ToResponse(role));
    }

    public async Task<ServiceResult<RoleResponse>> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim().ToLowerInvariant();
        if (await db.Roles.AnyAsync(x => x.Name == name, ct))
        {
            return ServiceResult<RoleResponse>.Conflict($"Role đã tồn tại: {name}");
        }

        var role = new Role { Name = name, Description = request.Description?.Trim() };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        return ServiceResult<RoleResponse>.Success(ToResponse(role));
    }

    public async Task<ServiceResult<RoleResponse>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles
            .Include(x => x.RolePermissions).ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (role is null)
        {
            return ServiceResult<RoleResponse>.NotFound("Role không tồn tại");
        }

        var name = request.Name.Trim().ToLowerInvariant();
        if (await db.Roles.AnyAsync(x => x.Name == name && x.Id != id, ct))
        {
            return ServiceResult<RoleResponse>.Conflict($"Role đã tồn tại: {name}");
        }

        role.Name = name;
        role.Description = request.Description?.Trim();
        await db.SaveChangesAsync(ct);

        return ServiceResult<RoleResponse>.Success(ToResponse(role));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (role is null)
        {
            return ServiceResult.NotFound("Role không tồn tại");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AssignPermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(x => x.Id == roleId, ct))
        {
            return ServiceResult.NotFound("Role không tồn tại");
        }

        if (!await db.Permissions.AnyAsync(x => x.Id == permissionId, ct))
        {
            return ServiceResult.NotFound("Permission không tồn tại");
        }

        var exists = await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, ct);
        if (!exists)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemovePermissionAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        var link = await db.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, ct);

        if (link is null)
        {
            return ServiceResult.NotFound("Role chưa có permission này");
        }

        db.RolePermissions.Remove(link);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
