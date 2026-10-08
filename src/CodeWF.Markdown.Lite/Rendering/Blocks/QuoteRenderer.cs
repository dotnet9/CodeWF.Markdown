using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using CodeWF.Markdown.Controls;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 引用块渲染器：左边框 + 底色容器，内容递归走完整管线。
/// </summary>
internal sealed class QuoteRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is not QuoteBlock quote)
        {
            return null;
        }

        var border = new Border();
        context.AddMarkdownClass(border, MarkdownStyleKeys.Quote);
        context.BindTheme(border, Border.BorderBrushProperty, MarkdownViewer.BorderLineBrushProperty, BindingPriority.Style);
        context.BindTheme(border, Border.BackgroundProperty, MarkdownViewer.QuoteBackgroundBrushProperty);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        context.AddMarkdownClass(stack, MarkdownStyleKeys.QuoteContent);
        foreach (var child in quote)
        {
            var control = context.ConvertBlock(child, null);
            if (control is not null)
            {
                stack.Children.Add(control);
            }
        }

        border.Child = stack;
        return border;
    }
}
