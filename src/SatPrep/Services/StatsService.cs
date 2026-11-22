using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>Mastery information for one skill, ready for display.</summary>
public record SkillProgress(
    int SkillId, string Code, string Name, int DomainId, string DomainCode, string DomainName, SatSection Section,
    int Percent, MasteryLevel Level, int Attempts, int Correct, DateTime? LastPracticed, int QuestionCount);

/// <summary>Read-only queries behind the home page, skill progress and mistakes pages.</summary>
public class StatsService
{
    private readonly AppDbContext _db; // Database

    /// <param name="db">Database context.</param>
    public StatsService(AppDbContext db) => _db = db;

    /// <summary>All 29 skills with the learner's mastery, in official order.</summary>
    /// <param name="userId">Learner.</param>
    public List<SkillProgress> SkillProgress(int userId)
    {
        var masteries = _db.Masteries.Where(m => m.UserId == userId).ToDictionary(m => m.SkillId);   // Learner's rows
        var counts = _db.Questions.Where(q => q.IsActive).GroupBy(q => q.SkillId)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionary(x => x.Key, x => x.N);             // Questions per skill

        return _db.Skills.Include(s => s.Domain).AsNoTracking().ToList()
            .OrderBy(s => s.Domain!.Section).ThenBy(s => s.Domain!.SortOrder).ThenBy(s => s.SortOrder)
            .Select(s =>
            {
                masteries.TryGetValue(s.Id, out var m);
                return new SkillProgress(s.Id, s.Code, s.Name, s.DomainId, s.Domain!.Code, s.Domain.Name, s.Domain.Section,
                    MasteryService.MasteryPercent(m), MasteryService.LevelOf(m), m?.AttemptCount ?? 0, m?.CorrectCount ?? 0,
                    m?.LastPracticedUtc, counts.GetValueOrDefault(s.Id));
            }).ToList();
    }

    /// <summary>Mastery-based score estimate per section (null = not enough data yet).</summary>
    /// <param name="skills">Output of <see cref="SkillProgress(int)"/>.</param>
    public static (int? Rw, int? Math) EstimateFromMastery(List<SkillProgress> skills)
    {
        int? Est(SatSection s) => ScoreEstimator.SectionEstimateFromMastery(
            skills.Where(x => x.Section == s).Select(x => (x.DomainCode, x.Percent, x.Attempts)));
        return (Est(SatSection.ReadingWriting), Est(SatSection.Math));
    }

    /// <summary>Number of spaced-review questions due now.</summary>
    /// <param name="userId">Learner.</param>
    public int ReviewDueCount(int userId)
    {
        var now = DateTime.UtcNow;
        return _db.Reviews.Count(r => r.UserId == userId && !r.IsRetired && r.DueUtc <= now && r.Question!.IsActive);
    }

    /// <summary>Answers given today (server local day) and how many were correct.</summary>
    /// <param name="userId">Learner.</param>
    public (int Answered, int Correct) Today(int userId)
    {
        var startUtc = DateTime.Today.ToUniversalTime();                       // Midnight local, as UTC
        var q = _db.Attempts.Where(a => a.UserId == userId && a.AnsweredUtc >= startUtc);
        return (q.Count(), q.Count(a => a.IsCorrect));
    }

    /// <summary>Consecutive days (ending today or yesterday) with at least one answer.</summary>
    /// <param name="userId">Learner.</param>
    public int StreakDays(int userId)
    {
        var since = DateTime.UtcNow.AddDays(-400);
        var days = _db.Attempts.Where(a => a.UserId == userId && a.AnsweredUtc >= since)
            .Select(a => a.AnsweredUtc).ToList()
            .Select(d => d.ToLocalTime().Date).ToHashSet();                     // Distinct local days with activity

        var day = DateTime.Today;
        if (!days.Contains(day)) day = day.AddDays(-1);                         // Streak survives until the end of today
        int streak = 0;
        while (days.Contains(day)) { streak++; day = day.AddDays(-1); }
        return streak;
    }

    /// <summary>Answers per day for the last <paramref name="days"/> days (for the activity strip).</summary>
    /// <param name="userId">Learner.</param>
    /// <param name="days">Number of days.</param>
    public List<(DateTime Day, int Count)> Activity(int userId, int days)
    {
        var start = DateTime.Today.AddDays(-(days - 1));
        var startUtc = start.ToUniversalTime();
        var byDay = _db.Attempts.Where(a => a.UserId == userId && a.AnsweredUtc >= startUtc)
            .Select(a => a.AnsweredUtc).ToList()
            .GroupBy(d => d.ToLocalTime().Date).ToDictionary(g => g.Key, g => g.Count());
        return Enumerable.Range(0, days).Select(i => start.AddDays(i)).Select(d => (d, byDay.GetValueOrDefault(d))).ToList();
    }

    /// <summary>Latest result per question for the learner: questionId -> (correct, when).</summary>
    /// <param name="userId">Learner.</param>
    public Dictionary<int, (bool Correct, DateTime When)> LatestResults(int userId)
        => _db.Attempts.Where(a => a.UserId == userId)
            .GroupBy(a => a.QuestionId)
            .Select(g => g.OrderByDescending(a => a.AnsweredUtc).Select(a => new { a.QuestionId, a.IsCorrect, a.AnsweredUtc }).First())
            .ToList()
            .ToDictionary(x => x.QuestionId, x => (x.IsCorrect, x.AnsweredUtc));
}
