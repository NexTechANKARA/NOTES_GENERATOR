using Microsoft.AspNetCore.Identity;

namespace SmartNotes.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SavedNote> SavedNotes { get; set; } = new List<SavedNote>();
}
