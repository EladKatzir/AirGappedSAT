using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Services;

namespace SatPrep.Pages.Study;

/// <summary>Study hub: lessons per domain (with mastery) and vocabulary progress.</summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;         // Database
    private readonly StatsService _stats;      // Skill progress
    private readonly LessonService _lessons;   // Lesson files

    /// <param name="db">Database.</param>
    /// <param name="stats">Stats queries.</param>
    /// <param name="lessons">Lesson loader.</param>
    public IndexModel(AppDbContext db, StatsService stats, LessonService lessons)
    {
        _db = db;
        _stats = stats;
        _lessons = lessons;
    }

    /// <summary>One lesson card.</summary>
    public record LessonCard(string Code, string Title, string Section, int Skills, int Avg, bool Started, bool Exists);

    public List<LessonCard> Lessons { get; private set; } = new();   // One per domain
    public int VocabTotal { get; private set; }                       // Words in the bank
    public int VocabKnown { get; private set; }                       // Words in the top Leitner box
    public int VocabDue { get; private set; }                         // Cards due now (incl. new)

    /// <summary>Loads lesson cards and vocab counts.</summary>
    public void OnGet()
    {
        int userId = User.UserId();
        var skills = _stats.SkillProgress(userId);
        Lessons = skills.GroupBy(s => (s.DomainCode, s.DomainName, s.Section)).Select(g =>
        {
            var practised = g.Where(s => s.Attempts > 0).ToList();
            return new LessonCard(g.Key.DomainCode, g.Key.DomainName,
                g.Key.Section == Models.SatSection.Math ? "Math" : "Reading and Writing", g.Count(),
                practised.Any() ? (int)practised.Average(s => s.Percent) : 0, practised.Any(), _lessons.Get(g.Key.DomainCode) is not null);
        }).ToList();

        var now = DateTime.UtcNow;
        VocabTotal = _db.VocabWords.Count();
        VocabKnown = _db.VocabProgress.Count(p => p.UserId == userId && p.Box >= 4);
        int seen = _db.VocabProgress.Count(p => p.UserId == userId);
        VocabDue = _db.VocabProgress.Count(p => p.UserId == userId && p.NextDueUtc <= now) + (VocabTotal - seen);
    }
}
