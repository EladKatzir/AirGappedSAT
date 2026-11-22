using SatPrep.Models;

namespace SatPrep.Services;

/// <summary>
/// Converts performance into SAT-style 200-800 section scores.
///
/// The real SAT uses unpublished IRT scoring, so this is a documented
/// approximation (see docs/PLAN.md section 4):
///   * Correct answers are weighted by difficulty (Easy 1, Medium 1.5, Hard 2).
///   * Weighted fraction f = weighted correct / weighted possible.
///   * Upper (harder) module 2 or a non-adaptive test: score = 200 + 600 f.
///   * Lower (easier) module 2: score = 200 + 410 f   (ceiling ~610, like the real test).
/// </summary>
public class ScoreEstimator
{
    /// <summary>Module-1 weighted fraction needed to be routed to the harder module 2.</summary>
    public const double RoutingThreshold = 0.6;

    /// <summary>Official approximate share of each domain within its section (used for the mastery-based estimate).</summary>
    public static readonly IReadOnlyDictionary<string, double> DomainWeights = new Dictionary<string, double>
    {
        ["INI"] = 0.26, ["CAS"] = 0.28, ["EOI"] = 0.20, ["SEC"] = 0.26,  // Reading and Writing
        ["ALG"] = 0.35, ["ADV"] = 0.35, ["PSD"] = 0.15, ["GEO"] = 0.15   // Math
    };

    /// <summary>Scoring weight of a difficulty.</summary>
    /// <param name="d">Difficulty.</param>
    public static double Weight(Difficulty d) => d switch { Difficulty.Easy => 1.0, Difficulty.Medium => 1.5, _ => 2.0 };

    /// <summary>Weighted fraction correct for a set of (difficulty, correct) results.</summary>
    /// <param name="results">One entry per question.</param>
    public static double WeightedFraction(IEnumerable<(Difficulty d, bool ok)> results)
    {
        double got = 0, total = 0;                                         // Weighted correct / weighted possible
        foreach (var (d, ok) in results)
        {
            total += Weight(d);
            if (ok) got += Weight(d);
        }
        return total == 0 ? 0 : got / total;
    }

    /// <summary>Decides module-2 routing from module-1 results.</summary>
    /// <param name="module1">Module 1 results.</param>
    public static ModuleRoute Route(IEnumerable<(Difficulty d, bool ok)> module1)
        => WeightedFraction(module1) >= RoutingThreshold ? ModuleRoute.Upper : ModuleRoute.Lower;

    /// <summary>Scaled section score (200-800, multiple of 10) from all questions in the section.</summary>
    /// <param name="results">All questions in the section (both modules).</param>
    /// <param name="route">Module-2 route (None for non-adaptive tests).</param>
    public static int SectionScore(IEnumerable<(Difficulty d, bool ok)> results, ModuleRoute route)
    {
        double f = WeightedFraction(results);
        double raw = route == ModuleRoute.Lower ? 200 + 410 * f : 200 + 600 * f;
        return RoundTo10(raw);
    }

    /// <summary>
    /// Mastery-based estimate for one section, without a test: domain-weighted
    /// average of expected accuracy on medium questions. Returns null when there
    /// is too little evidence (fewer than 15 answers in the section).
    /// </summary>
    /// <param name="skillPercents">For each skill in the section: (domain code, mastery %, attempt count).</param>
    public static int? SectionEstimateFromMastery(IEnumerable<(string domainCode, int percent, int attempts)> skillPercents)
    {
        var list = skillPercents.ToList();
        if (list.Sum(x => x.attempts) < 15) return null;                   // Not enough data yet

        double score = 0, weightSum = 0;                                   // Weighted average accumulator
        foreach (var g in list.GroupBy(x => x.domainCode))
        {
            // Untouched skills count as 35% (roughly guessing level) so gaps pull the estimate down
            double avg = g.Average(x => x.attempts == 0 ? 35.0 : x.percent) / 100.0;
            double w = DomainWeights.TryGetValue(g.Key, out var dw) ? dw : 0.25;
            score += avg * w;
            weightSum += w;
        }
        return RoundTo10(200 + 600 * (score / weightSum));
    }

    /// <summary>Clamps to 200-800 and rounds to the nearest 10.</summary>
    /// <param name="raw">Unrounded score.</param>
    public static int RoundTo10(double raw) => (int)(Math.Round(Math.Clamp(raw, 200, 800) / 10.0) * 10);
}
