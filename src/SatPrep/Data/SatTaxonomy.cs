using SatPrep.Models;

namespace SatPrep.Data;

/// <summary>
/// The official digital SAT content taxonomy: 8 domains and 29 skills,
/// as published on the College Board "content domains" page.
/// The Code values are the stable keys that content packs reference,
/// so never change an existing code once questions use it.
/// </summary>
public static class SatTaxonomy
{
    /// <summary>One domain definition with its skills as (code, name) pairs.</summary>
    public record DomainDef(string Code, string Name, SatSection Section, (string Code, string Name)[] Skills);

    /// <summary>All domains in display order.</summary>
    public static readonly DomainDef[] Domains =
    {
        // ---------------- Reading and Writing ----------------
        new("INI", "Information and Ideas", SatSection.ReadingWriting, new[]
        {
            ("INI.CID", "Central Ideas and Details"),
            ("INI.INF", "Inferences"),
            ("INI.COE", "Command of Evidence"),
        }),
        new("CAS", "Craft and Structure", SatSection.ReadingWriting, new[]
        {
            ("CAS.WIC", "Words in Context"),
            ("CAS.TSP", "Text Structure and Purpose"),
            ("CAS.CTC", "Cross-Text Connections"),
        }),
        new("EOI", "Expression of Ideas", SatSection.ReadingWriting, new[]
        {
            ("EOI.SYN", "Rhetorical Synthesis"),
            ("EOI.TRA", "Transitions"),
        }),
        new("SEC", "Standard English Conventions", SatSection.ReadingWriting, new[]
        {
            ("SEC.BOU", "Boundaries"),
            ("SEC.FSS", "Form, Structure, and Sense"),
        }),

        // ---------------- Math ----------------
        new("ALG", "Algebra", SatSection.Math, new[]
        {
            ("ALG.LE1", "Linear equations in one variable"),
            ("ALG.LF",  "Linear functions"),
            ("ALG.LE2", "Linear equations in two variables"),
            ("ALG.SYS", "Systems of two linear equations in two variables"),
            ("ALG.INE", "Linear inequalities in one or two variables"),
        }),
        new("ADV", "Advanced Math", SatSection.Math, new[]
        {
            ("ADV.NLF", "Nonlinear functions"),
            ("ADV.NLE", "Nonlinear equations in one variable and systems of equations in two variables"),
            ("ADV.EQX", "Equivalent expressions"),
        }),
        new("PSD", "Problem-Solving and Data Analysis", SatSection.Math, new[]
        {
            ("PSD.RAT", "Ratios, rates, proportional relationships, and units"),
            ("PSD.PCT", "Percentages"),
            ("PSD.OVD", "One-variable data: distributions and measures of center and spread"),
            ("PSD.TVD", "Two-variable data: models and scatterplots"),
            ("PSD.PRB", "Probability and conditional probability"),
            ("PSD.INF", "Inference from sample statistics and margin of error"),
            ("PSD.ESC", "Evaluating statistical claims: observational studies and experiments"),
        }),
        new("GEO", "Geometry and Trigonometry", SatSection.Math, new[]
        {
            ("GEO.AV",  "Area and volume"),
            ("GEO.LAT", "Lines, angles, and triangles"),
            ("GEO.RTT", "Right triangles and trigonometry"),
            ("GEO.CIR", "Circles"),
        }),
    };

    /// <summary>
    /// Inserts any missing domains/skills. Safe to call on every start-up:
    /// existing rows are matched by Code and left alone.
    /// </summary>
    /// <param name="db">Open database context.</param>
    public static void EnsureSeeded(AppDbContext db)
    {
        var existingDomainCodes = db.Domains.Select(d => d.Code).ToHashSet();   // Domain codes already in the DB
        var existingSkillCodes = db.Skills.Select(s => s.Code).ToHashSet();     // Skill codes already in the DB

        for (int di = 0; di < Domains.Length; di++)
        {
            var def = Domains[di];                                              // Current domain definition

            // Load the existing domain or create a new one
            var domain = db.Domains.FirstOrDefault(d => d.Code == def.Code);
            if (domain is null)
            {
                domain = new Domain { Code = def.Code, Name = def.Name, Section = def.Section, SortOrder = di };
                db.Domains.Add(domain);                                         // Queue insert
            }

            for (int si = 0; si < def.Skills.Length; si++)
            {
                var (code, name) = def.Skills[si];                              // Current skill definition
                if (existingSkillCodes.Contains(code)) continue;                // Already present - skip
                domain.Skills.Add(new Skill { Code = code, Name = name, SortOrder = si }); // Queue insert under domain
            }
        }

        db.SaveChanges();                                                       // Commit everything in one transaction
    }
}
