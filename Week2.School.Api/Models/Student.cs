namespace Week2.School.Api.Models;

public sealed class Student
{
    public long StudentId { get; set; }
    public long ProgrammeId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public int YearOfEntry { get; set; }
    public string Status { get; set; } = StudentStatus.Active;

    public Programme Programme { get; set; } = null!;
}

public static class StudentStatus
{
    public const string Active = "ACTIVE";
    public const string Suspended = "SUSPENDED";
    public const string Graduated = "GRADUATED";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>([Active, Suspended, Graduated]);
}
