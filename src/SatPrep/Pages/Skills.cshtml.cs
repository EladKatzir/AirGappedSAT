using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Extensions;
using SatPrep.Services;

namespace SatPrep.Pages;

/// <summary>Skill progress: mastery for all 29 skills, grouped by section and domain.</summary>
public class SkillsModel : PageModel
{
    private readonly StatsService _stats; // Progress queries

    /// <param name="stats">Stats queries.</param>
    public SkillsModel(StatsService stats) => _stats = stats;

    public List<SkillProgress> Skills { get; private set; } = new(); // All skills with mastery
    public int? EstRw { get; private set; }                           // Section estimate
    public int? EstMath { get; private set; }                         // Section estimate

    /// <summary>Loads the learner's skill progress.</summary>
    public void OnGet()
    {
        Skills = _stats.SkillProgress(User.UserId());
        (EstRw, EstMath) = StatsService.EstimateFromMastery(Skills);
    }
}
