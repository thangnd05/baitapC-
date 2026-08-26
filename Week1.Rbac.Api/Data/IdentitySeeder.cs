using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Authorization;
using Week1.Rbac.Api.Models;
using Week1.Rbac.Api.Services;

namespace Week1.Rbac.Api.Data;

/// <summary>
/// Seed du lieu lab: 3 vai tro, 3 tai khoan mau, va mot it du lieu truong hoc
/// de co the chung minh owner policy (can it nhat 2 student khac chu so huu).
/// Chi chay o moi truong Development. Mat khau lay tu cau hinh, KHONG viet cung trong ma nguon.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@hnmu.edu.vn";
    public const string StaffEmail = "staff@hnmu.edu.vn";
    public const string StudentEmail = "sv001@hnmu.edu.vn";

    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordService passwords,
        IConfiguration config,
        CancellationToken ct = default)
    {
        var seedPassword = config["Seed:Password"]
            ?? throw new InvalidOperationException(
                "Thiếu Seed:Password. Đặt Seed__Password trong .env hoặc "
                + "dotnet user-secrets set \"Seed:Password\" \"...\"");

        await SeedRolesAsync(db, ct);
        var studentIds = await SeedSchoolDataAsync(db, ct);
        await SeedAccountsAsync(db, passwords, seedPassword, studentIds, ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        foreach (var name in AppRoles.All)
        {
            if (!await db.Roles.AnyAsync(x => x.Name == name, ct))
            {
                db.Roles.Add(new Role
                {
                    Name = name,
                    Description = name switch
                    {
                        AppRoles.Admin => "Toàn quyền, gồm xóa và quản lý vai trò",
                        AppRoles.Staff => "Đọc tất cả và sửa hồ sơ sinh viên",
                        _ => "Chỉ đọc và sửa hồ sơ của chính mình"
                    }
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tra ve danh sach student_id theo thu tu student_code, de gan chu so huu.</summary>
    private static async Task<IReadOnlyList<long>> SeedSchoolDataAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.Programmes.AnyAsync(ct))
        {
            db.Programmes.Add(new Programme
            {
                ProgrammeCode = "SE",
                ProgrammeName = "Kỹ thuật phần mềm",
                DegreeLevel = "BACHELOR",
                DurationYears = 4
            });
            await db.SaveChangesAsync(ct);
        }

        var programmeId = await db.Programmes
            .OrderBy(x => x.ProgrammeCode)
            .Select(x => x.ProgrammeId)
            .FirstAsync(ct);

        if (!await db.Courses.AnyAsync(ct))
        {
            db.Courses.AddRange(
                new Course { CourseCode = "INF067", CourseName = "Lập trình Web nâng cao", Credits = 3 },
                new Course { CourseCode = "INF012", CourseName = "Cơ sở dữ liệu", Credits = 3 });
            await db.SaveChangesAsync(ct);
        }

        // Ba ho so: sv001 se thuoc ve tai khoan Student, sv002/sv003 la ho so cua NGUOI KHAC
        // - chinh la doi tuong dung trong kiem thu am BOLA.
        var demoStudents = new (string Code, string FullName, string Email, int Year)[]
        {
            ("SV001", "Nguyễn Văn An", "sv001@hnmu.edu.vn", 2024),
            ("SV002", "Trần Thị Bình", "sv002@hnmu.edu.vn", 2024),
            ("SV003", "Lê Hoàng Cường", "sv003@hnmu.edu.vn", 2025)
        };

        foreach (var (code, fullName, email, year) in demoStudents)
        {
            if (!await db.Students.AnyAsync(x => x.StudentCode == code, ct))
            {
                db.Students.Add(new Student
                {
                    ProgrammeId = programmeId,
                    StudentCode = code,
                    FullName = fullName,
                    Email = email,
                    YearOfEntry = year,
                    Status = StudentStatus.Active
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return await db.Students
            .OrderBy(x => x.StudentCode)
            .Select(x => x.StudentId)
            .ToListAsync(ct);
    }

    private static async Task SeedAccountsAsync(
        AppDbContext db,
        IPasswordService passwords,
        string seedPassword,
        IReadOnlyList<long> studentIds,
        CancellationToken ct)
    {
        var accounts = new (string Email, string DisplayName, string Role, long? StudentId)[]
        {
            (AdminEmail, "Campus Admin", AppRoles.Admin, null),
            (StaffEmail, "Phòng Đào tạo", AppRoles.Staff, null),
            // Tai khoan Student duoc gan dung ho so SV001.
            (StudentEmail, "Nguyễn Văn An", AppRoles.Student, studentIds.FirstOrDefault())
        };

        foreach (var (email, displayName, roleName, studentId) in accounts)
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);

            if (user is null)
            {
                user = new User
                {
                    Email = email,
                    DisplayName = displayName,
                    StudentId = studentId == 0 ? null : studentId
                };
                user.PasswordHash = passwords.Hash(user, seedPassword);
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);
            }
            else if (user.StudentId is null && studentId is > 0)
            {
                // Tai khoan da co tu truoc nhung chua duoc noi voi ho so student.
                user.StudentId = studentId;
                await db.SaveChangesAsync(ct);
            }

            var roleId = await db.Roles
                .Where(x => x.Name == roleName)
                .Select(x => x.Id)
                .SingleAsync(ct);

            if (!await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == roleId, ct))
            {
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
