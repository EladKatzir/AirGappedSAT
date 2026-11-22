using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages;

/// <summary>
/// "Today" home page: daily plan, score estimate vs target, streak, review
/// due, weakest skills and recent activity.
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;        // Database
    private readonly StatsService _stats;     // Progress queries
    private readonly StudyPlanner _planner;   // Daily plan builder

    /// <param name="db">Database.</param>
    /// <param name="stats">Stats queries.</param>
    /// <param name="planner">Study planner.</param>
    public IndexModel(AppDbContext db, StatsService stats, StudyPlanner planner)
    {
        _db = db;
        _stats = stats;
        _planner = planner;
    }

    public DailyPlan Plan { get; private set; } = null!;                       // Today's tasks
    public List<SkillProgress> Skills { get; private set; } = new();            // All skills with mastery
    public int? EstRw { get; private set; }                                     // Estimated R&W score
    public int? EstMath { get; private set; }                                   // Estimated Math score
    public int? LastTestScore { get; private set; }                             // Most recent full test total
    public int Streak { get; private set; }                                     // Days in a row
    public (int Answered, int Correct) TodayCounts { get; private set; }        // Today's answers
    public int ReviewDue { get; private set; }                                  // Reviews due now
    public List<(DateTime Day, int Count)> Activity { get; private set; } = new(); // Last 28 days
    public int TotalQuestions { get; private set; }                             // Bank size

    /// <summary>Loads everything shown on the home page.</summary>
    public void OnGet()
    {
        int userId = User.UserId();
        Plan = _planner.Today(userId);
        Skills = _stats.SkillProgress(userId);
        (EstRw, EstMath) = StatsService.EstimateFromMastery(Skills);
        LastTestScore = _db.Tests.Where(t => t.UserId == userId && t.Status == TestStatus.Completed && t.TotalScore != null)
            .OrderByDescending(t => t.CompletedUtc).Select(t => t.TotalScore).FirstOrDefault();
        Streak = _stats.StreakDays(userId);
        TodayCounts = _stats.Today(userId);
        ReviewDue = _stats.ReviewDueCount(userId);
        Activity = _stats.Activity(userId, 28);
        TotalQuestions = _db.Questions.Count(q => q.IsActive);
    }

    /// <summary>Total estimated score, if both sections have enough data.</summary>
    public int? EstTotal => EstRw is int r && EstMath is int m ? r + m : null;
}
