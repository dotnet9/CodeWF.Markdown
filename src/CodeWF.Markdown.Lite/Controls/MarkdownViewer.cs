using System.Text;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CodeWF.Markdown.Lite.Helpers;
using CodeWF.Markdown.Lite.Rendering;

using Markdig;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Inline = Avalonia.Controls.Documents.Inline;

namespace CodeWF.Markdown.Lite.Controls;

public enum MarkdownRenderMode
{
    /// <summary>
    /// 尽量复用已渲染块，只替换文本变化影响到的区段。
    /// </summary>
    Incremental,

    /// <summary>
    /// 丢弃现有渲染块并从 Markdown 文本完整重建文档。
    /// </summary>
    Full
}

/// <summary>
/// 将 Markdown 文本渲染为 Avalonia 控件树的只读预览控件。
/// </summary>
[TemplatePart(DocumentHostPartName, typeof(Panel), IsRequired = true)]
public class MarkdownViewer : TemplatedControl
{
    private const string DocumentHostPartName = "PART_DocumentHost";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly List<RenderedBlock> _renderedBlocks = [];
    private readonly List<IDisposable> _currentBlockDisposables = [];
    private Panel? _documentHost;
    private string _renderedMarkdown = string.Empty;
    private MarkdownRenderMode _queuedRenderMode = MarkdownRenderMode.Incremental;
    private bool _isRenderQueued;

    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(Markdown));

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(TextBrush), Brushes.Black);

    public static readonly StyledProperty<IBrush?> MutedTextBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(MutedTextBrush), Brushes.Gray);

    public static readonly StyledProperty<IBrush?> AccentBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(AccentBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<IBrush?> AccentForegroundBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(AccentForegroundBrush), Brushes.White);

    public static readonly StyledProperty<IBrush?> BorderLineBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(BorderLineBrush), Brushes.LightGray);

    public static readonly StyledProperty<IBrush?> QuoteBackgroundBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(QuoteBackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> CodeBackgroundBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(CodeBackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> InlineCodeBackgroundBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(InlineCodeBackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<IBrush?> TableHeaderBackgroundBrushProperty =
        AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(TableHeaderBackgroundBrush), Brushes.Transparent);

    public static readonly StyledProperty<double> ParagraphFontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(ParagraphFontSize), 16);

    public static readonly StyledProperty<double> ParagraphLineHeightProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(ParagraphLineHeight), 28);

    public static readonly StyledProperty<double> Heading1FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading1FontSize), 30);

    public static readonly StyledProperty<double> Heading2FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading2FontSize), 26);

    public static readonly StyledProperty<double> Heading3FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading3FontSize), 22);

    public static readonly StyledProperty<double> Heading4FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading4FontSize), 20);

    public static readonly StyledProperty<double> Heading5FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading5FontSize), 18);

    public static readonly StyledProperty<double> Heading6FontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading6FontSize), 16);

    public static readonly StyledProperty<double> BlockSpacingProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(BlockSpacing), 8);

    public static readonly StyledProperty<double> DocumentBottomPaddingProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(DocumentBottomPadding), 64);

    public static readonly StyledProperty<Thickness> ParagraphMarginProperty =
        AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ParagraphMargin), new Thickness(0, 4, 0, 10));

    public static readonly StyledProperty<Thickness> HeadingMarginProperty =
        AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(HeadingMargin), new Thickness(0, 18, 0, 10));

    public static readonly StyledProperty<FontFamily> ContentFontFamilyProperty =
        AvaloniaProperty.Register<MarkdownViewer, FontFamily>(nameof(ContentFontFamily), FontFamily.Default);

    public static readonly StyledProperty<FontFamily> CodeFontFamilyProperty =
        AvaloniaProperty.Register<MarkdownViewer, FontFamily>(
            nameof(CodeFontFamily),
            new FontFamily("Consolas, Cascadia Mono, JetBrains Mono, monospace"));

    public static readonly StyledProperty<double> CodeBlockFontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeBlockFontSize), 13);

    public static readonly StyledProperty<double> CodeBlockLineHeightProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeBlockLineHeight), 20);

    public static readonly StyledProperty<double> CodeLanguageFontSizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeLanguageFontSize), 12);

    public static readonly StyledProperty<double> UnorderedListMarkerWidthProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(UnorderedListMarkerWidth), 24);

    public static readonly StyledProperty<double> OrderedListMarkerMinWidthProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerMinWidth), 28);

    public static readonly StyledProperty<double> OrderedListMarkerCharacterWidthProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerCharacterWidth), 9);

    public static readonly StyledProperty<double> OrderedListMarkerExtraWidthProperty =
        AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerExtraWidth), 6);

    public static readonly StyledProperty<Thickness> ListFirstParagraphMarginProperty =
        AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ListFirstParagraphMargin), new Thickness(0, 0, 0, 2));

    public static readonly StyledProperty<Thickness> ListNestedParagraphMarginProperty =
        AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ListNestedParagraphMargin), new Thickness(0, 2, 0, 2));

    /// <summary>
    /// Markdown 原文；控件只暴露这一处文本输入入口。
    /// </summary>
    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public IBrush? MutedTextBrush
    {
        get => GetValue(MutedTextBrushProperty);
        set => SetValue(MutedTextBrushProperty, value);
    }

    public IBrush? AccentBrush
    {
        get => GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public IBrush? AccentForegroundBrush
    {
        get => GetValue(AccentForegroundBrushProperty);
        set => SetValue(AccentForegroundBrushProperty, value);
    }

    public IBrush? BorderLineBrush
    {
        get => GetValue(BorderLineBrushProperty);
        set => SetValue(BorderLineBrushProperty, value);
    }

    public IBrush? QuoteBackgroundBrush
    {
        get => GetValue(QuoteBackgroundBrushProperty);
        set => SetValue(QuoteBackgroundBrushProperty, value);
    }

    public IBrush? CodeBackgroundBrush
    {
        get => GetValue(CodeBackgroundBrushProperty);
        set => SetValue(CodeBackgroundBrushProperty, value);
    }

    public IBrush? InlineCodeBackgroundBrush
    {
        get => GetValue(InlineCodeBackgroundBrushProperty);
        set => SetValue(InlineCodeBackgroundBrushProperty, value);
    }

    public IBrush? TableHeaderBackgroundBrush
    {
        get => GetValue(TableHeaderBackgroundBrushProperty);
        set => SetValue(TableHeaderBackgroundBrushProperty, value);
    }

    public double ParagraphFontSize
    {
        get => GetValue(ParagraphFontSizeProperty);
        set => SetValue(ParagraphFontSizeProperty, value);
    }

    public double ParagraphLineHeight
    {
        get => GetValue(ParagraphLineHeightProperty);
        set => SetValue(ParagraphLineHeightProperty, value);
    }

    public double Heading1FontSize
    {
        get => GetValue(Heading1FontSizeProperty);
        set => SetValue(Heading1FontSizeProperty, value);
    }

    public double Heading2FontSize
    {
        get => GetValue(Heading2FontSizeProperty);
        set => SetValue(Heading2FontSizeProperty, value);
    }

    public double Heading3FontSize
    {
        get => GetValue(Heading3FontSizeProperty);
        set => SetValue(Heading3FontSizeProperty, value);
    }

    public double Heading4FontSize
    {
        get => GetValue(Heading4FontSizeProperty);
        set => SetValue(Heading4FontSizeProperty, value);
    }

    public double Heading5FontSize
    {
        get => GetValue(Heading5FontSizeProperty);
        set => SetValue(Heading5FontSizeProperty, value);
    }

    public double Heading6FontSize
    {
        get => GetValue(Heading6FontSizeProperty);
        set => SetValue(Heading6FontSizeProperty, value);
    }

    public double BlockSpacing
    {
        get => GetValue(BlockSpacingProperty);
        set => SetValue(BlockSpacingProperty, value);
    }

    public double DocumentBottomPadding
    {
        get => GetValue(DocumentBottomPaddingProperty);
        set => SetValue(DocumentBottomPaddingProperty, value);
    }

    public Thickness ParagraphMargin
    {
        get => GetValue(ParagraphMarginProperty);
        set => SetValue(ParagraphMarginProperty, value);
    }

    public Thickness HeadingMargin
    {
        get => GetValue(HeadingMarginProperty);
        set => SetValue(HeadingMarginProperty, value);
    }

    public FontFamily ContentFontFamily
    {
        get => GetValue(ContentFontFamilyProperty);
        set => SetValue(ContentFontFamilyProperty, value);
    }

    public FontFamily CodeFontFamily
    {
        get => GetValue(CodeFontFamilyProperty);
        set => SetValue(CodeFontFamilyProperty, value);
    }

    public double CodeBlockFontSize
    {
        get => GetValue(CodeBlockFontSizeProperty);
        set => SetValue(CodeBlockFontSizeProperty, value);
    }

    public double CodeBlockLineHeight
    {
        get => GetValue(CodeBlockLineHeightProperty);
        set => SetValue(CodeBlockLineHeightProperty, value);
    }

    public double CodeLanguageFontSize
    {
        get => GetValue(CodeLanguageFontSizeProperty);
        set => SetValue(CodeLanguageFontSizeProperty, value);
    }

    public double UnorderedListMarkerWidth
    {
        get => GetValue(UnorderedListMarkerWidthProperty);
        set => SetValue(UnorderedListMarkerWidthProperty, value);
    }

    public double OrderedListMarkerMinWidth
    {
        get => GetValue(OrderedListMarkerMinWidthProperty);
        set => SetValue(OrderedListMarkerMinWidthProperty, value);
    }

    public double OrderedListMarkerCharacterWidth
    {
        get => GetValue(OrderedListMarkerCharacterWidthProperty);
        set => SetValue(OrderedListMarkerCharacterWidthProperty, value);
    }

    public double OrderedListMarkerExtraWidth
    {
        get => GetValue(OrderedListMarkerExtraWidthProperty);
        set => SetValue(OrderedListMarkerExtraWidthProperty, value);
    }

    public Thickness ListFirstParagraphMargin
    {
        get => GetValue(ListFirstParagraphMarginProperty);
        set => SetValue(ListFirstParagraphMarginProperty, value);
    }

    public Thickness ListNestedParagraphMargin
    {
        get => GetValue(ListNestedParagraphMarginProperty);
        set => SetValue(ListNestedParagraphMarginProperty, value);
    }

    public event EventHandler? CopyClick;

    /// <summary>
    /// 在代码块工具栏创建完成后触发，调用方可追加自定义按钮。
    /// </summary>
    public event EventHandler<CodeBlockToolRenderEventArgs>? CodeBlockToolRender;

    static MarkdownViewer()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument(MarkdownRenderMode.Incremental));
    }

    private MenuItem? _viewerCopyMenuItem;

    public MarkdownViewer()
    {
        Focusable = true;
        ContextMenu = CreateViewerContextMenu();
        TextOptions.SetBaselinePixelAlignment(this, BaselinePixelAlignment.Aligned);
    }

    /// <summary>
    /// 复制当前渲染结果的纯文本，作为跨多个渲染块选择时的兜底复制方式。
    /// </summary>
    public async Task CopyRenderedTextAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(GetRenderedText());
        }
    }

    public string GetRenderedText()
    {
        var text = Markdown ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var document = Markdig.Markdown.Parse(text, Pipeline);
        var builder = new StringBuilder();
        foreach (var block in document)
        {
            AppendPlainTextBlock(builder, block, 0);
        }

        return builder.ToString().TrimEnd();
    }

    public void Rerender()
    {
        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    public void RenderIncremental()
    {
        QueueRenderDocument(MarkdownRenderMode.Incremental);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _documentHost = e.NameScope.Find<Panel>(DocumentHostPartName);
        RenderDocument(MarkdownRenderMode.Full);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }

        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        }

        base.OnDetachedFromVisualTree(e);
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (ReferenceEquals(e.Source, this)
            && e.Key == Key.C
            && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            await CopyRenderedTextAsync();
            e.Handled = true;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        // Lite 代码块颜色跟随明暗主题，主题变化时需要重建。
        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    private void QueueRenderDocument(MarkdownRenderMode mode)
    {
        if (mode == MarkdownRenderMode.Full)
        {
            _queuedRenderMode = MarkdownRenderMode.Full;
        }

        if (_documentHost is null)
        {
            return;
        }

        if (_isRenderQueued)
        {
            return;
        }

        _isRenderQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _isRenderQueued = false;

            var queuedMode = _queuedRenderMode;
            _queuedRenderMode = MarkdownRenderMode.Incremental;
            RenderDocument(queuedMode);
        }, DispatcherPriority.Render);
    }

    private void RenderDocument(MarkdownRenderMode mode)
    {
        if (_documentHost is null)
        {
            return;
        }

        var text = Markdown ?? string.Empty;
        if (mode == MarkdownRenderMode.Incremental && TryRenderIncremental(text))
        {
            return;
        }

        RenderDocumentFull(text);
    }

    private void RenderDocumentFull(string text)
    {
        if (_documentHost is null)
        {
            return;
        }

        foreach (var block in _renderedBlocks)
        {
            block.Cleanup();
        }
        _documentHost.Children.Clear();
        _renderedBlocks.Clear();
        _renderedMarkdown = text;

        if (string.IsNullOrWhiteSpace(text))
        {
            InvalidateDocumentLayout();
            return;
        }

        foreach (var renderedBlock in CreateRenderedBlocks(text, 0))
        {
            _documentHost.Children.Add(renderedBlock.Control);
            _renderedBlocks.Add(renderedBlock);
        }

        InvalidateDocumentLayout();
    }

    private bool TryRenderIncremental(string text)
    {
        if (_documentHost is null)
        {
            return false;
        }

        if (text == _renderedMarkdown)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(_renderedMarkdown) || _renderedBlocks.Count == 0)
        {
            return false;
        }

        var change = CalculateTextChange(_renderedMarkdown, text);
        if (ShouldFullRender(change, _renderedMarkdown.Length, text.Length))
        {
            return false;
        }

        // Markdig 不直接暴露块级增量渲染，这里用文本范围定位受影响块，再只替换该区间。
        var replaceStartIndex = FindReplaceStartIndex(change.OldStart);
        if (replaceStartIndex < 0)
        {
            return false;
        }

        var replaceEndIndex = FindReplaceEndIndex(change, replaceStartIndex);
        if (replaceEndIndex < replaceStartIndex)
        {
            return false;
        }

        var oldRegionStart = replaceStartIndex < _renderedBlocks.Count
            ? _renderedBlocks[replaceStartIndex].Start
            : _renderedMarkdown.Length;
        var oldRegionEnd = replaceEndIndex < _renderedBlocks.Count
            ? _renderedBlocks[replaceEndIndex].Start
            : _renderedMarkdown.Length;

        var newRegionStart = Math.Clamp(MapOldStartOffsetToNew(oldRegionStart, change), 0, text.Length);
        var newRegionEnd = Math.Clamp(MapOldEndOffsetToNew(oldRegionEnd, change), newRegionStart, text.Length);
        if (newRegionEnd - newRegionStart > Math.Max(4096, text.Length * 9 / 10))
        {
            return false;
        }

        var fragment = text[newRegionStart..newRegionEnd];
        var newBlocks = CreateRenderedBlocks(fragment, newRegionStart);
        ReplaceRenderedBlocks(replaceStartIndex, replaceEndIndex, newBlocks, change.Delta);

        _renderedMarkdown = text;
        return true;
    }

    private IReadOnlyList<RenderedBlock> CreateRenderedBlocks(string markdown, int sourceOffset)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return [];
        }

        var renderedBlocks = new List<RenderedBlock>();
        var document = Markdig.Markdown.Parse(markdown, Pipeline);

        foreach (var block in document)
        {
            var previousDisposableCount = _currentBlockDisposables.Count;
            var control = ConvertBlock(block, markdown);
            if (control is null)
            {
                for (var i = _currentBlockDisposables.Count - 1; i >= previousDisposableCount; i--)
                {
                    _currentBlockDisposables[i].Dispose();
                    _currentBlockDisposables.RemoveAt(i);
                }
                continue;
            }

            var range = GetBlockRange(block, sourceOffset, markdown.Length);
            var blockDisposables = _currentBlockDisposables.GetRange(previousDisposableCount,
                _currentBlockDisposables.Count - previousDisposableCount);
            renderedBlocks.Add(new RenderedBlock(range.Start, range.End, control, blockDisposables));
        }

        return renderedBlocks;
    }

    private void ReplaceRenderedBlocks(
        int replaceStartIndex,
        int replaceEndIndex,
        IReadOnlyList<RenderedBlock> newBlocks,
        int delta)
    {
        if (_documentHost is null)
        {
            return;
        }

        var removeCount = replaceEndIndex - replaceStartIndex;
        for (var i = 0; i < removeCount; i++)
        {
            _renderedBlocks[replaceStartIndex].Cleanup();
            _documentHost.Children.RemoveAt(replaceStartIndex);
            _renderedBlocks.RemoveAt(replaceStartIndex);
        }

        for (var i = 0; i < newBlocks.Count; i++)
        {
            var renderedBlock = newBlocks[i];
            _documentHost.Children.Insert(replaceStartIndex + i, renderedBlock.Control);
            _renderedBlocks.Insert(replaceStartIndex + i, renderedBlock);
        }

        for (var i = replaceStartIndex + newBlocks.Count; i < _renderedBlocks.Count; i++)
        {
            _renderedBlocks[i] = _renderedBlocks[i].Shift(delta);
        }

        InvalidateDocumentLayout();
    }

    private void InvalidateDocumentLayout()
    {
        _documentHost?.InvalidateMeasure();
        _documentHost?.InvalidateArrange();
        InvalidateMeasure();
        InvalidateArrange();
    }

    private int FindReplaceStartIndex(int oldChangeStart)
    {
        if (_renderedBlocks.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < _renderedBlocks.Count; i++)
        {
            var block = _renderedBlocks[i];
            if (oldChangeStart <= block.End)
            {
                return i > 0 && oldChangeStart <= block.Start ? i - 1 : i;
            }
        }

        return _renderedBlocks.Count - 1;
    }

    private int FindReplaceEndIndex(TextChange change, int replaceStartIndex)
    {
        var boundary = Math.Max(change.OldEnd, change.OldStart);
        var endIndex = replaceStartIndex;
        while (endIndex < _renderedBlocks.Count && _renderedBlocks[endIndex].Start < boundary)
        {
            endIndex++;
        }

        if (endIndex == replaceStartIndex)
        {
            endIndex++;
        }

        if (change.OldStart == change.OldEnd
            && endIndex < _renderedBlocks.Count
            && _renderedBlocks[endIndex].Start == change.OldStart)
        {
            endIndex++;
        }

        return Math.Min(endIndex, _renderedBlocks.Count);
    }

    private static TextRange GetBlockRange(Block block, int sourceOffset, int markdownLength)
    {
        var start = block.Span.Start >= 0 ? block.Span.Start : 0;
        var end = block.Span.End >= start ? block.Span.End + 1 : markdownLength;

        start = Math.Clamp(start, 0, markdownLength);
        end = Math.Clamp(end, start, markdownLength);

        return new TextRange(sourceOffset + start, sourceOffset + end);
    }

    private static TextChange CalculateTextChange(string oldText, string newText)
    {
        var prefixLength = 0;
        var minLength = Math.Min(oldText.Length, newText.Length);
        while (prefixLength < minLength && oldText[prefixLength] == newText[prefixLength])
        {
            prefixLength++;
        }

        var suffixLength = 0;
        while (suffixLength < oldText.Length - prefixLength
               && suffixLength < newText.Length - prefixLength
               && oldText[oldText.Length - suffixLength - 1] == newText[newText.Length - suffixLength - 1])
        {
            suffixLength++;
        }

        return new TextChange(
            prefixLength,
            oldText.Length - suffixLength,
            prefixLength,
            newText.Length - suffixLength,
            newText.Length - oldText.Length);
    }

    private static bool ShouldFullRender(TextChange change, int oldLength, int newLength)
    {
        var preservedLength = change.OldStart + oldLength - change.OldEnd;
        if (oldLength > 0 && preservedLength < oldLength / 2)
        {
            return true;
        }

        var newChangedLength = change.NewEnd - change.NewStart;
        return newLength > 4096 && newChangedLength > Math.Max(4096, newLength * 9 / 10);
    }

    private static int MapOldStartOffsetToNew(int oldOffset, TextChange change)
    {
        if (oldOffset <= change.OldStart)
        {
            return oldOffset;
        }

        if (oldOffset >= change.OldEnd)
        {
            return oldOffset + change.Delta;
        }

        return change.NewStart;
    }

    private static int MapOldEndOffsetToNew(int oldOffset, TextChange change)
    {
        if (oldOffset < change.OldStart)
        {
            return oldOffset;
        }

        if (oldOffset >= change.OldEnd)
        {
            return oldOffset + change.Delta;
        }

        return change.NewEnd;
    }

    private Control? ConvertBlock(Block block, string? sourceMarkdown = null)
    {
        if (TryCreateSpecialBlock(block, sourceMarkdown, out var specialBlock))
        {
            return specialBlock;
        }

        return block switch
        {
            ParagraphBlock paragraph => CreateParagraph(paragraph),
            HeadingBlock heading => CreateHeading(heading),
            LinkReferenceDefinitionGroup => null,
            LinkReferenceDefinition => null,
            FencedCodeBlock codeBlock => CreateCodeBlock(codeBlock),
            CodeBlock codeBlock => CreateCodeBlock(codeBlock),
            ListBlock list => CreateList(list),
            QuoteBlock quote => CreateQuote(quote),
            ThematicBreakBlock => CreateThematicBreak(),
            Table table => CreateTable(table),
            FootnoteGroup footnotes => CreateFootnoteGroup(footnotes),
            Footnote footnote => CreateFootnote(footnote),
            HtmlBlock htmlBlock => CreateHtmlBlock(htmlBlock.Lines.ToString()),
            _ => CreateUnknownBlock(block)
        };
    }

    private bool TryCreateSpecialBlock(Block block, string? sourceMarkdown, out Control? control)
    {
        control = null;
        if (block is ParagraphBlock paragraph && TryCreateImageBlock(paragraph, out control))
        {
            return true;
        }

        if (IsTocBlock(block))
        {
            control = CreateToc();
            return true;
        }

        var text = GetSpecialBlockText(block, sourceMarkdown);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (string.Equals(text, "[TOC]", StringComparison.OrdinalIgnoreCase))
        {
            control = CreateToc();
            return true;
        }

        if (TryExtractMathBlock(text, IsMathBlock(block), out var latex))
        {
            control = CreateMathBlock(latex);
            return true;
        }

        if (TryCreateSlideBlock(text, out var slideBlock))
        {
            control = slideBlock;
            return true;
        }

        return false;
    }

    private static bool IsTocBlock(Block block)
    {
        var typeName = block.GetType().Name;
        return typeName.Contains("TableOfContents", StringComparison.OrdinalIgnoreCase)
               || typeName.Equals("TocBlock", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMathBlock(Block block)
    {
        return block.GetType().Name.Contains("Math", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetSpecialBlockText(Block block, string? sourceMarkdown)
    {
        var sourceText = GetSourceText(block, sourceMarkdown);
        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            return sourceText.Trim();
        }

        return block switch
        {
            ParagraphBlock paragraphBlock => paragraphBlock.Lines.ToString().Trim(),
            HtmlBlock htmlBlock => htmlBlock.Lines.ToString().Trim(),
            LeafBlock leafBlock when IsMathBlock(block) => leafBlock.Lines.ToString().Trim(),
            _ when IsMathBlock(block) => block.ToString()?.Trim(),
            _ => null
        };
    }

    private static string? GetSourceText(Block block, string? sourceMarkdown)
    {
        if (string.IsNullOrEmpty(sourceMarkdown)
            || block.Span.Start < 0
            || block.Span.End < block.Span.Start
            || block.Span.Start >= sourceMarkdown.Length)
        {
            return null;
        }

        var end = Math.Min(block.Span.End, sourceMarkdown.Length - 1);
        return sourceMarkdown[block.Span.Start..(end + 1)];
    }

    private bool TryCreateImageBlock(ParagraphBlock paragraph, out Control? control)
    {
        control = null;
        var first = paragraph.Inline?.FirstChild;
        if (first is not LinkInline { IsImage: true } image || HasNonEmptySibling(first.NextSibling))
        {
            return false;
        }

        var markdownImage = new MarkdownImage
        {
            Source = image.Url,
            AltText = ExtractPlainText(image),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        AddMarkdownClass(markdownImage, MarkdownStyleKeys.Image);
        control = markdownImage;
        return true;
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

    private SelectableTextBlock CreateParagraph(
        ParagraphBlock paragraph,
        bool stripTaskPrefix = false,
        Thickness? marginOverride = null)
    {
        var textBlock = CreateSelectableText(MarkdownStyleKeys.Paragraph);
        BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, TextBrushProperty);
        BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
        BindTheme(textBlock, SelectableTextBlock.LineHeightProperty, ParagraphLineHeightProperty);
        if (marginOverride is { } margin)
        {
            textBlock.Margin = margin;
        }
        else
        {
            BindTheme(textBlock, MarginProperty, ParagraphMarginProperty);
        }

        foreach (var inline in ConvertInlines(paragraph.Inline, stripTaskPrefix))
        {
            textBlock.Inlines?.Add(inline);
        }

        if (TryGetSingleTextLink(paragraph.Inline, out var linkUrl))
        {
            textBlock.Cursor = new Cursor(StandardCursorType.Hand);
            textBlock.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.Left
                    && string.IsNullOrEmpty(textBlock.SelectedText)
                    && !string.IsNullOrWhiteSpace(linkUrl))
                {
                    UrlHelper.Open(linkUrl);
                }
            };
        }

        return textBlock;
    }

    private Control CreateHeading(HeadingBlock heading)
    {
        var border = new Border();
        AddMarkdownClass(
            border,
            MarkdownStyleKeys.HeadingBorder,
            MarkdownStyleKeys.GetHeadingBorderClass(heading.Level));
        BindTheme(border, Border.BorderBrushProperty, AccentBrushProperty);
        BindTheme(border, MarginProperty, HeadingMarginProperty);

        var textBlock = CreateSelectableText(MarkdownStyleKeys.Heading, MarkdownStyleKeys.GetHeadingClass(heading.Level));
        textBlock.FontWeight = FontWeight.Bold;
        BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, heading.Level <= 2 ? AccentBrushProperty : TextBrushProperty);
        BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, GetHeadingFontSizeProperty(heading.Level));

        foreach (var inline in ConvertInlines(heading.Inline))
        {
            textBlock.Inlines?.Add(inline);
        }

        border.Child = textBlock;
        return border;
    }

    private Control CreateCodeBlock(CodeBlock codeBlock)
    {
        var code = codeBlock.Lines.ToString();
        var language = codeBlock is FencedCodeBlock fenced ? fenced.Info ?? "text" : "text";

        var border = new Border();
        AddMarkdownClass(border, MarkdownStyleKeys.CodeBlock);
        BindTheme(border, Border.BackgroundProperty, CodeBackgroundBrushProperty);
        BindTheme(border, Border.BorderBrushProperty, BorderLineBrushProperty);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        AddMarkdownClass(stack, MarkdownStyleKeys.CodeBlockContent);
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        AddMarkdownClass(header, MarkdownStyleKeys.CodeBlockHeader);

        var languageText = CreateSelectableText(MarkdownStyleKeys.CodeLanguage);
        languageText.Text = string.IsNullOrWhiteSpace(language) ? "text" : language;
        languageText.VerticalAlignment = VerticalAlignment.Center;
        BindTheme(languageText, SelectableTextBlock.ForegroundProperty, MutedTextBrushProperty);
        BindTheme(languageText, SelectableTextBlock.FontSizeProperty, CodeLanguageFontSizeProperty);

        var copyButton = new Button
        {
            Content = "复制",
            Tag = code
        };
        AddMarkdownClass(copyButton, MarkdownStyleKeys.CopyButton);
        BindTheme(copyButton, Button.BackgroundProperty, AccentBrushProperty);
        BindTheme(copyButton, Button.ForegroundProperty, AccentForegroundBrushProperty);
        copyButton.Click += (_, _) =>
        {
            CopyClick?.Invoke(this, EventArgs.Empty);
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && copyButton.Tag is string tagCode)
            {
                _ = clipboard.SetTextAsync(tagCode);
            }
        };

        header.Children.Add(languageText);
        header.Children.Add(copyButton);
        stack.Children.Add(header);
        stack.Children.Add(CodeHighlighter.Render(
            code,
            language,
            ActualThemeVariant == ThemeVariant.Dark,
            CodeFontFamily,
            CodeBlockFontSize,
            CodeBlockLineHeight));

        CodeBlockToolRender?.Invoke(this, new CodeBlockToolRenderEventArgs(header, stack, codeBlock));

        border.Child = stack;
        return border;
    }

    private Control CreateList(ListBlock list)
    {
        var items = list.OfType<ListItemBlock>().ToList();
        var startIndex = GetOrderedStart(list);
        var markerWidth = CalculateListMarkerWidth(list.IsOrdered, startIndex, items.Count);
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        AddMarkdownClass(panel, MarkdownStyleKeys.List);

        for (var i = 0; i < items.Count; i++)
        {
            panel.Children.Add(CreateListItem(items[i], list.IsOrdered, startIndex + i, markerWidth));
        }

        return panel;
    }

    private Control CreateListItem(ListItemBlock item, bool ordered, int index, double markerWidth)
    {
        var itemGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };
        AddMarkdownClass(itemGrid, MarkdownStyleKeys.ListItem);

        var isTask = TryReadTaskState(item, out var isChecked);
        Control marker = isTask
            ? CreateTaskMarker(isChecked, markerWidth)
            : CreateListMarker(ordered ? $"{index}." : "•", markerWidth);

        Grid.SetColumn(marker, 0);
        itemGrid.Children.Add(marker);

        var content = new StackPanel { Orientation = Orientation.Vertical };
        AddMarkdownClass(content, MarkdownStyleKeys.ListItemContent);
        var firstParagraph = true;
        foreach (var block in item)
        {
            var child = block is ParagraphBlock paragraph
                ? CreateParagraph(paragraph, isTask && firstParagraph, GetListParagraphMargin(firstParagraph))
                : ConvertBlock(block);
            firstParagraph = false;

            if (child is not null)
            {
                content.Children.Add(child);
            }
        }

        Grid.SetColumn(content, 1);
        itemGrid.Children.Add(content);
        return itemGrid;
    }

    private Control CreateTaskMarker(bool isChecked, double markerWidth)
    {
        var checkBox = new CheckBox
        {
            IsChecked = isChecked,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top
        };
        AddMarkdownClass(checkBox, MarkdownStyleKeys.TaskMarkerBox);

        var marker = new Border
        {
            Child = checkBox,
            MinWidth = markerWidth,
            VerticalAlignment = VerticalAlignment.Top
        };
        AddMarkdownClass(marker, MarkdownStyleKeys.ListMarker);
        return marker;
    }

    private SelectableTextBlock CreateListMarker(string text, double markerWidth)
    {
        var marker = CreateSelectableText(MarkdownStyleKeys.ListMarker);
        marker.Text = text;
        marker.FontWeight = FontWeight.Bold;
        marker.MinWidth = markerWidth;
        marker.TextAlignment = TextAlignment.Right;
        marker.VerticalAlignment = VerticalAlignment.Top;
        BindTheme(marker, SelectableTextBlock.ForegroundProperty, AccentBrushProperty);
        BindTheme(marker, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(marker, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
        BindTheme(marker, SelectableTextBlock.LineHeightProperty, ParagraphLineHeightProperty);
        return marker;
    }

    private static int GetOrderedStart(ListBlock list)
    {
        return list.IsOrdered
               && int.TryParse(list.OrderedStart, out var start)
               && start > 0
            ? start
            : 1;
    }

    private double CalculateListMarkerWidth(bool ordered, int startIndex, int itemCount)
    {
        if (!ordered)
        {
            return UnorderedListMarkerWidth;
        }

        var lastMarkerLength = $"{Math.Max(startIndex, startIndex + itemCount - 1)}.".Length;
        return Math.Max(
            OrderedListMarkerMinWidth,
            lastMarkerLength * OrderedListMarkerCharacterWidth + OrderedListMarkerExtraWidth);
    }

    private Thickness GetListParagraphMargin(bool firstParagraph)
    {
        return firstParagraph ? ListFirstParagraphMargin : ListNestedParagraphMargin;
    }

    private Control CreateQuote(QuoteBlock quote)
    {
        var border = new Border();
        AddMarkdownClass(border, MarkdownStyleKeys.Quote);
        BindTheme(border, Border.BorderBrushProperty, AccentBrushProperty);
        BindTheme(border, Border.BackgroundProperty, QuoteBackgroundBrushProperty);

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        AddMarkdownClass(stack, MarkdownStyleKeys.QuoteContent);
        foreach (var block in quote)
        {
            var child = ConvertBlock(block);
            if (child is not null)
            {
                stack.Children.Add(child);
            }
        }

        border.Child = stack;
        return border;
    }

    private Control CreateThematicBreak()
    {
        var border = new Border();
        AddMarkdownClass(border, MarkdownStyleKeys.ThematicBreak);
        BindTheme(border, Border.BackgroundProperty, BorderLineBrushProperty);
        return border;
    }

    private Control CreateTable(Table table)
    {
        var rows = table.OfType<TableRow>().ToList();
        var columnCount = rows.Select(row => row.Count).DefaultIfEmpty(0).Max();
        var grid = new Grid();
        AddMarkdownClass(grid, MarkdownStyleKeys.Table);

        for (var i = 0; i < columnCount; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                if (row[columnIndex] is not TableCell cell)
                {
                    continue;
                }

                var cellBorder = CreateTableCell(cell, row.IsHeader);
                Grid.SetRow(cellBorder, rowIndex);
                Grid.SetColumn(cellBorder, columnIndex);
                grid.Children.Add(cellBorder);
            }
        }

        return grid;
    }

    private Border CreateTableCell(TableCell cell, bool isHeader)
    {
        var border = new Border();
        AddMarkdownClass(border, isHeader ? MarkdownStyleKeys.TableHeaderCell : MarkdownStyleKeys.TableCell);
        BindTheme(border, Border.BorderBrushProperty, BorderLineBrushProperty);
        if (isHeader)
        {
            BindTheme(border, Border.BackgroundProperty, TableHeaderBackgroundBrushProperty);
        }

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        AddMarkdownClass(stack, MarkdownStyleKeys.TableCellContent);
        foreach (var block in cell)
        {
            var child = ConvertBlock(block);
            if (child is not null)
            {
                stack.Children.Add(child);
            }
        }

        border.Child = stack;
        return border;
    }

    private Control CreateFootnoteGroup(FootnoteGroup footnotes)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            Margin = new Thickness(0, 18, 0, 8)
        };
        AddMarkdownClass(panel, MarkdownStyleKeys.List);

        panel.Children.Add(CreateThematicBreak());
        foreach (var footnote in footnotes.OfType<Footnote>().OrderBy(note => note.Order))
        {
            panel.Children.Add(CreateFootnote(footnote));
        }

        return panel;
    }

    private Control CreateFootnote(Footnote footnote)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            }
        };
        AddMarkdownClass(grid, MarkdownStyleKeys.ListItem);

        var marker = CreateSelectableText(MarkdownStyleKeys.ListMarker);
        marker.Text = $"[{Math.Max(1, footnote.Order)}]";
        marker.MinWidth = OrderedListMarkerMinWidth;
        marker.TextAlignment = TextAlignment.Right;
        marker.VerticalAlignment = VerticalAlignment.Top;
        BindTheme(marker, SelectableTextBlock.ForegroundProperty, AccentBrushProperty);
        BindTheme(marker, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(marker, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
        BindTheme(marker, SelectableTextBlock.LineHeightProperty, ParagraphLineHeightProperty);
        Grid.SetColumn(marker, 0);
        grid.Children.Add(marker);

        var content = new StackPanel { Orientation = Orientation.Vertical };
        AddMarkdownClass(content, MarkdownStyleKeys.ListItemContent);
        var firstParagraph = true;
        foreach (var block in footnote)
        {
            var child = block is ParagraphBlock paragraph
                ? CreateParagraph(paragraph, false, GetListParagraphMargin(firstParagraph))
                : ConvertBlock(block);
            firstParagraph = false;

            if (child is not null)
            {
                content.Children.Add(child);
            }
        }

        Grid.SetColumn(content, 1);
        grid.Children.Add(content);
        return grid;
    }

    private Control CreateHtmlBlock(string html)
    {
        var trimmed = html.Trim();
        if (Regex.IsMatch(trimmed, @"^<a\s+[^>]*id\s*=\s*[""'][^""']+[""'][^>]*>\s*</a>$", RegexOptions.IgnoreCase))
        {
            return new Border { Height = 0, IsHitTestVisible = false };
        }

        if (TryCreateStyledSpan(trimmed, out var spanBlock))
        {
            return spanBlock;
        }

        if (TryCreateSlideBlock(trimmed, out var slideBlock))
        {
            return slideBlock;
        }

        return CreateFallbackText(html, MarkdownStyleKeys.HtmlBlock);
    }

    private bool TryCreateStyledSpan(string html, out Control control)
    {
        control = null!;
        var match = Regex.Match(
            html,
            @"^<span\s+[^>]*style\s*=\s*[""'](?<style>[^""']*)[""'][^>]*>(?<text>.*?)</span>$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            return false;
        }

        var textBlock = CreateSelectableText(MarkdownStyleKeys.HtmlBlock);
        textBlock.Text = Regex.Replace(match.Groups["text"].Value, "<.*?>", string.Empty);
        BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, TextBrushProperty);
        BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
        BindTheme(textBlock, SelectableTextBlock.LineHeightProperty, ParagraphLineHeightProperty);

        var style = match.Groups["style"].Value;
        if (style.Contains("text-align:center", StringComparison.OrdinalIgnoreCase))
        {
            textBlock.TextAlignment = TextAlignment.Center;
        }
        else if (style.Contains("text-align:right", StringComparison.OrdinalIgnoreCase))
        {
            textBlock.TextAlignment = TextAlignment.Right;
        }

        var colorMatch = Regex.Match(style, @"color\s*:\s*(?<color>#[0-9a-fA-F]{3,8}|[a-zA-Z]+)");
        if (colorMatch.Success && Color.TryParse(colorMatch.Groups["color"].Value, out var color))
        {
            textBlock.Foreground = new SolidColorBrush(color);
        }

        control = textBlock;
        return true;
    }

    private Control CreateToc()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
        AddMarkdownClass(panel, MarkdownStyleKeys.List);

        var document = Markdig.Markdown.Parse(Markdown ?? string.Empty, Pipeline);
        foreach (var heading in document.OfType<HeadingBlock>().Where(h => h.Level is >= 1 and <= 3))
        {
            var item = CreateSelectableText(MarkdownStyleKeys.ListMarker);
            item.Text = $"{new string(' ', Math.Max(0, heading.Level - 1) * 2)}{ExtractPlainText(heading.Inline)}";
            item.TextWrapping = TextWrapping.Wrap;
            BindTheme(item, SelectableTextBlock.ForegroundProperty, heading.Level <= 2 ? AccentBrushProperty : TextBrushProperty);
            BindTheme(item, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
            BindTheme(item, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
            panel.Children.Add(item);
        }

        return panel;
    }

    private static bool TryExtractMathBlock(string text, bool allowBareLatex, out string latex)
    {
        latex = string.Empty;
        var trimmed = text.Trim();
        if (trimmed.StartsWith("$$", StringComparison.Ordinal) && trimmed.EndsWith("$$", StringComparison.Ordinal) && trimmed.Length > 4)
        {
            latex = trimmed[2..^2].Trim();
            return !string.IsNullOrWhiteSpace(latex);
        }

        if (trimmed.StartsWith(@"\[", StringComparison.Ordinal) && trimmed.EndsWith(@"\]", StringComparison.Ordinal) && trimmed.Length > 4)
        {
            latex = trimmed[2..^2].Trim();
            return !string.IsNullOrWhiteSpace(latex);
        }

        if (allowBareLatex && !string.IsNullOrWhiteSpace(trimmed) && !IsTypeNameFallback(trimmed, typeof(Block)))
        {
            latex = trimmed;
            return true;
        }

        return false;
    }

    private Control CreateMathBlock(string latex)
    {
        var border = new Border
        {
            Child = CreateFallbackText($"$$\n{latex}\n$$", MarkdownStyleKeys.HtmlBlock),
            Padding = new Thickness(0, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        AddMarkdownClass(border, MarkdownStyleKeys.HtmlBlock);
        return border;
    }

    private static string NormalizeLatex(string latex)
    {
        if (string.IsNullOrWhiteSpace(latex) || !latex.Contains(@"\ce{", StringComparison.Ordinal))
        {
            return latex;
        }

        var builder = new StringBuilder(latex.Length);
        var index = 0;
        while (index < latex.Length)
        {
            var ceStart = latex.IndexOf(@"\ce{", index, StringComparison.Ordinal);
            if (ceStart < 0)
            {
                builder.Append(latex[index..]);
                break;
            }

            builder.Append(latex[index..ceStart]);
            var contentStart = ceStart + 4;
            var contentEnd = FindMatchingBrace(latex, contentStart - 1);
            if (contentEnd < 0)
            {
                builder.Append(latex[ceStart..]);
                break;
            }

            builder.Append(ConvertChemExpression(latex[contentStart..contentEnd]));
            index = contentEnd + 1;
        }

        return builder.ToString();
    }

    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string ConvertChemExpression(string expression)
    {
        var tokens = Regex.Split(expression.Trim(), @"\s+").Where(token => token.Length > 0);
        return string.Join(" ", tokens.Select(ConvertChemToken));
    }

    private static string ConvertChemToken(string token)
    {
        var arrowMatch = Regex.Match(token, @"^(?<arrow><->|->|<-)(\[(?<label>[^\]]+)\])?$");
        if (arrowMatch.Success)
        {
            var arrow = arrowMatch.Groups["arrow"].Value switch
            {
                "<-" => @"\leftarrow",
                "<->" => @"\leftrightarrow",
                _ => @"\longrightarrow"
            };
            return arrowMatch.Groups["label"].Success
                ? $@"{arrow}^{{{ConvertChemFormula(arrowMatch.Groups["label"].Value)}}}"
                : arrow;
        }

        return ConvertChemFormula(token);
    }

    private static string ConvertChemFormula(string formula)
    {
        var builder = new StringBuilder(formula.Length * 2);
        for (var i = 0; i < formula.Length; i++)
        {
            var c = formula[i];
            if (char.IsUpper(c))
            {
                var start = i;
                i++;
                while (i < formula.Length && char.IsLower(formula[i]))
                {
                    i++;
                }
                builder.Append(@"\mathrm{").Append(formula[start..i]).Append('}');
                i--;
            }
            else if (char.IsDigit(c))
            {
                var start = i;
                while (i + 1 < formula.Length && char.IsDigit(formula[i + 1]))
                {
                    i++;
                }
                builder.Append("_{").Append(formula[start..(i + 1)]).Append('}');
            }
            else if (c == '^')
            {
                var value = ReadScriptValue(formula, ref i);
                builder.Append("^{").Append(ConvertScriptText(value)).Append('}');
            }
            else if ((c == '+' || c == '-') && i == formula.Length - 1)
            {
                builder.Append("^{").Append(c).Append('}');
            }
            else if (char.IsLetter(c))
            {
                builder.Append(@"\mathrm{").Append(c).Append('}');
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ReadScriptValue(string text, ref int index)
    {
        if (index + 1 >= text.Length)
        {
            return string.Empty;
        }

        if (text[index + 1] == '{')
        {
            var end = FindMatchingBrace(text, index + 1);
            if (end > index + 1)
            {
                var value = text[(index + 2)..end];
                index = end;
                return value;
            }
        }

        var start = index + 1;
        var endIndex = start;
        while (endIndex < text.Length && (char.IsLetterOrDigit(text[endIndex]) || text[endIndex] is '+' or '-'))
        {
            endIndex++;
        }

        index = Math.Max(start, endIndex) - 1;
        return text[start..endIndex];
    }

    private static string ConvertScriptText(string text)
    {
        return text.All(c => char.IsLetter(c))
            ? $@"\mathrm{{{text}}}"
            : text;
    }

    private bool TryCreateSlideBlock(string text, out Control control)
    {
        control = null!;
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("<", StringComparison.Ordinal) || !trimmed.EndsWith(">", StringComparison.Ordinal))
        {
            return false;
        }

        var content = trimmed[1..^1];
        var matches = Regex.Matches(content, @"!\[(?<alt>[^\]]*)\]\((?<url>[^)]+)\)");
        if (matches.Count < 2)
        {
            return false;
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
        control = scrollViewer;
        return true;
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

    private SelectableTextBlock CreateFallbackText(string text, string className)
    {
        var textBlock = CreateSelectableText(className);
        textBlock.Text = text;
        BindTheme(textBlock, SelectableTextBlock.ForegroundProperty, MutedTextBrushProperty);
        BindTheme(textBlock, SelectableTextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(textBlock, SelectableTextBlock.FontSizeProperty, ParagraphFontSizeProperty);
        BindTheme(textBlock, SelectableTextBlock.LineHeightProperty, ParagraphLineHeightProperty);
        return textBlock;
    }

    private Control? CreateUnknownBlock(Block block)
    {
        var text = block.ToString() ?? string.Empty;
        return IsTypeNameFallback(text, block.GetType())
            ? null
            : CreateFallbackText(text, MarkdownStyleKeys.UnknownBlock);
    }

    private IEnumerable<Inline> ConvertInlines(ContainerInline? container, bool stripTaskPrefix = false)
    {
        var child = container?.FirstChild;
        var shouldStripTaskPrefix = stripTaskPrefix;
        while (child is not null)
        {
            foreach (var inline in ConvertInline(child, ref shouldStripTaskPrefix))
            {
                yield return inline;
            }

            child = child.NextSibling;
        }
    }

    private IReadOnlyList<Inline> ConvertInline(Markdig.Syntax.Inlines.Inline inline, ref bool stripTaskPrefix)
    {
        var result = new List<Inline>();
        if (TryGetMathInlineLatex(inline, out var mathLatex))
        {
            stripTaskPrefix = false;
            result.Add(CreateMathInline(mathLatex));
            return result;
        }

        switch (inline)
        {
            case LiteralInline literal:
                var literalText = literal.Content.ToString();
                if (stripTaskPrefix && TryStripTaskPrefix(literalText, out var stripped))
                {
                    stripTaskPrefix = false;
                    if (!string.IsNullOrWhiteSpace(stripped))
                    {
                        result.Add(new Run(stripped.TrimStart()));
                    }

                    return result;
                }

                stripTaskPrefix = false;
                result.AddRange(CreateLiteralInlines(literalText));
                return result;

            case LineBreakInline:
                stripTaskPrefix = false;
                result.Add(new LineBreak());
                return result;

            case CodeInline code:
                stripTaskPrefix = false;
                result.Add(CreateInlineCode(code.Content));
                return result;

            case TaskList:
                stripTaskPrefix = false;
                return result;

            case FootnoteLink footnoteLink:
                stripTaskPrefix = false;
                result.Add(CreateFootnoteLink(footnoteLink));
                return result;

            case EmphasisInline emphasis:
                stripTaskPrefix = false;
                result.Add(CreateEmphasis(emphasis));
                return result;

            case LinkInline { IsImage: true } image:
                stripTaskPrefix = false;
                result.Add(CreateImage(image));
                return result;

            case LinkInline link:
                stripTaskPrefix = false;
                result.Add(CreateLink(link));
                return result;

            case HtmlInline html:
                stripTaskPrefix = false;
                result.AddRange(CreateHtmlInline(html.Tag));
                return result;

            case ContainerInline container:
                stripTaskPrefix = false;
                result.Add(CreateContainerSpan(container));
                return result;

            default:
                stripTaskPrefix = false;
                var text = inline.ToString() ?? string.Empty;
                if (!IsTypeNameFallback(text, inline.GetType()))
                {
                    result.Add(new Run(text));
                }

                return result;
        }
    }

    private static bool TryGetMathInlineLatex(Markdig.Syntax.Inlines.Inline inline, out string latex)
    {
        latex = string.Empty;
        if (!inline.GetType().Name.Contains("Math", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var text = inline.ToString()?.Trim() ?? string.Empty;
        if (IsTypeNameFallback(text, inline.GetType()))
        {
            return false;
        }

        text = TrimInlineMathDelimiters(text);
        if (string.IsNullOrWhiteSpace(text) || IsTypeNameFallback(text, inline.GetType()))
        {
            return false;
        }

        latex = text;
        return true;
    }

    private static string TrimInlineMathDelimiters(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("$$", StringComparison.Ordinal) && trimmed.EndsWith("$$", StringComparison.Ordinal) && trimmed.Length > 4)
        {
            return trimmed[2..^2].Trim();
        }

        if (trimmed.StartsWith('$') && trimmed.EndsWith('$') && trimmed.Length > 2)
        {
            return trimmed[1..^1].Trim();
        }

        if (trimmed.StartsWith(@"\(", StringComparison.Ordinal) && trimmed.EndsWith(@"\)", StringComparison.Ordinal) && trimmed.Length > 4)
        {
            return trimmed[2..^2].Trim();
        }

        return trimmed;
    }

    private IEnumerable<Inline> CreateLiteralInlines(string text)
    {
        var index = 0;
        while (index < text.Length)
        {
            if (TryReadInlineMath(text, index, out var latex, out var mathEnd))
            {
                yield return CreateMathInline(latex);
                index = mathEnd;
                continue;
            }

            if (TryReadRuby(text, index, out var rubyText, out var rubyAnnotation, out var rubyEnd))
            {
                yield return CreateRubyInline(rubyText, rubyAnnotation);
                index = rubyEnd;
                continue;
            }

            var next = FindNextSpecialInline(text, index + 1);
            yield return new Run(text[index..next]);
            index = next;
        }
    }

    private IEnumerable<Inline> CreateHtmlInline(string html)
    {
        if (Regex.IsMatch(html, @"^</?(a|span)\b", RegexOptions.IgnoreCase))
        {
            yield break;
        }

        yield return new Run(html);
    }

    private static int FindNextSpecialInline(string text, int start)
    {
        var dollar = text.IndexOf('$', start);
        var ruby = text.IndexOf('{', start);
        return (dollar, ruby) switch
        {
            (-1, -1) => text.Length,
            (-1, _) => ruby,
            (_, -1) => dollar,
            _ => Math.Min(dollar, ruby)
        };
    }

    private static bool TryReadInlineMath(string text, int start, out string latex, out int end)
    {
        latex = string.Empty;
        end = start;
        if (text[start] != '$' || start + 1 >= text.Length || text[start + 1] == '$')
        {
            return false;
        }

        var close = text.IndexOf('$', start + 1);
        if (close <= start + 1)
        {
            return false;
        }

        latex = text[(start + 1)..close].Trim();
        end = close + 1;
        return !string.IsNullOrWhiteSpace(latex);
    }

    private static bool TryReadRuby(string text, int start, out string rubyText, out string annotation, out int end)
    {
        rubyText = string.Empty;
        annotation = string.Empty;
        end = start;
        if (text[start] != '{')
        {
            return false;
        }

        var separator = text.IndexOf('|', start + 1);
        var close = text.IndexOf('}', start + 1);
        if (separator < 0 || close < 0 || separator > close)
        {
            return false;
        }

        rubyText = text[(start + 1)..separator];
        annotation = text[(separator + 1)..close];
        end = close + 1;
        return !string.IsNullOrWhiteSpace(rubyText) && !string.IsNullOrWhiteSpace(annotation);
    }

    private Inline CreateMathInline(string latex)
    {
        return new Run($"${latex}$");
    }

    private Inline CreateRubyInline(string text, string annotation)
    {
        var annotations = annotation.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (annotations.Length == text.Length)
        {
            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (var i = 0; i < text.Length; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                var top = CreateRubyTextBlock(annotations[i], Math.Max(8, ParagraphFontSize * 0.46), Math.Max(8, ParagraphLineHeight * 0.34));
                var bottom = CreateRubyTextBlock(text[i].ToString(), ParagraphFontSize, Math.Max(12, ParagraphLineHeight * 0.66));
                BindTheme(top, TextBlock.ForegroundProperty, MutedTextBrushProperty);
                BindTheme(bottom, TextBlock.ForegroundProperty, TextBrushProperty);
                BindTheme(top, TextBlock.FontFamilyProperty, ContentFontFamilyProperty);
                BindTheme(bottom, TextBlock.FontFamilyProperty, ContentFontFamilyProperty);

                Grid.SetRow(top, 0);
                Grid.SetColumn(top, i);
                Grid.SetRow(bottom, 1);
                Grid.SetColumn(bottom, i);
                grid.Children.Add(top);
                grid.Children.Add(bottom);
            }

            return CreateInlineContainer(grid);
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var topFallback = CreateRubyTextBlock(annotation, Math.Max(9, ParagraphFontSize * 0.62), Math.Max(10, ParagraphLineHeight * 0.45));
        var bottomFallback = CreateRubyTextBlock(text, ParagraphFontSize, ParagraphLineHeight * 0.72);
        BindTheme(topFallback, TextBlock.ForegroundProperty, MutedTextBrushProperty);
        BindTheme(bottomFallback, TextBlock.ForegroundProperty, TextBrushProperty);
        BindTheme(topFallback, TextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(bottomFallback, TextBlock.FontFamilyProperty, ContentFontFamilyProperty);

        panel.Children.Add(topFallback);
        panel.Children.Add(bottomFallback);
        return CreateInlineContainer(panel);
    }

    private static TextBlock CreateRubyTextBlock(string text, double fontSize, double lineHeight)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            LineHeight = lineHeight,
            Margin = new Thickness(1, 0)
        };
    }

    private Span CreateContainerSpan(ContainerInline container)
    {
        var span = new Span();
        foreach (var inline in ConvertInlines(container))
        {
            span.Inlines.Add(inline);
        }

        return span;
    }

    private Span CreateEmphasis(EmphasisInline emphasis)
    {
        var span = new Span();
        foreach (var inline in ConvertInlines(emphasis))
        {
            span.Inlines.Add(inline);
        }

        FontWeight? fontWeight = null;
        FontStyle? fontStyle = null;
        TextDecorationCollection? textDecorations = null;
        if (emphasis.DelimiterCount >= 2 && emphasis.DelimiterChar is '*' or '_')
        {
            span.FontWeight = FontWeight.Bold;
            fontWeight = FontWeight.Bold;
        }
        else if (emphasis.DelimiterChar is '*' or '_')
        {
            span.FontStyle = FontStyle.Italic;
            fontStyle = FontStyle.Italic;
        }

        if (emphasis.DelimiterChar == '~')
        {
            span.TextDecorations = TextDecorations.Strikethrough;
            textDecorations = TextDecorations.Strikethrough;
        }

        ApplyInlineTextStyle(span, fontWeight, fontStyle, textDecorations);
        return span;
    }

    private static void ApplyInlineTextStyle(
        Inline inline,
        FontWeight? fontWeight,
        FontStyle? fontStyle,
        TextDecorationCollection? textDecorations)
    {
        if (fontWeight.HasValue)
        {
            inline.FontWeight = fontWeight.Value;
        }

        if (fontStyle.HasValue)
        {
            inline.FontStyle = fontStyle.Value;
        }

        if (textDecorations is not null)
        {
            inline.TextDecorations = textDecorations;
        }

        if (inline is Span span)
        {
            foreach (var child in span.Inlines)
            {
                ApplyInlineTextStyle(child, fontWeight, fontStyle, textDecorations);
            }
        }
    }

    private Inline CreateInlineCode(string code)
    {
        var run = new Run(code)
        {
            FontFamily = CodeFontFamily,
            BaselineAlignment = BaselineAlignment.Center
        };
        BindTheme(run, TextElement.ForegroundProperty, AccentBrushProperty);
        BindTheme(run, TextElement.BackgroundProperty, InlineCodeBackgroundBrushProperty);
        BindTheme(run, TextElement.FontFamilyProperty, CodeFontFamilyProperty);
        BindTheme(run, TextElement.FontSizeProperty, ParagraphFontSizeProperty);
        return run;
    }

    private Inline CreateImage(LinkInline imageInline)
    {
        var image = new MarkdownImage
        {
            Source = imageInline.Url,
            AltText = ExtractPlainText(imageInline)
        };
        AddMarkdownClass(image, MarkdownStyleKeys.Image);
        return CreateInlineContainer(image);
    }

    private Inline CreateFootnoteLink(FootnoteLink footnoteLink)
    {
        var text = footnoteLink.IsBackLink
            ? "^"
            : $"[{Math.Max(1, footnoteLink.Footnote?.Order ?? footnoteLink.Index)}]";
        var run = new Run(text)
        {
            BaselineAlignment = BaselineAlignment.Superscript,
            FontSize = Math.Max(9, ParagraphFontSize * 0.72)
        };
        BindTheme(run, TextElement.ForegroundProperty, AccentBrushProperty);
        BindTheme(run, TextElement.FontFamilyProperty, ContentFontFamilyProperty);
        return run;
    }

    private Inline CreateLink(LinkInline linkInline)
    {
        var text = ExtractPlainText(linkInline);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = linkInline.Url ?? string.Empty;
        }

        var linkUrl = linkInline.Url;

        var textBlock = new TextBlock
        {
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
            Inlines = new InlineCollection(),
            TextWrapping = TextWrapping.NoWrap,
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        AddMarkdownClass(textBlock, MarkdownStyleKeys.Link);
        BindTheme(textBlock, TextBlock.ForegroundProperty, AccentBrushProperty);
        BindTheme(textBlock, TextBlock.FontFamilyProperty, ContentFontFamilyProperty);
        BindTheme(textBlock, TextBlock.FontSizeProperty, ParagraphFontSizeProperty);

        foreach (var inline in ConvertInlines(linkInline))
        {
            textBlock.Inlines.Add(inline);
        }

        if (textBlock.Inlines.Count == 0)
        {
            textBlock.Inlines.Add(new Run(text));
        }

        textBlock.PointerPressed += (_, e) =>
        {
            e.Handled = true;
        };

        textBlock.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left && !string.IsNullOrWhiteSpace(linkUrl))
            {
                UrlHelper.Open(linkUrl);
                e.Handled = true;
            }
        };

        return new InlineUIContainer(textBlock);
    }

    private static InlineUIContainer CreateInlineContainer(Control control)
    {
        return new InlineUIContainer(control);
    }

    private static string ExtractPlainText(ContainerInline? container)
    {
        if (container is null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        var child = container.FirstChild;
        while (child is not null)
        {
            parts.Add(child switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => code.Content,
                LineBreakInline => Environment.NewLine,
                TaskList => string.Empty,
                ContainerInline nested => ExtractPlainText(nested),
                _ => IsTypeNameFallback(child.ToString() ?? string.Empty, child.GetType())
                    ? string.Empty
                    : child.ToString() ?? string.Empty
            });
            child = child.NextSibling;
        }

        return string.Concat(parts);
    }

    private SelectableTextBlock CreateSelectableText(params string[] classes)
    {
        var textBlock = new SelectableTextBlock
        {
            Inlines = new InlineCollection(),
            TextWrapping = TextWrapping.Wrap
        };
        TextOptions.SetBaselinePixelAlignment(textBlock, BaselinePixelAlignment.Aligned);
        AddMarkdownClass(textBlock, classes);
        textBlock.ContextMenu = CreateSelectableTextContextMenu(textBlock);
        return textBlock;
    }

    private ContextMenu CreateViewerContextMenu()
    {
        if (_viewerCopyMenuItem is null)
        {
            _viewerCopyMenuItem = new MenuItem();
            _viewerCopyMenuItem.Click += async (_, _) => await CopyRenderedTextAsync();
        }
        _viewerCopyMenuItem.Header = "复制渲染文本";
        return new ContextMenu { ItemsSource = new[] { _viewerCopyMenuItem } };
    }

    private ContextMenu CreateSelectableTextContextMenu(SelectableTextBlock textBlock)
    {
        var copySelectionItem = new MenuItem
        {
            Header = "复制选中文本"
        };
        copySelectionItem.Click += (_, _) => textBlock.Copy();

        var copyRenderedTextItem = new MenuItem
        {
            Header = "复制渲染文本"
        };
        copyRenderedTextItem.Click += async (_, _) => await CopyRenderedTextAsync();

        return new ContextMenu { ItemsSource = new[] { copySelectionItem, copyRenderedTextItem } };
    }

    private void AddMarkdownClass(Control control, params string[] classes)
    {
        foreach (var className in classes.Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            control.Classes.Add(className);
        }
    }

    private static bool TryReadTaskState(ListItemBlock item, out bool isChecked)
    {
        isChecked = false;
        if (item.FirstOrDefault() is not ParagraphBlock paragraph)
        {
            return false;
        }

        if (paragraph.Inline?.FirstChild is TaskList taskList)
        {
            isChecked = taskList.Checked;
            return true;
        }

        if (paragraph.Inline?.FirstChild is not LiteralInline literal)
        {
            return false;
        }

        var text = literal.Content.ToString();
        if (text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
        {
            isChecked = true;
            return true;
        }

        return text.StartsWith("[ ]", StringComparison.Ordinal);
    }

    private static bool TryStripTaskPrefix(string text, out string stripped)
    {
        stripped = text;
        if (text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("[ ]", StringComparison.Ordinal))
        {
            stripped = text[3..];
            return true;
        }

        return false;
    }

    private static bool TryGetSingleTextLink(ContainerInline? container, out string? url)
    {
        url = null;
        if (container?.FirstChild is LinkInline { IsImage: false } link
            && link.NextSibling is null
            && !string.IsNullOrWhiteSpace(link.Url))
        {
            url = link.Url;
            return true;
        }

        return false;
    }

    private static StyledProperty<double> GetHeadingFontSizeProperty(int level)
    {
        return level switch
        {
            1 => Heading1FontSizeProperty,
            2 => Heading2FontSizeProperty,
            3 => Heading3FontSizeProperty,
            4 => Heading4FontSizeProperty,
            5 => Heading5FontSizeProperty,
            _ => Heading6FontSizeProperty
        };
    }

    private IDisposable BindTheme<T>(
        AvaloniaObject target,
        AvaloniaProperty<T> targetProperty,
        StyledProperty<T> sourceProperty)
    {
        var disposable = target.Bind(targetProperty, this.GetObservable(sourceProperty));
        _currentBlockDisposables.Add(disposable);
        return disposable;
    }

    private static void AppendPlainTextBlock(StringBuilder builder, Block block, int indent)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AppendIndentedLine(builder, indent, ExtractPlainText(heading.Inline));
                builder.AppendLine();
                break;
            case ParagraphBlock paragraph:
                AppendIndentedLine(builder, indent, ExtractPlainText(paragraph.Inline));
                builder.AppendLine();
                break;
            case LinkReferenceDefinitionGroup:
            case LinkReferenceDefinition:
                break;
            case CodeBlock codeBlock:
                AppendIndentedLines(builder, indent, codeBlock.Lines.ToString().TrimEnd());
                builder.AppendLine();
                break;
            case ListBlock list:
                AppendPlainTextList(builder, list, indent);
                builder.AppendLine();
                break;
            case QuoteBlock quote:
                foreach (var child in quote)
                {
                    AppendPlainTextBlock(builder, child, indent);
                }

                break;
            case ThematicBreakBlock:
                AppendIndentedLine(builder, indent, "---");
                builder.AppendLine();
                break;
            case Table table:
                AppendPlainTextTable(builder, table, indent);
                builder.AppendLine();
                break;
            case HtmlBlock htmlBlock:
                AppendIndentedLines(builder, indent, htmlBlock.Lines.ToString().TrimEnd());
                builder.AppendLine();
                break;
            default:
                var text = block.ToString() ?? string.Empty;
                if (!IsTypeNameFallback(text, block.GetType()))
                {
                    AppendIndentedLine(builder, indent, text);
                    builder.AppendLine();
                }

                break;
        }
    }

    private static bool IsTypeNameFallback(string text, Type type)
    {
        return string.Equals(text, type.FullName, StringComparison.Ordinal)
               || string.Equals(text, type.Name, StringComparison.Ordinal);
    }

    private static void AppendPlainTextList(StringBuilder builder, ListBlock list, int indent)
    {
        var index = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var marker = list.IsOrdered ? $"{index++}. " : "- ";
            AppendPlainTextListItem(builder, item, marker, indent);
        }
    }

    private static void AppendPlainTextListItem(StringBuilder builder, ListItemBlock item, string marker, int indent)
    {
        var isFirstBlock = true;
        foreach (var block in item)
        {
            if (isFirstBlock && block is ParagraphBlock paragraph)
            {
                var text = ExtractPlainText(paragraph.Inline);
                if (TryStripTaskPrefix(text, out var stripped))
                {
                    text = stripped.TrimStart();
                }

                AppendIndent(builder, indent);
                builder.Append(marker);
                builder.AppendLine(text);
            }
            else
            {
                AppendPlainTextBlock(builder, block, indent + 2);
            }

            isFirstBlock = false;
        }
    }

    private static void AppendPlainTextTable(StringBuilder builder, Table table, int indent)
    {
        foreach (var row in table.OfType<TableRow>())
        {
            var cells = row.OfType<TableCell>()
                .Select(cell => ExtractBlocksPlainText(cell).ReplaceLineEndings(" "))
                .ToArray();
            AppendIndentedLine(builder, indent, string.Join('\t', cells));
        }
    }

    private static string ExtractBlocksPlainText(IEnumerable<Block> blocks)
    {
        var builder = new StringBuilder();
        foreach (var block in blocks)
        {
            AppendPlainTextBlock(builder, block, 0);
        }

        return builder.ToString().Trim();
    }

    private static void AppendIndentedLines(StringBuilder builder, int indent, string text)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            AppendIndentedLine(builder, indent, line);
        }
    }

    private static void AppendIndentedLine(StringBuilder builder, int indent, string text)
    {
        AppendIndent(builder, indent);
        builder.AppendLine(text);
    }

    private static void AppendIndent(StringBuilder builder, int indent)
    {
        if (indent > 0)
        {
            builder.Append(' ', indent);
        }
    }

    private sealed record RenderedBlock(int Start, int End, Control Control, List<IDisposable>? Bindings)
    {
        public void Cleanup()
        {
            if (Bindings is not null)
            {
                foreach (var d in Bindings)
                {
                    d.Dispose();
                }
                Bindings.Clear();
            }
        }

        public RenderedBlock Shift(int delta)
        {
            return this with
            {
                Start = Start + delta,
                End = End + delta
            };
        }
    }

    private readonly record struct TextRange(int Start, int End);

    private readonly record struct TextChange(int OldStart, int OldEnd, int NewStart, int NewEnd, int Delta);
}
