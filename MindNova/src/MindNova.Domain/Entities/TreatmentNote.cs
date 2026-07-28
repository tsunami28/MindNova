namespace MindNova.Domain.Entities;

public class TreatmentNote
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string TherapistUserId { get; set; } = string.Empty;
    public string PresentingIssue { get; set; } = string.Empty;
    public string Interventions { get; set; } = string.Empty;
    public string Homework { get; set; } = string.Empty;
    public int ProgressRating { get; set; }
    public string FreeText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedByUserId { get; set; }
}
