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
    string? LinkUrl = null,
    int SourceStart = 0);

/// <summary>把行内 Markdown 文本切成带样式的 <see cref="MarkdownTextRun"/> 列表。</summary>
public static class MarkdownTextRunParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UsePreciseSourceLocation()
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

    /// <summary>把可见文字偏移映射回行内 Markdown，跳过隐藏的格式标记。</summary>
    public static int GetSourceOffset(string markdown, int textOffset)
    {
        var remaining = Math.Max(0, textOffset);
        foreach (var run in Parse(markdown))
        {
            if (remaining <= run.Text.Length) return Math.Clamp(run.SourceStart + remaining, 0, markdown.Length);
            remaining -= run.Text.Length;
        }
        return markdown.Length;
    }

    private static bool AppendInlines(
        List<MarkdownTextRun> runs,
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool bold,
        bool italic,
        bool strike,
        string? linkUrl = null)
    {
        if (container is null)
        {
            return false;
        }

        var added = false;
        foreach (var inline in container)
        {
            added |= Append(runs, inline, bold, italic, strike, linkUrl);
        }

        return added;
    }

    private static bool Append(
        List<MarkdownTextRun> runs,
        Markdig.Syntax.Inlines.Inline inline,
        bool bold,
        bool italic,
        bool strike,
        string? linkUrl)
    {
        switch (inline)
        {
            case Markdig.Syntax.Inlines.LiteralInline literal:
                runs.Add(new MarkdownTextRun(literal.Content.ToString(), bold, italic, strike, LinkUrl: linkUrl, SourceStart: literal.Span.Start));
                return true;

            case Markdig.Syntax.Inlines.CodeInline code:
                runs.Add(new MarkdownTextRun(code.Content, bold, italic, strike, Code: true, LinkUrl: linkUrl, SourceStart: code.Span.Start + 1));
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
                    strike || isStrike,
                    linkUrl);

            case Markdig.Syntax.Inlines.LinkInline link:
                return AppendInlines(runs, link, bold, italic, strike, link.IsImage ? null : link.Url);

            case Markdig.Syntax.Inlines.LineBreakInline lineBreak:
                runs.Add(new MarkdownTextRun(lineBreak.IsHard ? "\n" : " "));
                return true;

            case Markdig.Syntax.Inlines.ContainerInline nested:
                return AppendInlines(runs, nested, bold, italic, strike, linkUrl);

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
