namespace MindNova.Api.Contracts;

public class TherapistUtilisationEntry
{
    public string TherapistUserId { get; set; } = string.Empty;
    public int SessionCount { get; set; }
}
