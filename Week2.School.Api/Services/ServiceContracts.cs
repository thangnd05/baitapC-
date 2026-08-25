using Week2.School.Api.Contracts.Requests;
using Week2.School.Api.Contracts.Responses;

namespace Week2.School.Api.Services;

public interface IProgrammeService
{
    Task<IReadOnlyList<ProgrammeResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<ProgrammeResponse>> GetByIdAsync(long id, CancellationToken ct);
    Task<ServiceResult<ProgrammeResponse>> CreateAsync(CreateProgrammeRequest request, CancellationToken ct);
    Task<ServiceResult<ProgrammeResponse>> UpdateAsync(long id, UpdateProgrammeRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(long id, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<StudentResponse>>> GetStudentsAsync(long id, CancellationToken ct);
}

public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<CourseResponse>> GetByIdAsync(long id, CancellationToken ct);
    Task<ServiceResult<CourseResponse>> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<ServiceResult<CourseResponse>> UpdateAsync(long id, UpdateCourseRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(long id, CancellationToken ct);
}

public interface IStudentService
{
    Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken ct);
    Task<ServiceResult<StudentResponse>> GetByIdAsync(long id, CancellationToken ct);
    Task<ServiceResult<StudentResponse>> CreateAsync(CreateStudentRequest request, CancellationToken ct);
    Task<ServiceResult<StudentResponse>> UpdateAsync(long id, UpdateStudentRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(long id, CancellationToken ct);
}
