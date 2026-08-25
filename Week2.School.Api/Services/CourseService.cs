using Microsoft.EntityFrameworkCore;
using Week2.School.Api.Contracts.Requests;
using Week2.School.Api.Contracts.Responses;
using Week2.School.Api.Data;
using Week2.School.Api.Models;

namespace Week2.School.Api.Services;

public sealed class CourseService(SchoolDbContext db) : ICourseService
{
    private static CourseResponse ToResponse(Course course) => new(
        course.CourseId,
        course.CourseCode,
        course.CourseName,
        course.Credits,
        course.IsActive);

    public async Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken ct) =>
        await db.Courses.AsNoTracking()
            .OrderBy(x => x.CourseCode)
            .Select(x => new CourseResponse(x.CourseId, x.CourseCode, x.CourseName, x.Credits, x.IsActive))
            .ToListAsync(ct);

    public async Task<ServiceResult<CourseResponse>> GetByIdAsync(long id, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.CourseId == id, ct);

        return course is null
            ? ServiceResult<CourseResponse>.NotFound("Course không tồn tại")
            : ServiceResult<CourseResponse>.Success(ToResponse(course));
    }

    public async Task<ServiceResult<CourseResponse>> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var code = request.CourseCode.Trim().ToUpperInvariant();
        if (await db.Courses.AnyAsync(x => x.CourseCode == code, ct))
        {
            return ServiceResult<CourseResponse>.Conflict($"Course code đã tồn tại: {code}");
        }

        var course = new Course
        {
            CourseCode = code,
            CourseName = request.CourseName.Trim(),
            Credits = request.Credits,
            IsActive = request.IsActive
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct);

        return ServiceResult<CourseResponse>.Success(ToResponse(course));
    }

    public async Task<ServiceResult<CourseResponse>> UpdateAsync(long id, UpdateCourseRequest request, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.CourseId == id, ct);
        if (course is null)
        {
            return ServiceResult<CourseResponse>.NotFound("Course không tồn tại");
        }

        var code = request.CourseCode.Trim().ToUpperInvariant();
        if (await db.Courses.AnyAsync(x => x.CourseCode == code && x.CourseId != id, ct))
        {
            return ServiceResult<CourseResponse>.Conflict($"Course code đã tồn tại: {code}");
        }

        course.CourseCode = code;
        course.CourseName = request.CourseName.Trim();
        course.Credits = request.Credits;
        course.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return ServiceResult<CourseResponse>.Success(ToResponse(course));
    }

    public async Task<ServiceResult> DeleteAsync(long id, CancellationToken ct)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.CourseId == id, ct);
        if (course is null)
        {
            return ServiceResult.NotFound("Course không tồn tại");
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
