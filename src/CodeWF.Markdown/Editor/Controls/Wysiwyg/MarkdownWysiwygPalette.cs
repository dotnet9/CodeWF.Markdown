using Avalonia.Media;

namespace CodeWF.Markdown.Editor.Controls.Wysiwyg;

/// <summary>
/// 所见即所得视图的字体常量。颜色不在这里：由 <see cref="MarkdownLiveEditorView"/> 按宿主
/// 令牌（<c>CodeWFMarkdownEditor*Brush</c>）解析成 <see cref="MarkdownBlockViewOptions"/> 注入块视图。
/// </summary>
public static class MarkdownWysiwygPalette
{
    public static FontFamily CodeFontFamily { get; } = new("Cascadia Mono, Consolas, Microsoft YaHei UI");
}
