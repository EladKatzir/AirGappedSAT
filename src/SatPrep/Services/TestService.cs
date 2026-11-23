using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>Definition of one module inside a practice test.</summary>
/// <param name="Section">Which SAT section the module belongs to.</param>
/// <param name="IsSecondModule">True for module 2 (adaptive routing applies).</param>
/// <param name="QuestionCount">Number of questions.</param>
/// <param name="Minutes">Time limit.</param>
/// <param name="Adaptive">Whether module 2 difficulty depends on module 1.</param>
public record ModuleSpec(SatSection Section, bool IsSecondModule, int QuestionCount, int Minutes, bool Adaptive)
{
    /// <summary>Human-readable title, e.g. "Reading and Writing - Module 2".</summary>
    public string Title => $"{(Section == SatSection.Math ? "Math" : "Reading and Writing")} - Module {(IsSecondModule ? 2 : 1)}";
}

/// <summary>
/// Builds, runs and scores practice tests, mirroring the digital SAT:
/// R&amp;W 2 x 27 questions / 32 min, Math 2 x 22 / 35 min, module 2 harder or
/// easier depending on module 1. Questions are drawn from the bank by a
/// domain blueprint, preferring questions the learner has never seen.
/// </summary>
public class TestService
{
    /// <summary>Questions per domain in one full R&amp;W module (27), in official section order.</summary>
    private static readonly (string Domain, int Count)[] RwBlueprint = { ("CAS", 7), ("INI", 7), ("SEC", 8), ("EOI", 5) };

    /// <summary>Questions per domain in one full Math module (22).</summary>
    private static readonly (string Domain, int Count)[] MathBlueprint = { ("ALG", 8), ("ADV", 7), ("PSD", 4), ("GEO", 3) };

    private readonly AppDbContext _db;           // Database
    private readonly ProgressService _progress;  // Records answers / mastery
    private readonly Random _rng = new();        // Question shuffling
    private HashSet<int> _officialIds = new();   // Ids of official questions (filled per module)

    /// <param name="db">Database context.</param>
    /// <param name="progress">Progress recorder.</param>
    public TestService(AppDbContext db, ProgressService progress)
    {
        _db = db;
        _progress = progress;
    }

    /// <summary>The ordered list of modules for a test type.</summary>
    /// <param name="type">Test type.</param>
    public static IReadOnlyList<ModuleSpec> ModulePlan(TestType type) => type switch
    {
        TestType.Full => new[]
        {
            new ModuleSpec(SatSection.ReadingWriting, false, 27, 32, true),
            new ModuleSpec(SatSection.ReadingWriting, true, 27, 32, true),
            new ModuleSpec(SatSection.Math, false, 22, 35, true),
            new ModuleSpec(SatSection.Math, true, 22, 35, true),
        },
        TestType.ReadingWriting => new[]
        {
            new ModuleSpec(SatSection.ReadingWriting, false, 27, 32, true),
            new ModuleSpec(SatSection.ReadingWriting, true, 27, 32, true),
        },
        TestType.Math => new[]
        {
            new ModuleSpec(SatSection.Math, false, 22, 35, true),
            new ModuleSpec(SatSection.Math, true, 22, 35, true),
        },
        _ => new[] // Short: one half-length module per section, not adaptive
        {
            new ModuleSpec(SatSection.ReadingWriting, false, 14, 17, false),
            new ModuleSpec(SatSection.Math, false, 11, 18, false),
        }
    };

    /// <summary>Display name of a test type.</summary>
    /// <param name="t">Test type.</param>
    public static string TypeName(TestType t) => t switch
    {
        TestType.Full => "Full-length SAT",
        TestType.ReadingWriting => "Reading and Writing section",
        TestType.Math => "Math section",
        _ => "Short test"
    };

    /// <summary>True if a 10-minute break is offered before this module.</summary>
    /// <param name="type">Test type.</param>
    /// <param name="moduleIndex">Module about to start.</param>
    public static bool HasBreakBefore(TestType type, int moduleIndex) => type == TestType.Full && moduleIndex == 2;

    /// <summary>Creates a new test and fills module 1.</summary>
    /// <param name="userId">Learner.</param>
    /// <param name="type">Test type.</param>
    public TestAttempt Start(int userId, TestType type)
    {
        var test = new TestAttempt { UserId = userId, Type = type, Status = TestStatus.InProgress };
        _db.Tests.Add(test);
        _db.SaveChanges();                                                     // Need the id for items
        FillModule(test, 0);
        _db.SaveChanges();
        return test;
    }

    /// <summary>Starts the clock on the current module (after the intro / break screen).</summary>
    /// <param name="test">Test (current module must already be filled).</param>
    public void StartModuleClock(TestAttempt test)
    {
        test.ModuleStartedUtc ??= DateTime.UtcNow;                             // Idempotent: refreshing does not reset the clock
        _db.SaveChanges();
    }

    /// <summary>Seconds left on the current module's clock (full time if not started).</summary>
    /// <param name="test">Test.</param>
    public static int SecondsLeft(TestAttempt test)
    {
        var spec = ModulePlan(test.Type)[test.CurrentModule];
        if (test.ModuleStartedUtc is null) return spec.Minutes * 60;
        var elapsed = (DateTime.UtcNow - test.ModuleStartedUtc.Value).TotalSeconds;
        return (int)Math.Max(0, spec.Minutes * 60 - elapsed);
    }

    /// <summary>Saves the learner's work on one question (called as they go, so nothing is lost).</summary>
    /// <param name="test">Test.</param>
    /// <param name="itemId">Test item id.</param>
    /// <param name="answer">Current answer ("" = blank).</param>
    /// <param name="marked">Mark-for-review flag.</param>
    /// <param name="eliminatedJson">JSON array of crossed-out choice letters.</param>
    /// <param name="addSeconds">Seconds spent since the last save.</param>
    public void SaveItem(TestAttempt test, int itemId, string? answer, bool marked, string? eliminatedJson, int addSeconds)
    {
        var item = _db.TestItems.FirstOrDefault(i => i.Id == itemId && i.TestAttemptId == test.Id
                                                     && i.ModuleIndex == test.CurrentModule); // Only the active module is editable
        if (item is null) return;
        item.GivenAnswer = (answer ?? "").Trim();
        item.IsMarked = marked;
        item.EliminatedJson = string.IsNullOrWhiteSpace(eliminatedJson) ? "[]" : eliminatedJson;
        item.SecondsSpent += Math.Clamp(addSeconds, 0, 600);
        _db.SaveChanges();
    }

    /// <summary>
    /// Grades the current module, records every answer (updates mastery),
    /// routes / fills the next module, or finishes and scores the test.
    /// </summary>
    /// <param name="test">Test (with Items loaded).</param>
    public void SubmitModule(TestAttempt test)
    {
        var plan = ModulePlan(test.Type);
        var items = _db.TestItems.Include(i => i.Question)
            .Where(i => i.TestAttemptId == test.Id && i.ModuleIndex == test.CurrentModule).ToList();

        foreach (var item in items)                                             // Grade + record each answer
        {
            var r = _progress.Record(test.UserId, item.Question!, item.GivenAnswer, item.SecondsSpent,
                AttemptSource.PracticeTest, test.Id, save: false);
            item.IsCorrect = r.IsCorrect;
        }

        var spec = plan[test.CurrentModule];
        if (spec.Adaptive && !spec.IsSecondModule)                              // End of module 1: decide routing
        {
            var route = ScoreEstimator.Route(items.Select(i => (i.Question!.Difficulty, i.IsCorrect)));
            if (spec.Section == SatSection.Math) test.MathRoute = route; else test.RwRoute = route;
        }

        test.CurrentModule++;
        test.ModuleStartedUtc = null;                                           // Next module starts from its intro screen

        if (test.CurrentModule >= plan.Count) Finish(test);
        else FillModule(test, test.CurrentModule);

        _db.SaveChanges();
    }

    /// <summary>Computes section and total scores and marks the test completed.</summary>
    /// <param name="test">Test.</param>
    private void Finish(TestAttempt test)
    {
        test.CurrentModule = ModulePlan(test.Type).Count - 1;                   // Keep index valid
        test.Status = TestStatus.Completed;
        test.CompletedUtc = DateTime.UtcNow;

        // All items of every module (tracked instances keep this module's unsaved grading)
        var all = _db.TestItems.Include(i => i.Question).Where(i => i.TestAttemptId == test.Id).ToList();
        var plan = ModulePlan(test.Type);

        int? Section(SatSection s, ModuleRoute route)
        {
            var results = all.Where(i => plan[i.ModuleIndex].Section == s)
                .Select(i => (i.Question!.Difficulty, i.IsCorrect)).ToList();
            return results.Count == 0 ? null : ScoreEstimator.SectionScore(results, route);
        }

        test.RwScore = Section(SatSection.ReadingWriting, test.RwRoute);
        test.MathScore = Section(SatSection.Math, test.MathRoute);
        test.TotalScore = test.RwScore is int rw && test.MathScore is int m ? rw + m : null;
    }

    /// <summary>Chooses questions for a module and inserts its TestItems.</summary>
    /// <param name="test">Test.</param>
    /// <param name="moduleIndex">Module to fill.</param>
    private void FillModule(TestAttempt test, int moduleIndex)
    {
        var spec = ModulePlan(test.Type)[moduleIndex];
        var route = spec.Section == SatSection.Math ? test.MathRoute : test.RwRoute;
        var blueprint = Scale(spec.Section == SatSection.Math ? MathBlueprint : RwBlueprint, spec.QuestionCount);

        // Target difficulty mix (easy, medium, hard) for this module
        (double e, double m, double h) mix = !spec.IsSecondModule || route == ModuleRoute.None ? (0.34, 0.33, 0.33)
            : route == ModuleRoute.Upper ? (0.15, 0.35, 0.50) : (0.50, 0.35, 0.15);

        // Questions already used in this test, and everything the learner has seen before
        var usedInTest = _db.TestItems.Where(i => i.TestAttemptId == test.Id).Select(i => i.QuestionId).ToHashSet();
        var seen = _db.Attempts.Where(a => a.UserId == test.UserId).Select(a => a.QuestionId).Distinct().ToHashSet();
        _officialIds = _db.Questions.Where(q => q.ExternalId.StartsWith(PracticeSelector.OfficialPrefix)).Select(q => q.Id).ToHashSet();

        int order = 1;                                                           // Position shown to the learner
        foreach (var (domainCode, count) in blueprint)
        {
            var pool = _db.Questions.Where(q => q.IsActive && q.Skill!.Domain!.Code == domainCode)
                .Select(q => new { q.Id, q.Difficulty }).ToList()
                .Where(q => !usedInTest.Contains(q.Id)).ToList();

            int nEasy = (int)Math.Round(count * mix.e), nHard = (int)Math.Round(count * mix.h);
            int nMed = Math.Max(0, count - nEasy - nHard);
            var picked = new List<(int Id, Difficulty D)>();

            // Pick per difficulty: unseen first, then seen; then fill any shortfall from any difficulty
            foreach (var (d, n) in new[] { (Difficulty.Easy, nEasy), (Difficulty.Medium, nMed), (Difficulty.Hard, nHard) })
                picked.AddRange(Take(pool.Where(q => q.Difficulty == d).Select(q => (q.Id, q.Difficulty)), n, seen, picked));
            if (picked.Count < count)
                picked.AddRange(Take(pool.Select(q => (q.Id, q.Difficulty)), count - picked.Count, seen, picked));

            // Within a domain, easier questions come first (as on the real test)
            foreach (var p in picked.OrderBy(p => p.D))
            {
                _db.TestItems.Add(new TestItem { TestAttemptId = test.Id, ModuleIndex = moduleIndex, Order = order++, QuestionId = p.Id });
                usedInTest.Add(p.Id);
            }
        }
    }

    /// <summary>Takes up to n questions, unseen ones first, randomly, skipping already-picked ids.</summary>
    /// <param name="source">Candidate (id, difficulty) pairs.</param>
    /// <param name="n">How many to take.</param>
    /// <param name="seen">Question ids the learner has answered before.</param>
    /// <param name="already">Questions already picked for this domain.</param>
    private IEnumerable<(int Id, Difficulty D)> Take(IEnumerable<(int Id, Difficulty D)> source, int n,
        HashSet<int> seen, List<(int Id, Difficulty D)> already)
    {
        var taken = already.Select(a => a.Id).ToHashSet();
        return source.Where(q => !taken.Contains(q.Id))
            .OrderBy(q => seen.Contains(q.Id) ? 1 : 0)                          // Unseen first
            .ThenBy(q => _officialIds.Contains(q.Id) ? 0 : 1)                   // Then official SAT questions
            .ThenBy(_ => _rng.Next())
            .Take(n).ToList();
    }

    /// <summary>Scales a blueprint to a different module length (used by the short test).</summary>
    /// <param name="bp">Full-length blueprint.</param>
    /// <param name="total">Desired question count.</param>
    private static (string Domain, int Count)[] Scale((string Domain, int Count)[] bp, int total)
    {
        int full = bp.Sum(x => x.Count);
        if (full == total) return bp;
        var scaled = bp.Select(x => (x.Domain, Count: (int)Math.Round(x.Count * (double)total / full))).ToArray();
        scaled[0].Count += total - scaled.Sum(x => x.Count);                     // Fix rounding drift
        return scaled;
    }
}
