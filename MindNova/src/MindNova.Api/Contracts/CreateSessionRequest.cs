namespace MindNova.Api.Contracts;

public class CreateSessionRequest
{
    public Guid ClientId { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
