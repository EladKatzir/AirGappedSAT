using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SatPrep.Data;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Tests;

/// <summary>
/// End-to-end service tests on a real (in-memory SQLite) database with the
/// bundled content: practice answers, spaced review, full adaptive test flow.
/// </summary>
public class TestFlowTests : IDisposable
{
    private readonly SqliteConnection _conn;   // Keeps the in-memory DB alive
    private readonly AppDbContext _db;         // Context under test
    private readonly int _userId;              // Learner used by the tests

    /// <summary>Creates the schema, seeds the taxonomy, imports all bundled packs, adds a learner.</summary>
    public TestFlowTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);
        _db.Database.Migrate();                                                // Same path as production
        SatTaxonomy.EnsureSeeded(_db);
        new ContentImporter(_db, NullLogger<ContentImporter>.Instance)
            .ImportFolder(Path.Combine(AppContext.BaseDirectory, "Content", "packs"));
        var u = new AppUser { UserName = "tester" };
        _db.Users.Add(u);
        _db.SaveChanges();
        _userId = u.Id;
    }

    [Fact]
    public void Bundled_content_covers_every_skill()
    {
        var empty = _db.Skills.Where(s => !s.Questions.Any()).Select(s => s.Code).ToList();
        Assert.Empty(empty);
        Assert.True(_db.Questions.Count() > 1000);
    }

    [Fact]
    public void Every_question_accepts_its_own_answer_key()
    {
        // Guards the whole bank (official + generated): the stored key must grade as correct
        var failures = _db.Questions.AsEnumerable()
            .Where(q => !AnswerChecker.IsCorrect(q, q.CorrectAnswer.Split('|')[0]))
            .Select(q => q.ExternalId).Take(10).ToList();
        Assert.Empty(failures);
    }

    [Fact]
    public void Official_questions_are_imported_as_html()
    {
        var official = _db.Questions.Where(q => q.ExternalId.StartsWith("cb-")).ToList();
        Assert.True(official.Count > 4000);
        Assert.All(official, q => Assert.True(q.IsHtml));
    }

    [Fact]
    public void Wrong_answer_creates_review_item_and_updates_mastery()
    {
        var progress = new ProgressService(_db, new MasteryService());
        var q = _db.Questions.First(x => x.Format == AnswerFormat.MultipleChoice);
        var wrong = new[] { "A", "B", "C", "D" }.First(l => l != q.CorrectAnswer);

        var r = progress.Record(_userId, q, wrong, 30, AttemptSource.Practice);

        Assert.False(r.IsCorrect);
        Assert.Equal(1, r.Mastery.AttemptCount);
        var review = _db.Reviews.Single(x => x.UserId == _userId && x.QuestionId == q.Id);
        Assert.Equal(0, review.Stage);
        Assert.True(review.DueUtc > DateTime.UtcNow.AddHours(20));             // Due tomorrow
    }

    [Fact]
    public void Practice_selector_returns_question_from_requested_skill()
    {
        var skill = _db.Skills.First(s => s.Code == "ALG.LF");
        var sel = new PracticeSelector(_db);
        var q = sel.Next(_userId, new PracticeFilter(null, null, skill.Id, null, PracticeMode.New));
        Assert.NotNull(q);
        Assert.Equal(skill.Id, q!.SkillId);
    }

    [Fact]
    public void Full_test_routes_module_two_and_scores_all_sections()
    {
        var svc = new TestService(_db, new ProgressService(_db, new MasteryService()));
        var test = svc.Start(_userId, TestType.Full);
        var plan = TestService.ModulePlan(TestType.Full);

        for (int m = 0; m < plan.Count; m++)
        {
            var items = _db.TestItems.Include(i => i.Question).Where(i => i.TestAttemptId == test.Id && i.ModuleIndex == m).ToList();
            Assert.Equal(plan[m].QuestionCount, items.Count);                      // Module fully assembled
            Assert.All(items, i => Assert.Equal(plan[m].Section, SectionOf(i.Question!))); // Right section only
            Assert.Equal(items.Count, items.Select(i => i.QuestionId).Distinct().Count()); // No duplicates

            svc.StartModuleClock(test);
            foreach (var i in items)                                                // Answer everything correctly
                svc.SaveItem(test, i.Id, i.Question!.Format == AnswerFormat.MultipleChoice ? i.Question.CorrectAnswer
                    : i.Question.CorrectAnswer.Split('|')[0], false, "[]", 40);
            svc.SubmitModule(test);
        }

        Assert.Equal(TestStatus.Completed, test.Status);
        Assert.Equal(ModuleRoute.Upper, test.RwRoute);                              // Perfect module 1 -> harder module 2
        Assert.Equal(ModuleRoute.Upper, test.MathRoute);
        Assert.Equal(800, test.RwScore);
        Assert.Equal(800, test.MathScore);
        Assert.Equal(1600, test.TotalScore);
        Assert.Equal(98, _db.Attempts.Count(a => a.TestAttemptId == test.Id));      // Every answer recorded for mastery
    }

    [Fact]
    public void Blank_module_one_routes_to_easier_module_two()
    {
        var svc = new TestService(_db, new ProgressService(_db, new MasteryService()));
        var test = svc.Start(_userId, TestType.Math);
        svc.StartModuleClock(test);
        svc.SubmitModule(test);                                                     // Submit with no answers
        Assert.Equal(ModuleRoute.Lower, test.MathRoute);
        svc.StartModuleClock(test);
        svc.SubmitModule(test);
        Assert.Equal(TestStatus.Completed, test.Status);
        Assert.Equal(200, test.MathScore);
        Assert.Null(test.TotalScore);                                               // Section test has no total
    }

    /// <summary>Section of a question via its skill's domain.</summary>
    /// <param name="q">Question.</param>
    private SatSection SectionOf(Question q) => _db.Skills.Include(s => s.Domain).First(s => s.Id == q.SkillId).Domain!.Section;

    /// <summary>Closes the in-memory DB.</summary>
    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
