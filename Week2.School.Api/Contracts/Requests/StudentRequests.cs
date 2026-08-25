using System.ComponentModel.DataAnnotations;

namespace Week2.School.Api.Contracts.Requests;

public sealed record CreateStudentRequest(
    [Range(1, long.MaxValue)] long ProgrammeId,
    [Required, MaxLength(20)] string StudentCode,
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    DateOnly? DateOfBirth,
    [Range(2000, 2100)] int YearOfEntry,
    [Required, RegularExpression("^(ACTIVE|SUSPENDED|GRADUATED)$")] string Status = "ACTIVE");

public sealed record UpdateStudentRequest(
    [Range(1, long.MaxValue)] long ProgrammeId,
    [Required, MaxLength(20)] string StudentCode,
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    DateOnly? DateOfBirth,
    [Range(2000, 2100)] int YearOfEntry,
    [Required, RegularExpression("^(ACTIVE|SUSPENDED|GRADUATED)$")] string Status = "ACTIVE");
