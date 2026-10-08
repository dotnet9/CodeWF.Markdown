using CodeWF.Markdown.Editor.Controls.Wysiwyg;
using Xunit;

namespace CodeWF.Markdown.Tests.Editor;

public sealed class MarkdownTableEditorTests
{
    [Fact]
    public void AddAndRemoveRowAndColumn_PreservesDataAndAlignment()
    {
        var model = Assert.Single(MarkdownBlockParser.Parse("| **a** | b |\n| :--- | ---: |\n| 1 | 2 |"));
        Assert.True(MarkdownTableEditor.ChangeStructure(model, 0));
        Assert.True(MarkdownTableEditor.ChangeStructure(model, 2));
        Assert.Equal(3, model.Cells.Count);
        Assert.All(model.Cells, row => Assert.Equal(3, row.Count));
        Assert.True(MarkdownTableEditor.ChangeStructure(model, 1));
        Assert.True(MarkdownTableEditor.ChangeStructure(model, 3));
        Assert.Equal("**a**", model.Cells[0][0]);
        Assert.Equal(new[] { TableCellAlignment.Left, TableCellAlignment.Right }, model.Alignments);
        var reparsed = Assert.Single(MarkdownBlockParser.Parse(MarkdownBlockParser.Write([model])));
        Assert.Equal(model.Cells.SelectMany(row => row), reparsed.Cells.SelectMany(row => row));
    }

    [Fact]
    public void Remove_DoesNotDestroyHeaderOrLastColumn()
    {
        var model = new MarkdownBlockModel { Kind = MarkdownBlockKind.Table, HasHeader = true, Cells = [["header"]] };
        Assert.False(MarkdownTableEditor.ChangeStructure(model, 1));
        Assert.False(MarkdownTableEditor.ChangeStructure(model, 3));
        Assert.Equal("header", Assert.Single(Assert.Single(model.Cells)));
    }
}
