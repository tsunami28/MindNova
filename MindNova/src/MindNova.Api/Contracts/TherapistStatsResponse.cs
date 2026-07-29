namespace MindNova.Api.Contracts;

public class TherapistStatsResponse
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<TherapistStatEntryResponse> Items { get; set; } = new();
}

public class TherapistStatEntryResponse
{
    public string TherapistUserId { get; set; } = string.Empty;
    public string TherapistName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
    public int CancelledCount { get; set; }
    public int AvailableSlotCount { get; set; }
    public double UtilisationRate { get; set; }
}
