namespace MindNova.Web.Models;

public class TherapistModel
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> Specialisations { get; set; } = new();
    public int MaxCaseload { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTherapistModel
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Specialisations { get; set; } = new() { "CBT" };
    public int MaxCaseload { get; set; } = 10;
}

public class UpdateTherapistModel
{
    public List<string> Specialisations { get; set; } = new();
    public int MaxCaseload { get; set; }
}

public class AvailabilitySlotModel
{
    public Guid Id { get; set; }
    public Guid TherapistProfileId { get; set; }
    public int? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSlotModel
{
    public int? DayOfWeek { get; set; }
    public DateTime? SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public bool IsRecurring { get; set; } = true;
}

public class CaseloadEntry
{
    public string TherapistUserId { get; set; } = string.Empty;
    public int SessionCount { get; set; }
}
