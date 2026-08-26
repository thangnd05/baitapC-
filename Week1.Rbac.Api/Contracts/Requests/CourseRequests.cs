using System.ComponentModel.DataAnnotations;

namespace Week1.Rbac.Api.Contracts.Requests;

public sealed record CreateCourseRequest(
    [Required, MaxLength(20)] string CourseCode,
    [Required, MaxLength(150)] string CourseName,
    [Range(1, 10)] short Credits,
    bool IsActive = true);

public sealed record UpdateCourseRequest(
    [Required, MaxLength(20)] string CourseCode,
    [Required, MaxLength(150)] string CourseName,
    [Range(1, 10)] short Credits,
    bool IsActive = true);
