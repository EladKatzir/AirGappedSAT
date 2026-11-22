using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>Mastery bands shown to the learner, lowest to highest.</summary>
public enum MasteryLevel
{
    NotStarted = 0, // No attempts yet
    Learning = 1,   // Below ~50% expected accuracy on medium questions
    Familiar = 2,   // Roughly 50-70%
    Proficient = 3, // Roughly 70-85%
    Mastered = 4    // 85%+ AND enough evidence, including correct Hard questions
}

/// <summary>
/// Skill-mastery engine.
///
/// Model: a simple Elo / 1-parameter IRT update per skill.
///   * Each user has an ability Rating per skill (logit scale, starts at 0).
///   * Each question has a difficulty b: Easy = -1, Medium = 0, Hard = +1.
///   * Expected chance of a correct answer: P = 1 / (1 + e^-(Rating - b)).
///   * After answering: Rating += K * (actual - P), where actual is 1 or 0.
///   * K starts large (fast movement for new skills) and shrinks as evidence grows.
///
/// Why: plain accuracy treats an easy and a hard question the same; this
/// rewards getting hard questions right and punishes missing easy ones,
/// and the score keeps moving when the learner improves.
///
/// The class is pure (no database access) so it is trivial to unit-test.
/// </summary>
public class MasteryService
{
    public const int MinAttemptsForMastered = 8;   // Evidence needed before "Mastered" can be shown
    public const int MinHardCorrectForMastered = 2; // Must have solved at least this many Hard questions
    private const double KStart = 0.6;              // Learning rate for a brand-new skill
    private const double KMin = 0.15;               // Floor so the rating never freezes
    private const double KDecayAttempts = 20.0;     // Attempts over which K shrinks toward KMin

    /// <summary>Maps a difficulty to its b parameter on the logit scale.</summary>
    /// <param name="d">Question difficulty.</param>
    public static double DifficultyParameter(Difficulty d) => d switch
    {
        Difficulty.Easy => -1.0,
        Difficulty.Medium => 0.0,
        Difficulty.Hard => 1.0,
        _ => 0.0
    };

    /// <summary>Probability the learner answers a question of difficulty b correctly.</summary>
    /// <param name="rating">Current ability rating.</param>
    /// <param name="b">Question difficulty parameter.</param>
    public static double ExpectedCorrect(double rating, double b) => 1.0 / (1.0 + Math.Exp(-(rating - b)));

    /// <summary>Learning rate that shrinks as attempts accumulate.</summary>
    /// <param name="attemptCount">Attempts already recorded on this skill.</param>
    public static double LearningRate(int attemptCount)
        => KMin + (KStart - KMin) * Math.Exp(-attemptCount / KDecayAttempts);

    /// <summary>
    /// Applies one graded answer to a mastery record (mutates it in place).
    /// </summary>
    /// <param name="m">Mastery row for (user, skill). Create a new one with Rating 0 if none exists.</param>
    /// <param name="difficulty">Difficulty of the question just answered.</param>
    /// <param name="isCorrect">Whether the answer was correct.</param>
    /// <param name="whenUtc">Time of the answer.</param>
    public void ApplyAttempt(SkillMastery m, Difficulty difficulty, bool isCorrect, DateTime whenUtc)
    {
        double b = DifficultyParameter(difficulty);           // Question difficulty on the rating scale
        double expected = ExpectedCorrect(m.Rating, b);       // What the model predicted before this answer
        double actual = isCorrect ? 1.0 : 0.0;                // What actually happened
        double k = LearningRate(m.AttemptCount);              // How far to move this time

        m.Rating += k * (actual - expected);                  // Core Elo/IRT update
        m.AttemptCount++;                                     // One more piece of evidence
        if (isCorrect) m.CorrectCount++;                      // Track raw accuracy for display
        if (isCorrect && difficulty == Difficulty.Hard) m.HardCorrectCount++; // Gate for Mastered
        m.LastPracticedUtc = whenUtc;                         // Used for "needs review" / spaced repetition
    }

    /// <summary>
    /// 0-100 score shown in the UI: expected accuracy on a Medium question.
    /// </summary>
    /// <param name="m">Mastery row (null means never practised).</param>
    public static int MasteryPercent(SkillMastery? m)
        => m is null || m.AttemptCount == 0 ? 0 : (int)Math.Round(ExpectedCorrect(m.Rating, 0.0) * 100);

    /// <summary>Converts a mastery row into a display band.</summary>
    /// <param name="m">Mastery row (null means never practised).</param>
    public static MasteryLevel LevelOf(SkillMastery? m)
    {
        if (m is null || m.AttemptCount == 0) return MasteryLevel.NotStarted;

        int pct = MasteryPercent(m);                          // Expected accuracy on medium questions
        bool enoughEvidence = m.AttemptCount >= MinAttemptsForMastered
                              && m.HardCorrectCount >= MinHardCorrectForMastered;

        if (pct >= 85 && enoughEvidence) return MasteryLevel.Mastered;
        if (pct >= 70) return MasteryLevel.Proficient;
        if (pct >= 50) return MasteryLevel.Familiar;
        return MasteryLevel.Learning;
    }

    /// <summary>
    /// Picks the best difficulty to serve next so practice stays in the
    /// "productive struggle" zone (~60-75% expected success).
    /// </summary>
    /// <param name="m">Mastery row (null means never practised).</param>
    public static Difficulty RecommendedDifficulty(SkillMastery? m)
    {
        double r = m?.Rating ?? 0.0;                          // Unknown learners start at medium
        if (r < -0.5) return Difficulty.Easy;
        if (r < 0.9) return Difficulty.Medium;
        return Difficulty.Hard;
    }
}
