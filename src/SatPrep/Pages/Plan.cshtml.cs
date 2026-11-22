using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;

namespace SatPrep.Pages;

/// <summary>Study plan settings: test date, target score, daily minutes, practice-test weekday.</summary>
public class PlanModel : PageModel
{
    private readonly AppDbContext _db; // Database

    /// <param name="db">Database.</param>
    public PlanModel(AppDbContext db) => _db = db;

    [BindProperty, DataType(DataType.Date)] public DateTime TestDate { get; set; } = DateTime.Today.AddDays(60); // SAT date
    [BindProperty, Range(400, 1600)] public int TargetScore { get; set; } = 1400;    // Goal
    [BindProperty, Range(10, 300)] public int MinutesPerDay { get; set; } = 45;      // Daily budget
    [BindProperty, Range(0, 6)] public int PracticeTestDay { get; set; } = 6;        // 0 = Sunday ... 6 = Saturday
    public bool Saved { get; private set; }                                          // Show "saved" message

    /// <summary>Loads existing settings.</summary>
    public void OnGet()
    {
        var p = _db.StudyPlans.FirstOrDefault(x => x.UserId == User.UserId());
        if (p is null) return;
        TestDate = p.TestDate;
        TargetScore = p.TargetScore;
        MinutesPerDay = p.MinutesPerDay;
        PracticeTestDay = p.PracticeTestDay;
    }

    /// <summary>Saves settings and returns to the home page.</summary>
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();
        int userId = User.UserId();
        var p = _db.StudyPlans.FirstOrDefault(x => x.UserId == userId);
        if (p is null)
        {
            p = new StudyPlan { UserId = userId };
            _db.StudyPlans.Add(p);
        }
        p.TestDate = TestDate.Date;
        p.TargetScore = TargetScore / 10 * 10;                                       // SAT scores are multiples of 10
        p.MinutesPerDay = MinutesPerDay;
        p.PracticeTestDay = PracticeTestDay;
        _db.SaveChanges();
        return Redirect("/");
    }
}
