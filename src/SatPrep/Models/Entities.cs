namespace SatPrep.Models;

// =============================================================================
// Core domain entities for SatPrep.
//
// Structure mirrors the official digital SAT layout:
//   Section (Reading & Writing / Math)
//     -> Domain (8 total, e.g. "Algebra")
//        -> Skill (29 total, e.g. "Linear functions")
//           -> Question (tagged with one skill + a difficulty)
//
// Progress is tracked per user per skill (SkillMastery), fed by every
// QuestionAttempt the user makes, whether in free practice or a test.
// =============================================================================

/// <summary>The two scored sections of the digital SAT.</summary>
public enum SatSection
{
    ReadingWriting = 0, // Reading and Writing section (54 questions, 64 min)
    Math = 1            // Math section (44 questions, 70 min)
}

/// <summary>Official three-level difficulty used by the College Board question bank.</summary>
public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

/// <summary>How the answer is given: pick a choice, or type a number (math "grid-in").</summary>
public enum AnswerFormat
{
    MultipleChoice = 0, // Four choices A-D
    StudentProduced = 1 // Free-entry numeric answer (Math only)
}

/// <summary>Where an attempt happened. Used to filter analytics.</summary>
public enum AttemptSource
{
    Practice = 0,     // Free practice by skill / domain
    PracticeTest = 1, // Inside a timed full / section test
    Review = 2        // Re-doing a previously missed question
}

/// <summary>Kinds of practice test a learner can start.</summary>
public enum TestType
{
    Full = 0,           // R&W (2 modules) + break + Math (2 modules), adaptive
    ReadingWriting = 1, // R&W section only (2 modules), adaptive
    Math = 2,           // Math section only (2 modules), adaptive
    Short = 3           // One R&W module + one Math module, no adaptation
}

/// <summary>Lifecycle of a practice test attempt.</summary>
public enum TestStatus
{
    InProgress = 0, // Learner is still taking it
    Completed = 1,  // All modules submitted and scored
    Abandoned = 2   // Learner gave up / deleted it
}

/// <summary>Which version of module 2 the learner was routed to.</summary>
public enum ModuleRoute
{
    None = 0,  // Not decided yet / not adaptive
    Lower = 1, // Easier second module (score ceiling ~600)
    Upper = 2  // Harder second module (score ceiling 800)
}

/// <summary>A content domain such as "Information and Ideas" or "Algebra".</summary>
public class Domain
{
    public int Id { get; set; }                          // Primary key
    public string Code { get; set; } = "";               // Stable short code used by content packs, e.g. "ALG"
    public string Name { get; set; } = "";               // Display name
    public SatSection Section { get; set; }              // Which section the domain belongs to
    public int SortOrder { get; set; }                   // Display order inside its section
    public List<Skill> Skills { get; set; } = new();     // Child skills
}

/// <summary>A single testable skill such as "Transitions" or "Circles".</summary>
public class Skill
{
    public int Id { get; set; }                          // Primary key
    public string Code { get; set; } = "";               // Stable short code used by content packs, e.g. "ALG.LF"
    public string Name { get; set; } = "";               // Display name
    public int DomainId { get; set; }                    // FK to parent domain
    public Domain? Domain { get; set; }                  // Parent domain navigation
    public int SortOrder { get; set; }                   // Display order inside its domain
    public List<Question> Questions { get; set; } = new(); // Questions tagged with this skill
}

/// <summary>One practice question with its answer and explanation.</summary>
public class Question
{
    public int Id { get; set; }                          // Primary key
    public string ExternalId { get; set; } = "";         // Unique id from the content pack (lets re-imports update instead of duplicate)
    public int SkillId { get; set; }                     // FK to the skill this question tests
    public Skill? Skill { get; set; }                    // Skill navigation
    public Difficulty Difficulty { get; set; }           // Easy / Medium / Hard
    public AnswerFormat Format { get; set; }             // Multiple choice or student-produced
    public string? Passage { get; set; }                 // Optional passage / text (Markdown + KaTeX allowed)
    public string Stem { get; set; } = "";               // The actual question prompt (Markdown + KaTeX allowed)
    public string ChoicesJson { get; set; } = "[]";      // JSON array of choice strings, A-D order (empty for student-produced)
    public string CorrectAnswer { get; set; } = "";      // "A".."D" for MC; for SPR a '|' separated list of accepted answers, e.g. "3/2|1.5"
    public string Explanation { get; set; } = "";        // Full worked explanation shown after answering
    public string Source { get; set; } = "";             // Content pack name / attribution (who wrote it, licence)
    public bool IsActive { get; set; } = true;           // Lets an admin retire a bad question without deleting history
    public bool IsManual { get; set; }                   // Created/edited in the admin editor (re-imports never overwrite it)
    public bool IsHtml { get; set; }                     // Text fields are HTML (e.g. official items with MathML) instead of Markdown
}

/// <summary>
/// A learner. No passwords by design: the learner just enters a name and
/// everything they do is remembered under it (closed, trusted network).
/// </summary>
public class AppUser
{
    public int Id { get; set; }                          // Primary key
    public string UserName { get; set; } = "";           // The name typed on the start screen (unique, case-insensitive)
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // First time this name was used
    public DateTime? LastSeenUtc { get; set; }           // Last time this name signed in
}

/// <summary>One answer submitted by a user to one question.</summary>
public class QuestionAttempt
{
    public long Id { get; set; }                         // Primary key
    public int UserId { get; set; }                      // Who answered
    public int QuestionId { get; set; }                  // Which question
    public Question? Question { get; set; }              // Question navigation
    public string GivenAnswer { get; set; } = "";        // What the user answered (empty = skipped)
    public bool IsCorrect { get; set; }                  // Graded result
    public int SecondsSpent { get; set; }                // Time on question (powers pacing analytics)
    public AttemptSource Source { get; set; }            // Practice / test / review
    public int? TestAttemptId { get; set; }              // Set when the attempt was part of a practice test
    public DateTime AnsweredUtc { get; set; } = DateTime.UtcNow; // When it was answered
}

/// <summary>
/// Current mastery of one skill for one user. Updated incrementally after
/// every attempt by <see cref="SatPrep.Services.MasteryService"/>.
/// </summary>
public class SkillMastery
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Owner
    public int SkillId { get; set; }                     // Skill being tracked
    public Skill? Skill { get; set; }                    // Skill navigation
    public double Rating { get; set; }                   // Ability estimate (logit scale, 0 = medium-level ability)
    public int AttemptCount { get; set; }                // Total graded attempts on this skill
    public int CorrectCount { get; set; }                // Total correct attempts on this skill
    public int HardCorrectCount { get; set; }            // Correct answers on Hard questions (gate for "Mastered")
    public DateTime LastPracticedUtc { get; set; }       // Last time this skill was practised
}

/// <summary>A question the learner bookmarked / marked for review.</summary>
public class QuestionMark
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Owner
    public int QuestionId { get; set; }                  // Marked question
    public Question? Question { get; set; }              // Question navigation
    public string? Note { get; set; }                    // Optional personal note
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // When it was marked
}

/// <summary>
/// Spaced-repetition entry: a missed question that should come back later.
/// Stage 0 -> due after 1 day, 1 -> 3 days, 2 -> 7 days, then retired.
/// </summary>
public class ReviewItem
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Owner
    public int QuestionId { get; set; }                  // Question to review
    public Question? Question { get; set; }              // Question navigation
    public int Stage { get; set; }                       // Current interval stage
    public DateTime DueUtc { get; set; }                 // When it is next due
    public bool IsRetired { get; set; }                  // True once answered correctly at the last stage
}

/// <summary>One practice test taken by a user.</summary>
public class TestAttempt
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Who is taking it
    public TestType Type { get; set; }                   // Full / section / short
    public TestStatus Status { get; set; }               // In progress / completed
    public int CurrentModule { get; set; }               // Index into the module plan (0-based)
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;   // Start time
    public DateTime? ModuleStartedUtc { get; set; }      // When the current module's clock started
    public DateTime? CompletedUtc { get; set; }          // Finish time
    public ModuleRoute RwRoute { get; set; }             // Routing decision for R&W module 2
    public ModuleRoute MathRoute { get; set; }           // Routing decision for Math module 2
    public int? RwScore { get; set; }                    // Scaled 200-800 (null if section not taken)
    public int? MathScore { get; set; }                  // Scaled 200-800 (null if section not taken)
    public int? TotalScore { get; set; }                 // Sum of section scores (full test only)
    public List<TestItem> Items { get; set; } = new();   // All questions in the test
}

/// <summary>One question slot inside a practice test.</summary>
public class TestItem
{
    public int Id { get; set; }                          // Primary key
    public int TestAttemptId { get; set; }               // Parent test
    public TestAttempt? TestAttempt { get; set; }        // Parent navigation
    public int ModuleIndex { get; set; }                 // Which module (0-based in the module plan)
    public int Order { get; set; }                       // Position inside the module (1-based, as shown to the learner)
    public int QuestionId { get; set; }                  // The question
    public Question? Question { get; set; }              // Question navigation
    public string GivenAnswer { get; set; } = "";        // Learner's answer ("" = blank, counts as wrong)
    public bool IsCorrect { get; set; }                  // Graded when the module is submitted
    public bool IsMarked { get; set; }                   // "Mark for review" flag inside the test
    public int SecondsSpent { get; set; }                // Time spent on this question
    public string EliminatedJson { get; set; } = "[]";   // Choices crossed out with the answer eliminator
}

/// <summary>A vocabulary word with definition and example.</summary>
public class VocabWord
{
    public int Id { get; set; }                          // Primary key
    public string Word { get; set; } = "";               // The word
    public string PartOfSpeech { get; set; } = "";       // noun / verb / adjective ...
    public string Definition { get; set; } = "";         // Short definition
    public string Example { get; set; } = "";            // Example sentence using the word
}

/// <summary>Leitner-box flashcard progress for one user and one word.</summary>
public class VocabProgress
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Owner
    public int VocabWordId { get; set; }                 // Word
    public VocabWord? VocabWord { get; set; }            // Word navigation
    public int Box { get; set; }                         // Leitner box 0-4 (4 = known)
    public DateTime NextDueUtc { get; set; }             // When the card is due again
    public int TimesSeen { get; set; }                   // Total reviews
    public int TimesCorrect { get; set; }                // Reviews marked "knew it"
    public DateTime? LastReviewedUtc { get; set; }       // Last time the card was reviewed (daily plan tick-off)
}

/// <summary>A learner's study-plan settings (one per user).</summary>
public class StudyPlan
{
    public int Id { get; set; }                          // Primary key
    public int UserId { get; set; }                      // Owner
    public DateTime TestDate { get; set; }               // Target SAT date
    public int TargetScore { get; set; }                 // Goal, 400-1600
    public int MinutesPerDay { get; set; }               // Daily time budget
    public int PracticeTestDay { get; set; } = 6;        // Day of week for the weekly practice test (0=Sunday)
}

