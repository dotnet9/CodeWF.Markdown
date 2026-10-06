using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using CodeWF.Markdown.Controls;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 段落渲染器：默认排版属性 + 内联内容 + 链接交互；
/// 同时作为嵌套内容路径（列表/表格/脚注）的段落构造入口。
/// </summary>
internal sealed class ParagraphRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        return block is ParagraphBlock paragraph ? CreateParagraph(paragraph, false, null, context) : null;
    }

    public static SelectableTextBlock CreateParagraph(
        ParagraphBlock paragraph,
        bool stripTaskPrefix,
        Thickness? marginOverride,
        IMarkdownRenderContext context)
    {
        var textBlock = context.CreateSelectableText(MarkdownStyleKeys.Paragraph);
        context.BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, MarkdownViewer.TextBrushProperty);
        context.BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, MarkdownViewer.ContentFontFamilyProperty);
        context.BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, MarkdownViewer.ParagraphFontSizeProperty);
        context.BindTheme(textBlock, SelectableTextBlock.LineHeightProperty, MarkdownViewer.ParagraphLineHeightProperty);
        if (marginOverride is { } margin)
        {
            textBlock.Margin = margin;
        }
        else
        {
            context.BindTheme(textBlock, Layoutable.MarginProperty, MarkdownViewer.ParagraphMarginProperty);
        }

        foreach (var inline in context.ConvertInlines(paragraph.Inline, stripTaskPrefix))
        {
            textBlock.Inlines?.Add(inline);
        }

        context.AttachLinkInteraction(textBlock, paragraph.Inline, stripTaskPrefix);
        return textBlock;
    }
}
