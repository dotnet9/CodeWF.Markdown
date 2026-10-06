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
public interface IMarkdownRenderContext
{
    double ParagraphLineHeight { get; }

    double ParagraphFontSize { get; }

    /// <summary>当前渲染的 Markdown 源文本（TOC 等需要整篇重解析的块使用）。</summary>
    string? MarkdownText { get; }

    /// <summary>相对图片路径的解析基路径。</summary>
    string? ImageBasePath { get; }

    double UnorderedListMarkerWidth { get; }

    double OrderedListMarkerMinWidth { get; }

    double OrderedListMarkerCharacterWidth { get; }

    double OrderedListMarkerExtraWidth { get; }

    Thickness ListFirstParagraphMargin { get; }

    Thickness ListNestedParagraphMargin { get; }

    /// <summary>递归转换嵌套块（引用、列表、表格单元格内容）。</summary>
    Control? ConvertBlock(Block block, string? sourceMarkdown);

    SelectableTextBlock CreateParagraph(ParagraphBlock paragraph, bool stripTaskPrefix, Thickness? marginOverride);

    /// <summary>内联树 → Avalonia 内联集合（链接、行内代码、公式、图片等）。</summary>
    System.Collections.Generic.IEnumerable<Avalonia.Controls.Documents.Inline> ConvertInlines(
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool stripTaskPrefix);

    /// <summary>为文本块提取链接区间并挂接手型光标与点击打开交互。</summary>
    void AttachLinkInteraction(
        SelectableTextBlock textBlock,
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool stripTaskPrefix);

    IDisposable BindTheme<T>(
        AvaloniaObject target,
        AvaloniaProperty<T> targetProperty,
        StyledProperty<T> sourceProperty);

    SelectableTextBlock CreateSelectableText(params string[] classes);

    void AddMarkdownClass(Control control, params string[] classes);

    /// <summary>
    /// 数学视图工厂（CodeWF.Markdown.Math 包注册）；为 null 或返回 null 时
    /// 由调用方降级为原文渲染。
    /// </summary>
    MarkdownMathViewFactory? MathViewFactory { get; }

    Control? CreateMathView(string latex, double fontSize, MarkdownMathLineStyle lineStyle);

    Control CreateFallbackText(string text, string className);

    // ---- 代码块渲染所需 ----

    /// <summary>
    /// 代码高亮能力（CodeWF.Markdown.Highlighting 包注册）；为 null 时
    /// 由 Core 降级为单色等宽渲染。
    /// </summary>
    MarkdownCodeHighlighter? CodeHighlighter { get; }

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
