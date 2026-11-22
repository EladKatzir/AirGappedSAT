using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;

namespace SatPrep.Pages;

/// <summary>
/// Error log: every question whose latest answer was wrong, with filters,
/// retry links, and the spaced-review schedule.
/// </summary>
public class MistakesModel : PageModel
{
    private readonly AppDbContext _db; // Database

    /// <param name="db">Database.</param>
    public MistakesModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public int? Domain { get; set; }             // Filter: domain id
    [BindProperty(SupportsGet = true)] public Difficulty? Difficulty { get; set; }  // Filter: difficulty

    /// <summary>One row in the mistakes table.</summary>
    public record Row(int QuestionId, string Skill, string Domain, Difficulty Difficulty, string Preview,
        string Given, DateTime When, int TimesWrong, DateTime? ReviewDue);

    public List<Row> Rows { get; private set; } = new();                            // Filtered rows
    public List<Domain> Domains { get; private set; } = new();                      // Filter options
    public int DueNow { get; private set; }                                         // Reviews due now

    /// <summary>Loads missed questions (latest attempt wrong).</summary>
    public void OnGet()
    {
        int userId = User.UserId();
        Domains = _db.Domains.OrderBy(d => d.Section).ThenBy(d => d.SortOrder).ToList();

        // All attempts for the learner, newest first, reduced to latest per question in memory
        var attempts = _db.Attempts.Where(a => a.UserId == userId)
            .Select(a => new { a.QuestionId, a.IsCorrect, a.GivenAnswer, a.AnsweredUtc }).ToList();
        var latestWrong = attempts.GroupBy(a => a.QuestionId)
            .Select(g => new { Latest = g.OrderByDescending(a => a.AnsweredUtc).First(), Wrong = g.Count(a => !a.IsCorrect) })
            .Where(x => !x.Latest.IsCorrect).ToDictionary(x => x.Latest.QuestionId);

        var ids = latestWrong.Keys.ToList();
        var questions = _db.Questions.Include(q => q.Skill).ThenInclude(s => s!.Domain)
            .Where(q => ids.Contains(q.Id) && q.IsActive).ToList();
        if (Domain is int did) questions = questions.Where(q => q.Skill!.DomainId == did).ToList();
        if (Difficulty is Difficulty d) questions = questions.Where(q => q.Difficulty == d).ToList();

        var reviews = _db.Reviews.Where(r => r.UserId == userId && ids.Contains(r.QuestionId) && !r.IsRetired)
            .ToDictionary(r => r.QuestionId, r => r.DueUtc);
        var now = DateTime.UtcNow;
        DueNow = reviews.Count(r => r.Value <= now);

        Rows = questions.Select(q =>
        {
            var lw = latestWrong[q.Id];
            var preview = (q.Passage ?? q.Stem).Replace("\n", " ");
            if (preview.Length > 110) preview = preview[..110] + "…";
            return new Row(q.Id, q.Skill!.Name, q.Skill.Domain!.Name, q.Difficulty, preview,
                string.IsNullOrEmpty(lw.Latest.GivenAnswer) ? "blank" : lw.Latest.GivenAnswer,
                lw.Latest.AnsweredUtc, lw.Wrong, reviews.TryGetValue(q.Id, out var due) ? due : null);
        }).OrderByDescending(r => r.When).ToList();
    }
}
