namespace Week1.Rbac.Api.Contracts.Responses;

public sealed record CourseResponse(
    long CourseId,
    string CourseCode,
    string CourseName,
    short Credits,
    bool IsActive);
