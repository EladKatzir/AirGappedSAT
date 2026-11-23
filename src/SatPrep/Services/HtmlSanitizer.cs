using System.Text.RegularExpressions;

namespace SatPrep.Services;

/// <summary>
/// Minimal allow-list sanitizer for HTML question content (official items use
/// HTML with MathML, tables, inline SVG and data: images). Removes anything
/// that could run script or load something from the network, so imported
/// content can never break the air gap or attack the browser.
/// </summary>
public static partial class HtmlSanitizer
{
    /// <summary>Elements removed together with their content.</summary>
    [GeneratedRegex(@"<\s*(script|style|iframe|object|embed|link|meta|base|form|input|button|textarea|select)\b[^>]*>(.*?<\s*/\s*\1\s*>)?",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousElements();

    /// <summary>Inline event handlers such as onclick="...".</summary>
    [GeneratedRegex(@"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlers();

    /// <summary>URL attributes that point anywhere except data: URIs or in-page anchors.</summary>
    [GeneratedRegex(@"\s+(href|src|xlink:href|srcset|action|formaction|background|poster)\s*=\s*(""(?!data:|#)[^""]*""|'(?!data:|#)[^']*'|(?!data:|#)[^\s>""']+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ExternalUrls();

    /// <summary>CSS that could fetch resources (url(...), @import, expression).</summary>
    [GeneratedRegex(@"\s+style\s*=\s*(""[^""]*(url\(|expression|@import)[^""]*""|'[^']*(url\(|expression|@import)[^']*')", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousStyles();

    /// <summary>Returns safe HTML (null-safe).</summary>
    /// <param name="html">Untrusted HTML.</param>
    public static string Clean(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var s = DangerousElements().Replace(html, "");     // Drop script-capable / form elements
        s = EventHandlers().Replace(s, "");                // Drop on* handlers
        s = ExternalUrls().Replace(s, "");                 // Drop network URLs (keep data: images)
        s = DangerousStyles().Replace(s, "");              // Drop styles that load resources
        s = Regex.Replace(s, "javascript:", "", RegexOptions.IgnoreCase); // Belt and braces
        return s;
    }
}
