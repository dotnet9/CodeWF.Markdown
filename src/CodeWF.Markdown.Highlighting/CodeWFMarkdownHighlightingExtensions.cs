using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Rendering;

namespace CodeWF.Markdown.Highlighting;

/// <summary>
/// 代码高亮能力接入扩展：注册后代码块使用 TextMate 语法高亮；
/// 不注册时由 Core 降级为单色等宽渲染。
/// </summary>
public static class CodeWFMarkdownHighlightingExtensions
{
    /// <summary>全局注册高亮能力，应用启动时调用一次。</summary>
    public static void UseHighlighting() =>
        MarkdownViewer.RegisterCodeHighlighter(CodeHighlighter.Render);

    /// <summary>按实例注册高亮能力（覆盖全局注册）。</summary>
    public static MarkdownViewer UseHighlighting(this MarkdownViewer viewer)
    {
        viewer.CodeHighlighter = CodeHighlighter.Render;
        return viewer;
    }
}
