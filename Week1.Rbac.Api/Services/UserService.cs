using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public sealed class UserService(AppDbContext db) : IUserService
{
    private static readonly PasswordHasher<User> Hasher = new();

    private static UserResponse ToResponse(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.IsActive,
        user.CreatedAt,
        user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray());

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct)
    {
        var users = await db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .OrderBy(x => x.Email)
            .ToListAsync(ct);

        return users.Select(ToResponse).ToList();
    }

    public async Task<ServiceResult<UserResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return user is null
            ? ServiceResult<UserResponse>.NotFound("User không tồn tại")
            : ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<UserResponse>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
        {
            return ServiceResult<UserResponse>.Conflict($"Email đã tồn tại: {email}");
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };
        user.PasswordHash = Hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult<UserResponse>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (user is null)
        {
            return ServiceResult<UserResponse>.NotFound("User không tồn tại");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email && x.Id != id, ct))
        {
            return ServiceResult<UserResponse>.Conflict($"Email đã tồn tại: {email}");
        }

        user.Email = email;
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return ServiceResult<UserResponse>.Success(ToResponse(user));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null)
        {
            return ServiceResult.NotFound("User không tồn tại");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
        {
            return ServiceResult.NotFound("User không tồn tại");
        }

        if (!await db.Roles.AnyAsync(x => x.Id == roleId, ct))
        {
            return ServiceResult.NotFound("Role không tồn tại");
        }

        var exists = await db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, ct);
        if (!exists)
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        var link = await db.UserRoles.FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId, ct);
        if (link is null)
        {
            return ServiceResult.NotFound("User chưa được gán role này");
        }

        db.UserRoles.Remove(link);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IReadOnlyList<string>>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
        {
            return ServiceResult<IReadOnlyList<string>>.NotFound("User không tồn tại");
        }

        var codes = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<string>>.Success(codes);
    }
}
