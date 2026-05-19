namespace SmartNotes.Models;

public class SavedNote
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;

    public string NotesContent { get; set; } = string.Empty;
    public string SummaryContent { get; set; } = string.Empty;
    public string QuestionsContent { get; set; } = string.Empty;
    public string DiagramsContent { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
}
