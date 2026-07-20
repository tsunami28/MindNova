namespace MindNova.Api.Contracts;

public class TherapistProfileResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> Specialisations { get; set; } = new();
    public int MaxCaseload { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
