namespace Week2.School.Api.Contracts.Responses;

public sealed record CourseResponse(
    long CourseId,
    string CourseCode,
    string CourseName,
    short Credits,
    bool IsActive);
