using System.Text;

using Markdig;

namespace CodeWF.Markdown.Editor.Controls.Wysiwyg;

/// <summary>一段带样式的文本（富文本渲染的最小单元）。</summary>
public readonly record struct MarkdownTextRun(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Strike = false,
    bool Code = false,
    string? LinkUrl = null);

/// <summary>把行内 Markdown 文本切成带样式的 <see cref="MarkdownTextRun"/> 列表。</summary>
public static class MarkdownTextRunParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .Build();

    /// <summary>解析行内 Markdown（支持 **粗**、*斜*、`码`、~~删~~、[链接](url)）。</summary>
    public static List<MarkdownTextRun> Parse(string? inlineMarkdown)
    {
        var runs = new List<MarkdownTextRun>();
        var text = inlineMarkdown ?? string.Empty;
        if (text.Length == 0)
        {
            return runs;
        }

        var container = Markdig.Markdown.Parse(text, Pipeline);
        var added = false;
        foreach (var block in container)
        {
            if (block is Markdig.Syntax.LeafBlock leaf)
            {
                added |= AppendInlines(runs, leaf.Inline, bold: false, italic: false, strike: false);
            }
        }

        if (!added)
        {
            runs.Add(new MarkdownTextRun(text));
        }

        return runs;
    }

    /// <summary>纯文本（不做 Markdown 解析），用于代码块/原始块。</summary>
    public static List<MarkdownTextRun> Plain(string? text, bool code = false)
    {
        var value = text ?? string.Empty;
        return value.Length == 0 ? [] : [new MarkdownTextRun(value, Code: code)];
    }

    /// <summary>产出用于拼接回 Markdown 的纯文本（去掉样式标记）。</summary>
    public static string ToPlainText(IEnumerable<MarkdownTextRun> runs)
    {
        var builder = new StringBuilder();
        foreach (var run in runs)
        {
            builder.Append(run.Text);
        }

        return builder.ToString();
    }

    private static bool AppendInlines(
        List<MarkdownTextRun> runs,
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool bold,
        bool italic,
        bool strike)
    {
        if (container is null)
        {
            return false;
        }

        var added = false;
        foreach (var inline in container)
        {
            added |= Append(runs, inline, bold, italic, strike);
        }

        return added;
    }

    private static bool Append(
        List<MarkdownTextRun> runs,
        Markdig.Syntax.Inlines.Inline inline,
        bool bold,
        bool italic,
        bool strike)
    {
        switch (inline)
        {
            case Markdig.Syntax.Inlines.LiteralInline literal:
                runs.Add(new MarkdownTextRun(literal.Content.ToString(), bold, italic, strike));
                return true;

            case Markdig.Syntax.Inlines.CodeInline code:
                runs.Add(new MarkdownTextRun(code.Content, bold, italic, strike, Code: true));
                return true;

            case Markdig.Syntax.Inlines.EmphasisInline emphasis:
                var isStrike = emphasis.DelimiterChar == '~';
                var isBold = !isStrike && emphasis.DelimiterCount >= 2;
                var isItalic = !isStrike && !isBold;
                return AppendInlines(
                    runs,
                    emphasis,
                    bold || isBold,
                    italic || isItalic,
                    strike || isStrike);

            case Markdig.Syntax.Inlines.LinkInline link:
                var linkRuns = Parse(CollectText(link));
                foreach (var run in linkRuns)
                {
                    runs.Add(run with { LinkUrl = link.Url });
                }

                return true;

            case Markdig.Syntax.Inlines.LineBreakInline:
                runs.Add(new MarkdownTextRun(" "));
                return true;

            case Markdig.Syntax.Inlines.ContainerInline nested:
                return AppendInlines(runs, nested, bold, italic, strike);

            default:
                return false;
        }
    }

    private static string CollectText(Markdig.Syntax.Inlines.ContainerInline container)
    {
        var builder = new StringBuilder();
        foreach (var child in container)
        {
            switch (child)
            {
                case Markdig.Syntax.Inlines.LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case Markdig.Syntax.Inlines.CodeInline code:
                    builder.Append(code.Content);
                    break;
                case Markdig.Syntax.Inlines.ContainerInline nested:
                    builder.Append(CollectText(nested));
                    break;
            }
        }

        return builder.ToString();
    }
}
