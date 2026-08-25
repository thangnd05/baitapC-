namespace Week2.School.Api.Models;

public sealed class Course
{
    public long CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public short Credits { get; set; }
    public bool IsActive { get; set; } = true;
}
