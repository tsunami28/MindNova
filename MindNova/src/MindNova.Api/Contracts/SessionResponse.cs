namespace MindNova.Api.Contracts;

public class SessionResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
