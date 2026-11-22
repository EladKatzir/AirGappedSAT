using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Pages.Shared;
using SatPrep.Services;

namespace SatPrep.Pages.Tests;

/// <summary>
/// Results of a completed test: scores, per-domain accuracy, pacing, and a
/// full question-by-question review with explanations.
/// </summary>
public class ResultsModel : PageModel
{
    /// <summary>Average seconds per question on the real test (R&amp;W: 64 min / 54, Math: 70 min / 44).</summary>
    public static readonly IReadOnlyDictionary<SatSection, int> PaceSeconds =
        new Dictionary<SatSection, int> { [SatSection.ReadingWriting] = 71, [SatSection.Math] = 95 };

    private readonly AppDbContext _db; // Database

    /// <param name="db">Database.</param>
    public ResultsModel(AppDbContext db) => _db = db;

    /// <summary>Per-domain summary row.</summary>
    public record DomainRow(string Domain, SatSection Section, int Correct, int Total, double AvgSeconds);

    /// <summary>One reviewed question.</summary>
    public record ReviewRow(int Module, int Order, TestItem Item, QuestionCardModel Card, SatSection Section);

    public TestAttempt Test { get; private set; } = null!;                // The test
    public List<DomainRow> Domains { get; private set; } = new();         // Per-domain table
    public List<ReviewRow> Review { get; private set; } = new();          // All questions in order
    public IReadOnlyList<ModuleSpec> Plan { get; private set; } = null!;  // Module plan (for labels)
    public int Correct { get; private set; }                              // Total correct
    public int Total { get; private set; }                                // Total questions
    [BindProperty(SupportsGet = true)] public bool WrongOnly { get; set; } // Review filter

    /// <summary>Loads a completed test owned by the learner.</summary>
    /// <param name="id">Test id.</param>
    public IActionResult OnGet(int id)
    {
        var t = _db.Tests.FirstOrDefault(x => x.Id == id && x.UserId == User.UserId());
        if (t is null) return NotFound();
        if (t.Status != TestStatus.Completed) return Redirect($"/Tests/Take/{id}");
        Test = t;
        Plan = TestService.ModulePlan(t.Type);

        var items = _db.TestItems.Include(i => i.Question).ThenInclude(q => q!.Skill).ThenInclude(s => s!.Domain)
            .Where(i => i.TestAttemptId == id).OrderBy(i => i.ModuleIndex).ThenBy(i => i.Order).ToList();
        Correct = items.Count(i => i.IsCorrect);
        Total = items.Count;

        Domains = items.GroupBy(i => (i.Question!.Skill!.Domain!.Name, i.Question.Skill.Domain.Section, i.Question.Skill.Domain.SortOrder))
            .OrderBy(g => g.Key.Section).ThenBy(g => g.Key.SortOrder)
            .Select(g => new DomainRow(g.Key.Name, g.Key.Section, g.Count(i => i.IsCorrect), g.Count(), g.Average(i => i.SecondsSpent)))
            .ToList();

        Review = items.Where(i => !WrongOnly || !i.IsCorrect).Select(i => new ReviewRow(i.ModuleIndex, i.Order, i,
            new QuestionCardModel
            {
                Question = i.Question!,
                Answered = true,
                IsCorrect = i.IsCorrect,
                Given = i.GivenAnswer,
                ShowMarkButton = false
            }, Plan[i.ModuleIndex].Section)).ToList();
        return Page();
    }

    /// <summary>Per-question timing data for the chart: (label, seconds, correct, pace).</summary>
    public object ChartData()
        => _db.TestItems.Include(i => i.Question).ThenInclude(q => q!.Skill).ThenInclude(s => s!.Domain)
            .Where(i => i.TestAttemptId == Test.Id).OrderBy(i => i.ModuleIndex).ThenBy(i => i.Order).ToList()
            .Select(i => new
            {
                label = $"M{i.ModuleIndex + 1}·{i.Order}",
                seconds = i.SecondsSpent,
                correct = i.IsCorrect,
                pace = PaceSeconds[Plan[i.ModuleIndex].Section]
            });
}
