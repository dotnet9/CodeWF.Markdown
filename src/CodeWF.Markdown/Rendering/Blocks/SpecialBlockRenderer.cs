using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Shared.Rendering;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 特殊块渲染器（管线首位）：单图段落、TOC 块/[TOC] 文本、多图幻灯片。
/// </summary>
internal sealed class SpecialBlockRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is ParagraphBlock paragraph && TryCreateImageBlock(paragraph, context) is { } imageBlock)
        {
            return imageBlock;
        }

        if (MarkdownSpecialBlocks.IsTocBlock(block))
        {
            return CreateToc(context);
        }

        var text = MarkdownSpecialBlocks.GetSpecialBlockText(block, sourceMarkdown);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (string.Equals(text, "[TOC]", StringComparison.OrdinalIgnoreCase))
        {
            return CreateToc(context);
        }

        if (TryCreateSlideBlock(text, context) is { } slideBlock)
        {
            return slideBlock;
        }

        return null;
    }

    private static Control? TryCreateImageBlock(ParagraphBlock paragraph, IMarkdownRenderContext context)
    {
        var first = paragraph.Inline?.FirstChild;
        if (first is not LinkInline { IsImage: true } image || HasNonEmptySibling(first.NextSibling))
        {
            return null;
        }

        var markdownImage = new MarkdownImage
        {
            Source = image.Url,
            AltText = MarkdownPlainTextExtractor.ExtractPlainText(image),
            ImageBasePath = context.ImageBasePath,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        context.AddMarkdownClass(markdownImage, MarkdownStyleKeys.Image);
        return markdownImage;
    }

    private static bool HasNonEmptySibling(Markdig.Syntax.Inlines.Inline? inline)
    {
        while (inline is not null)
        {
            if (inline is LiteralInline literal && string.IsNullOrWhiteSpace(literal.Content.ToString()))
            {
                inline = inline.NextSibling;
                continue;
            }

            return true;
        }

        return false;
    }

    private static Control CreateToc(IMarkdownRenderContext context)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        context.AddMarkdownClass(panel, MarkdownStyleKeys.List);

        var document = Markdig.Markdown.Parse(context.MarkdownText ?? string.Empty, MarkdownViewer.Pipeline);
        foreach (var heading in document.OfType<HeadingBlock>().Where(h => h.Level is >= 1 and <= 3))
        {
            var item = context.CreateSelectableText(MarkdownStyleKeys.ListMarker);
            item.Text = $"{new string(' ', Math.Max(0, heading.Level - 1) * 2)}{MarkdownPlainTextExtractor.ExtractPlainText(heading.Inline)}";
            item.TextWrapping = TextWrapping.Wrap;
            context.BindTheme(item, SelectableTextBlock.ForegroundProperty, heading.Level <= 2 ? MarkdownViewer.AccentBrushProperty : MarkdownViewer.TextBrushProperty);
            context.BindTheme(item, SelectableTextBlock.FontFamilyProperty, MarkdownViewer.ContentFontFamilyProperty);
            context.BindTheme(item, SelectableTextBlock.FontSizeProperty, MarkdownViewer.ParagraphFontSizeProperty);
            panel.Children.Add(item);
        }

        return panel;
    }

    internal static Control? TryCreateSlideBlock(string text, IMarkdownRenderContext context)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("<", StringComparison.Ordinal) || !trimmed.EndsWith(">", StringComparison.Ordinal))
        {
            return null;
        }

        var content = trimmed[1..^1];
        var matches = Regex.Matches(content, @"!\[(?<alt>[^\]]*)\]\((?<url>[^)]+)\)");
        if (matches.Count < 2)
        {
            return null;
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 24,
            Margin = new Thickness(0, 4, 0, 8)
        };

        foreach (Match match in matches)
        {
            panel.Children.Add(new MarkdownImage
            {
                Source = match.Groups["url"].Value,
                AltText = match.Groups["alt"].Value,
                ImageBasePath = context.ImageBasePath,
                Width = 320,
                Height = 220,
                MaxWidth = 360,
                MaxHeight = 260
            });
        }

        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 8, 0, 12)
        };
        scrollViewer.PointerWheelChanged += OnSlidePointerWheelChanged;
        return scrollViewer;
    }

    private static void OnSlidePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || Math.Abs(e.Delta.Y) <= 0)
        {
            return;
        }

        var maxX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
        if (maxX <= 0)
        {
            return;
        }

        var x = Math.Clamp(scrollViewer.Offset.X - e.Delta.Y * 80, 0, maxX);
        scrollViewer.Offset = new Vector(x, scrollViewer.Offset.Y);
        e.Handled = true;
    }
}
