namespace MindNova.Api.Contracts;

public class AvailabilityBlockResponse
{
    public Guid Id { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public int? DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime? SpecificDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
