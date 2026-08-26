using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // --- Danh tinh & phan quyen (tuan 1) ---
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // --- Nghiep vu truong hoc (tuan 2) ---
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();

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
        ConfigureIdentity(modelBuilder);
        ConfigureSchool(modelBuilder);
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
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
            entity.Property(x => x.StudentId).HasColumnName("student_id");
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_users_email");

            // Mot tai khoan tro toi toi da mot ho so student.
            // SetNull: xoa student thi tai khoan van con nhung mat quyen so huu.
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_token");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            entity.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(40);
            entity.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");

            // Tra cuu luc refresh la tim theo hash -> phai unique va co index.
            entity.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("ix_refresh_token_hash");
            entity.HasIndex(x => x.UserId).HasDatabaseName("ix_refresh_token_user");

            // Xoa user thi xoa luon moi refresh token cua user do.
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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

    private static void ConfigureSchool(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Programme>(entity =>
        {
            entity.ToTable("programme");
            entity.HasKey(x => x.ProgrammeId);
            entity.Property(x => x.ProgrammeId).HasColumnName("programme_id");
            entity.Property(x => x.ProgrammeCode).HasColumnName("programme_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.ProgrammeName).HasColumnName("programme_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.DegreeLevel).HasColumnName("degree_level").HasMaxLength(30).IsRequired();
            entity.Property(x => x.DurationYears).HasColumnName("duration_years");
            entity.HasIndex(x => x.ProgrammeCode).IsUnique().HasDatabaseName("ix_programme_code");
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("course");
            entity.HasKey(x => x.CourseId);
            entity.Property(x => x.CourseId).HasColumnName("course_id");
            entity.Property(x => x.CourseCode).HasColumnName("course_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.CourseName).HasColumnName("course_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Credits).HasColumnName("credits");
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(x => x.CourseCode).IsUnique().HasDatabaseName("ix_course_code");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("student");
            entity.HasKey(x => x.StudentId);
            entity.Property(x => x.StudentId).HasColumnName("student_id");
            entity.Property(x => x.ProgrammeId).HasColumnName("programme_id");
            entity.Property(x => x.StudentCode).HasColumnName("student_code").HasMaxLength(20).IsRequired();
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            entity.Property(x => x.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date");
            entity.Property(x => x.YearOfEntry).HasColumnName("year_of_entry");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.StudentCode).IsUnique().HasDatabaseName("ix_student_code");
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_student_email");

            entity.HasOne(x => x.Programme).WithMany(x => x.Students)
                .HasForeignKey(x => x.ProgrammeId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
