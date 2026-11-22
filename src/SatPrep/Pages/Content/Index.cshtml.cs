using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Pages.Content;

/// <summary>
/// Content management (no admin role, per owner decision): bank overview,
/// upload / re-import question packs, question statistics with retire /
/// restore, link to the editor, and a database backup download.
/// </summary>
public class IndexModel : PageModel
{
    private const int PageSize = 40;            // Rows per page in the stats table
    private readonly AppDbContext _db;          // Database
    private readonly ContentImporter _importer; // Pack importer
    private readonly IWebHostEnvironment _env;  // Locates the Content folder

    /// <param name="db">Database.</param>
    /// <param name="importer">Pack importer.</param>
    /// <param name="env">Host environment.</param>
    public IndexModel(AppDbContext db, ContentImporter importer, IWebHostEnvironment env)
    {
        _db = db;
        _importer = importer;
        _env = env;
    }

    /// <summary>Questions per source pack.</summary>
    public record SourceRow(string Source, int Active, int Retired);

    /// <summary>One question with usage statistics across all learners.</summary>
    public record StatRow(int Id, string Skill, Difficulty Difficulty, string Preview, int Attempts, int PctCorrect, bool Active, bool Manual, bool Suspicious);

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }          // Text filter
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "attempts"; // attempts | hardest | easiest | flagged
    [BindProperty(SupportsGet = true, Name = "p")] public int PageNo { get; set; } = 1; // 1-based page
    [TempData] public string? Message { get; set; }                                 // Result of the last action

    public List<SourceRow> Sources { get; private set; } = new();                   // Bank overview
    public List<StatRow> Rows { get; private set; } = new();                        // Current page of stats
    public int Total { get; private set; }                                          // Matching rows
    public int Learners { get; private set; }                                       // Number of learners
    public string PacksFolder => Path.Combine(_env.ContentRootPath, "Content", "packs"); // Where packs live

    /// <summary>Loads overview and statistics.</summary>
    public void OnGet()
    {
        Learners = _db.Users.Count();
        Sources = _db.Questions.GroupBy(q => q.Source)
            .Select(g => new SourceRow(g.Key, g.Count(q => q.IsActive), g.Count(q => !q.IsActive)))
            .ToList().OrderByDescending(s => s.Active).ToList();

        var stats = _db.Attempts.GroupBy(a => a.QuestionId)
            .Select(g => new { g.Key, N = g.Count(), C = g.Count(a => a.IsCorrect) })
            .ToDictionary(x => x.Key, x => (x.N, x.C));

        var q = _db.Questions.Include(x => x.Skill).AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(x => x.Stem.Contains(Search) || (x.Passage != null && x.Passage.Contains(Search)) || x.ExternalId.Contains(Search));

        var rows = q.Select(x => new { x.Id, Skill = x.Skill!.Name, x.Difficulty, x.Stem, x.Passage, x.IsActive, x.IsManual }).ToList()
            .Select(x =>
            {
                stats.TryGetValue(x.Id, out var s);
                int pct = s.N == 0 ? 0 : 100 * s.C / s.N;
                // Flag questions whose real accuracy contradicts their label (needs at least 8 answers)
                bool suspicious = s.N >= 8 && ((x.Difficulty == Difficulty.Easy && pct < 30) || (x.Difficulty == Difficulty.Hard && pct > 90));
                var text = (x.Passage ?? x.Stem).Replace("\n", " ");
                return new StatRow(x.Id, x.Skill, x.Difficulty, text.Length > 100 ? text[..100] + "…" : text, s.N, pct, x.IsActive, x.IsManual, suspicious);
            });

        rows = Sort switch
        {
            "hardest" => rows.Where(r => r.Attempts > 0).OrderBy(r => r.PctCorrect),
            "easiest" => rows.Where(r => r.Attempts > 0).OrderByDescending(r => r.PctCorrect),
            "flagged" => rows.Where(r => r.Suspicious || !r.Active),
            "manual" => rows.Where(r => r.Manual),
            _ => rows.OrderByDescending(r => r.Attempts).ThenBy(r => r.Id)
        };
        var list = rows.ToList();
        Total = list.Count;
        PageNo = Math.Clamp(PageNo, 1, Math.Max(1, (Total + PageSize - 1) / PageSize));
        Rows = list.Skip((PageNo - 1) * PageSize).Take(PageSize).ToList();
    }

    /// <summary>Uploads a JSON pack: saves it into Content/packs (so it survives restarts) and imports it.</summary>
    /// <param name="pack">Uploaded file.</param>
    public IActionResult OnPostUpload(IFormFile? pack)
    {
        if (pack is null || pack.Length == 0 || !pack.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            Message = "Choose a .json question pack to upload.";
            return RedirectToPage();
        }
        Directory.CreateDirectory(PacksFolder);
        var safeName = string.Concat(Path.GetFileNameWithoutExtension(pack.FileName).Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        var target = Path.Combine(PacksFolder, $"uploaded-{(safeName.Length > 0 ? safeName : "pack")}.json");
        using (var fs = System.IO.File.Create(target)) pack.CopyTo(fs);                 // Keep a copy next to the app

        var r = _importer.ImportFile(target);
        Message = $"Imported {Path.GetFileName(target)}: {r.Added} added, {r.Updated} updated, {r.Skipped} skipped."
                  + (r.Errors.Any() ? $" Problems: {string.Join("; ", r.Errors.Take(5))}" : "");
        if (r.Added + r.Updated == 0 && r.Errors.Any()) System.IO.File.Delete(target);   // Don't keep a broken file
        return RedirectToPage();
    }

    /// <summary>Re-imports every pack in the Content folder (after copying new files in by hand).</summary>
    public IActionResult OnPostReimport()
    {
        var r = _importer.ImportFolder(PacksFolder);
        _importer.ImportVocab(Path.Combine(_env.ContentRootPath, "Content", "vocab.json"));
        Message = $"Re-import finished: {r.Added} added, {r.Updated} updated, {r.Skipped} skipped, {r.Errors.Count} problems.";
        return RedirectToPage();
    }

    /// <summary>Retires or restores a question (history is kept either way).</summary>
    /// <param name="id">Question id.</param>
    public IActionResult OnPostToggle(int id)
    {
        var q = _db.Questions.Find(id);
        if (q is not null)
        {
            q.IsActive = !q.IsActive;
            q.IsManual = true;                                   // Keep the decision across re-imports
            _db.SaveChanges();
            Message = q.IsActive ? "Question restored." : "Question retired: it will no longer appear in practice or tests.";
        }
        return Redirect($"/Content?search={Uri.EscapeDataString(Search ?? "")}&sort={Sort}&p={PageNo}");
    }

    /// <summary>Downloads a consistent snapshot of the database (all learners' progress).</summary>
    public IActionResult OnPostBackup()
    {
        var path = DatabaseMaintenance.Snapshot(_db, "manual");
        var bytes = System.IO.File.ReadAllBytes(path);
        return File(bytes, "application/octet-stream", Path.GetFileName(path));
    }
}
