using CodeWF.Markdown.Editor.Controls.Wysiwyg;

using Xunit;

namespace CodeWF.Markdown.Tests.Editor;

/// <summary>
/// 所见即所得视图的解析 / 回写往返测试：<c>markdown → 块模型 → markdown</c> 必须保持语义稳定，
/// 且块类型识别正确（标题级别、任务勾选、表格、代码、引用、列表）。
/// </summary>
public sealed class MarkdownBlockParserTests
{
    [Theory]
    [InlineData("# Title", 1)]
    [InlineData("### Third", 3)]
    [InlineData("###### Sixth", 6)]
    public void Parse_Heading_KeepsLevel(string markdown, int expectedLevel)
    {
        var blocks = MarkdownBlockParser.Parse(markdown);

        var heading = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Heading, heading.Kind);
        Assert.Equal(expectedLevel, heading.HeadingLevel);
    }

    [Fact]
    public void Parse_TaskList_KeepsCheckedState()
    {
        var blocks = MarkdownBlockParser.Parse("- [x] done\n- [ ] todo");

        Assert.Equal(2, blocks.Count);
        Assert.True(blocks[0].IsTask);
        Assert.True(blocks[0].IsChecked);
        Assert.Equal("done", blocks[0].Text);
        Assert.False(blocks[1].IsChecked);
        Assert.Equal("todo", blocks[1].Text);
    }

    [Fact]
    public void Parse_Table_KeepsHeaderAndAlignment()
    {
        var blocks = MarkdownBlockParser.Parse("| a | b |\n| :--- | ---: |\n| 1 | 2 |");

        var table = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Table, table.Kind);
        Assert.True(table.HasHeader);
        Assert.Equal(2, table.Cells.Count);
        Assert.Equal("a", table.Cells[0][0]);
        Assert.Equal("2", table.Cells[1][1]);
        Assert.Equal(TableCellAlignment.Left, table.Alignments[0]);
        Assert.Equal(TableCellAlignment.Right, table.Alignments[1]);
    }

    [Fact]
    public void Parse_FencedCode_KeepsLanguageAndBody()
    {
        var blocks = MarkdownBlockParser.Parse("```csharp\nvar a = 1;\n```");

        var code = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Code, code.Kind);
        Assert.Equal("csharp", code.Language);
        Assert.Equal("var a = 1;", code.Text);
    }

    [Fact]
    public void Parse_Quote_KeepsKind()
    {
        var blocks = MarkdownBlockParser.Parse("> quoted");

        var quote = Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Quote, quote.Kind);
        Assert.Equal("quoted", quote.Text);
    }

    [Theory]
    [InlineData("# Title\n\npara")]
    [InlineData("- [x] done\n- [ ] todo")]
    [InlineData("> quoted\n\n- item")]
    [InlineData("| a | b |\n| --- | --- |\n| 1 | 2 |")]
    [InlineData("```js\nlet a = 1;\n```")]
    [InlineData("**bold** and `code`")]
    [InlineData("---")]
    public void ParseThenWriteThenParse_IsStable(string markdown)
    {
        var first = MarkdownBlockParser.Parse(markdown);
        var written = MarkdownBlockParser.Write(first);
        var second = MarkdownBlockParser.Parse(written);

        Assert.Equal(first.Count, second.Count);
        for (var index = 0; index < first.Count; index++)
        {
            Assert.Equal(first[index].Kind, second[index].Kind);
            Assert.Equal(first[index].HeadingLevel, second[index].HeadingLevel);
            Assert.Equal(first[index].IsTask, second[index].IsTask);
            Assert.Equal(first[index].IsChecked, second[index].IsChecked);
            Assert.Equal(first[index].Text, second[index].Text);
        }

        // 二次回写应完全幂等。
        Assert.Equal(written, MarkdownBlockParser.Write(second));
    }

    [Fact]
    public void Parse_Empty_ReturnsSingleParagraph()
    {
        var blocks = MarkdownBlockParser.Parse(string.Empty);

        Assert.Single(blocks);
        Assert.Equal(MarkdownBlockKind.Paragraph, blocks[0].Kind);
    }

    [Fact]
    public void Parse_RoundTripsInlineFormatting()
    {
        var blocks = MarkdownBlockParser.Parse("**bold** *italic* `code` [link](https://example.com)");

        var paragraph = Assert.Single(blocks);
        Assert.Contains("**bold**", paragraph.Text, StringComparison.Ordinal);
        Assert.Contains("*italic*", paragraph.Text, StringComparison.Ordinal);
        Assert.Contains("`code`", paragraph.Text, StringComparison.Ordinal);
        Assert.Contains("(https://example.com)", paragraph.Text, StringComparison.Ordinal);
    }
}
