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
}

/// <summary>A learner account. Kept minimal; auth mirrors ShiftManager's cookie login.</summary>
public class AppUser
{
    public int Id { get; set; }                          // Primary key
    public string UserName { get; set; } = "";           // Login name
    public string DisplayName { get; set; } = "";        // Name shown in UI
    public string PasswordHash { get; set; } = "";       // PBKDF2 hash (never store plain text)
    public bool IsAdmin { get; set; }                    // Admins can import content and see all learners
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; // Account creation time
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
