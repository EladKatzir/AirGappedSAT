using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>One task on today's plan.</summary>
/// <param name="Title">Short title, e.g. "Practice: Transitions".</param>
/// <param name="Detail">One-line explanation of why / what.</param>
/// <param name="Url">Where the "Start" button goes.</param>
/// <param name="Minutes">Estimated minutes.</param>
/// <param name="Done">True when today's activity already covers it.</param>
/// <param name="Icon">Small emoji-free label used for styling ("review", "practice", "lesson", "vocab", "test").</param>
public record PlanTask(string Title, string Detail, string Url, int Minutes, bool Done, string Icon);

/// <summary>Today's plan plus the countdown / goal information shown with it.</summary>
public record DailyPlan(StudyPlan? Settings, int? DaysLeft, List<PlanTask> Tasks, int TotalMinutes, int DoneCount);

/// <summary>
/// Builds the daily study plan from the learner's settings and progress.
/// Rules (in priority order, within the daily minute budget):
///   1. Spaced review of missed questions that are due.
///   2. Weekly practice test on the chosen weekday (Full if budget >= 150 min, else Short).
///   3. The weakest skills, weighted by how much their domain counts on the SAT:
///      a lesson first if the skill was never practised, then 10 adaptive questions.
///   4. 10 vocabulary flashcards.
/// The plan is rebuilt every time it is shown, so it adapts as mastery changes;
/// tasks tick off automatically from today's activity.
/// </summary>
public class StudyPlanner
{
    private readonly AppDbContext _db;      // Database
    private readonly StatsService _stats;   // Progress queries

    /// <param name="db">Database context.</param>
    /// <param name="stats">Stats service.</param>
    public StudyPlanner(AppDbContext db, StatsService stats)
    {
        _db = db;
        _stats = stats;
    }

    /// <summary>Builds today's plan for a learner.</summary>
    /// <param name="userId">Learner.</param>
    public DailyPlan Today(int userId)
    {
        var settings = _db.StudyPlans.FirstOrDefault(p => p.UserId == userId);           // May be null (defaults used)
        int budget = settings?.MinutesPerDay ?? 45;                                      // Daily minutes
        int? daysLeft = settings is null ? null : Math.Max(0, (settings.TestDate.Date - DateTime.Today).Days);
        var startUtc = DateTime.Today.ToUniversalTime();                                 // Start of today
        var tasks = new List<PlanTask>();
        int used = 0;                                                                    // Minutes allocated so far

        // Today's activity, used to tick tasks off
        var todayBySkill = _db.Attempts.Where(a => a.UserId == userId && a.AnsweredUtc >= startUtc && a.Source != AttemptSource.PracticeTest)
            .GroupBy(a => a.Question!.SkillId).Select(g => new { g.Key, N = g.Count() })
            .ToDictionary(x => x.Key, x => x.N);
        int vocabToday = _db.VocabProgress.Count(v => v.UserId == userId && v.LastReviewedUtc >= startUtc); // Cards reviewed today

        // 1. Spaced review
        int due = _stats.ReviewDueCount(userId);
        int reviewedToday = _db.Attempts.Count(a => a.UserId == userId && a.AnsweredUtc >= startUtc && a.Source == AttemptSource.Review);
        if (due > 0 || reviewedToday > 0)
        {
            int n = Math.Min(Math.Max(due, 1), 15);
            tasks.Add(new PlanTask($"Review {due} missed question{(due == 1 ? "" : "s")}",
                "Questions you got wrong earlier come back after 1, 3 and 7 days until they stick.",
                "/Practice?mode=ReviewDue", (int)Math.Ceiling(n * 1.5), due == 0, "review"));
            used += (int)Math.Ceiling(n * 1.5);
        }

        // 2. Weekly practice test
        int testDay = settings?.PracticeTestDay ?? 6;
        if ((int)DateTime.Today.DayOfWeek == testDay && (daysLeft is null || daysLeft > 1))
        {
            bool full = budget >= 150;
            bool startedToday = _db.Tests.Any(t => t.UserId == userId && t.StartedUtc >= startUtc);
            tasks.Add(new PlanTask(full ? "Weekly full-length practice test" : "Weekly short practice test",
                "Builds stamina and updates your score estimate.", $"/Tests?start={(full ? "Full" : "Short")}",
                full ? 145 : 40, startedToday, "test"));
            used += full ? 145 : 40;
        }

        // 3. Weakest skills (weighted by domain importance), only skills that have questions
        var skills = _stats.SkillProgress(userId).Where(s => s.QuestionCount > 0).ToList();
        var ranked = skills
            .Select(s => new
            {
                s,
                need = (100 - (s.Attempts == 0 ? 0 : s.Percent)) * ScoreEstimator.DomainWeights.GetValueOrDefault(s.DomainCode, 0.25)
                       * (s.Level == MasteryLevel.Mastered ? 0.2 : 1.0)
            })
            .OrderByDescending(x => x.need)
            // Rotate daily among similar needs so the plan is not identical every day
            .ThenBy(x => (x.s.SkillId * 7 + DateTime.Today.DayOfYear) % 13)
            .Select(x => x.s).ToList();

        foreach (var s in ranked)
        {
            if (used >= budget - 5 || tasks.Count(t => t.Icon == "practice") >= 4) break;
            if (s.Attempts == 0)                                                         // Never practised: read first
            {
                tasks.Add(new PlanTask($"Read: {s.Name}", $"Short lesson on a skill you have not started ({s.DomainName}).",
                    $"/Study/Lesson/{s.DomainCode}#{s.Code}", 8, false, "lesson"));
                used += 8;
            }
            int doneN = todayBySkill.GetValueOrDefault(s.SkillId);
            tasks.Add(new PlanTask($"Practice: {s.Name}",
                s.Attempts == 0 ? "10 questions to get a first mastery reading." : $"Mastery {s.Percent}% ({s.Level}). 10 adaptive questions.",
                $"/Practice?skill={s.SkillId}", 15, doneN >= 10, "practice"));
            used += 15;
        }

        // 4. Vocabulary
        if (_db.VocabWords.Any())
        {
            tasks.Add(new PlanTask("Vocabulary: 10 flashcards", "Words that show up in Words in Context questions.",
                "/Study/Vocab", 5, vocabToday >= 10, "vocab"));
        }

        return new DailyPlan(settings, daysLeft, tasks, tasks.Sum(t => t.Minutes), tasks.Count(t => t.Done));
    }
}
