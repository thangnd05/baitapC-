using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record CreateProgrammeRequest(
    [Required, MaxLength(20)] string ProgrammeCode,
    [Required, MaxLength(150)] string ProgrammeName,
    [Required, MaxLength(30)] string DegreeLevel,
    [Range(1, 8)] short DurationYears);

public sealed record UpdateProgrammeRequest(
    [Required, MaxLength(20)] string ProgrammeCode,
    [Required, MaxLength(150)] string ProgrammeName,
    [Required, MaxLength(30)] string DegreeLevel,
    [Range(1, 8)] short DurationYears);
