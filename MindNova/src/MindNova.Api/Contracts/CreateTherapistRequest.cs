namespace MindNova.Api.Contracts;

public class CreateTherapistRequest
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Specialisations { get; set; } = new();
    public int MaxCaseload { get; set; }
}
