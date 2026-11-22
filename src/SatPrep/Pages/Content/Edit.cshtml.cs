using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages.Content;

/// <summary>
/// Create or edit a single question. Edited questions are flagged IsManual so
/// re-importing packs never overwrites local fixes.
/// </summary>
public class EditModel : PageModel
{
    private readonly AppDbContext _db; // Database

    /// <param name="db">Database.</param>
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty] public int? Id { get; set; }                                      // Null = new question
    [BindProperty] public int SkillId { get; set; }                                  // Skill
    [BindProperty] public Difficulty Difficulty { get; set; } = Difficulty.Medium;   // Difficulty
    [BindProperty] public AnswerFormat Format { get; set; }                          // MC or typed
    [BindProperty] public string? Passage { get; set; }                              // Optional passage
    [BindProperty] public string Stem { get; set; } = "";                            // Prompt
    [BindProperty] public string?[] Choices { get; set; } = new string?[4];          // A-D texts
    [BindProperty] public string CorrectAnswer { get; set; } = "A";                  // Letter or accepted answers
    [BindProperty] public string Explanation { get; set; } = "";                     // Worked explanation
    public List<Skill> Skills { get; private set; } = new();                         // Skill dropdown
    public string? Error { get; private set; }                                       // Validation message

    /// <summary>Loads an existing question, or an empty form.</summary>
    /// <param name="id">Question id (optional).</param>
    public IActionResult OnGet(int? id)
    {
        LoadSkills();
        if (id is null)
        {
            SkillId = Skills.First().Id;
            return Page();
        }
        var q = _db.Questions.Find(id);
        if (q is null) return NotFound();
        Id = q.Id;
        SkillId = q.SkillId;
        Difficulty = q.Difficulty;
        Format = q.Format;
        Passage = q.Passage;
        Stem = q.Stem;
        var choices = JsonSerializer.Deserialize<List<string>>(q.ChoicesJson) ?? new();
        Choices = Enumerable.Range(0, 4).Select(i => i < choices.Count ? choices[i] : null).ToArray();
        CorrectAnswer = q.CorrectAnswer;
        Explanation = q.Explanation;
        return Page();
    }

    /// <summary>Validates and saves the question.</summary>
    public IActionResult OnPost()
    {
        LoadSkills();
        var choices = Choices.Select(c => (c ?? "").Trim()).ToList();
        if (string.IsNullOrWhiteSpace(Stem)) Error = "The question text is required.";
        else if (Format == AnswerFormat.MultipleChoice && choices.Any(string.IsNullOrEmpty)) Error = "Multiple choice needs all four choices.";
        else if (Format == AnswerFormat.MultipleChoice && !new[] { "A", "B", "C", "D" }.Contains(CorrectAnswer.Trim().ToUpperInvariant())) Error = "The correct answer must be A, B, C or D.";
        else if (string.IsNullOrWhiteSpace(CorrectAnswer)) Error = "Enter the correct answer.";
        if (Error is not null) return Page();

        var q = Id is int id ? _db.Questions.Find(id) : null;
        if (q is null)
        {
            q = new Question { ExternalId = $"manual-{Guid.NewGuid():N}", Source = "manual | Created in SatPrep | Internal" };
            _db.Questions.Add(q);
        }
        q.SkillId = SkillId;
        q.Difficulty = Difficulty;
        q.Format = Format;
        q.Passage = string.IsNullOrWhiteSpace(Passage) ? null : Passage.Trim();
        q.Stem = Stem.Trim();
        q.ChoicesJson = JsonSerializer.Serialize(Format == AnswerFormat.MultipleChoice ? choices : new List<string>());
        q.CorrectAnswer = Format == AnswerFormat.MultipleChoice ? CorrectAnswer.Trim().ToUpperInvariant() : CorrectAnswer.Trim();
        q.Explanation = Explanation.Trim();
        q.IsManual = true;                                                           // Protect from re-import
        _db.SaveChanges();
        TempData["Message"] = "Question saved.";
        return Redirect($"/Practice?q={q.Id}&from=bank");
    }

    /// <summary>Skill dropdown, in official order.</summary>
    private void LoadSkills()
        => Skills = _db.Skills.Include(s => s.Domain).ToList()
            .OrderBy(s => s.Domain!.Section).ThenBy(s => s.Domain!.SortOrder).ThenBy(s => s.SortOrder).ToList();
}
