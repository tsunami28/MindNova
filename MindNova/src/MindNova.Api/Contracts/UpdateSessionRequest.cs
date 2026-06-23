namespace MindNova.Api.Contracts;

public class UpdateSessionRequest
{
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
