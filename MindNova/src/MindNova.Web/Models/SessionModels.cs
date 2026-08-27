namespace MindNova.Web.Models;

public class SessionModel
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

public class CreateSessionModel
{
    public Guid ClientId { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 50;
    public string SessionType { get; set; } = "Individual";
    public string Notes { get; set; } = string.Empty;
}

public class UpdateSessionModel
{
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CalendarEntryModel
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
}

public class CalendarResponseModel
{
    public List<CalendarEntryModel> Items { get; set; } = new();
    public Guid TherapistProfileId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}
