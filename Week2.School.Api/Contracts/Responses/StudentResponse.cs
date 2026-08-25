namespace Week2.School.Api.Contracts.Responses;

public sealed record StudentResponse(
    long StudentId,
    long ProgrammeId,
    string ProgrammeCode,
    string ProgrammeName,
    string StudentCode,
    string FullName,
    string Email,
    DateOnly? DateOfBirth,
    int YearOfEntry,
    string Status);
