using SatPrep.Services;

namespace SatPrep.Tests;

/// <summary>HTML question content must never run script or reach the network.</summary>
public class HtmlSanitizerTests
{
    [Theory]
    [InlineData("<p>ok</p><script>alert(1)</script>", "<script")]
    [InlineData("<img src=\"https://evil.example/x.png\">", "https://")]
    [InlineData("<p onclick=\"steal()\">x</p>", "onclick")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript:")]
    [InlineData("<iframe src=\"http://x\"></iframe>", "<iframe")]
    [InlineData("<div style=\"background:url(http://x)\">x</div>", "url(")]
    public void Removes_dangerous_content(string html, string forbidden)
        => Assert.DoesNotContain(forbidden, HtmlSanitizer.Clean(html), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Keeps_math_tables_and_embedded_images()
    {
        var html = "<p>Solve <math><mi>x</mi><mo>+</mo><mn>1</mn></math></p><table><tr><td>1</td></tr></table><img src=\"data:image/png;base64,AAAA\" alt=\"graph\">";
        var clean = HtmlSanitizer.Clean(html);
        Assert.Contains("<math>", clean);
        Assert.Contains("<table>", clean);
        Assert.Contains("data:image/png;base64,AAAA", clean);
    }
}
