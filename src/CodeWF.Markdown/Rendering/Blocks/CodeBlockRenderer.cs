using Avalonia.Controls;
using Avalonia.Layout;
using CodeWF.Markdown.Controls;
using Lang.Avalonia;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 代码块渲染器：语言标头 + 复制按钮 + TextMate 语法高亮，
/// 受理 FencedCodeBlock 与缩进 CodeBlock。
/// </summary>
internal sealed class CodeBlockRenderer : IMarkdownBlockRenderer
{
    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is not CodeBlock codeBlock)
        {
            return null;
        }

        var code = codeBlock.Lines.ToString();
        var language = codeBlock is FencedCodeBlock fenced ? fenced.Info ?? "text" : "text";

        var border = new Border();
        context.AddMarkdownClass(border, MarkdownStyleKeys.CodeBlock);
        context.BindTheme(border, Border.BackgroundProperty, MarkdownViewer.CodeBackgroundBrushProperty);
        context.BindTheme(border, Border.BorderBrushProperty, MarkdownViewer.BorderLineBrushProperty);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        context.AddMarkdownClass(stack, MarkdownStyleKeys.CodeBlockContent);
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        context.AddMarkdownClass(header, MarkdownStyleKeys.CodeBlockHeader);

        var languageText = context.CreateSelectableText(MarkdownStyleKeys.CodeLanguage);
        languageText.Text = string.IsNullOrWhiteSpace(language) ? "text" : language;
        languageText.VerticalAlignment = VerticalAlignment.Center;
        context.BindTheme(languageText, SelectableTextBlock.ForegroundProperty, MarkdownViewer.MutedTextBrushProperty);
        context.BindTheme(languageText, SelectableTextBlock.FontSizeProperty, MarkdownViewer.CodeLanguageFontSizeProperty);

        var copyButton = new Button
        {
            Content = I18nManager.Instance.GetResource(MarkdownL.Copy),
            Tag = code
        };
        context.AddMarkdownClass(copyButton, MarkdownStyleKeys.CopyButton);
        context.BindTheme(copyButton, Button.BackgroundProperty, MarkdownViewer.AccentBrushProperty);
        context.BindTheme(copyButton, Button.ForegroundProperty, MarkdownViewer.AccentForegroundBrushProperty);
        copyButton.Click += (_, _) =>
        {
            context.CopyCodeToClipboard(code);
        };

        header.Children.Add(languageText);
        header.Children.Add(copyButton);
        stack.Children.Add(header);
        stack.Children.Add(CodeHighlighter.Render(
            code,
            language,
            context.CodeBlockIsDark,
            context.CodeFontFamily,
            context.CodeBlockFontSize,
            context.CodeBlockLineHeight,
            () => context.HasSelection,
            context.CopySelectionAsync));

        context.RaiseCodeBlockToolRender(header, stack, codeBlock);

        border.Child = stack;
        return border;
    }
}
