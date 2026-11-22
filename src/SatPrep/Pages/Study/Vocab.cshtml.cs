using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Extensions;
using SatPrep.Models;

namespace SatPrep.Pages.Study;

/// <summary>
/// Vocabulary flashcards using a Leitner system:
///   box 0 -> due again in 10 min, 1 -> 1 day, 2 -> 3 days, 3 -> 7 days, 4 = learned (21 days).
/// "Knew it" moves a card up a box; "Still learning" drops it back to box 0.
/// Also shows the full word list (?view=list).
/// </summary>
public class VocabModel : PageModel
{
    /// <summary>Delay before a card in each box is due again.</summary>
    public static readonly TimeSpan[] BoxDelays =
        { TimeSpan.FromMinutes(10), TimeSpan.FromDays(1), TimeSpan.FromDays(3), TimeSpan.FromDays(7), TimeSpan.FromDays(21) };

    private readonly AppDbContext _db; // Database

    /// <param name="db">Database.</param>
    public VocabModel(AppDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)] public string? View { get; set; }   // "list" shows the word list
    public VocabWord? Card { get; private set; }                             // Card to study now
    public int CardBox { get; private set; }                                 // Its current box
    public int DueCount { get; private set; }                                // Cards due (incl. new)
    public int LearnedCount { get; private set; }                            // Cards in box 4
    public List<(VocabWord Word, int Box)> All { get; private set; } = new(); // Word list view

    /// <summary>Shows the next due card or the word list.</summary>
    public void OnGet()
    {
        int userId = User.UserId();
        var progress = _db.VocabProgress.Where(p => p.UserId == userId).ToDictionary(p => p.VocabWordId);
        var words = _db.VocabWords.OrderBy(w => w.Word).ToList();
        var now = DateTime.UtcNow;
        LearnedCount = progress.Values.Count(p => p.Box >= 4);

        if (View == "list")
        {
            All = words.Select(w => (w, progress.TryGetValue(w.Id, out var p) ? p.Box : -1)).ToList();
            return;
        }

        // Due reviews first (oldest due first), then new words in a stable pseudo-random order
        var due = progress.Values.Where(p => p.NextDueUtc <= now).OrderBy(p => p.NextDueUtc).ToList();
        var unseen = words.Where(w => !progress.ContainsKey(w.Id)).OrderBy(w => (w.Id * 7919) % 1009).ToList();
        DueCount = due.Count + unseen.Count;

        if (due.Any())
        {
            Card = words.First(w => w.Id == due[0].VocabWordId);
            CardBox = due[0].Box;
        }
        else if (unseen.Any())
        {
            Card = unseen[0];
            CardBox = 0;
        }
    }

    /// <summary>Records a flashcard result and returns to the next card.</summary>
    /// <param name="wordId">Word reviewed.</param>
    /// <param name="knew">True = "Knew it", false = "Still learning".</param>
    public IActionResult OnPostReview(int wordId, bool knew)
    {
        int userId = User.UserId();
        var p = _db.VocabProgress.FirstOrDefault(x => x.UserId == userId && x.VocabWordId == wordId);
        if (p is null)
        {
            p = new VocabProgress { UserId = userId, VocabWordId = wordId };
            _db.VocabProgress.Add(p);
        }
        p.Box = knew ? Math.Min(4, p.Box + 1) : 0;                       // Leitner move
        p.TimesSeen++;
        if (knew) p.TimesCorrect++;
        p.LastReviewedUtc = DateTime.UtcNow;
        p.NextDueUtc = DateTime.UtcNow + BoxDelays[p.Box];
        _db.SaveChanges();
        return RedirectToPage();
    }
}
