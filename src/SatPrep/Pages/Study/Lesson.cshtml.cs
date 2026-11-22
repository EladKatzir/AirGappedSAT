using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Extensions;
using SatPrep.Services;

namespace SatPrep.Pages.Study;

/// <summary>One domain lesson with a table of contents and per-skill practice buttons.</summary>
public class LessonModel : PageModel
{
    private readonly LessonService _lessons;  // Lesson files
    private readonly StatsService _stats;     // Skill mastery

    /// <param name="lessons">Lesson loader.</param>
    /// <param name="stats">Stats queries.</param>
    public LessonModel(LessonService lessons, StatsService stats)
    {
        _lessons = lessons;
        _stats = stats;
    }

    public Lesson? Lesson { get; private set; }                       // Lesson to show
    public List<SkillProgress> Skills { get; private set; } = new();  // Skills in this domain

    /// <summary>Loads the lesson for the domain code in the URL.</summary>
    /// <param name="code">Domain code, e.g. "ALG".</param>
    public IActionResult OnGet(string code)
    {
        Lesson = _lessons.Get(code);
        if (Lesson is null) return NotFound();
        Skills = _stats.SkillProgress(User.UserId()).Where(s => s.DomainCode.Equals(code, StringComparison.OrdinalIgnoreCase)).ToList();
        return Page();
    }
}
