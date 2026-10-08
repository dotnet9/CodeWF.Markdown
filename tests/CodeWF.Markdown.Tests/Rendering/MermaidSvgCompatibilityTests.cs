using System.Xml.Linq;
using CodeWF.Markdown.Mermaid;
using Mermaider;
using Mermaider.Models;
using Xunit;
using Avalonia.Styling;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MermaidSvgCompatibilityTests
{
    [Fact]
    public void CustomTheme_InheritsItsBasePalette()
    {
        Assert.True(MermaidBlockRenderer.IsDarkTheme(new ThemeVariant("NightSky", ThemeVariant.Dark)));
        Assert.True(MermaidBlockRenderer.IsDarkTheme(new ThemeVariant("NestedNight", new ThemeVariant("Night", ThemeVariant.Dark))));
        Assert.False(MermaidBlockRenderer.IsDarkTheme(new ThemeVariant("Aquatic", ThemeVariant.Light)));
    }

    [Theory]
    [InlineData("#FFFFFF", "#27272A", "#E2E9F0")]
    [InlineData("#18181B", "#FAFAFA", "#202731")]
    public void RectangularNodes_ResolveMixColorsForSkia(string background, string foreground, string expectedFill)
    {
        var options = new RenderOptions { Bg = background, Fg = foreground, Accent = "#1677FF" };
        var raw = MermaidRenderer.RenderSvg("graph LR\nA[开始] --> B[结束]", options);
        var svg = MermaidBlockRenderer.InlineCssVariables(raw, options);
        var document = XDocument.Parse(svg);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var node = document.Descendants(ns + "g").First(element => element.Attribute("class")?.Value == "node");
        Assert.Equal(expectedFill, node.Element(ns + "rect")!.Attribute("fill")!.Value);
        Assert.All(document.Descendants().Attributes().Where(attribute => attribute.Name.LocalName is "fill" or "stroke" or "font-size"),
            attribute =>
            {
                Assert.DoesNotContain("color-mix(", attribute.Value);
                Assert.DoesNotContain("var(", attribute.Value);
            });
    }
}
