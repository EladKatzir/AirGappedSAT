using System.Text.Json;
using SatPrep.Models;

namespace SatPrep.Pages.Shared;

/// <summary>View model for the _QuestionCard partial.</summary>
public class QuestionCardModel
{
    public required Question Question { get; init; }         // Question to render (Skill + Domain loaded)
    public bool Answered { get; init; }                      // True once graded: show colours + explanation
    public bool IsCorrect { get; init; }                     // Grading result
    public string? Given { get; init; }                      // Learner's answer
    public int? MasteryBefore { get; init; }                 // Skill mastery % before this answer
    public int? MasteryAfter { get; init; }                  // Skill mastery % after this answer
    public bool IsMarked { get; init; }                      // Bookmarked?
    public bool ShowMarkButton { get; init; } = true;        // Hide in contexts without a mark form

    /// <summary>Choice texts parsed from the question's JSON column.</summary>
    public List<string> Choices => JsonSerializer.Deserialize<List<string>>(Question.ChoicesJson) ?? new();
}
