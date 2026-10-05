using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Shared.Rendering;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 块级渲染器与宿主 MarkdownViewer 之间的服务契约：主题属性绑定、样式类
/// 与共享控件构造。渲染器不直接依赖 Viewer 实例，能力包按此契约接入。
/// </summary>
internal interface IMarkdownRenderContext
{
    double ParagraphLineHeight { get; }

    IDisposable BindTheme<T>(
        AvaloniaObject target,
        AvaloniaProperty<T> targetProperty,
        StyledProperty<T> sourceProperty);

    SelectableTextBlock CreateSelectableText(params string[] classes);

    void AddMarkdownClass(Control control, params string[] classes);

    MarkdownMathView CreateMathView(string latex, double fontSize, CSharpMath.Atom.LineStyle lineStyle);

    Control CreateFallbackText(string text, string className);

    // ---- 代码块渲染所需 ----

    bool CodeBlockIsDark { get; }

    FontFamily CodeFontFamily { get; }

    double CodeBlockFontSize { get; }

    double CodeBlockLineHeight { get; }

    bool HasSelection { get; }

    /// <summary>复制按钮点击：先触发宿主 CopyClick 事件，再写入剪贴板。</summary>
    void CopyCodeToClipboard(string code);

    Task CopySelectionAsync();

    /// <summary>宿主扩展点：允许替换/增强代码块工具条，sender 为宿主 Viewer。</summary>
    void RaiseCodeBlockToolRender(StackPanel header, StackPanel content, CodeBlock codeBlock);
}
