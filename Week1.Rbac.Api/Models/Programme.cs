namespace Week1.Rbac.Api.Models;

public sealed class Programme
{
    public long ProgrammeId { get; set; }
    public string ProgrammeCode { get; set; } = string.Empty;
    public string ProgrammeName { get; set; } = string.Empty;
    public string DegreeLevel { get; set; } = string.Empty;
    public short DurationYears { get; set; }

    public ICollection<Student> Students { get; set; } = [];
}
