using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    private static readonly DateTimeOffset SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid OrganizerRoleId =
        Guid.Parse("a0000000-0000-0000-0000-000000000001");

    private static readonly Guid EventCreateId =
        Guid.Parse("e0000000-0000-0000-0000-000000000001");

    private static readonly Guid EventUpdateId =
        Guid.Parse("e0000000-0000-0000-0000-000000000002");

    private static readonly Guid EventDeleteId =
        Guid.Parse("e0000000-0000-0000-0000-000000000003");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_users_email");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ix_roles_name");

            entity.HasData(new Role
            {
                Id = OrganizerRoleId,
                Name = "organizer",
                Description = "Quản lý sự kiện",
                CreatedAt = SeedTimestamp
            });
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(256);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_permissions_code");

            entity.HasData(
                new Permission
                {
                    Id = EventCreateId,
                    Code = "event.create",
                    Description = "Tạo sự kiện",
                    CreatedAt = SeedTimestamp
                },
                new Permission
                {
                    Id = EventUpdateId,
                    Code = "event.update",
                    Description = "Sửa sự kiện",
                    CreatedAt = SeedTimestamp
                },
                new Permission
                {
                    Id = EventDeleteId,
                    Code = "event.delete",
                    Description = "Xóa sự kiện",
                    CreatedAt = SeedTimestamp
                });
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.RoleId).HasColumnName("role_id");
            entity.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.Property(x => x.RoleId).HasColumnName("role_id");
            entity.Property(x => x.PermissionId).HasColumnName("permission_id");
            entity.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission).WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new RolePermission
                {
                    RoleId = OrganizerRoleId,
                    PermissionId = EventCreateId,
                    AssignedAt = SeedTimestamp
                },
                new RolePermission
                {
                    RoleId = OrganizerRoleId,
                    PermissionId = EventUpdateId,
                    AssignedAt = SeedTimestamp
                },
                new RolePermission
                {
                    RoleId = OrganizerRoleId,
                    PermissionId = EventDeleteId,
                    AssignedAt = SeedTimestamp
                });
        });
    }
}
