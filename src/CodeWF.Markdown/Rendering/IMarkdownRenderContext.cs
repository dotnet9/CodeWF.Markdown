using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Shared.Rendering;

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
}
