using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using CodeWF.Markdown.Lite;

namespace CodeWF.Markdown.Lite.Rendering;

internal static class CodeHighlighter
{
    public static Control Render(
        string code,
        string language,
        bool isDark,
        FontFamily fontFamily,
        double fontSize,
        double lineHeight)
    {
        var normalized = code.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var textBlock = new SelectableTextBlock
        {
            Inlines = new InlineCollection(),
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = fontFamily,
            FontSize = fontSize,
            LineHeight = lineHeight,
            Margin = new Thickness(0, 0, 0, 6),
            SelectionBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xCC, 0x2F, 0x6F, 0xD6)),
            SelectionForegroundBrush = Brushes.White
        };
        textBlock.Classes.Add(MarkdownStyleKeys.CodeBlockText);
        TextOptions.SetBaselinePixelAlignment(textBlock, BaselinePixelAlignment.Aligned);
        textBlock.ContextMenu = CreateCopyContextMenu(textBlock);

        for (var i = 0; i < lines.Length; i++)
        {
            textBlock.Inlines.Add(new Run(lines[i])
            {
                Foreground = isDark ? Brushes.White : Brushes.Black
            });

            if (i < lines.Length - 1)
            {
                textBlock.Inlines.Add(new LineBreak());
            }
        }

        return Wrap(textBlock, Math.Max(1, lines.Length));
    }

    private static Control Wrap(Control content, int lineCount)
    {
        if (content is SelectableTextBlock textBlock)
        {
            var lineNumbers = new SelectableTextBlock
            {
                Text = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount).Select(i => i.ToString())),
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                LineHeight = textBlock.LineHeight,
                Foreground = Brushes.Gray,
                Opacity = 0.72,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 12, 6),
                IsHitTestVisible = false
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star))
                }
            };
            grid.Children.Add(lineNumbers);
            Grid.SetColumn(content, 1);
            grid.Children.Add(content);
            content = grid;
        }

        var scrollViewer = new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        scrollViewer.Classes.Add(MarkdownStyleKeys.CodeBlockScrollViewer);
        return scrollViewer;
    }

    private static ContextMenu CreateCopyContextMenu(SelectableTextBlock textBlock)
    {
        var copySelectionItem = new MenuItem
        {
            Header = "复制渲染文本"
        };
        copySelectionItem.Click += (_, _) => textBlock.Copy();
        return new ContextMenu { ItemsSource = new[] { copySelectionItem } };
    }
}
