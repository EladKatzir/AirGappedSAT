using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;
using SatPrep.Pages.Shared;
using SatPrep.Services;

namespace SatPrep.Pages.Practice;

/// <summary>
/// Practice by topic.
///   GET  /Practice                       -> topic picker
///   GET  /Practice?skill=5&amp;difficulty=…  -> next question for that filter
///   GET  /Practice?q=123                 -> one specific question (from bank / mistakes)
///   POST (same URL)                      -> grade, record, show explanation + "Next"
///   POST ?handler=Mark                   -> toggle bookmark (JSON, used by JS)
/// </summary>
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;            // Database
    private readonly PracticeSelector _selector;  // Picks questions
    private readonly ProgressService _progress;   // Records answers
    private readonly StatsService _stats;         // Skill progress for the picker

    /// <param name="db">Database.</param>
    /// <param name="selector">Question selector.</param>
    /// <param name="progress">Progress recorder.</param>
    /// <param name="stats">Stats queries.</param>
    public IndexModel(AppDbContext db, PracticeSelector selector, ProgressService progress, StatsService stats)
    {
        _db = db;
        _selector = selector;
        _progress = progress;
        _stats = stats;
    }

    // ----- filter (query string) -----
    [BindProperty(SupportsGet = true)] public SatSection? Section { get; set; }      // Section filter
    [BindProperty(SupportsGet = true)] public int? Domain { get; set; }              // Domain id filter
    [BindProperty(SupportsGet = true)] public int? Skill { get; set; }               // Skill id filter
    [BindProperty(SupportsGet = true)] public Difficulty? Difficulty { get; set; }   // null = adaptive
    [BindProperty(SupportsGet = true)] public PracticeMode Mode { get; set; } = PracticeMode.New; // Pool
    [BindProperty(SupportsGet = true)] public int? Q { get; set; }                   // Specific question id
    [BindProperty(SupportsGet = true)] public int? Exclude { get; set; }             // Question just answered
    [BindProperty(SupportsGet = true)] public string? From { get; set; }             // "bank" / "mistakes" (back link)

    // ----- posted answer -----
    [BindProperty] public int QuestionId { get; set; }                               // Question answered
    [BindProperty] public string? Answer { get; set; }                               // Learner's answer
    [BindProperty] public long StartedMs { get; set; }                               // When the question was shown (unix ms)

    // ----- view state -----
    public bool ShowPicker { get; private set; }                                     // True = topic picker
    public QuestionCardModel? Card { get; private set; }                             // Question to show
    public string ScopeTitle { get; private set; } = "";                             // e.g. "Algebra · Adaptive"
    public List<SkillProgress> Skills { get; private set; } = new();                 // For the picker
    public int ReviewDue { get; private set; }                                       // Due-review count (picker)
    public int AnsweredThisSession { get; private set; }                             // Counter shown in the header
    public bool NothingLeft { get; private set; }                                    // Filter matched no questions

    /// <summary>True when a topic/pool filter is active (so "Next question" makes sense).</summary>
    public bool HasFilter => Section is not null || Domain is not null || Skill is not null || Request.Query.ContainsKey("mode");

    /// <summary>Shows the picker, a specific question, or the next question for the filter.</summary>
    public IActionResult OnGet()
    {
        int userId = User.UserId();
        if (Q is int qid)                                                            // Specific question
        {
            var q = Load(qid);
            if (q is null) return NotFound();
            Card = new QuestionCardModel { Question = q, IsMarked = IsMarked(userId, q.Id) };
            ScopeTitle = q.Skill!.Name;
            return Page();
        }

        if (!HasFilter)                                                              // Picker
        {
            ShowPicker = true;
            Skills = _stats.SkillProgress(userId);
            ReviewDue = _stats.ReviewDueCount(userId);
            return Page();
        }

        var next = _selector.Next(userId, Filter(), Exclude);
        ScopeTitle = DescribeScope();
        if (next is null)
        {
            NothingLeft = true;
            return Page();
        }
        Card = new QuestionCardModel { Question = next, IsMarked = IsMarked(userId, next.Id) };
        return Page();
    }

    /// <summary>Grades the answer and shows the explanation.</summary>
    public IActionResult OnPost()
    {
        int userId = User.UserId();
        var q = Load(QuestionId);
        if (q is null) return NotFound();

        int seconds = StartedMs > 0 ? (int)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartedMs) / 1000) : 0;
        var source = Mode is PracticeMode.ReviewDue or PracticeMode.Incorrect || From == "mistakes"
            ? AttemptSource.Review : AttemptSource.Practice;                         // Review vs fresh practice
        var result = _progress.Record(userId, q, Answer, seconds, source);

        Card = new QuestionCardModel
        {
            Question = q,
            Answered = true,
            IsCorrect = result.IsCorrect,
            Given = Answer?.Trim(),
            MasteryBefore = result.MasteryBefore,
            MasteryAfter = MasteryService.MasteryPercent(result.Mastery),
            IsMarked = IsMarked(userId, q.Id)
        };
        ScopeTitle = Q is null ? DescribeScope() : q.Skill!.Name;
        AnsweredThisSession = _stats.Today(userId).Answered;
        return Page();
    }

    /// <summary>Toggles the bookmark on a question (AJAX).</summary>
    /// <param name="questionId">Question id.</param>
    public IActionResult OnPostMark(int questionId)
        => new JsonResult(new { marked = _progress.ToggleMark(User.UserId(), questionId) });

    /// <summary>URL for "Next question" with the same filter, excluding the current question.</summary>
    public string NextUrl()
    {
        var parts = new List<string>();
        if (Section is not null) parts.Add($"section={Section}");
        if (Domain is not null) parts.Add($"domain={Domain}");
        if (Skill is not null) parts.Add($"skill={Skill}");
        if (Difficulty is not null) parts.Add($"difficulty={Difficulty}");
        parts.Add($"mode={Mode}");
        if (Card is not null) parts.Add($"exclude={Card.Question.Id}");
        return "/Practice?" + string.Join("&", parts);
    }

    /// <summary>Builds the selector filter from the query string.</summary>
    private PracticeFilter Filter() => new(Section, Domain, Skill, Difficulty, Mode);

    /// <summary>Loads a question with skill + domain for display.</summary>
    /// <param name="id">Question id.</param>
    private Question? Load(int id)
        => _db.Questions.Include(x => x.Skill).ThenInclude(s => s!.Domain).FirstOrDefault(x => x.Id == id);

    /// <summary>True if the learner bookmarked the question.</summary>
    /// <param name="userId">Learner.</param>
    /// <param name="questionId">Question.</param>
    private bool IsMarked(int userId, int questionId) => _db.Marks.Any(m => m.UserId == userId && m.QuestionId == questionId);

    /// <summary>Human-readable description of the current filter.</summary>
    private string DescribeScope()
    {
        string topic = Skill is int s ? _db.Skills.Find(s)?.Name ?? "Skill"
            : Domain is int d ? _db.Domains.Find(d)?.Name ?? "Domain"
            : Section == SatSection.Math ? "Math"
            : Section == SatSection.ReadingWriting ? "Reading and Writing"
            : "All topics";
        string pool = Mode switch
        {
            PracticeMode.ReviewDue => "Review due",
            PracticeMode.Incorrect => "Previously missed",
            PracticeMode.Marked => "Marked questions",
            PracticeMode.All => "All questions",
            _ => "New questions"
        };
        return $"{topic} · {Difficulty?.ToString() ?? "Adaptive"} · {pool}";
    }
}
