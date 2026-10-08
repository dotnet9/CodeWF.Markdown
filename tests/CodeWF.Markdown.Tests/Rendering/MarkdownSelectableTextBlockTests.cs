using System.Globalization;

using Avalonia.Media;
using Avalonia.Media.TextFormatting;

using CodeWF.Markdown.Shared.Rendering;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

[Collection("AvaloniaPlatform")]
public sealed class MarkdownSelectableTextBlockTests(AvaloniaPlatformFixture platform)
{
    private static TextCharacters CreateRun(string text, double fontSize,
        BaselineAlignment baseline = BaselineAlignment.Baseline, IBrush? background = null)
    {
        var properties = new GenericTextRunProperties(
            new Typeface(FontFamily.Parse("Inter")), fontSize,
            backgroundBrush: background, baselineAlignment: baseline,
            cultureInfo: CultureInfo.InvariantCulture);
        return new TextCharacters(text, properties);
    }

    [Fact]
    public void BuildSelectionOverrides_PreservesRunFontSizeAndBaselineAlignment() => platform.Run(() =>
    {
        var selectionForeground = Brushes.White;
        var codeBackground = Brushes.LightGray;
        var runs = new[]
        {
            CreateRun("问题 2: ", 26),
            CreateRun("restrict", 16, BaselineAlignment.Center, codeBackground),
            CreateRun(" 关键字", 26)
        };

        // 选中范围覆盖行内代码后半段与后续正文：位置 10 起 8 个字符
        var overrides = MarkdownSelectableTextBlock.BuildSelectionOverrides(
            runs, 10, 8, selectionForeground,
            new Typeface(FontFamily.Parse("Inter")), 26, null);

        Assert.NotNull(overrides);
        Assert.Equal(2, overrides.Count);

        var codeOverride = overrides[0];
        Assert.Equal(10, codeOverride.Start);
        Assert.Equal(4, codeOverride.Length);
        Assert.Equal(16, codeOverride.Value.FontRenderingEmSize);
        Assert.Equal(BaselineAlignment.Center, codeOverride.Value.BaselineAlignment);
        Assert.Same(codeBackground, codeOverride.Value.BackgroundBrush);
        Assert.Same(selectionForeground, codeOverride.Value.ForegroundBrush);

        var textOverride = overrides[1];
        Assert.Equal(14, textOverride.Start);
        Assert.Equal(4, textOverride.Length);
        Assert.Equal(26, textOverride.Value.FontRenderingEmSize);
        Assert.Equal(BaselineAlignment.Baseline, textOverride.Value.BaselineAlignment);
        Assert.Same(selectionForeground, textOverride.Value.ForegroundBrush);
    });

    [Fact]
    public void BuildSelectionOverrides_OnlyCoversSelectedRange() => platform.Run(() =>
    {
        var runs = new[]
        {
            CreateRun("问题 2: ", 26),
            CreateRun("restrict", 16, BaselineAlignment.Center)
        };

        // 只选中正文，行内代码不在范围内
        var overrides = MarkdownSelectableTextBlock.BuildSelectionOverrides(
            runs, 0, 6, Brushes.White, Typeface.Default, 26, null);

        Assert.NotNull(overrides);
        var single = Assert.Single(overrides);
        Assert.Equal(0, single.Start);
        Assert.Equal(6, single.Length);
        Assert.Equal(26, single.Value.FontRenderingEmSize);
    });

    [Fact]
    public void BuildSelectionOverrides_NoSelectionOrNoForeground_ReturnsNull() => platform.Run(() =>
    {
        var runs = new[] { CreateRun("问题", 26) };

        Assert.Null(MarkdownSelectableTextBlock.BuildSelectionOverrides(
            runs, 0, 0, Brushes.White, Typeface.Default, 26, null));
        Assert.Null(MarkdownSelectableTextBlock.BuildSelectionOverrides(
            runs, 0, 2, null, Typeface.Default, 26, null));
    });
}
