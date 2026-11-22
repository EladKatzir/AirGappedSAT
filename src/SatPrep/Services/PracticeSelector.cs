using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>Which pool of questions practice draws from.</summary>
public enum PracticeMode
{
    New = 0,       // Questions never answered before (falls back to all when exhausted)
    All = 1,       // Any question
    Incorrect = 2, // Questions whose latest attempt was wrong
    Marked = 3,    // Bookmarked questions
    ReviewDue = 4  // Spaced-repetition items due now
}

/// <summary>Filters chosen on the practice screen.</summary>
public record PracticeFilter(
    SatSection? Section,     // Limit to one section
    int? DomainId,           // Limit to one domain
    int? SkillId,            // Limit to one skill
    Difficulty? Difficulty,  // Fixed difficulty, or null for adaptive
    PracticeMode Mode);      // Question pool

/// <summary>
/// Chooses the next practice question. In adaptive mode it targets the
/// learner's weakest skill in scope at the difficulty that keeps expected
/// success around 60-75% (MasteryService.RecommendedDifficulty).
/// </summary>
public class PracticeSelector
{
    private readonly AppDbContext _db;       // Database
    private readonly Random _rng = new();    // Randomness so the order is not predictable

    /// <param name="db">Database context.</param>
    public PracticeSelector(AppDbContext db) => _db = db;

    /// <summary>Returns the ids of questions in scope for the filter (ignoring difficulty).</summary>
    /// <param name="userId">Learner.</param>
    /// <param name="f">Filter.</param>
    public IQueryable<Question> Scope(int userId, PracticeFilter f)
    {
        var q = _db.Questions.Where(x => x.IsActive);                                // Never serve retired questions
        if (f.SkillId is int sid) q = q.Where(x => x.SkillId == sid);
        else if (f.DomainId is int did) q = q.Where(x => x.Skill!.DomainId == did);
        else if (f.Section is SatSection sec) q = q.Where(x => x.Skill!.Domain!.Section == sec);

        switch (f.Mode)
        {
            case PracticeMode.Marked:
                q = q.Where(x => _db.Marks.Any(m => m.UserId == userId && m.QuestionId == x.Id));
                break;
            case PracticeMode.ReviewDue:
                var now = DateTime.UtcNow;
                q = q.Where(x => _db.Reviews.Any(r => r.UserId == userId && r.QuestionId == x.Id && !r.IsRetired && r.DueUtc <= now));
                break;
            case PracticeMode.Incorrect:
                // Latest attempt on the question was wrong
                q = q.Where(x => _db.Attempts.Where(a => a.UserId == userId && a.QuestionId == x.Id)
                    .OrderByDescending(a => a.AnsweredUtc).Select(a => (bool?)a.IsCorrect).FirstOrDefault() == false);
                break;
        }
        return q;
    }

    /// <summary>Picks the next question, or null if nothing matches.</summary>
    /// <param name="userId">Learner.</param>
    /// <param name="f">Filter.</param>
    /// <param name="excludeId">The question just answered (never repeat it immediately).</param>
    public Question? Next(int userId, PracticeFilter f, int? excludeId = null)
    {
        var scope = Scope(userId, f);
        if (excludeId is int ex) scope = scope.Where(x => x.Id != ex);

        Difficulty? target = f.Difficulty;                                            // Fixed difficulty, if chosen
        if (target is null && f.Mode is PracticeMode.New or PracticeMode.All)
        {
            // Adaptive: focus on the weakest skill in scope, at the right difficulty for it
            var skillId = f.SkillId ?? WeakestSkill(userId, scope);
            if (skillId is int s)
            {
                scope = scope.Where(x => x.SkillId == s);
                var m = _db.Masteries.FirstOrDefault(x => x.UserId == userId && x.SkillId == s);
                target = MasteryService.RecommendedDifficulty(m);
            }
        }

        // Preference order: unseen at target difficulty -> unseen any difficulty -> seen at target -> anything
        var unseen = scope.Where(x => !_db.Attempts.Any(a => a.UserId == userId && a.QuestionId == x.Id));
        bool needUnseen = f.Mode == PracticeMode.New;
        var candidates = new List<IQueryable<Question>>();
        if (target is Difficulty d) candidates.Add(unseen.Where(x => x.Difficulty == d));
        candidates.Add(unseen);
        if (!needUnseen || !unseen.Any())                                             // New mode falls back once everything is seen
        {
            if (target is Difficulty d2) candidates.Add(scope.Where(x => x.Difficulty == d2));
            candidates.Add(scope);
        }

        foreach (var c in candidates)
        {
            int count = c.Count();                                                    // How many match this tier
            if (count == 0) continue;
            return c.Include(x => x.Skill).ThenInclude(s => s!.Domain)
                .OrderBy(x => x.Id).Skip(_rng.Next(count)).First();                   // Random pick inside the tier
        }
        return null;
    }

    /// <summary>
    /// Picks the skill (among those with questions in scope) with the lowest
    /// mastery, with a little randomness so practice does not get stuck on one skill.
    /// Untouched skills count as weakest.
    /// </summary>
    /// <param name="userId">Learner.</param>
    /// <param name="scope">Questions in scope.</param>
    private int? WeakestSkill(int userId, IQueryable<Question> scope)
    {
        var skillIds = scope.Select(x => x.SkillId).Distinct().ToList();              // Skills that have questions
        if (skillIds.Count == 0) return null;

        var masteries = _db.Masteries.Where(m => m.UserId == userId && skillIds.Contains(m.SkillId))
            .ToDictionary(m => m.SkillId);

        // Lower percent = weaker; untouched skills score -1 so they come first; jitter by up to 15 points
        return skillIds
            .Select(id => new
            {
                id,
                score = (masteries.TryGetValue(id, out var m) && m.AttemptCount > 0 ? MasteryService.MasteryPercent(m) : -1)
                        + _rng.Next(15)
            })
            .OrderBy(x => x.score).First().id;
    }
}
