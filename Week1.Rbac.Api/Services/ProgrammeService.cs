using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Week1.Rbac.Api.Contracts.Requests;
using Week1.Rbac.Api.Contracts.Responses;
using Week1.Rbac.Api.Data;
using Week1.Rbac.Api.Models;

namespace Week1.Rbac.Api.Services;

public sealed class ProgrammeService(AppDbContext db) : IProgrammeService
{
    private static readonly Expression<Func<Programme, ProgrammeResponse>> ToResponse =
        x => new ProgrammeResponse(
            x.ProgrammeId,
            x.ProgrammeCode,
            x.ProgrammeName,
            x.DegreeLevel,
            x.DurationYears,
            x.Students.Count);

    public async Task<IReadOnlyList<ProgrammeResponse>> GetAllAsync(CancellationToken ct) =>
        await db.Programmes.AsNoTracking()
            .OrderBy(x => x.ProgrammeCode)
            .Select(ToResponse)
            .ToListAsync(ct);

    public async Task<ServiceResult<ProgrammeResponse>> GetByIdAsync(long id, CancellationToken ct)
    {
        var programme = await db.Programmes.AsNoTracking()
            .Where(x => x.ProgrammeId == id)
            .Select(ToResponse)
            .SingleOrDefaultAsync(ct);

        return programme is null
            ? ServiceResult<ProgrammeResponse>.NotFound("Programme không tồn tại")
            : ServiceResult<ProgrammeResponse>.Success(programme);
    }

    public async Task<ServiceResult<ProgrammeResponse>> CreateAsync(CreateProgrammeRequest request, CancellationToken ct)
    {
        var code = request.ProgrammeCode.Trim().ToUpperInvariant();
        if (await db.Programmes.AnyAsync(x => x.ProgrammeCode == code, ct))
        {
            return ServiceResult<ProgrammeResponse>.Conflict($"Programme code đã tồn tại: {code}");
        }

        var programme = new Programme
        {
            ProgrammeCode = code,
            ProgrammeName = request.ProgrammeName.Trim(),
            DegreeLevel = request.DegreeLevel.Trim().ToUpperInvariant(),
            DurationYears = request.DurationYears
        };

        db.Programmes.Add(programme);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(programme.ProgrammeId, ct);
    }

    public async Task<ServiceResult<ProgrammeResponse>> UpdateAsync(long id, UpdateProgrammeRequest request, CancellationToken ct)
    {
        var programme = await db.Programmes.FirstOrDefaultAsync(x => x.ProgrammeId == id, ct);
        if (programme is null)
        {
            return ServiceResult<ProgrammeResponse>.NotFound("Programme không tồn tại");
        }

        var code = request.ProgrammeCode.Trim().ToUpperInvariant();
        if (await db.Programmes.AnyAsync(x => x.ProgrammeCode == code && x.ProgrammeId != id, ct))
        {
            return ServiceResult<ProgrammeResponse>.Conflict($"Programme code đã tồn tại: {code}");
        }

        programme.ProgrammeCode = code;
        programme.ProgrammeName = request.ProgrammeName.Trim();
        programme.DegreeLevel = request.DegreeLevel.Trim().ToUpperInvariant();
        programme.DurationYears = request.DurationYears;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task<ServiceResult> DeleteAsync(long id, CancellationToken ct)
    {
        var programme = await db.Programmes.FirstOrDefaultAsync(x => x.ProgrammeId == id, ct);
        if (programme is null)
        {
            return ServiceResult.NotFound("Programme không tồn tại");
        }

        if (await db.Students.AnyAsync(x => x.ProgrammeId == id, ct))
        {
            return ServiceResult.Conflict("Không xóa được programme khi vẫn còn student thuộc programme này");
        }

        db.Programmes.Remove(programme);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IReadOnlyList<StudentResponse>>> GetStudentsAsync(long id, CancellationToken ct)
    {
        if (!await db.Programmes.AnyAsync(x => x.ProgrammeId == id, ct))
        {
            return ServiceResult<IReadOnlyList<StudentResponse>>.NotFound("Programme không tồn tại");
        }

        var students = await db.Students.AsNoTracking()
            .Where(x => x.ProgrammeId == id)
            .OrderBy(x => x.StudentCode)
            .Select(StudentService.ToResponse)
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<StudentResponse>>.Success(students);
    }
}
