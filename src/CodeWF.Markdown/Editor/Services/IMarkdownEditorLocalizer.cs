namespace CodeWF.Markdown.Editor.Services;

/// <summary>编辑器动作与搜索结果的本地化文案键（宿主按自己的 i18n 体系解析）。</summary>
public enum MarkdownEditorText
{
    BoldPlaceholder,
    ItalicPlaceholder,
    InlineCodePlaceholder,
    LinkPlaceholder,
    ImageAltPlaceholder,
    CodeFencePlaceholder,
    MathPlaceholder,
    TableColumn,
    TableValue,
    TableItem,
    TableDescription,
    EnterSearchTextFirst,
    SearchNoMatchFormat,
    SearchFoundOnLineFormat,
    SearchFoundWrappedOnLineFormat,
    SearchReplacedNextFormat,
    SearchReplacedAllFormat,
    SearchMatchCountFormat,
    SearchInvalidRegexFormat
}

/// <summary>
/// 编辑器本地化接缝：库内不绑定任何 i18n 框架，宿主把键映射到自己的资源。
/// </summary>
public interface IMarkdownEditorLocalizer
{
    /// <summary>取单条文案。</summary>
    string Get(MarkdownEditorText text);

    /// <summary>取带参数的格式化文案；<paramref name="args"/> 按顺序填入占位符。</summary>
    string Format(MarkdownEditorText text, params object?[] args);
}
