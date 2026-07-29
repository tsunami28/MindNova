namespace MindNova.Api.Contracts;

public class PracticeStatsResponse
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    public double NoShowRate { get; set; }
    public int NewClientsCount { get; set; }
    public List<TherapistUtilisationEntry> TherapistUtilisation { get; set; } = new();
}
