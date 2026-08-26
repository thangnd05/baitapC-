using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public sealed class StudentService(AppDbContext db) : IStudentService
{
    internal static readonly Expression<Func<Student, StudentResponse>> ToResponse =
        x => new StudentResponse(
            x.StudentId,
            x.ProgrammeId,
            x.Programme.ProgrammeCode,
            x.Programme.ProgrammeName,
            x.StudentCode,
            x.FullName,
            x.Email,
            x.DateOfBirth,
            x.YearOfEntry,
            x.Status);

    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken ct) =>
        await db.Students.AsNoTracking()
            .OrderBy(x => x.StudentCode)
            .Select(ToResponse)
            .ToListAsync(ct);

    public async Task<ServiceResult<StudentResponse>> GetByIdAsync(long id, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking()
            .Where(x => x.StudentId == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync(ct);

        return student is null
            ? ServiceResult<StudentResponse>.NotFound("Student không tồn tại")
            : ServiceResult<StudentResponse>.Success(student);
    }

    public async Task<ServiceResult<StudentResponse>> CreateAsync(CreateStudentRequest request, CancellationToken ct)
    {
        if (!await db.Programmes.AnyAsync(x => x.ProgrammeId == request.ProgrammeId, ct))
        {
            return ServiceResult<StudentResponse>.NotFound($"Programme không tồn tại: {request.ProgrammeId}");
        }

        var code = request.StudentCode.Trim().ToUpperInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Students.AnyAsync(x => x.StudentCode == code, ct))
        {
            return ServiceResult<StudentResponse>.Conflict($"Student code đã tồn tại: {code}");
        }

        if (await db.Students.AnyAsync(x => x.Email == email, ct))
        {
            return ServiceResult<StudentResponse>.Conflict($"Email đã tồn tại: {email}");
        }

        var student = new Student
        {
            ProgrammeId = request.ProgrammeId,
            StudentCode = code,
            FullName = request.FullName.Trim(),
            Email = email,
            DateOfBirth = request.DateOfBirth,
            YearOfEntry = request.YearOfEntry,
            Status = request.Status.Trim().ToUpperInvariant()
        };

        db.Students.Add(student);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(student.StudentId, ct);
    }

    public async Task<ServiceResult<StudentResponse>> UpdateAsync(long id, UpdateStudentRequest request, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.StudentId == id, ct);
        if (student is null)
        {
            return ServiceResult<StudentResponse>.NotFound("Student không tồn tại");
        }

        if (!await db.Programmes.AnyAsync(x => x.ProgrammeId == request.ProgrammeId, ct))
        {
            return ServiceResult<StudentResponse>.NotFound($"Programme không tồn tại: {request.ProgrammeId}");
        }

        var code = request.StudentCode.Trim().ToUpperInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Students.AnyAsync(x => x.StudentCode == code && x.StudentId != id, ct))
        {
            return ServiceResult<StudentResponse>.Conflict($"Student code đã tồn tại: {code}");
        }

        if (await db.Students.AnyAsync(x => x.Email == email && x.StudentId != id, ct))
        {
            return ServiceResult<StudentResponse>.Conflict($"Email đã tồn tại: {email}");
        }

        student.ProgrammeId = request.ProgrammeId;
        student.StudentCode = code;
        student.FullName = request.FullName.Trim();
        student.Email = email;
        student.DateOfBirth = request.DateOfBirth;
        student.YearOfEntry = request.YearOfEntry;
        student.Status = request.Status.Trim().ToUpperInvariant();
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ServiceResult> DeleteAsync(long id, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.StudentId == id, ct);
        if (student is null)
        {
            return ServiceResult.NotFound("Student không tồn tại");
        }

        db.Students.Remove(student);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
