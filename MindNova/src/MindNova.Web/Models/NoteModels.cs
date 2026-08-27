namespace MindNova.Web.Models;

public class NoteModel
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
}

public class CreateNoteModel
{
    public string PresentingIssue { get; set; } = string.Empty;
    public string Interventions { get; set; } = string.Empty;
    public string Homework { get; set; } = string.Empty;
    public int ProgressRating { get; set; } = 5;
    public string FreeText { get; set; } = string.Empty;
}

public class UpdateNoteModel
{
    public string PresentingIssue { get; set; } = string.Empty;
    public string Interventions { get; set; } = string.Empty;
    public string Homework { get; set; } = string.Empty;
    public int ProgressRating { get; set; }
    public string FreeText { get; set; } = string.Empty;
}
