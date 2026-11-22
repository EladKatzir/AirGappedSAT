using System.Text.Json;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>
/// Imports question "content packs" (JSON files) into the database.
///
/// Packs live in Content/packs/*.json and ship inside the release zip, so new
/// questions reach the air-gapped server simply by dropping a file in and
/// restarting (or pressing "Import" in admin, later). Import is idempotent:
/// questions are matched on ExternalId and updated in place.
///
/// Pack format (see Content/packs/README.md for the full spec):
/// {
///   "pack": "sample-original", "source": "Written in-house", "licence": "Internal use",
///   "questions": [ { "id": "...", "skill": "ALG.LF", "difficulty": "Medium",
///                    "format": "MultipleChoice", "passage": null, "stem": "...",
///                    "choices": ["..","..","..",".."], "answer": "B", "explanation": "..." } ]
/// }
/// </summary>
public class ContentImporter
{
    private readonly AppDbContext _db;                 // Database to write into
    private readonly ILogger<ContentImporter> _log;    // Logger for import summaries / bad rows

    /// <summary>JSON options: case-insensitive names, enums as strings.</summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <param name="db">Database context.</param>
    /// <param name="log">Logger.</param>
    public ContentImporter(AppDbContext db, ILogger<ContentImporter> log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>Result counts returned to the caller / admin UI.</summary>
    public record ImportResult(int Added, int Updated, int Skipped, List<string> Errors);

    /// <summary>Imports every *.json pack in a folder.</summary>
    /// <param name="folder">Absolute path of the packs folder.</param>
    public ImportResult ImportFolder(string folder)
    {
        var total = new ImportResult(0, 0, 0, new List<string>()); // Running totals across all files
        if (!Directory.Exists(folder)) return total;                // Nothing to import

        foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(f => f))
        {
            var r = ImportFile(file);                               // Import one pack
            total = total with
            {
                Added = total.Added + r.Added,
                Updated = total.Updated + r.Updated,
                Skipped = total.Skipped + r.Skipped
            };
            total.Errors.AddRange(r.Errors);                        // Keep every error message
        }

        _log.LogInformation("Content import: {Added} added, {Updated} updated, {Skipped} skipped, {Errors} errors",
            total.Added, total.Updated, total.Skipped, total.Errors.Count);
        return total;
    }

    /// <summary>Imports a single pack file.</summary>
    /// <param name="path">Path of the JSON file.</param>
    public ImportResult ImportFile(string path)
    {
        var errors = new List<string>();                            // Problems found in this file
        int added = 0, updated = 0, skipped = 0;                    // Counters for this file

        PackDto? pack;
        try
        {
            pack = JsonSerializer.Deserialize<PackDto>(File.ReadAllText(path), JsonOpts); // Parse the pack
        }
        catch (JsonException ex)
        {
            errors.Add($"{Path.GetFileName(path)}: invalid JSON - {ex.Message}");
            return new ImportResult(0, 0, 0, errors);
        }
        if (pack?.Questions is null) return new ImportResult(0, 0, 0, errors);

        var skillsByCode = _db.Skills.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase); // Code -> Skill lookup
        string source = $"{pack.Pack} | {pack.Source} | {pack.Licence}";                          // Attribution stored per question

        foreach (var q in pack.Questions)
        {
            var problem = Validate(q, skillsByCode);                 // Reject malformed rows instead of crashing
            if (problem is not null)
            {
                errors.Add($"{Path.GetFileName(path)} [{q.Id}]: {problem}");
                skipped++;
                continue;
            }

            var entity = _db.Questions.FirstOrDefault(x => x.ExternalId == q.Id); // Existing row?
            if (entity is null)
            {
                entity = new Question { ExternalId = q.Id! };
                _db.Questions.Add(entity);                           // Queue insert
                added++;
            }
            else if (entity.IsManual)
            {
                skipped++;                                           // Edited in the app - never overwrite local edits
                continue;
            }
            else
            {
                updated++;                                           // Will overwrite fields below
            }

            // Copy pack fields onto the entity
            entity.SkillId = skillsByCode[q.Skill!].Id;
            entity.Difficulty = q.Difficulty;
            entity.Format = q.Format;
            entity.Passage = q.Passage;
            entity.Stem = q.Stem!;
            entity.ChoicesJson = JsonSerializer.Serialize(q.Choices ?? new List<string>());
            entity.CorrectAnswer = q.Answer!.Trim();
            entity.Explanation = q.Explanation ?? "";
            entity.Source = source;
            entity.IsActive = true;
        }

        _db.SaveChanges();                                           // Commit the whole file at once
        return new ImportResult(added, updated, skipped, errors);
    }

    /// <summary>
    /// Imports the vocabulary bank (Content/vocab.json): an array of
    /// { "word", "pos", "definition", "example" }. Idempotent (matched on word).
    /// </summary>
    /// <param name="path">Path of vocab.json.</param>
    /// <returns>Number of words added or updated.</returns>
    public int ImportVocab(string path)
    {
        if (!File.Exists(path)) return 0;
        List<VocabDto>? words;
        try { words = JsonSerializer.Deserialize<List<VocabDto>>(File.ReadAllText(path), JsonOpts); }
        catch (JsonException ex)
        {
            _log.LogWarning("vocab.json is invalid: {Message}", ex.Message);
            return 0;
        }
        if (words is null) return 0;

        var existing = _db.VocabWords.ToDictionary(w => w.Word, StringComparer.OrdinalIgnoreCase); // Word -> row
        int n = 0;
        foreach (var w in words.Where(w => !string.IsNullOrWhiteSpace(w.Word)))
        {
            if (!existing.TryGetValue(w.Word!.Trim(), out var row))
            {
                row = new VocabWord { Word = w.Word.Trim() };
                _db.VocabWords.Add(row);
                existing[row.Word] = row;
            }
            row.PartOfSpeech = w.Pos ?? "";
            row.Definition = w.Definition ?? "";
            row.Example = w.Example ?? "";
            n++;
        }
        _db.SaveChanges();
        _log.LogInformation("Vocabulary import: {Count} words", n);
        return n;
    }

    /// <summary>One vocabulary entry as written in vocab.json.</summary>
    public class VocabDto
    {
        public string? Word { get; set; }          // The word
        public string? Pos { get; set; }           // Part of speech
        public string? Definition { get; set; }    // Definition
        public string? Example { get; set; }       // Example sentence
    }

    /// <summary>Returns an error message for a bad question, or null if it is valid.</summary>
    /// <param name="q">Question as read from JSON.</param>
    /// <param name="skills">Known skills by code.</param>
    public static string? Validate(QuestionDto q, IReadOnlyDictionary<string, Skill> skills)
    {
        if (string.IsNullOrWhiteSpace(q.Id)) return "missing id";
        if (string.IsNullOrWhiteSpace(q.Skill) || !skills.ContainsKey(q.Skill)) return $"unknown skill '{q.Skill}'";
        if (string.IsNullOrWhiteSpace(q.Stem)) return "missing stem";
        if (string.IsNullOrWhiteSpace(q.Answer)) return "missing answer";

        if (q.Format == AnswerFormat.MultipleChoice)
        {
            if (q.Choices is null || q.Choices.Count != 4) return "multiple choice needs exactly 4 choices";
            if (!new[] { "A", "B", "C", "D" }.Contains(q.Answer.Trim().ToUpperInvariant())) return "answer must be A-D";
        }
        return null;
    }

    /// <summary>Top-level pack shape.</summary>
    public class PackDto
    {
        public string Pack { get; set; } = "";               // Pack name
        public string Source { get; set; } = "";             // Who wrote / where it came from
        public string Licence { get; set; } = "";            // Licence or permission reference
        public List<QuestionDto>? Questions { get; set; }    // The questions
    }

    /// <summary>One question as written in a pack.</summary>
    public class QuestionDto
    {
        public string? Id { get; set; }                      // Globally unique id, e.g. "orig-alg-lf-0001"
        public string? Skill { get; set; }                   // Skill code from SatTaxonomy
        public Difficulty Difficulty { get; set; }           // Easy / Medium / Hard
        public AnswerFormat Format { get; set; }             // MultipleChoice / StudentProduced
        public string? Passage { get; set; }                 // Optional passage
        public string? Stem { get; set; }                    // Question prompt
        public List<string>? Choices { get; set; }           // 4 choices for MC
        public string? Answer { get; set; }                  // "A".."D" or accepted numeric answers "a|b"
        public string? Explanation { get; set; }             // Worked explanation
    }
}
