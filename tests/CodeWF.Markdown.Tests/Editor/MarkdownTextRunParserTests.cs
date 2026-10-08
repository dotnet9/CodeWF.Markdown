using CodeWF.Markdown.Editor.Controls.Wysiwyg;
using Xunit;

namespace CodeWF.Markdown.Tests.Editor;

public sealed class MarkdownTextRunParserTests
{
    [Theory]
    [InlineData("**[Dotnet9](#jump_8)**", "Dotnet9", "#jump_8", true, false)]
    [InlineData("[*italic* **bold**](https://x)", "bold", "https://x", true, false)]
    [InlineData("[*italic* **bold**](https://x)", "italic", "https://x", false, true)]
    public void Links_PreserveInheritedAndNestedStyles(string text, string label, string url, bool bold, bool italic)
    {
        var run = Assert.Single(MarkdownTextRunParser.Parse(text), run => run.Text == label);
        Assert.Equal(url, run.LinkUrl);
        Assert.Equal(bold, run.Bold);
        Assert.Equal(italic, run.Italic);
    }

    [Fact]
    public void HardLineBreak_RemainsVisible()
    {
        var text = MarkdownTextRunParser.ToPlainText(MarkdownTextRunParser.Parse("first  \nsecond"));
        Assert.Equal("first\nsecond", text);
    }

    [Theory]
    [InlineData("**bold**", 0, 2)]
    [InlineData("[link](https://x)", 2, 3)]
    [InlineData("`code`", 1, 2)]
    public void VisibleOffset_MapsToSource(string text, int visible, int source) =>
        Assert.Equal(source, MarkdownTextRunParser.GetSourceOffset(text, visible));
}
