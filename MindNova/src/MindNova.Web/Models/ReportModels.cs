namespace MindNova.Web.Models;

public class PracticeStatsModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    public double NoShowRate { get; set; }
    public int NewClientsCount { get; set; }
    public List<TherapistUtilisationModel> TherapistUtilisation { get; set; } = new();
}

public class TherapistUtilisationModel
{
    public string TherapistUserId { get; set; } = string.Empty;
    public int SessionCount { get; set; }
}

public class TherapistStatsModel
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<TherapistStatEntryModel> Items { get; set; } = new();
}

public class TherapistStatEntryModel
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
