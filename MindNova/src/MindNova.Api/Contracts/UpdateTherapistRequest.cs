namespace MindNova.Api.Contracts;

public class UpdateTherapistRequest
{
    public List<string> Specialisations { get; set; } = new();
    public int MaxCaseload { get; set; }
}
