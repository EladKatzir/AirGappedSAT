using System.Globalization;
using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>
/// Grades answers. Multiple choice compares letters. Student-produced
/// responses follow SAT rules: fractions and decimals are equivalent, and a
/// decimal that fills the answer box (3+ decimal places, truncated or
/// rounded) is accepted for a repeating value (2/3 accepts .666 and .667).
/// </summary>
public static class AnswerChecker
{
    /// <summary>True if the given answer is correct for the question.</summary>
    /// <param name="q">Question being answered.</param>
    /// <param name="given">Raw answer from the learner.</param>
    public static bool IsCorrect(Question q, string? given)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;                          // Blank = wrong (as on the SAT)
        given = given.Trim();

        if (q.Format == AnswerFormat.MultipleChoice)
            return string.Equals(given, q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase); // Letter compare

        foreach (var accepted in q.CorrectAnswer.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(Normalize(accepted), Normalize(given), StringComparison.OrdinalIgnoreCase)) return true; // Same text
            if (TryParseNumber(accepted, out var a) && TryParseNumber(given, out var g))
            {
                if (Math.Abs(a - g) < 1e-9) return true;                             // Exactly equal values (e.g. 3/2 vs 1.5)
                if (DecimalPlaces(given) >= 3 && Math.Abs(a - g) < 0.001) return true; // Filled-box approximation of a repeating decimal
            }
        }
        return false;
    }

    /// <summary>Removes spaces and a leading "+" so "  +12 " equals "12".</summary>
    /// <param name="s">Raw text.</param>
    private static string Normalize(string s) => s.Replace(" ", "").TrimStart('+');

    /// <summary>Parses "1.5", ".5", "-3/4" or "7" into a number.</summary>
    /// <param name="s">Text to parse.</param>
    /// <param name="value">Parsed value on success.</param>
    public static bool TryParseNumber(string s, out double value)
    {
        s = Normalize(s);
        int slash = s.IndexOf('/');                                                  // Fraction separator
        if (slash > 0)
        {
            if (double.TryParse(s[..slash], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) &&
                double.TryParse(s[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && d != 0)
            {
                value = n / d;
                return true;
            }
            value = 0;
            return false;
        }
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Counts digits after the decimal point ("0.667" -> 3).</summary>
    /// <param name="s">Number text.</param>
    private static int DecimalPlaces(string s)
    {
        int dot = s.IndexOf('.');
        return dot < 0 || s.Contains('/') ? 0 : s.Length - dot - 1;
    }
}
