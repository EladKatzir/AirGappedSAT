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
    public DbSet<QuestionMark> Marks => Set<QuestionMark>();           // Bookmarked questions
    public DbSet<ReviewItem> Reviews => Set<ReviewItem>();             // Spaced-repetition queue
    public DbSet<TestAttempt> Tests => Set<TestAttempt>();             // Practice tests
    public DbSet<TestItem> TestItems => Set<TestItem>();               // Questions inside tests
    public DbSet<VocabWord> VocabWords => Set<VocabWord>();            // Vocabulary bank
    public DbSet<VocabProgress> VocabProgress => Set<VocabProgress>(); // Flashcard progress
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();            // Study plan settings

    /// <summary>Indexes and constraints that matter for correctness and speed.</summary>
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Domain>().HasIndex(d => d.Code).IsUnique();                       // Codes are stable keys
        b.Entity<Skill>().HasIndex(s => s.Code).IsUnique();                        // Codes are stable keys
        b.Entity<Question>().HasIndex(q => q.ExternalId).IsUnique();               // Re-import updates, never duplicates
        b.Entity<Question>().HasIndex(q => new { q.SkillId, q.Difficulty });       // Fast "skill + difficulty" filtering
        b.Entity<AppUser>().HasIndex(u => u.UserName).IsUnique();                  // One learner per name
        b.Entity<AppUser>().Property(u => u.UserName).UseCollation("NOCASE");     // "Elad" and "elad" are the same learner
        b.Entity<QuestionAttempt>().HasIndex(a => new { a.UserId, a.QuestionId }); // Error log / "already seen" lookups
        b.Entity<QuestionAttempt>().HasIndex(a => new { a.UserId, a.AnsweredUtc });// Activity / analytics by date
        b.Entity<SkillMastery>().HasIndex(m => new { m.UserId, m.SkillId }).IsUnique(); // Exactly one row per user+skill
        b.Entity<QuestionMark>().HasIndex(m => new { m.UserId, m.QuestionId }).IsUnique(); // One mark per question
        b.Entity<ReviewItem>().HasIndex(r => new { r.UserId, r.QuestionId }).IsUnique();   // One queue entry per question
        b.Entity<ReviewItem>().HasIndex(r => new { r.UserId, r.DueUtc });           // "What is due" lookups
        b.Entity<TestAttempt>().HasIndex(t => new { t.UserId, t.StartedUtc });      // Test history
        b.Entity<TestItem>().HasIndex(i => new { i.TestAttemptId, i.ModuleIndex, i.Order }).IsUnique(); // Slot uniqueness
        b.Entity<VocabWord>().HasIndex(w => w.Word).IsUnique();                     // Re-import updates
        b.Entity<VocabProgress>().HasIndex(p => new { p.UserId, p.VocabWordId }).IsUnique(); // One card per user+word
        b.Entity<StudyPlan>().HasIndex(p => p.UserId).IsUnique();                   // One plan per user
    }
}
