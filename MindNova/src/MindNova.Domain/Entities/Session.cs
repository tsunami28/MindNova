namespace MindNova.Domain.Entities;

public class Session
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public SessionType SessionType { get; set; }
    public SessionStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
