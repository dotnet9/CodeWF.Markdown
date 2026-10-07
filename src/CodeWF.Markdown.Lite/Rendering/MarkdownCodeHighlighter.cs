using Avalonia.Controls;
using Avalonia.Media;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 代码高亮能力委托：输入代码与排版参数，返回高亮内容控件；
/// 返回 null 表示无法高亮（调用方应使用 Core 的单色等宽降级渲染）。
/// </summary>
public delegate Control? MarkdownCodeHighlighter(
    string code,
    string language,
    bool isDark,
    FontFamily fontFamily,
    double fontSize,
    double lineHeight,
    Func<bool>? hasGlobalSelection,
    Func<Task>? copySelectionAsync);
