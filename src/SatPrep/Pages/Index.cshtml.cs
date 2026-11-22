using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Pages;

/// <summary>
/// Home page: shows the full SAT skill map with the number of questions
/// available per skill. Becomes the "skill mastery" dashboard once login
/// is added (mastery columns are already wired through MasteryService).
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db; // Database access

    /// <param name="db">Database context.</param>
    public IndexModel(AppDbContext db) => _db = db;

    /// <summary>One row in the skill table.</summary>
    public record SkillRow(string Code, string Name, int Easy, int Medium, int Hard);

    /// <summary>One domain with its skills.</summary>
    public record DomainRow(string Name, SatSection Section, List<SkillRow> Skills);

    public List<DomainRow> Domains { get; private set; } = new(); // Data rendered by the view
    public int TotalQuestions { get; private set; }                 // Header counter

    /// <summary>Loads the taxonomy and per-difficulty question counts.</summary>
    public async Task OnGetAsync()
    {
        // Count active questions grouped by skill and difficulty in one query
        var counts = await _db.Questions
            .Where(q => q.IsActive)
            .GroupBy(q => new { q.SkillId, q.Difficulty })
            .Select(g => new { g.Key.SkillId, g.Key.Difficulty, N = g.Count() })
            .ToListAsync();

        // Local helper: looks up the count for one skill + difficulty
        int Count(int skillId, Difficulty d) => counts.FirstOrDefault(c => c.SkillId == skillId && c.Difficulty == d)?.N ?? 0;

        // Load domains with their skills in display order
        var domains = await _db.Domains.Include(d => d.Skills)
            .OrderBy(d => d.Section).ThenBy(d => d.SortOrder)
            .ToListAsync();

        Domains = domains.Select(d => new DomainRow(
            d.Name,
            d.Section,
            d.Skills.OrderBy(s => s.SortOrder)
                .Select(s => new SkillRow(s.Code, s.Name,
                    Count(s.Id, Difficulty.Easy), Count(s.Id, Difficulty.Medium), Count(s.Id, Difficulty.Hard)))
                .ToList())).ToList();

        TotalQuestions = counts.Sum(c => c.N);
    }
}
