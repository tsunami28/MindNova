namespace MindNova.Api.Contracts;

public class CaseloadSummaryResponse
{
    public Guid TherapistProfileId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int MaxCaseload { get; set; }
    public int CurrentCaseload { get; set; }
    public int AvailableCapacity { get; set; }
}
