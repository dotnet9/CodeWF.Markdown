namespace CodeWF.Markdown.Editor.Services;

/// <summary>默认（英文）文案实现：宿主没提供 <see cref="IMarkdownEditorLocalizer"/> 时使用。</summary>
public sealed class InvariantMarkdownEditorLocalizer : IMarkdownEditorLocalizer
{
    public string Get(MarkdownEditorText text) => text switch
    {
        MarkdownEditorText.BoldPlaceholder => "bold text",
        MarkdownEditorText.ItalicPlaceholder => "italic text",
        MarkdownEditorText.InlineCodePlaceholder => "code",
        MarkdownEditorText.LinkPlaceholder => "link text",
        MarkdownEditorText.ImageAltPlaceholder => "alt text",
        MarkdownEditorText.CodeFencePlaceholder => "code",
        MarkdownEditorText.MathPlaceholder => "formula",
        MarkdownEditorText.TableColumn => "Column",
        MarkdownEditorText.TableValue => "Value",
        MarkdownEditorText.TableItem => "Item",
        MarkdownEditorText.TableDescription => "Description",
        MarkdownEditorText.EnterSearchTextFirst => "Enter search text first.",
        MarkdownEditorText.SearchNoMatchFormat => "No match for \"{0}\".",
        MarkdownEditorText.SearchFoundOnLineFormat => "Found \"{0}\" on line {1} ({2}/{3}).",
        MarkdownEditorText.SearchFoundWrappedOnLineFormat => "Wrapped to line {1} for \"{0}\" ({2}/{3}).",
        MarkdownEditorText.SearchReplacedNextFormat => "Replaced next \"{0}\".",
        MarkdownEditorText.SearchReplacedAllFormat => "Replaced {0} occurrence(s).",
        MarkdownEditorText.SearchMatchCountFormat => "{0} match(es) for \"{1}\" ({2}).",
        MarkdownEditorText.SearchInvalidRegexFormat => "Invalid regular expression: {0}",
        _ => string.Empty
    };

    public string Format(MarkdownEditorText text, params object?[] args)
    {
        var template = Get(text);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
