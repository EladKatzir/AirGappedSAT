using SatPrep.Models;
using SatPrep.Services;

namespace SatPrep.Tests;

/// <summary>SAT student-produced-response grading rules.</summary>
public class AnswerCheckerTests
{
    /// <summary>Builds a student-produced question with the given accepted answers.</summary>
    /// <param name="accepted">Accepted answers separated by '|'.</param>
    private static Question Spr(string accepted) => new() { Format = AnswerFormat.StudentProduced, CorrectAnswer = accepted };

    [Theory]
    [InlineData("3/2", "1.5", true)]      // Fraction vs decimal
    [InlineData("3/2", "6/4", true)]      // Unreduced fraction
    [InlineData("2/3", ".666", true)]     // Truncated repeating decimal filling the box
    [InlineData("2/3", "0.667", true)]    // Rounded repeating decimal
    [InlineData("2/3", "0.67", false)]    // Not enough precision
    [InlineData("-4", "-4", true)]
    [InlineData("12", " 12 ", true)]      // Whitespace ignored
    [InlineData("12", "", false)]         // Blank is wrong
    public void Student_produced_rules(string accepted, string given, bool expected)
        => Assert.Equal(expected, AnswerChecker.IsCorrect(Spr(accepted), given));

    [Fact]
    public void Multiple_choice_is_case_insensitive()
        => Assert.True(AnswerChecker.IsCorrect(new Question { Format = AnswerFormat.MultipleChoice, CorrectAnswer = "C" }, "c"));
}
