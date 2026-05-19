namespace SmartNotes.Models;

public class NoteGenerationRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ExamType { get; set; } = "Engineering";
    public string Difficulty { get; set; } = "Intermediate";
}

public class NoteGenerationResult
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public string NotesContent { get; set; } = string.Empty;
    public string SummaryContent { get; set; } = string.Empty;
    public string QuestionsContent { get; set; } = string.Empty;
    public string DiagramsContent { get; set; } = string.Empty;
    public List<string> Subtopics { get; set; } = new();
    public bool Success { get; set; }
    public string? Error { get; set; }
}
