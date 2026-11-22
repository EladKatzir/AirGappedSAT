using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages.Tests;

/// <summary>Practice tests hub: start a new test, resume one in progress, see past results.</summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;       // Database
    private readonly TestService _tests;     // Test builder

    /// <param name="db">Database.</param>
    /// <param name="tests">Test service.</param>
    public IndexModel(AppDbContext db, TestService tests)
    {
        _db = db;
        _tests = tests;
    }

    [BindProperty(SupportsGet = true)] public TestType? Start { get; set; }  // Pre-selected type (from the daily plan)
    public List<TestAttempt> History { get; private set; } = new();          // Learner's tests, newest first

    /// <summary>Loads test history.</summary>
    public void OnGet()
        => History = _db.Tests.Where(t => t.UserId == User.UserId() && t.Status != TestStatus.Abandoned)
            .OrderByDescending(t => t.StartedUtc).Take(50).ToList();

    /// <summary>Creates a new test and opens the player.</summary>
    /// <param name="type">Type of test to start.</param>
    public IActionResult OnPostStart(TestType type)
    {
        var test = _tests.Start(User.UserId(), type);
        return Redirect($"/Tests/Take/{test.Id}");
    }

    /// <summary>Abandons an unfinished test (hides it from the list; answers already recorded stay).</summary>
    /// <param name="id">Test id.</param>
    public IActionResult OnPostAbandon(int id)
    {
        var t = _db.Tests.FirstOrDefault(x => x.Id == id && x.UserId == User.UserId() && x.Status == TestStatus.InProgress);
        if (t is not null)
        {
            t.Status = TestStatus.Abandoned;
            _db.SaveChanges();
        }
        return RedirectToPage();
    }
}
