using CodeWF.Markdown.Editor.Services;

namespace CodeWF.Markdown.Editor.Controls.Wysiwyg;

/// <summary>
/// 所见即所得视图的默认文案：Demo 与原型都是中文界面，宿主未注入
/// <see cref="IMarkdownEditorLocalizer"/> 时用它，避免动作占位符落到英文兜底实现。
/// </summary>
internal sealed class LiveEditorLocalizer : IMarkdownEditorLocalizer
{
    public string Get(MarkdownEditorText text) => text switch
    {
        MarkdownEditorText.BoldPlaceholder => "粗体",
        MarkdownEditorText.ItalicPlaceholder => "斜体",
        MarkdownEditorText.InlineCodePlaceholder => "代码",
        MarkdownEditorText.LinkPlaceholder => "链接文字",
        MarkdownEditorText.ImageAltPlaceholder => "替代文本",
        MarkdownEditorText.CodeFencePlaceholder => "代码",
        MarkdownEditorText.MathPlaceholder => "公式",
        MarkdownEditorText.TableColumn => "列",
        MarkdownEditorText.TableValue => "值",
        MarkdownEditorText.TableItem => "项目",
        MarkdownEditorText.TableDescription => "说明",
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
