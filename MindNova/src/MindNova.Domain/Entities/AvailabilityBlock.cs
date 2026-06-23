namespace MindNova.Domain.Entities;

public class AvailabilityBlock
{
    public Guid Id { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime? SpecificDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
