using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartNotes.Models;

namespace SmartNotes.Data;

public class SmartNotesDbContext : IdentityDbContext<ApplicationUser>
{
    public SmartNotesDbContext(DbContextOptions<SmartNotesDbContext> options) : base(options) { }

    public DbSet<SavedNote> SavedNotes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SavedNote>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany(u => u.SavedNotes)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.Property(n => n.NotesContent).HasColumnType("TEXT");
            e.Property(n => n.SummaryContent).HasColumnType("TEXT");
            e.Property(n => n.QuestionsContent).HasColumnType("TEXT");
            e.Property(n => n.DiagramsContent).HasColumnType("TEXT");
        });
    }
}
