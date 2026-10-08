using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Shared.Rendering;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 标题渲染器：下边框强调 + 按级别取字号，内容走内联转换与链接交互。
/// </summary>
internal sealed class HeadingRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is not HeadingBlock heading)
        {
            return null;
        }

        var border = new Border();
        context.AddMarkdownClass(
            border,
            MarkdownStyleKeys.HeadingBorder,
            MarkdownStyleKeys.GetHeadingBorderClass(heading.Level));
        context.BindTheme(border, Border.BorderBrushProperty, MarkdownViewer.AccentBrushProperty, BindingPriority.Style);
        context.BindTheme(border, Layoutable.MarginProperty, MarkdownViewer.HeadingMarginProperty, BindingPriority.Style);

        var textBlock = context.CreateSelectableText(MarkdownStyleKeys.Heading, MarkdownStyleKeys.GetHeadingClass(heading.Level));
        textBlock.FontWeight = FontWeight.Bold;
        context.BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, MarkdownViewer.TextBrushProperty);
        context.BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, MarkdownViewer.ContentFontFamilyProperty);
        context.BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, GetHeadingFontSizeProperty(heading.Level));

        foreach (var inline in context.ConvertInlines(heading.Inline, false))
        {
            textBlock.Inlines?.Add(inline);
        }

        context.AttachLinkInteraction(textBlock, heading.Inline, false);

        border.Child = textBlock;
        return border;
    }

    private static StyledProperty<double> GetHeadingFontSizeProperty(int level)
    {
        return level switch
        {
            1 => MarkdownViewer.Heading1FontSizeProperty,
            2 => MarkdownViewer.Heading2FontSizeProperty,
            3 => MarkdownViewer.Heading3FontSizeProperty,
            4 => MarkdownViewer.Heading4FontSizeProperty,
            5 => MarkdownViewer.Heading5FontSizeProperty,
            _ => MarkdownViewer.Heading6FontSizeProperty
        };
    }
}
