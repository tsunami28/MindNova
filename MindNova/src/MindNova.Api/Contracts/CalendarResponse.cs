namespace MindNova.Api.Contracts;

public class CalendarResponse
{
    public List<CalendarEntryResponse> Items { get; set; } = new();
    public Guid TherapistProfileId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}
