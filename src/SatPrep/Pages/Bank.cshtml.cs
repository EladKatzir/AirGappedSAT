using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages;

/// <summary>Question bank browser: filter every question by topic, difficulty and personal status.</summary>
public class BankModel : PageModel
{
    private const int PageSize = 50;          // Rows per page
    private readonly AppDbContext _db;        // Database
    private readonly StatsService _stats;     // Latest results per question

    /// <param name="db">Database.</param>
    /// <param name="stats">Stats queries.</param>
    public BankModel(AppDbContext db, StatsService stats)
    {
        _db = db;
        _stats = stats;
    }

    [BindProperty(SupportsGet = true)] public int? Skill { get; set; }             // Filter: skill id
    [BindProperty(SupportsGet = true)] public Difficulty? Difficulty { get; set; } // Filter: difficulty
    [BindProperty(SupportsGet = true)] public string Status { get; set; } = "";    // "", unseen, correct, incorrect, marked
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }         // Text search in stem / passage
    [BindProperty(SupportsGet = true, Name = "p")] public int PageNo { get; set; } = 1; // 1-based page

    /// <summary>One row in the table.</summary>
    public record Row(int Id, string Skill, Difficulty Difficulty, string Preview, string Status);

    public List<Row> Rows { get; private set; } = new();                           // Current page
    public int Total { get; private set; }                                         // Matches before paging
    public List<Skill> Skills { get; private set; } = new();                       // Filter options
    public int Pages => Math.Max(1, (Total + PageSize - 1) / PageSize);            // Page count

    /// <summary>Applies filters and loads one page.</summary>
    public void OnGet()
    {
        int userId = User.UserId();
        Skills = _db.Skills.Include(s => s.Domain).ToList()
            .OrderBy(s => s.Domain!.Section).ThenBy(s => s.Domain!.SortOrder).ThenBy(s => s.SortOrder).ToList();

        var latest = _stats.LatestResults(userId);                                 // questionId -> last result
        var marked = _db.Marks.Where(m => m.UserId == userId).Select(m => m.QuestionId).ToHashSet();

        var q = _db.Questions.Include(x => x.Skill).Where(x => x.IsActive);
        if (Skill is int sid) q = q.Where(x => x.SkillId == sid);
        if (Difficulty is Difficulty d) q = q.Where(x => x.Difficulty == d);
        if (!string.IsNullOrWhiteSpace(Search)) q = q.Where(x => x.Stem.Contains(Search) || (x.Passage != null && x.Passage.Contains(Search)));

        var list = q.Select(x => new { x.Id, Skill = x.Skill!.Name, x.Difficulty, x.Stem, x.Passage }).ToList();

        // Status of a question for this learner
        string StatusOf(int id) => marked.Contains(id) && Status == "marked" ? "marked"
            : latest.TryGetValue(id, out var r) ? (r.Correct ? "correct" : "incorrect") : "unseen";

        var filtered = list.Where(x => Status switch
        {
            "unseen" => !latest.ContainsKey(x.Id),
            "correct" => latest.TryGetValue(x.Id, out var r) && r.Correct,
            "incorrect" => latest.TryGetValue(x.Id, out var r2) && !r2.Correct,
            "marked" => marked.Contains(x.Id),
            _ => true
        }).ToList();

        Total = filtered.Count;
        PageNo = Math.Clamp(PageNo, 1, Pages);
        Rows = filtered.Skip((PageNo - 1) * PageSize).Take(PageSize).Select(x =>
        {
            var text = Md.Preview(string.IsNullOrWhiteSpace(x.Passage) ? x.Stem : x.Passage + " — " + x.Stem, 140);
            return new Row(x.Id, x.Skill, x.Difficulty, text, StatusOf(x.Id));
        }).ToList();
    }

    /// <summary>Builds a URL for another page with the same filters.</summary>
    /// <param name="page">Target page number.</param>
    public string PageUrl(int page)
        => $"/Bank?skill={Skill}&difficulty={Difficulty}&status={Status}&search={Uri.EscapeDataString(Search ?? "")}&p={page}";
}
