namespace MindNova.Domain.Entities;

public class TimelineEvent
{
    public DateTime Date { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
}
