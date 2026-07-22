namespace MindNova.Domain.Entities;

public class CalendarEntry
{
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
}
