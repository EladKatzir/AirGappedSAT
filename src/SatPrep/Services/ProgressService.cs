using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>
/// The single place where an answer is recorded. Every answer, whether from
/// practice, review or a test:
///   1. is graded,
///   2. is stored as a QuestionAttempt (history / analytics / error log),
///   3. updates the user's SkillMastery (Elo/IRT model),
///   4. updates the spaced-repetition queue (misses come back after 1/3/7 days).
/// </summary>
public class ProgressService
{
    /// <summary>Days until a missed question is due again, by stage.</summary>
    public static readonly int[] ReviewIntervalsDays = { 1, 3, 7 };

    private readonly AppDbContext _db;          // Database
    private readonly MasteryService _mastery;   // Mastery maths

    /// <param name="db">Database context.</param>
    /// <param name="mastery">Mastery engine.</param>
    public ProgressService(AppDbContext db, MasteryService mastery)
    {
        _db = db;
        _mastery = mastery;
    }

    /// <summary>Result of recording an answer.</summary>
    public record AnswerResult(bool IsCorrect, SkillMastery Mastery, int MasteryBefore);

    /// <summary>
    /// Grades and records one answer. Saves changes unless <paramref name="save"/> is false
    /// (tests record a whole module and save once).
    /// </summary>
    /// <param name="userId">Who answered.</param>
    /// <param name="q">The question (must have Skill loaded or SkillId set).</param>
    /// <param name="given">The learner's answer.</param>
    /// <param name="seconds">Seconds spent on the question.</param>
    /// <param name="source">Practice / review / test.</param>
    /// <param name="testAttemptId">Test id when the answer came from a test.</param>
    /// <param name="save">Whether to call SaveChanges.</param>
    public AnswerResult Record(int userId, Question q, string? given, int seconds, AttemptSource source,
        int? testAttemptId = null, bool save = true)
    {
        var now = DateTime.UtcNow;                                                    // One timestamp for everything
        bool ok = AnswerChecker.IsCorrect(q, given);                                  // Grade

        _db.Attempts.Add(new QuestionAttempt                                          // History row
        {
            UserId = userId,
            QuestionId = q.Id,
            GivenAnswer = given?.Trim() ?? "",
            IsCorrect = ok,
            SecondsSpent = Math.Clamp(seconds, 0, 3600),
            Source = source,
            TestAttemptId = testAttemptId,
            AnsweredUtc = now
        });

        var m = GetOrCreateMastery(userId, q.SkillId);                                // Mastery row for this skill
        int before = MasteryService.MasteryPercent(m);                                // For the "+3%" feedback badge
        _mastery.ApplyAttempt(m, q.Difficulty, ok, now);                              // Elo/IRT update

        UpdateReviewQueue(userId, q.Id, ok, now);                                     // Spaced repetition

        if (save) _db.SaveChanges();
        return new AnswerResult(ok, m, before);
    }

    /// <summary>Loads the mastery row for (user, skill), creating it if needed.</summary>
    /// <param name="userId">User.</param>
    /// <param name="skillId">Skill.</param>
    private SkillMastery GetOrCreateMastery(int userId, int skillId)
    {
        // Check rows added earlier in this same unit of work first (tests record many answers before saving)
        var m = _db.Masteries.Local.FirstOrDefault(x => x.UserId == userId && x.SkillId == skillId)
                ?? _db.Masteries.FirstOrDefault(x => x.UserId == userId && x.SkillId == skillId);
        if (m is null)
        {
            m = new SkillMastery { UserId = userId, SkillId = skillId, Rating = 0 };
            _db.Masteries.Add(m);
        }
        return m;
    }

    /// <summary>
    /// Wrong answer: (re)schedule for tomorrow at stage 0.
    /// Right answer on a due item: advance a stage (1 -> 3 -> 7 days), retire after the last.
    /// </summary>
    /// <param name="userId">User.</param>
    /// <param name="questionId">Question answered.</param>
    /// <param name="ok">Whether it was correct.</param>
    /// <param name="now">Current time.</param>
    private void UpdateReviewQueue(int userId, int questionId, bool ok, DateTime now)
    {
        var item = _db.Reviews.Local.FirstOrDefault(r => r.UserId == userId && r.QuestionId == questionId)
                   ?? _db.Reviews.FirstOrDefault(r => r.UserId == userId && r.QuestionId == questionId);

        if (!ok)
        {
            if (item is null)
            {
                item = new ReviewItem { UserId = userId, QuestionId = questionId };
                _db.Reviews.Add(item);
            }
            item.Stage = 0;                                                           // Start over
            item.IsRetired = false;
            item.DueUtc = now.AddDays(ReviewIntervalsDays[0]);
            return;
        }

        if (item is null || item.IsRetired || item.DueUtc > now) return;              // Not in the queue / not due yet

        item.Stage++;                                                                 // Remembered it - push further out
        if (item.Stage >= ReviewIntervalsDays.Length) item.IsRetired = true;          // Learned
        else item.DueUtc = now.AddDays(ReviewIntervalsDays[item.Stage]);
    }

    /// <summary>Toggles the bookmark / "marked for review" flag on a question.</summary>
    /// <param name="userId">User.</param>
    /// <param name="questionId">Question.</param>
    /// <returns>True if the question is now marked.</returns>
    public bool ToggleMark(int userId, int questionId)
    {
        var mark = _db.Marks.FirstOrDefault(m => m.UserId == userId && m.QuestionId == questionId);
        if (mark is null) _db.Marks.Add(new QuestionMark { UserId = userId, QuestionId = questionId }); // Mark
        else _db.Marks.Remove(mark);                                                                    // Unmark
        _db.SaveChanges();
        return mark is null;
    }
}
