using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CodeWF.Markdown.Controls;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 表格渲染器：星号列布局的网格表，含表头底色与行/列分隔线。
/// </summary>
internal sealed class TableRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is not Table table)
        {
            return null;
        }

        var rows = table.OfType<TableRow>().ToList();
        var columnCount = rows.Select(row => row.Count).DefaultIfEmpty(0).Max();
        var grid = new Grid();
        context.AddMarkdownClass(grid, MarkdownStyleKeys.Table);

        for (var i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                if (row[columnIndex] is not TableCell cell)
                {
                    continue;
                }

                var cellBorder = CreateTableCell(cell, row.IsHeader, rowIndex, columnIndex, context);
                Grid.SetRow(cellBorder, rowIndex);
                Grid.SetColumn(cellBorder, columnIndex);
                grid.Children.Add(cellBorder);
            }
        }

        var container = new Border
        {
            Child = grid,
            ClipToBounds = true
        };
        context.AddMarkdownClass(container, MarkdownStyleKeys.TableContainer);
        context.BindTheme(container, Border.BorderBrushProperty, MarkdownViewer.BorderLineBrushProperty);
        return container;
    }

    private static Border CreateTableCell(TableCell cell, bool isHeader, int rowIndex, int columnIndex, IMarkdownRenderContext context)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(columnIndex == 0 ? 0 : 1, rowIndex == 0 ? 0 : 1, 0, 0)
        };
        context.AddMarkdownClass(border, isHeader ? MarkdownStyleKeys.TableHeaderCell : MarkdownStyleKeys.TableCell);
        context.BindTheme(border, Border.BorderBrushProperty, MarkdownViewer.BorderLineBrushProperty);
        if (isHeader)
        {
            context.BindTheme(border, Border.BackgroundProperty, MarkdownViewer.TableHeaderBackgroundBrushProperty);
        }

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        context.AddMarkdownClass(stack, MarkdownStyleKeys.TableCellContent);
        foreach (var block in cell)
        {
            var child = CreateTableCellBlock(block, isHeader, context);
            if (child is not null)
            {
                stack.Children.Add(child);
            }
        }

        border.Child = stack;
        return border;
    }

    private static Control? CreateTableCellBlock(Block block, bool isHeader, IMarkdownRenderContext context)
    {
        var child = block is ParagraphBlock paragraph
            ? context.CreateParagraph(paragraph, false, new Thickness(0))
            : context.ConvertBlock(block, null);

        if (isHeader && child is SelectableTextBlock textBlock)
        {
            textBlock.FontWeight = FontWeight.SemiBold;
        }

        return child;
    }
}
