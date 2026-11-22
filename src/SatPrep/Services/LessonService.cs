using System.Text.RegularExpressions;
using Markdig;

namespace SatPrep.Services;

/// <summary>One lesson (one per domain) loaded from Content/lessons/*.md.</summary>
/// <param name="DomainCode">Domain code, e.g. "ALG" (file name).</param>
/// <param name="Title">Lesson title (first "# " heading).</param>
/// <param name="Html">Rendered HTML.</param>
/// <param name="SkillCodes">Skill codes that have a section in this lesson.</param>
public record Lesson(string DomainCode, string Title, string Html, List<string> SkillCodes);

/// <summary>
/// Renders Markdown (lessons, question stems, explanations) to safe HTML.
/// Math written as $...$ / $$...$$ is emitted as \( \) / \[ \] spans that the
/// bundled KaTeX renders in the browser. Raw HTML in content is disabled.
/// </summary>
public static class Md
{
    /// <summary>Shared Markdig pipeline: math, tables, emphasis extras, generic attributes ({#id}), no raw HTML.</summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseMathematics()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseGenericAttributes()
        .DisableHtml()
        .Build();

    /// <summary>Renders Markdown to HTML.</summary>
    /// <param name="markdown">Source text (null-safe).</param>
    public static string Render(string? markdown) => string.IsNullOrEmpty(markdown) ? "" : Markdown.ToHtml(markdown, Pipeline);

    /// <summary>Renders a single line without the surrounding &lt;p&gt; (used for answer choices).</summary>
    /// <param name="markdown">Source text.</param>
    public static string RenderInline(string? markdown)
    {
        var html = Render(markdown).Trim();
        if (html.StartsWith("<p>") && html.EndsWith("</p>") && html.IndexOf("<p>", 3, StringComparison.Ordinal) < 0)
            html = html[3..^4];                                                    // Strip the single wrapping paragraph
        return html;
    }
}

/// <summary>
/// Loads lessons from Content/lessons once and caches them.
/// File format: "# Title" then "## Skill name {#SKILL.CODE}" sections.
/// </summary>
public class LessonService
{
    private readonly Dictionary<string, Lesson> _lessons;                         // DomainCode -> lesson

    /// <param name="env">Host environment (locates the Content folder).</param>
    public LessonService(IWebHostEnvironment env)
    {
        var folder = Path.Combine(env.ContentRootPath, "Content", "lessons");
        _lessons = new(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(folder)) return;

        foreach (var file in Directory.GetFiles(folder, "*.md"))
        {
            var md = File.ReadAllText(file);
            var code = Path.GetFileNameWithoutExtension(file).ToUpperInvariant();            // ALG.md -> ALG
            var title = Regex.Match(md, @"^#\s+(.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            var skills = Regex.Matches(md, @"\{#([A-Z]{3}\.[A-Z0-9]+)\}").Select(m => m.Groups[1].Value).ToList();
            // Markdig would read "{#ALG.LF}" as id "ALG" + class "LF", so anchors use dashes: "{#ALG-LF}"
            var html = Md.Render(Regex.Replace(md, @"\{#([A-Z]{3})\.([A-Z0-9]+)\}", "{#$1-$2}"));
            _lessons[code] = new Lesson(code, title, html, skills);
        }
    }

    /// <summary>Lesson for a domain, or null.</summary>
    /// <param name="domainCode">Domain code.</param>
    public Lesson? Get(string domainCode) => _lessons.GetValueOrDefault(domainCode);

    /// <summary>HTML anchor id for a skill section ("ALG.LF" -> "ALG-LF").</summary>
    /// <param name="skillCode">Skill code.</param>
    public static string Anchor(string skillCode) => skillCode.Replace('.', '-');

    /// <summary>All loaded lessons.</summary>
    public IEnumerable<Lesson> All => _lessons.Values;
}
