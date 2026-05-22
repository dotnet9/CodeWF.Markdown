using System.Text.RegularExpressions;
using CodeWF.Markdown.Shared.Rendering;

namespace CodeWF.Markdown.Controls;

internal static partial class MarkdownVisualEditFormatter
{
    // 可视化编辑只处理源文本边界明确的常见块，复杂块留给源码编辑器兜底。
    public static bool CanEdit(MarkdownBlockKind kind)
    {
        return kind is MarkdownBlockKind.Paragraph
            or MarkdownBlockKind.Heading
            or MarkdownBlockKind.Code
            or MarkdownBlockKind.Quote
            or MarkdownBlockKind.List;
    }

    public static string ToEditorText(MarkdownBlockKind kind, string source, string plainText)
    {
        var normalized = source.ReplaceLineEndings("\n").TrimEnd();
        return kind switch
        {
            MarkdownBlockKind.Heading => HeadingPrefixRegex().Replace(normalized, string.Empty).Trim(),
            MarkdownBlockKind.Code => StripCodeFence(normalized),
            MarkdownBlockKind.Quote => StripQuotePrefix(normalized),
            MarkdownBlockKind.List => StripListPrefix(normalized),
            _ => string.IsNullOrWhiteSpace(plainText) ? normalized : plainText
        };
    }

    public static string FromEditorText(MarkdownBlockKind kind, string source, string editedText)
    {
        var text = editedText.ReplaceLineEndings("\n").TrimEnd();
        return kind switch
        {
            MarkdownBlockKind.Heading => BuildHeading(source, text),
            MarkdownBlockKind.Code => BuildCodeBlock(source, text),
            MarkdownBlockKind.Quote => BuildQuote(text),
            MarkdownBlockKind.List => BuildList(source, text),
            _ => text
        };
    }

    private static string BuildHeading(string source, string text)
    {
        var match = HeadingPrefixRegex().Match(source.TrimStart());
        var prefix = match.Success ? match.Groups["prefix"].Value : "#";
        return $"{prefix} {text.Replace('\n', ' ').Trim()}";
    }

    private static string StripCodeFence(string source)
    {
        var lines = source.Split('\n');
        if (lines.Length >= 2 && FenceRegex().IsMatch(lines[0]) && FenceRegex().IsMatch(lines[^1]))
        {
            return string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
        }

        return source;
    }

    private static string BuildCodeBlock(string source, string text)
    {
        var firstLine = source.ReplaceLineEndings("\n").Split('\n').FirstOrDefault() ?? "```";
        var match = FenceRegex().Match(firstLine);
        var fence = match.Success ? match.Groups["fence"].Value : "```";
        var info = match.Success ? match.Groups["info"].Value.Trim() : string.Empty;
        return $"{fence}{info}\n{text}\n{fence}";
    }

    private static string StripQuotePrefix(string source)
    {
        return string.Join('\n', source.Split('\n').Select(line => QuotePrefixRegex().Replace(line, string.Empty)));
    }

    private static string BuildQuote(string text)
    {
        return string.Join('\n', text.Split('\n').Select(line => string.IsNullOrWhiteSpace(line) ? ">" : $"> {line}"));
    }

    private static string StripListPrefix(string source)
    {
        return string.Join('\n', source.Split('\n').Select(line => ListPrefixRegex().Replace(line, string.Empty)));
    }

    private static string BuildList(string source, string text)
    {
        var firstLine = source.ReplaceLineEndings("\n").Split('\n').FirstOrDefault() ?? "- ";
        var isOrdered = OrderedListPrefixRegex().IsMatch(firstLine);
        var isTask = TaskListPrefixRegex().IsMatch(firstLine);
        var taskMarker = TaskCheckedPrefixRegex().IsMatch(firstLine) ? "[x]" : "[ ]";
        var lines = text.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (lines.Length == 0)
        {
            return isOrdered ? "1. " : "- ";
        }

        return string.Join(
            '\n',
            lines.Select((line, index) =>
                isTask ? $"- {taskMarker} {line.Trim()}"
                : isOrdered ? $"{index + 1}. {line.Trim()}"
                : $"- {line.Trim()}"));
    }

    [GeneratedRegex(@"^\s*(?<prefix>#{1,6})\s*", RegexOptions.Compiled)]
    private static partial Regex HeadingPrefixRegex();

    [GeneratedRegex(@"^\s*(?<fence>`{3,}|~{3,})(?<info>.*)$", RegexOptions.Compiled)]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"^\s*>\s?", RegexOptions.Compiled)]
    private static partial Regex QuotePrefixRegex();

    [GeneratedRegex(@"^\s*(?:[-+*]\s+\[[ xX]\]\s+|\d+[.)]\s+|[-+*]\s+|\[[ xX]\]\s*)", RegexOptions.Compiled)]
    private static partial Regex ListPrefixRegex();

    [GeneratedRegex(@"^\s*\d+[.)]\s+", RegexOptions.Compiled)]
    private static partial Regex OrderedListPrefixRegex();

    [GeneratedRegex(@"^\s*[-+*]\s+\[[ xX]\]\s+", RegexOptions.Compiled)]
    private static partial Regex TaskListPrefixRegex();

    [GeneratedRegex(@"^\s*[-+*]\s+\[x\]\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TaskCheckedPrefixRegex();
}
