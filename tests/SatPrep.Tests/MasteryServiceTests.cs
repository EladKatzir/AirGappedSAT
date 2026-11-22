using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Tests;

/// <summary>Behavioural tests for the skill-mastery engine.</summary>
public class MasteryServiceTests
{
    private readonly MasteryService _svc = new();          // System under test
    private static readonly DateTime Now = DateTime.UtcNow; // Fixed timestamp for attempts

    /// <summary>Builds a fresh mastery row and applies a sequence of (difficulty, correct) answers.</summary>
    /// <param name="answers">Answers to apply in order.</param>
    private SkillMastery Run(params (Difficulty d, bool ok)[] answers)
    {
        var m = new SkillMastery();                          // Rating 0, no attempts
        foreach (var (d, ok) in answers) _svc.ApplyAttempt(m, d, ok, Now); // Feed each answer
        return m;
    }

    [Fact]
    public void Untouched_skill_is_not_started()
    {
        Assert.Equal(MasteryLevel.NotStarted, MasteryService.LevelOf(null));
        Assert.Equal(0, MasteryService.MasteryPercent(new SkillMastery()));
    }

    [Fact]
    public void Correct_hard_answer_raises_rating_more_than_correct_easy_answer()
    {
        var hard = Run((Difficulty.Hard, true));
        var easy = Run((Difficulty.Easy, true));
        Assert.True(hard.Rating > easy.Rating);
    }

    [Fact]
    public void Missing_an_easy_question_hurts_more_than_missing_a_hard_one()
    {
        var easyMiss = Run((Difficulty.Easy, false));
        var hardMiss = Run((Difficulty.Hard, false));
        Assert.True(easyMiss.Rating < hardMiss.Rating);
    }

    [Fact]
    public void High_accuracy_without_hard_questions_cannot_reach_mastered()
    {
        var answers = Enumerable.Repeat((Difficulty.Medium, true), 30).ToArray();
        var m = Run(answers);
        Assert.True(MasteryService.MasteryPercent(m) >= 85);
        Assert.Equal(MasteryLevel.Proficient, MasteryService.LevelOf(m)); // Gated: no Hard evidence
    }

    [Fact]
    public void Consistent_success_including_hard_questions_reaches_mastered()
    {
        var answers = Enumerable.Repeat((Difficulty.Medium, true), 10)
            .Concat(Enumerable.Repeat((Difficulty.Hard, true), 6)).ToArray();
        Assert.Equal(MasteryLevel.Mastered, MasteryService.LevelOf(Run(answers)));
    }

    [Fact]
    public void Recommended_difficulty_tracks_ability()
    {
        Assert.Equal(Difficulty.Medium, MasteryService.RecommendedDifficulty(null));
        Assert.Equal(Difficulty.Easy, MasteryService.RecommendedDifficulty(new SkillMastery { Rating = -1.2 }));
        Assert.Equal(Difficulty.Hard, MasteryService.RecommendedDifficulty(new SkillMastery { Rating = 1.5 }));
    }
}
