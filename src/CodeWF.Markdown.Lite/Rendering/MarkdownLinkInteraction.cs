using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using CodeWF.Markdown.Helpers;
using CodeWF.Markdown.Shared.Rendering;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax.Inlines;

namespace CodeWF.Markdown.Rendering;

/// <summary>渲染文本中的链接区间（纯文本偏移 → URL）。</summary>
internal sealed record MarkdownLinkSpan(int Start, int End, string Url);

/// <summary>
/// 链接区间提取与指针交互：提取内联树中的链接纯文本区间，
/// 挂接手型光标与点击打开行为，并支持命中查询。
/// </summary>
internal static class MarkdownLinkInteraction
{
    public static IReadOnlyList<MarkdownLinkSpan> ExtractLinkSpans(ContainerInline? container, bool stripTaskPrefix = false)
    {
        var links = new List<MarkdownLinkSpan>();
        CollectLinkSpans(container, 0, links, ref stripTaskPrefix);
        return links;
    }

    private static int CollectLinkSpans(
        ContainerInline? container,
        int offset,
        ICollection<MarkdownLinkSpan> links,
        ref bool stripTaskPrefix)
    {
        var child = container?.FirstChild;
        while (child is not null)
        {
            switch (child)
            {
                case LiteralInline literal:
                    var literalText = literal.Content.ToString();
                    if (stripTaskPrefix && MarkdownTaskListHelper.TryStripTaskPrefix(literalText, out var stripped))
                    {
                        stripTaskPrefix = false;
                        if (!string.IsNullOrWhiteSpace(stripped))
                        {
                            offset += stripped.TrimStart().Length;
                        }
                    }
                    else
                    {
                        stripTaskPrefix = false;
                        offset += literalText.Length;
                    }

                    break;
                case CodeInline code:
                    stripTaskPrefix = false;
                    offset += code.Content.Length;
                    break;
                case LineBreakInline:
                    stripTaskPrefix = false;
                    offset += Environment.NewLine.Length;
                    break;
                case TaskList:
                    stripTaskPrefix = false;
                    break;
                case LinkInline { IsImage: true } image:
                    stripTaskPrefix = false;
                    offset += MarkdownPlainTextExtractor.ExtractImageText(image).Length;
                    break;
                case LinkInline { IsImage: false } link:
                    stripTaskPrefix = false;
                    var start = offset;
                    offset = CollectLinkSpans(link, offset, links, ref stripTaskPrefix);
                    if (offset == start && !string.IsNullOrWhiteSpace(link.Url))
                    {
                        offset += link.Url!.Length;
                    }

                    if (offset > start && !string.IsNullOrWhiteSpace(link.Url))
                    {
                        links.Add(new MarkdownLinkSpan(start, offset, link.Url!));
                    }

                    break;
                case ContainerInline nested:
                    stripTaskPrefix = false;
                    offset = CollectLinkSpans(nested, offset, links, ref stripTaskPrefix);
                    break;
                default:
                    stripTaskPrefix = false;
                    var text = child.ToString() ?? string.Empty;
                    if (!MarkdownPlainTextExtractor.IsTypeNameFallback(text, child.GetType()))
                    {
                        offset += text.Length;
                    }

                    break;
            }

            child = child.NextSibling;
        }

        return offset;
    }

    public static void AttachLinkInteraction(SelectableTextBlock textBlock, IReadOnlyList<MarkdownLinkSpan> links)
    {
        if (links.Count == 0)
        {
            return;
        }

        textBlock.Tag = links;
        textBlock.PointerMoved += (_, e) =>
        {
            string? ignored;
            textBlock.Cursor = TryGetLinkAtPointer(textBlock, e, out ignored)
                ? new Cursor(StandardCursorType.Hand)
                : null;
        };
        textBlock.PointerExited += (_, _) => textBlock.Cursor = null;
        textBlock.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left
                && string.IsNullOrEmpty(textBlock.SelectedText)
                && TryGetLinkAtPointer(textBlock, e, out var url)
                && !string.IsNullOrWhiteSpace(url))
            {
                UrlHelper.Open(url);
                e.Handled = true;
            }
        };
    }

    public static bool TryGetLinkAtPointer(SelectableTextBlock textBlock, PointerEventArgs e, out string? url)
    {
        url = null;
        if (textBlock.Tag is not IReadOnlyList<MarkdownLinkSpan> links || links.Count == 0)
        {
            return false;
        }

        var hit = textBlock.TextLayout.HitTestPoint(e.GetPosition(textBlock));
        if (!hit.IsInside)
        {
            return false;
        }

        var textPosition = hit.TextPosition;
        foreach (var link in links)
        {
            if (textPosition >= link.Start && textPosition < link.End)
            {
                url = link.Url;
                return true;
            }
        }

        return false;
    }
}
