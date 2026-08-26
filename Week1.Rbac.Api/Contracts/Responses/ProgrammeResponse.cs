namespace Week1.Rbac.Api.Contracts.Responses;

public sealed record ProgrammeResponse(
    long ProgrammeId,
    string ProgrammeCode,
    string ProgrammeName,
    string DegreeLevel,
    short DurationYears,
    int StudentCount);
