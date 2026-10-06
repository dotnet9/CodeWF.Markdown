using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeWF.Markdown.Helpers;
using CodeWF.Markdown.Rendering;
using CodeWF.Markdown.Shared.Rendering;
using Lang.Avalonia;
using Markdig;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;
using System.Text.RegularExpressions;
using Inline = Avalonia.Controls.Documents.Inline;

namespace CodeWF.Markdown.Controls;

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
public class MarkdownViewer : TemplatedControl, Rendering.IMarkdownRenderContext
{
    private const string DocumentHostPartName = "PART_DocumentHost";
    private const string DefaultTypographyTheme = "Basic";
    private const string DefaultTypographySize = "Normal";

    internal static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly List<RenderedBlock> _renderedBlocks = [];
    private readonly List<IDisposable> _currentBlockDisposables = [];
    private readonly MarkdownSelectionController _selectionController = new();
    private Panel? _documentHost;
    private string _renderedMarkdown = string.Empty;
    private MarkdownDocumentModel _renderedModel = MarkdownDocumentModel.Empty;
    private MarkdownRenderMode _queuedRenderMode = MarkdownRenderMode.Incremental;
    private bool _isRenderQueued;
    private MarkdownPointerSelectionState? _pointerSelectionState;
    private SelectableTextBlock? _nativeSelectionTextBlock;
    private bool _isPointerSelecting;
    private string _selectedText = string.Empty;
    private bool _hasSelection;

    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(Markdown));

    public static readonly StyledProperty<string?> TypographyThemeProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(TypographyTheme));

    public static readonly StyledProperty<string?> TypographySizeProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(TypographySize));

    public static readonly StyledProperty<string?> ImageBasePathProperty =
        AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(ImageBasePath));

    public static readonly DirectProperty<MarkdownViewer, string> SelectedTextProperty =
        AvaloniaProperty.RegisterDirect<MarkdownViewer, string>(
            nameof(SelectedText),
            viewer => viewer.SelectedText);

    public static readonly DirectProperty<MarkdownViewer, bool> HasSelectionProperty =
        AvaloniaProperty.RegisterDirect<MarkdownViewer, bool>(
            nameof(HasSelection),
            viewer => viewer.HasSelection);

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

    private static readonly FontFamily DefaultContentFontFamily =
        new("Inter, Microsoft YaHei UI, Microsoft YaHei, Segoe UI, PingFang SC, Hiragino Sans GB, Noto Sans CJK SC, Noto Sans SC, sans-serif");

    public static readonly StyledProperty<FontFamily> ContentFontFamilyProperty =
        AvaloniaProperty.Register<MarkdownViewer, FontFamily>(nameof(ContentFontFamily), DefaultContentFontFamily);

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

    /// <summary>
    /// 单个 MarkdownViewer 的排版主题 Key；未设置或设置为空时使用全局 MarkdownThemes 资源，默认行为为 Basic。
    /// </summary>
    public string? TypographyTheme
    {
        get => GetValue(TypographyThemeProperty) ?? DefaultTypographyTheme;
        set => SetValue(TypographyThemeProperty, value);
    }

    /// <summary>
    /// 单个 MarkdownViewer 的排版尺寸 Key；未设置或设置为空时使用全局 MarkdownThemes 资源，默认行为为 Normal。
    /// </summary>
    public string? TypographySize
    {
        get => GetValue(TypographySizeProperty) ?? DefaultTypographySize;
        set => SetValue(TypographySizeProperty, value);
    }

    /// <summary>
    /// Base file or directory path used to resolve relative Markdown image URLs.
    /// </summary>
    public string? ImageBasePath
    {
        get => GetValue(ImageBasePathProperty);
        set => SetValue(ImageBasePathProperty, value);
    }

    public string SelectedText
    {
        get => _selectedText;
        private set => SetAndRaise(SelectedTextProperty, ref _selectedText, value);
    }

    public bool HasSelection
    {
        get => _hasSelection;
        private set => SetAndRaise(HasSelectionProperty, ref _hasSelection, value);
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

    public event EventHandler? SelectionChanged;

    /// <summary>
    /// 在代码块工具栏创建完成后触发，调用方可追加自定义按钮。
    /// </summary>
    public event EventHandler<CodeBlockToolRenderEventArgs>? CodeBlockToolRender;

    static MarkdownViewer()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument(MarkdownRenderMode.Incremental));
        TypographyThemeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument(MarkdownRenderMode.Full));
        TypographySizeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument(MarkdownRenderMode.Full));
        ImageBasePathProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument(MarkdownRenderMode.Full));
    }

    private MenuItem? _viewerCopyMenuItem;

    private readonly MarkdownBlockRendererPipeline _blockPipeline = CreateDefaultPipeline();

    private static readonly List<Rendering.IMarkdownBlockRenderer> s_externalRenderers = [];
    private static readonly object s_externalRenderersLock = new();

    /// <summary>
    /// 注册外部块级渲染器（能力包扩展点）：注册后创建的所有 Viewer 实例都会
    /// 在内置渲染器之后尝试该渲染器。典型用法见 CodeWF.Markdown.Mermaid 包。
    /// </summary>
    public static void RegisterBlockRenderer(Rendering.IMarkdownBlockRenderer renderer)
    {
        lock (s_externalRenderersLock)
        {
            s_externalRenderers.Add(renderer);
        }
    }

    private MarkdownInlineRenderer? _inlineRenderer;

    private MarkdownInlineRenderer InlineRenderer => _inlineRenderer ??= new(this);

    private static MarkdownBlockRendererPipeline CreateDefaultPipeline()
    {
        var pipeline = new MarkdownBlockRendererPipeline();
        pipeline.Register(new SpecialBlockRenderer());
        pipeline.Register(new ParagraphRenderer());
        pipeline.Register(new MathBlockRenderer());
        pipeline.Register(new CodeBlockRenderer());
        pipeline.Register(new ListRenderer());
        pipeline.Register(new QuoteRenderer());
        pipeline.Register(new TableRenderer());
        pipeline.Register(new ThematicBreakRenderer());
        pipeline.Register(new HeadingRenderer());
        pipeline.Register(new FootnoteRenderer());
        pipeline.Register(new HtmlBlockRenderer());
        lock (s_externalRenderersLock)
        {
            foreach (var renderer in s_externalRenderers)
            {
                pipeline.Register(renderer);
            }
        }

        return pipeline;
    }

    public MarkdownViewer()
    {
        Focusable = true;
        ContextMenu = CreateViewerContextMenu();
        TextOptions.SetBaselinePixelAlignment(this, BaselinePixelAlignment.Aligned);
        AddHandler(PointerPressedEvent, OnViewerPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnViewerPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnViewerPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnViewerKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// 复制当前渲染结果的纯文本，作为跨多个渲染块选择时的兜底复制方式。
    /// </summary>
    public async Task CopyRenderedTextAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(HasSelection ? SelectedText : GetRenderedText());
        }
    }

    public async Task CopySelectionAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard && HasSelection)
        {
            await clipboard.SetTextAsync(SelectedText);
        }
    }

    public string GetRenderedText()
    {
        return MarkdownParser.Parse(Markdown, Pipeline).PlainText;
    }

    public void Rerender()
    {
        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    public void RenderIncremental()
    {
        QueueRenderDocument(MarkdownRenderMode.Incremental);
    }

    /// <summary>
    /// Try to locate the rendered block that contains the specified 1-based Markdown source line.
    /// </summary>
    public bool TryGetSourceLineBounds(int sourceLine, out Rect bounds)
    {
        var text = Markdown ?? string.Empty;
        var offset = GetSourceLineOffset(text, sourceLine);
        return TryGetSourceOffsetBounds(offset, out bounds);
    }

    /// <summary>
    /// Try to locate the rendered block that contains the specified Markdown source offset.
    /// The returned bounds are relative to this <see cref="MarkdownViewer"/>.
    /// </summary>
    public bool TryGetSourceOffsetBounds(int sourceOffset, out Rect bounds)
    {
        bounds = default;
        if (_documentHost is null)
        {
            return false;
        }

        var text = Markdown ?? string.Empty;
        if (!string.Equals(_renderedMarkdown, text, StringComparison.Ordinal))
        {
            RenderDocument(MarkdownRenderMode.Incremental);
        }

        if (_renderedBlocks.Count == 0)
        {
            return false;
        }

        var offset = Math.Clamp(sourceOffset, 0, text.Length);
        var renderedBlock = FindRenderedBlockBySourceOffset(offset);
        if (renderedBlock is null || renderedBlock.Control.TranslatePoint(new Point(0, 0), this) is not { } topLeft)
        {
            return false;
        }

        bounds = new Rect(topLeft, renderedBlock.Control.Bounds.Size);
        return true;
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

        I18nManager.Instance.CultureChanged += OnCultureChanged;
        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        }

        I18nManager.Instance.CultureChanged -= OnCultureChanged;
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

    private async void OnViewerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C
            && (e.KeyModifiers & KeyModifiers.Control) != 0
            && HasSelection)
        {
            await CopySelectionAsync();
            e.Handled = true;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        // 代码高亮颜色由 TextMate 直接生成，明暗主题变化时需要重建。
        QueueRenderDocument(MarkdownRenderMode.Full);
    }

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        ContextMenu = CreateViewerContextMenu();
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
        DisposePendingBlockDisposables(0);
        _documentHost.Children.Clear();
        _renderedBlocks.Clear();
        _renderedMarkdown = text;
        _renderedModel = MarkdownParser.Parse(text, Pipeline);
        ResetSelectionState();

        if (string.IsNullOrWhiteSpace(text))
        {
            InvalidateDocumentLayout();
            return;
        }

        foreach (var renderedBlock in CreateRenderedBlocks(_renderedModel.Blocks, text))
        {
            _documentHost.Children.Add(renderedBlock.Control);
            _renderedBlocks.Add(renderedBlock);
        }

        RefreshSelectionBlocks();
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

        var newModel = MarkdownParser.Parse(text, Pipeline);
        var diff = MarkdownDiffService.Compare(_renderedModel, newModel);
        if (diff.RequiresFullRender)
        {
            return false;
        }

        if (diff.OldRemoveCount == 0 && diff.NewInsertCount == 0)
        {
            _renderedMarkdown = text;
            _renderedModel = newModel;
            return true;
        }

        var renderedNewBlocks = CreateRenderedBlocks(
            newModel.Blocks.Skip(diff.NewStartIndex).Take(diff.NewInsertCount),
            text);
        ReplaceRenderedBlocks(diff.ReplaceStartIndex, diff.ReplaceEndIndex, renderedNewBlocks, newModel);

        _renderedMarkdown = text;
        _renderedModel = newModel;
        return true;
    }

    private IReadOnlyList<RenderedBlock> CreateRenderedBlocks(IEnumerable<MarkdownDocumentBlock> modelBlocks, string markdown)
    {
        var renderedBlocks = new List<RenderedBlock>();
        foreach (var modelBlock in modelBlocks)
        {
            var previousDisposableCount = _currentBlockDisposables.Count;
            var control = ConvertBlock(modelBlock.SyntaxBlock, markdown);
            if (control is null)
            {
                DisposePendingBlockDisposables(previousDisposableCount);
                continue;
            }

            var blockDisposables = ExtractCurrentBlockDisposables(previousDisposableCount);
            renderedBlocks.Add(RenderedBlock.FromModel(modelBlock, control, blockDisposables));
        }

        return renderedBlocks;
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
                DisposePendingBlockDisposables(previousDisposableCount);
                continue;
            }

            var range = GetBlockRange(block, sourceOffset, markdown.Length);
            var blockDisposables = ExtractCurrentBlockDisposables(previousDisposableCount);
            renderedBlocks.Add(new RenderedBlock(
                range.Start,
                range.End,
                0,
                0,
                string.Empty,
                MarkdownBlockKind.Unknown,
                MarkdownDependencyFlags.None,
                0,
                control,
                blockDisposables));
        }

        return renderedBlocks;
    }

    private List<IDisposable>? ExtractCurrentBlockDisposables(int startIndex)
    {
        var count = _currentBlockDisposables.Count - startIndex;
        if (count <= 0)
        {
            return null;
        }

        var blockDisposables = _currentBlockDisposables.GetRange(startIndex, count);
        _currentBlockDisposables.RemoveRange(startIndex, count);
        return blockDisposables;
    }

    private void DisposePendingBlockDisposables(int startIndex)
    {
        for (var i = _currentBlockDisposables.Count - 1; i >= startIndex; i--)
        {
            _currentBlockDisposables[i].Dispose();
            _currentBlockDisposables.RemoveAt(i);
        }
    }

    private void ReplaceRenderedBlocks(
        int replaceStartIndex,
        int replaceEndIndex,
        IReadOnlyList<RenderedBlock> newBlocks,
        MarkdownDocumentModel newModel)
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

        for (var i = 0; i < _renderedBlocks.Count && i < newModel.Blocks.Count; i++)
        {
            _renderedBlocks[i] = _renderedBlocks[i].WithModel(newModel.Blocks[i]);
        }

        ResetSelectionState();
        RefreshSelectionBlocks();
        InvalidateDocumentLayout();
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

    private void OnViewerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_documentHost is null
            || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed
            || IsLinkPointerSource(e)
            || IsInteractiveSelectionSource(e.Source as Visual))
        {
            return;
        }

        var documentPoint = e.GetPosition(_documentHost);
        if (_selectionController.Begin(documentPoint))
        {
            _pointerSelectionState = new MarkdownPointerSelectionState(e.GetPosition(this));
            _nativeSelectionTextBlock = FindSelectableTextBlock(e.Source as Visual);
            _isPointerSelecting = false;
            UpdateSelectionState();
        }
    }

    private void OnViewerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_documentHost is null || _pointerSelectionState is not { } state)
        {
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            _pointerSelectionState = null;
            _nativeSelectionTextBlock = null;
            _isPointerSelecting = false;
            e.Pointer.Capture(null);
            return;
        }

        if (!state.IsDragging(e.GetPosition(this)))
        {
            return;
        }

        if (!_isPointerSelecting
            && _nativeSelectionTextBlock is { } nativeTextBlock
            && IsPointerInside(nativeTextBlock, e))
        {
            return;
        }

        if (_selectionController.Extend(e.GetPosition(_documentHost)))
        {
            _isPointerSelecting = true;
            e.Pointer.Capture(this);
            ClearNativeTextSelections();
            UpdateSelectionState();
            e.Handled = true;
            return;
        }

        if (_isPointerSelecting)
        {
            e.Handled = true;
        }
    }

    private void OnViewerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointerSelectionState is null)
        {
            return;
        }

        if (_isPointerSelecting)
        {
            e.Handled = true;
        }
        else
        {
            _selectionController.CommitClickWithoutDrag();
            UpdateSelectionState();
        }

        _pointerSelectionState = null;
        _nativeSelectionTextBlock = null;
        _isPointerSelecting = false;
        e.Pointer.Capture(null);
    }

    private void RefreshSelectionBlocks()
    {
        _selectionController.SetBlocks(_renderedBlocks.Select(block =>
            new MarkdownSelectionBlock(
                block.Control,
                new MarkdownTextSpan(block.PlainTextStart, block.PlainTextEnd),
                block.PlainText)), _documentHost);
        UpdateSelectionState();
    }

    private void ResetSelectionState()
    {
        _selectionController.Clear();
        _pointerSelectionState = null;
        _nativeSelectionTextBlock = null;
        _isPointerSelecting = false;
        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        var selectedText = _selectionController.SelectedText;
        var hasSelection = !string.IsNullOrEmpty(selectedText);
        var changed = selectedText != SelectedText || hasSelection != HasSelection;
        SelectedText = selectedText;
        HasSelection = hasSelection;
        if (changed)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ClearNativeTextSelections()
    {
        if (_documentHost is null)
        {
            return;
        }

        foreach (var textBlock in _documentHost.GetVisualDescendants().OfType<SelectableTextBlock>())
        {
            if (!string.IsNullOrEmpty(textBlock.SelectedText))
            {
                textBlock.ClearSelection();
            }
        }
    }

    private static bool IsInteractiveSelectionSource(Visual? source)
    {
        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Button
                or MenuItem
                or CheckBox
                or ScrollBar
                or MarkdownImage)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointerInside(Control control, PointerEventArgs e)
    {
        var point = e.GetPosition(control);
        return new Rect(control.Bounds.Size).Contains(point);
    }

    private static bool IsLinkPointerSource(PointerEventArgs e)
    {
        if (FindSelectableTextBlock(e.Source as Visual) is not { } textBlock)
        {
            return false;
        }

        string? url;
        return MarkdownLinkInteraction.TryGetLinkAtPointer(textBlock, e, out url);
    }

    private void InvalidateDocumentLayout()
    {
        _documentHost?.InvalidateMeasure();
        _documentHost?.InvalidateArrange();
        InvalidateMeasure();
        InvalidateArrange();
    }

    private RenderedBlock? FindRenderedBlockBySourceOffset(int sourceOffset)
    {
        if (_renderedBlocks.Count == 0)
        {
            return null;
        }

        foreach (var block in _renderedBlocks)
        {
            if (sourceOffset >= block.Start && sourceOffset <= block.End)
            {
                return block;
            }

            if (sourceOffset < block.Start)
            {
                return block;
            }
        }

        return _renderedBlocks[^1];
    }

    private static int GetSourceLineOffset(string text, int sourceLine)
    {
        if (sourceLine <= 1 || string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var line = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            line++;
            if (line == sourceLine)
            {
                return Math.Min(i + 1, text.Length);
            }
        }

        return text.Length;
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
        // 管线渲染器优先（特殊块/数学/代码/排版块等），未受理再走内置兜底。
        if (_blockPipeline.Render(block, sourceMarkdown, this) is { } pipelineBlock)
        {
            return pipelineBlock;
        }

        if (block is LinkReferenceDefinitionGroup or LinkReferenceDefinition)
        {
            return null;
        }

        return block switch
        {
            LinkReferenceDefinitionGroup => null,
            LinkReferenceDefinition => null,
            _ => CreateUnknownBlock(block)
        };
    }

    /// <summary>
    /// 决定代码块 token 配色是否使用深色主题：
    /// 以实际生效的代码块背景（排版主题按变体提供的 <see cref="TextBlockCodeBackground"/> 或
    /// 显式设置的 <see cref="CodeBackgroundBrush"/>）的感知亮度为准。
    /// 此前按 <c>ActualThemeVariant == ThemeVariant.Dark</c> 判定，自定义主题变体
    /// （Desert/NightSky 等，基座为 Light/Dark 但变体本身不等于 Dark）恒判为浅色，
    /// 导致深色代码块底上渲染浅色主题的深色 token（键名/标点不可读）。
    /// </summary>
    private bool ResolveCodeBlockIsDark()
    {
        if (GetValue(CodeBackgroundBrushProperty) is ISolidColorBrush solid
            && solid.Color.A > 0)
        {
            return GetPerceivedLuminance(solid.Color) < 0.5;
        }

        // 排版主题未定义代码背景资源（如 Simple）时解析失败，默认按深色底处理：
        // 内置排版主题的代码块底色即为深色系，DarkPlus token 在其上可读。
        if (TryGetResource(MarkdownStyleKeys.CodeBackgroundBrushResource, ActualThemeVariant, out var resource)
            && resource is ISolidColorBrush themeBrush)
        {
            return GetPerceivedLuminance(themeBrush.Color) < 0.5;
        }

        return true;
    }

    private static double GetPerceivedLuminance(Color color)
    {
        return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
    }

    private MarkdownMathView CreateMathView(string latex, double fontSize, CSharpMath.Atom.LineStyle lineStyle)
    {
        var view = new MarkdownMathView
        {
            LaTeX = MathLatexNormalizer.NormalizeLatex(latex),
            FontSize = (float)fontSize,
            LineStyle = lineStyle,
            DisplayErrorInline = false
        };
        BindTheme(view, MarkdownMathView.ForegroundProperty, TextBrushProperty);
        return view;
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
        return MarkdownPlainTextExtractor.IsTypeNameFallback(text, block.GetType())
            ? null
            : CreateFallbackText(text, MarkdownStyleKeys.UnknownBlock);
    }

    private static SelectableTextBlock? FindSelectableTextBlock(Visual? source)
    {
        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is SelectableTextBlock textBlock)
            {
                return textBlock;
            }
        }

        return null;
    }

    private SelectableTextBlock CreateSelectableText(params string[] classes)
    {
        var textBlock = new MarkdownSelectableTextBlock
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
        _viewerCopyMenuItem.Header = I18nManager.Instance.GetResource(MarkdownL.CopyRenderedText);
        return new ContextMenu { ItemsSource = new[] { _viewerCopyMenuItem } };
    }

    private ContextMenu CreateSelectableTextContextMenu(SelectableTextBlock textBlock)
    {
        var copySelectionItem = new MenuItem
        {
            Header = I18nManager.Instance.GetResource(MarkdownL.CopySelectedText)
        };
        copySelectionItem.Click += async (_, _) =>
        {
            if (HasSelection)
            {
                await CopySelectionAsync();
            }
            else
            {
                textBlock.Copy();
            }
        };

        var copyRenderedTextItem = new MenuItem
        {
            Header = I18nManager.Instance.GetResource(MarkdownL.CopyRenderedText)
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

    private IDisposable BindTheme<T>(
        AvaloniaObject target,
        AvaloniaProperty<T> targetProperty,
        StyledProperty<T> sourceProperty)
    {
        var disposable = target.Bind(targetProperty, this.GetObservable(sourceProperty));
        _currentBlockDisposables.Add(disposable);
        return disposable;
    }

    IDisposable Rendering.IMarkdownRenderContext.BindTheme<T>(
        AvaloniaObject target,
        AvaloniaProperty<T> targetProperty,
        StyledProperty<T> sourceProperty) => BindTheme(target, targetProperty, sourceProperty);

    SelectableTextBlock Rendering.IMarkdownRenderContext.CreateSelectableText(params string[] classes) =>
        CreateSelectableText(classes);

    void Rendering.IMarkdownRenderContext.AddMarkdownClass(Control control, params string[] classes) =>
        AddMarkdownClass(control, classes);

    MarkdownMathView Rendering.IMarkdownRenderContext.CreateMathView(string latex, double fontSize, CSharpMath.Atom.LineStyle lineStyle) =>
        CreateMathView(latex, fontSize, lineStyle);

    Control Rendering.IMarkdownRenderContext.CreateFallbackText(string text, string className) =>
        CreateFallbackText(text, className);

    bool Rendering.IMarkdownRenderContext.CodeBlockIsDark => ResolveCodeBlockIsDark();

    FontFamily Rendering.IMarkdownRenderContext.CodeFontFamily => CodeFontFamily;

    double Rendering.IMarkdownRenderContext.CodeBlockFontSize => CodeBlockFontSize;

    double Rendering.IMarkdownRenderContext.CodeBlockLineHeight => CodeBlockLineHeight;

    bool Rendering.IMarkdownRenderContext.HasSelection => HasSelection;

    void Rendering.IMarkdownRenderContext.CopyCodeToClipboard(string code)
    {
        CopyClick?.Invoke(this, EventArgs.Empty);
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            _ = clipboard.SetTextAsync(code);
        }
    }

    Task Rendering.IMarkdownRenderContext.CopySelectionAsync() => CopySelectionAsync();

    void Rendering.IMarkdownRenderContext.RaiseCodeBlockToolRender(StackPanel header, StackPanel content, CodeBlock codeBlock) =>
        CodeBlockToolRender?.Invoke(this, new CodeBlockToolRenderEventArgs(header, content, codeBlock));

    Control? Rendering.IMarkdownRenderContext.ConvertBlock(Block block, string? sourceMarkdown) =>
        ConvertBlock(block, sourceMarkdown);

    SelectableTextBlock Rendering.IMarkdownRenderContext.CreateParagraph(ParagraphBlock paragraph, bool stripTaskPrefix, Thickness? marginOverride) =>
        ParagraphRenderer.CreateParagraph(paragraph, stripTaskPrefix, marginOverride, this);

    private System.Collections.Generic.IEnumerable<Avalonia.Controls.Documents.Inline> ConvertInlines(
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool stripTaskPrefix = false) => InlineRenderer.ConvertInlines(container, stripTaskPrefix);

    System.Collections.Generic.IEnumerable<Avalonia.Controls.Documents.Inline> Rendering.IMarkdownRenderContext.ConvertInlines(
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool stripTaskPrefix) => ConvertInlines(container, stripTaskPrefix);

    void Rendering.IMarkdownRenderContext.AttachLinkInteraction(
        SelectableTextBlock textBlock,
        Markdig.Syntax.Inlines.ContainerInline? container,
        bool stripTaskPrefix) =>
        MarkdownLinkInteraction.AttachLinkInteraction(textBlock, MarkdownLinkInteraction.ExtractLinkSpans(container, stripTaskPrefix));

    string? Rendering.IMarkdownRenderContext.MarkdownText => Markdown;

    string? Rendering.IMarkdownRenderContext.ImageBasePath => ImageBasePath;

    private sealed record RenderedBlock(
        int Start,
        int End,
        int PlainTextStart,
        int PlainTextEnd,
        string PlainText,
        MarkdownBlockKind Kind,
        MarkdownDependencyFlags DependencyFlags,
        ulong ContentHash,
        Control Control,
        List<IDisposable>? Bindings)
    {
        public static RenderedBlock FromModel(
            MarkdownDocumentBlock modelBlock,
            Control control,
            List<IDisposable>? bindings)
        {
            return new RenderedBlock(
                modelBlock.SourceSpan.Start,
                modelBlock.SourceSpan.End,
                modelBlock.PlainTextSpan.Start,
                modelBlock.PlainTextSpan.End,
                modelBlock.PlainText,
                modelBlock.Kind,
                modelBlock.DependencyFlags,
                modelBlock.ContentHash,
                control,
                bindings);
        }

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

        public RenderedBlock WithModel(MarkdownDocumentBlock modelBlock)
        {
            return this with
            {
                Start = modelBlock.SourceSpan.Start,
                End = modelBlock.SourceSpan.End,
                PlainTextStart = modelBlock.PlainTextSpan.Start,
                PlainTextEnd = modelBlock.PlainTextSpan.End,
                PlainText = modelBlock.PlainText,
                Kind = modelBlock.Kind,
                DependencyFlags = modelBlock.DependencyFlags,
                ContentHash = modelBlock.ContentHash
            };
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
