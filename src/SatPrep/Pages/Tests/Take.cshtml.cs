using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages.Tests;

/// <summary>
/// The test player. Server responsibilities: which module is active, the
/// clock, saving answers as the learner goes, grading/routing on submit.
/// The JS player (wwwroot/js/test-player.js) handles navigation, timer
/// display, mark for review, answer eliminator, highlighter, calculator.
/// </summary>
public class TakeModel : PageModel
{
    private readonly AppDbContext _db;     // Database
    private readonly TestService _tests;   // Test logic

    /// <param name="db">Database.</param>
    /// <param name="tests">Test service.</param>
    public TakeModel(AppDbContext db, TestService tests)
    {
        _db = db;
        _tests = tests;
    }

    public TestAttempt Test { get; private set; } = null!;         // The test
    public ModuleSpec Spec { get; private set; } = null!;          // Current module definition
    public int ModuleNumber { get; private set; }                  // 1-based module number in the plan
    public int ModuleCount { get; private set; }                   // Modules in the plan
    public bool Started { get; private set; }                      // Clock running?
    public bool BreakBefore { get; private set; }                  // Show the 10-minute break screen
    public int SecondsLeft { get; private set; }                   // Remaining module time
    public string ItemsJson { get; private set; } = "[]";          // Questions for the JS player

    /// <summary>Shows the intro/break screen or the player for the current module.</summary>
    /// <param name="id">Test id.</param>
    public IActionResult OnGet(int id)
    {
        if (!Load(id)) return NotFound();
        if (Test.Status == TestStatus.Completed) return Redirect($"/Tests/Results/{id}");

        if (Started && SecondsLeft <= 0)                                   // Time ran out while away: submit what was saved
        {
            _tests.SubmitModule(Test);
            return Redirect($"/Tests/Take/{id}");
        }
        if (Started) ItemsJson = BuildItemsJson();
        return Page();
    }

    /// <summary>Starts the module clock.</summary>
    /// <param name="id">Test id.</param>
    public IActionResult OnPostStartModule(int id)
    {
        if (!Load(id)) return NotFound();
        _tests.StartModuleClock(Test);
        return Redirect($"/Tests/Take/{id}");
    }

    /// <summary>Autosave of one question (AJAX).</summary>
    /// <param name="id">Test id.</param>
    /// <param name="itemId">Test item id.</param>
    /// <param name="answer">Current answer.</param>
    /// <param name="marked">Mark-for-review flag.</param>
    /// <param name="eliminated">JSON array of crossed-out letters.</param>
    /// <param name="seconds">Seconds since last save on this question.</param>
    public IActionResult OnPostSave(int id, int itemId, string? answer, bool marked, string? eliminated, int seconds)
    {
        if (!Load(id) || Test.Status != TestStatus.InProgress) return new JsonResult(new { ok = false });
        _tests.SaveItem(Test, itemId, answer, marked, eliminated, seconds);
        return new JsonResult(new { ok = true, secondsLeft = TestService.SecondsLeft(Test) });
    }

    /// <summary>Submits the current module (button or timer) and moves on.</summary>
    /// <param name="id">Test id.</param>
    public IActionResult OnPostSubmitModule(int id)
    {
        if (!Load(id)) return NotFound();
        if (Test.Status == TestStatus.InProgress) _tests.SubmitModule(Test);
        return Test.Status == TestStatus.Completed ? Redirect($"/Tests/Results/{id}") : Redirect($"/Tests/Take/{id}");
    }

    /// <summary>Loads the test (only the learner's own) and module state.</summary>
    /// <param name="id">Test id.</param>
    private bool Load(int id)
    {
        var t = _db.Tests.FirstOrDefault(x => x.Id == id && x.UserId == User.UserId());
        if (t is null) return false;
        Test = t;
        var plan = TestService.ModulePlan(t.Type);
        Spec = plan[t.CurrentModule];
        ModuleNumber = t.CurrentModule + 1;
        ModuleCount = plan.Count;
        Started = t.ModuleStartedUtc is not null;
        BreakBefore = TestService.HasBreakBefore(t.Type, t.CurrentModule);
        SecondsLeft = TestService.SecondsLeft(t);
        return true;
    }

    /// <summary>Serializes the current module's questions (pre-rendered HTML) for the player.</summary>
    private string BuildItemsJson()
    {
        var items = _db.TestItems.Include(i => i.Question)
            .Where(i => i.TestAttemptId == Test.Id && i.ModuleIndex == Test.CurrentModule)
            .OrderBy(i => i.Order).ToList();

        var data = items.Select(i => new
        {
            id = i.Id,
            order = i.Order,
            format = i.Question!.Format == AnswerFormat.StudentProduced ? "spr" : "mc",
            passage = Md.Question(i.Question.Passage, i.Question.IsHtml),
            stem = Md.Question(i.Question.Stem, i.Question.IsHtml),
            choices = (JsonSerializer.Deserialize<List<string>>(i.Question.ChoicesJson) ?? new())
                .Select(c => Md.QuestionInline(c, i.Question.IsHtml)).ToList(),
            answer = i.GivenAnswer,
            marked = i.IsMarked,
            eliminated = JsonSerializer.Deserialize<List<string>>(i.EliminatedJson) ?? new()
        });
        return JsonSerializer.Serialize(data);
    }
}
