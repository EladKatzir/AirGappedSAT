using Microsoft.EntityFrameworkCore;
using SatPrep.Models;

namespace SatPrep.Data;

/// <summary>
/// EF Core database context. Uses a single SQLite file (same approach as
/// ShiftManager) so the whole app is one folder you can copy by USB.
/// </summary>
public class AppDbContext : DbContext
{
    /// <param name="options">Connection options supplied by Program.cs (SQLite file path).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Domain> Domains => Set<Domain>();                     // 8 content domains
    public DbSet<Skill> Skills => Set<Skill>();                        // 29 skills
    public DbSet<Question> Questions => Set<Question>();               // Question bank
    public DbSet<AppUser> Users => Set<AppUser>();                     // Learner accounts
    public DbSet<QuestionAttempt> Attempts => Set<QuestionAttempt>();  // Every answer ever submitted
    public DbSet<SkillMastery> Masteries => Set<SkillMastery>();       // Per-user per-skill mastery state

    /// <summary>Indexes and constraints that matter for correctness and speed.</summary>
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Domain>().HasIndex(d => d.Code).IsUnique();                       // Codes are stable keys
        b.Entity<Skill>().HasIndex(s => s.Code).IsUnique();                        // Codes are stable keys
        b.Entity<Question>().HasIndex(q => q.ExternalId).IsUnique();               // Re-import updates, never duplicates
        b.Entity<Question>().HasIndex(q => new { q.SkillId, q.Difficulty });       // Fast "skill + difficulty" filtering
        b.Entity<AppUser>().HasIndex(u => u.UserName).IsUnique();                  // One account per login name
        b.Entity<QuestionAttempt>().HasIndex(a => new { a.UserId, a.QuestionId }); // Error log / "already seen" lookups
        b.Entity<SkillMastery>().HasIndex(m => new { m.UserId, m.SkillId }).IsUnique(); // Exactly one row per user+skill
    }
}
