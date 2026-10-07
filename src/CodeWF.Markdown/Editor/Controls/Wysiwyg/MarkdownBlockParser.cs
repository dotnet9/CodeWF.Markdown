using System.Text;

using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using CodeWF.Markdown.Editor.Services;

namespace CodeWF.Markdown.Editor.Controls.Wysiwyg;

/// <summary>
/// Markdown ↔ <see cref="MarkdownBlockModel"/> 双向转换：解析用一个 Markdig 管道，
/// 回写用 <see cref="MarkdownSmartNewLine"/> 同样的行前缀规则，保证「渲染 → 编辑 → 回写」幂等。
/// </summary>
public static class MarkdownBlockParser
{
    private const string TaskUncheckedPrefix = "- [ ] ";
    private const string TaskCheckedPrefix = "- [x] ";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    /// <summary>解析 Markdown 文本为块列表。</summary>
    public static List<MarkdownBlockModel> Parse(string? markdown)
    {
        var blocks = new List<MarkdownBlockModel>();
        var text = markdown ?? string.Empty;
        if (text.Length == 0)
        {
            blocks.Add(new MarkdownBlockModel { Kind = MarkdownBlockKind.Paragraph });
            return blocks;
        }

        var document = Markdig.Markdown.Parse(text, Pipeline);
        foreach (var block in document)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Heading,
                        HeadingLevel = Math.Clamp(heading.Level, 1, 6),
                        Text = ExtractInlineText(heading.Inline)
                    });
                    break;

                case ParagraphBlock paragraph:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Paragraph,
                        Text = ExtractInlineText(paragraph.Inline)
                    });
                    break;

                case ListBlock list:
                    AppendList(blocks, list, indentLevel: 0, quoteDepth: 0);
                    break;

                case QuoteBlock quote:
                    AppendQuote(blocks, quote);
                    break;

                case FencedCodeBlock fenced:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Code,
                        IsFenced = true,
                        Language = fenced.Info ?? string.Empty,
                        Text = ExtractCode(fenced)
                    });
                    break;

                case CodeBlock code:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Code,
                        IsFenced = false,
                        Text = ExtractCode(code)
                    });
                    break;

                case Table table:
                    blocks.Add(ParseTable(table));
                    break;

                case ThematicBreakBlock:
                    blocks.Add(new MarkdownBlockModel { Kind = MarkdownBlockKind.ThematicBreak });
                    break;

                case HtmlBlock html:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Raw,
                        Raw = ExtractLines(html.Lines)
                    });
                    break;

                case ContainerBlock container:
                    foreach (var child in container)
                    {
                        if (child is LeafBlock leaf)
                        {
                            blocks.Add(new MarkdownBlockModel
                            {
                                Kind = MarkdownBlockKind.Raw,
                                Raw = ExtractLines(leaf.Lines)
                            });
                        }
                    }

                    break;

                default:
                    if (block is LeafBlock leafBlock)
                    {
                        blocks.Add(new MarkdownBlockModel
                        {
                            Kind = MarkdownBlockKind.Raw,
                            Raw = ExtractLines(leafBlock.Lines)
                        });
                    }

                    break;
            }
        }

        if (blocks.Count == 0)
        {
            blocks.Add(new MarkdownBlockModel { Kind = MarkdownBlockKind.Paragraph });
        }

        return blocks;
    }

    /// <summary>把块列表回写为 Markdown 文本。</summary>
    public static string Write(IReadOnlyList<MarkdownBlockModel> blocks)
    {
        var builder = new StringBuilder();
        foreach (var block in blocks)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            AppendBlock(builder, block);
        }

        var text = builder.ToString();
        return text.Length == 0 ? text : text.TrimEnd('\n') + "\n";
    }

    private static void AppendBlock(StringBuilder builder, MarkdownBlockModel block)
    {
        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading:
                builder.Append('#', Math.Clamp(block.HeadingLevel, 1, 6)).Append(' ').Append(block.Text).Append('\n');
                break;

            case MarkdownBlockKind.Paragraph:
                builder.Append(block.Text).Append('\n');
                break;

            case MarkdownBlockKind.ListItem:
                builder.Append(' ', block.IndentLevel * 2);
                for (var depth = 0; depth < block.QuoteDepth; depth++)
                {
                    builder.Append("> ");
                }

                if (block.IsTask)
                {
                    builder.Append(block.IsChecked ? TaskCheckedPrefix : TaskUncheckedPrefix);
                }
                else if (block.OrderedNumber > 0)
                {
                    builder.Append(block.OrderedNumber).Append(". ");
                }
                else
                {
                    builder.Append("- ");
                }

                builder.Append(block.Text).Append('\n');
                break;

            case MarkdownBlockKind.Quote:
                builder.Append("> ").Append(block.Text).Append('\n');
                break;

            case MarkdownBlockKind.Code:
                var language = block.IsFenced ? block.Language : string.Empty;
                builder.Append("```").Append(language).Append('\n');
                builder.Append(block.Text);
                builder.Append('\n').Append("```").Append('\n');
                break;

            case MarkdownBlockKind.Table:
                AppendTable(builder, block);
                break;

            case MarkdownBlockKind.ThematicBreak:
                builder.Append("---").Append('\n');
                break;

            default:
                builder.Append(block.Raw).Append('\n');
                break;
        }
    }

    private static void AppendTable(StringBuilder builder, MarkdownBlockModel block)
    {
        var columnCount = block.Cells.Count == 0 ? 0 : block.Cells.Max(row => row.Count);
        if (columnCount == 0)
        {
            return;
        }

        for (var row = 0; row < block.Cells.Count; row++)
        {
            builder.Append('|');
            for (var column = 0; column < columnCount; column++)
            {
                var value = column < block.Cells[row].Count ? block.Cells[row][column] : string.Empty;
                builder.Append(' ').Append(EscapeCell(value)).Append(" |");
            }

            builder.Append('\n');

            if (row == 0 && block.HasHeader)
            {
                builder.Append('|');
                for (var column = 0; column < columnCount; column++)
                {
                    var alignment = column < block.Alignments.Count ? block.Alignments[column] : TableCellAlignment.None;
                    builder.Append(alignment switch
                    {
                        TableCellAlignment.Left => " :--- |",
                        TableCellAlignment.Center => " :---: |",
                        TableCellAlignment.Right => " ---: |",
                        _ => " --- |"
                    });
                }

                builder.Append('\n');
            }
        }
    }

    private static void AppendList(List<MarkdownBlockModel> blocks, ListBlock list, int indentLevel, int quoteDepth)
    {
        var number = list.IsOrdered ? (list.OrderedStart is { } start && int.TryParse(start, out var parsed) ? parsed : 1) : 0;
        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            var isTask = false;
            var isChecked = false;
            var first = true;

            foreach (var child in listItem)
            {
                switch (child)
                {
                    case ParagraphBlock paragraph:
                        var inline = paragraph.Inline;
                        var text = ExtractInlineText(inline);
                        if (first && TryGetTaskState(inline, out var taskChecked))
                        {
                            isTask = true;
                            isChecked = taskChecked;
                            // TaskLists extension keeps the space after the marker; trim it for editing.
                            text = text.TrimStart();
                        }

                        blocks.Add(new MarkdownBlockModel
                        {
                            Kind = MarkdownBlockKind.ListItem,
                            Text = text,
                            IndentLevel = indentLevel,
                            QuoteDepth = quoteDepth,
                            IsTask = isTask,
                            IsChecked = isChecked,
                            OrderedNumber = isTask ? 0 : number
                        });
                        first = false;
                        break;

                    case ListBlock nested:
                        AppendList(blocks, nested, indentLevel + 1, quoteDepth);
                        break;

                    case FencedCodeBlock nestedFenced:
                        blocks.Add(new MarkdownBlockModel
                        {
                            Kind = MarkdownBlockKind.Code,
                            IsFenced = true,
                            Language = nestedFenced.Info ?? string.Empty,
                            Text = ExtractCode(nestedFenced),
                            IndentLevel = indentLevel + 1,
                            QuoteDepth = quoteDepth
                        });
                        break;

                    case ContainerBlock container:
                        foreach (var nestedChild in container)
                        {
                            if (nestedChild is LeafBlock leaf)
                            {
                                blocks.Add(new MarkdownBlockModel
                                {
                                    Kind = MarkdownBlockKind.Raw,
                                    Raw = ExtractLines(leaf.Lines),
                                    IndentLevel = indentLevel + 1,
                                    QuoteDepth = quoteDepth
                                });
                            }
                        }

                        break;
                }
            }

            if (number > 0)
            {
                number++;
            }
        }
    }

    private static void AppendQuote(List<MarkdownBlockModel> blocks, QuoteBlock quote)
    {
        foreach (var child in quote)
        {
            switch (child)
            {
                case ParagraphBlock paragraph:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Quote,
                        Text = ExtractInlineText(paragraph.Inline)
                    });
                    break;

                case HeadingBlock heading:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Quote,
                        Text = $"{new string('#', Math.Clamp(heading.Level, 1, 6))} {ExtractInlineText(heading.Inline)}"
                    });
                    break;

                case ListBlock list:
                    AppendList(blocks, list, indentLevel: 0, quoteDepth: 1);
                    break;

                case LeafBlock leaf:
                    blocks.Add(new MarkdownBlockModel
                    {
                        Kind = MarkdownBlockKind.Quote,
                        Text = ExtractLines(leaf.Lines)
                    });
                    break;
            }
        }
    }

    private static MarkdownBlockModel ParseTable(Table table)
    {
        var rows = new List<List<string>>();
        var alignments = new List<TableCellAlignment>();
        var hasHeader = false;

        // 该版本 Markdig 不填充 TableCell.ColumnIndex（恒为 -1），因此按行内顺序对照 ColumnDefinitions。
        var columnCount = table.ColumnDefinitions.Count;
        foreach (var row in table)
        {
            if (row is not TableRow tableRow)
            {
                continue;
            }

            if (tableRow.IsHeader)
            {
                hasHeader = true;
            }

            var cells = new List<string>();
            var cellIndex = 0;
            foreach (var cell in tableRow)
            {
                if (cell is not TableCell tableCell)
                {
                    continue;
                }

                var builder = new StringBuilder();
                foreach (var block in tableCell)
                {
                    if (block is ParagraphBlock paragraph)
                    {
                        builder.Append(ExtractInlineText(paragraph.Inline));
                    }
                }

                if (tableRow.IsHeader)
                {
                    alignments.Add(cellIndex < columnCount
                        ? table.ColumnDefinitions[cellIndex].Alignment switch
                        {
                            TableColumnAlign.Left => TableCellAlignment.Left,
                            TableColumnAlign.Center => TableCellAlignment.Center,
                            TableColumnAlign.Right => TableCellAlignment.Right,
                            _ => TableCellAlignment.None
                        }
                        : TableCellAlignment.None);
                }

                cells.Add(builder.ToString());
                cellIndex++;
            }

            rows.Add(cells);
        }

        // Fallback single empty row when the table has no parsed rows.
        if (rows.Count == 0)
        {
            rows.Add([string.Empty]);
        }

        return new MarkdownBlockModel
        {
            Kind = MarkdownBlockKind.Table,
            HasHeader = hasHeader,
            Alignments = alignments,
            Cells = rows
        };
    }

    /// <summary>
    /// 识别 Markdig 的任务列表标记（<c>- [x]</c> / <c>- [ ]</c>）：开启 TaskLists 扩展后它是
    /// <see cref="TaskListInline"/>，正文里已经不含 <c>[x]</c>，因此不能再按前缀剥离。
    /// </summary>
    private static bool TryGetTaskState(ContainerInline? inline, out bool isChecked)
    {
        isChecked = false;
        if (inline is null)
        {
            return false;
        }

        foreach (var child in inline)
        {
            if (child is TaskList task)
            {
                isChecked = task.Checked;
                return true;
            }
        }

        return false;
    }

    private static bool TryStripTaskMarker(ContainerInline? inline, out bool isChecked, out string text)
    {
        isChecked = false;
        text = string.Empty;
        if (inline is null)
        {
            return false;
        }

        var full = ExtractInlineText(inline);
        if (full.StartsWith(TaskUncheckedPrefix, StringComparison.Ordinal))
        {
            text = full[TaskUncheckedPrefix.Length..];
            return true;
        }

        if (full.StartsWith(TaskCheckedPrefix, StringComparison.Ordinal))
        {
            isChecked = true;
            text = full[TaskCheckedPrefix.Length..];
            return true;
        }

        return false;
    }

    private static string ExtractInlineText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var child in inline)
        {
            AppendInline(builder, child);
        }

        return builder.ToString();
    }

    private static void AppendInline(StringBuilder builder, Markdig.Syntax.Inlines.Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content.ToString());
                break;

            case CodeInline code:
                builder.Append('`').Append(code.Content).Append('`');
                break;

            case EmphasisInline emphasis:
                var marker = emphasis.DelimiterCount >= 2 ? "**" : "*";
                builder.Append(marker);
                foreach (var child in emphasis)
                {
                    AppendInline(builder, child);
                }

                builder.Append(marker);
                break;

            case LinkInline link when link.IsImage:
                builder.Append("![")
                    .Append(ExtractInlineText(link))
                    .Append("](")
                    .Append(link.Url ?? string.Empty)
                    .Append(')');
                break;

            case LinkInline link:
                builder.Append('[')
                    .Append(ExtractInlineText(link))
                    .Append("](")
                    .Append(link.Url ?? string.Empty)
                    .Append(')');
                break;

            case LineBreakInline:
                builder.Append('\n');
                break;

            case ContainerInline container:
                foreach (var child in container)
                {
                    AppendInline(builder, child);
                }

                break;
        }
    }

    private static string ExtractCode(LeafBlock block)
    {
        var lines = block.Lines;
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines.Lines[index];
            builder.Append(lines.Lines[index].Slice.ToString());
            if (index < lines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string ExtractLines(StringLineGroup lines)
    {
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < lines.Count; index++)
        {
            builder.Append(lines.Lines[index].Slice.ToString());
            if (index < lines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
