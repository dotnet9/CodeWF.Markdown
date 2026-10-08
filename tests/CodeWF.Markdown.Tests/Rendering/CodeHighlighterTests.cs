using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;
using CodeWF.Markdown.Rendering;
using CodeWF.Markdown.Controls;
using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

[Collection("AvaloniaPlatform")]
public sealed class CodeHighlighterTests(AvaloniaPlatformFixture platform)
{
    [Theory]
    [InlineData("csharp", false)]
    [InlineData("cs", true)]
    [InlineData("c#", true)]
    public void CSharpAliases_ProduceColoredTokensWithoutDefaultEmphasis(string language, bool dark) => platform.Run(() =>
    {
        var control = CodeHighlighter.Render("public int value = 42; // comment", language, dark,
            FontFamily.Default, 13, 22);
        var text = control.GetLogicalDescendants().OfType<SelectableTextBlock>().Single(block => block.Classes.Contains(MarkdownStyleKeys.CodeBlockText));
        var runs = text.Inlines!.OfType<Run>().ToArray();
        Assert.True(runs.Select(run => run.Foreground).OfType<ISolidColorBrush>().Select(brush => brush.Color).Distinct().Count() >= 3);
        Assert.Contains(runs, run => run.FontStyle == FontStyle.Normal && run.FontWeight == FontWeight.Normal);
    });
}
