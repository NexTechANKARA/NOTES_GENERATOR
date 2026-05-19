using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNotes.Data;
using SmartNotes.Models;
using SmartNotes.Services;

namespace SmartNotes.Controllers;

[Authorize]
public class NotesController : Controller
{
    private readonly GroqNotesService _notesService;
    private readonly SmartNotesDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotesController(GroqNotesService notesService, SmartNotesDbContext db, UserManager<ApplicationUser> userManager)
    {
        _notesService = notesService;
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Generate()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Generate(NoteGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            ModelState.AddModelError("Topic", "Please enter a topic.");
            return View(request);
        }

        var result = await _notesService.GenerateNotesAsync(request);
        return View("Result", result);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveNoteRequest saveRequest)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var note = new SavedNote
        {
            UserId = user.Id,
            Topic = saveRequest.Topic,
            Subject = saveRequest.Subject,
            ExamType = saveRequest.ExamType,
            NotesContent = saveRequest.NotesContent,
            SummaryContent = saveRequest.SummaryContent,
            QuestionsContent = saveRequest.QuestionsContent,
            DiagramsContent = saveRequest.DiagramsContent,
            GeneratedAt = DateTime.UtcNow
        };

        _db.SavedNotes.Add(note);
        await _db.SaveChangesAsync();

        return Json(new { success = true, id = note.Id });
    }

    [HttpGet]
    public async Task<IActionResult> MyNotes()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var notes = await _db.SavedNotes
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.GeneratedAt)
            .ToListAsync();

        return View(notes);
    }

    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var note = await _db.SavedNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (note == null) return NotFound();

        note.LastAccessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return View("ViewNote", note);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var note = await _db.SavedNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (note == null) return NotFound();

        _db.SavedNotes.Remove(note);
        await _db.SaveChangesAsync();

        return RedirectToAction("MyNotes");
    }
}

public class SaveNoteRequest
{
    public string Topic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public string NotesContent { get; set; } = string.Empty;
    public string SummaryContent { get; set; } = string.Empty;
    public string QuestionsContent { get; set; } = string.Empty;
    public string DiagramsContent { get; set; } = string.Empty;
}
