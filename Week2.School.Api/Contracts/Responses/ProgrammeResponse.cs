namespace Week2.School.Api.Contracts.Responses;

public sealed record ProgrammeResponse(
    long ProgrammeId,
    string ProgrammeCode,
    string ProgrammeName,
    string DegreeLevel,
    short DurationYears,
    int StudentCount);
