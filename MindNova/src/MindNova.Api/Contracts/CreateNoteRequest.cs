namespace MindNova.Api.Contracts;

public class CreateNoteRequest
{
    public string PresentingIssue { get; set; } = string.Empty;
    public string Interventions { get; set; } = string.Empty;
    public string Homework { get; set; } = string.Empty;
    public int ProgressRating { get; set; }
    public string FreeText { get; set; } = string.Empty;
}
