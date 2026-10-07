using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

using CodeWF.AvaloniaControls.Text;

using CodeWF.Markdown.Editor.Controls.Wysiwyg;
using CodeWF.Markdown.Editor.Services;

namespace CodeWF.Markdown.Editor.Controls;

/// <summary>
/// 单栏实时编辑（所见即所得）视图：每个 Markdown 块渲染成富文本，
/// 点入该块就地按源码编辑，离开即回渲染；表格直接渲染为表格、任务列表可直接勾选。
/// <para>
/// 与 <see cref="MarkdownEditorView"/>（源码编辑器）保持一致的对外语义：
/// <see cref="SetText"/> 与 <see cref="ApplyExternalEdit"/> 不会触发 <see cref="MarkdownChanged"/>，
/// 只有用户编辑才回抛。
/// </para>
/// </summary>
public class MarkdownLiveEditorView : UserControl
{
    private const string DefaultEditorBackgroundKey = "CodeWFMarkdownEditorBackgroundBrush";
    private const string DefaultEditorForegroundKey = "CodeWFMarkdownEditorForegroundBrush";
    private const string DefaultSeparatorKey = "CodeWFMarkdownEditorSeparatorBrush";

    private readonly MarkdownEditorOptions _options;
    private readonly IMarkdownEditorLocalizer _localizer;
    private readonly MarkdownBlockViewOptions _blockOptions;
    private readonly StackPanel _blockHost = new();
    private readonly List<Control> _blockViews = [];
    private readonly List<MarkdownBlockModel> _models = [];
    private readonly Border _container;
    private readonly Border _paneHead;
    private readonly TextBlock _paneTitle;
    private readonly Border _chipHost;
    private readonly TextBlock _chipText = new();

    private bool _suppressChanged;
    private bool _autoPairEnabled = true;
    private int _activeIndex = -1;

    public MarkdownLiveEditorView()
        : this(new MarkdownEditorOptions(), null)
    {
    }

    public MarkdownLiveEditorView(
        MarkdownEditorOptions? options = null,
        IMarkdownEditorLocalizer? localizer = null)
    {
        _options = options ?? new MarkdownEditorOptions();
        _autoPairEnabled = _options.EnableAutoPair;
        _localizer = localizer ?? new LiveEditorLocalizer();
        _blockOptions = new MarkdownBlockViewOptions();

        _blockHost.Orientation = Orientation.Vertical;
        _blockHost.Spacing = 6;
        _blockHost.Margin = new Thickness(6, 20, 34, 28);

        BuildPaneHead(out _paneHead, out _chipHost, out _paneTitle);
        UpdatePaneHead();

        _container = new Border { Child = _blockHost };
        Content = BuildContent();

        SetText(string.Empty);
        ActualThemeVariantChanged += (_, _) => ApplyThemedVisuals();
        AttachedToVisualTree += (_, _) => ApplyThemedVisuals();
        ApplyThemedVisuals();
    }

    /// <summary>文档内容变化（用户编辑产生）；参数为当前全文 Markdown。</summary>
    public event EventHandler<string>? MarkdownChanged;

    /// <summary>插入点变化：行、列、总块数。</summary>
    public event EventHandler<MarkdownCaretEventArgs>? SelectionChanged;

    /// <summary>动作已执行，宿主可同步自身状态。</summary>
    public event EventHandler<MarkdownEditorAction>? ActionRequested;

    /// <summary>窗格头标题；为空时不显示窗格头。</summary>
    public static readonly StyledProperty<string?> HeaderTextProperty =
        AvaloniaProperty.Register<MarkdownLiveEditorView, string?>(nameof(HeaderText));

    /// <summary>窗格头右侧芯片文案；为空时不显示。</summary>
    public static readonly StyledProperty<string?> ChipTextProperty =
        AvaloniaProperty.Register<MarkdownLiveEditorView, string?>(nameof(ChipText), "Markdown");

    /// <summary>设计令牌键：背景色。</summary>
    public static readonly StyledProperty<string> EditorBackgroundKeyProperty =
        AvaloniaProperty.Register<MarkdownLiveEditorView, string>(nameof(EditorBackgroundKey), DefaultEditorBackgroundKey);

    /// <summary>设计令牌键：前景色。</summary>
    public static readonly StyledProperty<string> EditorForegroundKeyProperty =
        AvaloniaProperty.Register<MarkdownLiveEditorView, string>(nameof(EditorForegroundKey), DefaultEditorForegroundKey);

    public string? HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public string? ChipText
    {
        get => GetValue(ChipTextProperty);
        set => SetValue(ChipTextProperty, value);
    }

    public string EditorBackgroundKey
    {
        get => GetValue(EditorBackgroundKeyProperty);
        set => SetValue(EditorBackgroundKeyProperty, value);
    }

    public string EditorForegroundKey
    {
        get => GetValue(EditorForegroundKeyProperty);
        set => SetValue(EditorForegroundKeyProperty, value);
    }

    /// <summary>自动配对开关（编辑态生效）。</summary>
    public bool EnableAutoPair
    {
        get => _autoPairEnabled;
        set => _autoPairEnabled = value;
    }

    /// <summary>编辑器字号。</summary>
    public double EditorFontSize { get; set; } = 15;

    /// <summary>正文字色（预览渲染用，随 <see cref="EditorForegroundKey"/> 令牌解析）。</summary>
    public IBrush TextBrush => _blockOptions.TextBrush;

    /// <summary>次要字色（列表符号、弱化文案）。</summary>
    public IBrush MutedBrush => _blockOptions.MutedBrush;

    /// <summary>强调色（引用条、编辑框描边）。</summary>
    public IBrush AccentBrush => _blockOptions.AccentBrush;

    /// <summary>行内代码字色。</summary>
    public IBrush CodeBrush => _blockOptions.CodeBrush;

    /// <summary>链接字色。</summary>
    public IBrush LinkBrush => _blockOptions.LinkBrush;

    /// <summary>行内代码与代码块背景色。</summary>
    public IBrush CodeBackgroundBrush => _blockOptions.CodeBackgroundBrush;

    /// <summary>分隔线 / 表格边框色。</summary>
    public IBrush SeparatorBrush => _blockOptions.SeparatorBrush;

    /// <summary>当前 Markdown 全文。</summary>
    public string Text => MarkdownBlockParser.Write(_models);

    /// <summary>块数量。</summary>
    public int BlockCount => _models.Count;

    /// <summary>当前处于编辑态的块序号；无则 -1。</summary>
    public int ActiveBlockIndex => _activeIndex;

    /// <summary>替换全文（载入文档）；不触发 <see cref="MarkdownChanged"/>。</summary>
    public void SetText(string? markdown)
    {
        _suppressChanged = true;
        try
        {
            _models.Clear();
            _models.AddRange(MarkdownBlockParser.Parse(markdown));
            RebuildBlocks();
        }
        finally
        {
            _suppressChanged = false;
        }
    }

    /// <summary>按源码偏移应用一次外部改写（预览勾选任务回写）；语义与源码编辑器一致。</summary>
    public void ApplyExternalEdit(string markdown, int start, int length)
    {
        SetText(markdown);
    }

    /// <summary>在当前活动块插入文本；无活动块时追加一个段落块。</summary>
    public void InsertText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (_activeIndex >= 0 && _activeIndex < _models.Count)
        {
            var model = _models[_activeIndex];
            model.Text += text;
            if (_blockViews[_activeIndex] is MarkdownBlockView view)
            {
                view.RefreshFromModel();
            }

            RaiseChanged();
            return;
        }

        _models.Add(new MarkdownBlockModel { Kind = MarkdownBlockKind.Paragraph, Text = text });
        RebuildBlocks();
        RaiseChanged();
    }

    /// <summary>复制为纯文本：有活动编辑块时复制其内容，否则复制全文。</summary>
    public async Task CopyPlainTextAsync()
    {
        var text = MarkdownTextRunParser.ToPlainText(
            _activeIndex >= 0 && _activeIndex < _models.Count
                ? MarkdownTextRunParser.Parse(_models[_activeIndex].Text)
                : []);

        if (text.Length == 0)
        {
            text = Text;
        }

        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    /// <summary>执行一次格式化动作（作用于活动块的编辑文本框）。</summary>
    public Task ExecuteAsync(MarkdownEditorAction action)
    {
        ActionRequested?.Invoke(this, action);
        ApplyAction(action);
        return Task.CompletedTask;
    }

    /// <summary>按块序号聚焦（0 基）。</summary>
    public void FocusBlock(int index)
    {
        if (index < 0 || index >= _blockViews.Count)
        {
            return;
        }

        _activeIndex = index;
        if (_blockViews[index] is MarkdownBlockView view)
        {
            view.BeginEditAtEnd();
        }
        else if (_blockViews[index] is Grid { Children: { Count: > 1 } children } && children[1] is MarkdownBlockView inner)
        {
            inner.BeginEditAtEnd();
        }

        RaiseSelectionChanged();
    }

    public void FocusEditor()
    {
        if (_models.Count == 0)
        {
            return;
        }

        FocusBlock(Math.Clamp(_activeIndex < 0 ? 0 : _activeIndex, 0, _models.Count - 1));
    }

    #region 视图构建

    private Control BuildContent()
    {
        var host = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        host.Children.Add(_paneHead);

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = _container
        };
        Grid.SetRow(scroll, 1);
        host.Children.Add(scroll);
        return host;
    }

    private void BuildPaneHead(out Border head, out Border chipHost, out TextBlock title)
    {
        title = new TextBlock
        {
            Classes = { "pane-title" },
            VerticalAlignment = VerticalAlignment.Center
        };

        var icon = new Avalonia.Controls.Shapes.Path
        {
            Classes = { "stroke-ico" },
            Width = 12,
            Height = 12,
            Data = Geometry.Parse("M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7 M18.5 2.5a2.12 2.12 0 0 1 3 3L12 15l-4 1 1-4z")
        };

        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        left.Children.Add(icon);
        left.Children.Add(title);

        chipHost = new Border { Classes = { "kbd-chip" }, Child = _chipText };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(left);
        Grid.SetColumn(chipHost, 1);
        grid.Children.Add(chipHost);

        head = new Border { Classes = { "pane-head" }, Child = grid };
    }

    private void UpdatePaneHead()
    {
        _paneTitle.Text = HeaderText ?? string.Empty;
        _paneHead.IsVisible = !string.IsNullOrEmpty(HeaderText);
        _chipText.Text = ChipText ?? string.Empty;
        _chipHost.IsVisible = !string.IsNullOrEmpty(ChipText);
    }

    private void RebuildBlocks()
    {
        _blockHost.Children.Clear();
        _blockViews.Clear();

        for (var index = 0; index < _models.Count; index++)
        {
            _blockHost.Children.Add(BuildBlockView(index));
        }
    }

    private Control BuildBlockView(int index)
    {
        var model = _models[index];

        if (model.Kind == MarkdownBlockKind.Table)
        {
            var table = new MarkdownTableView(model, _blockOptions)
            {
                CellsChanged = _ => RaiseChanged(),
                StructureChangeRequested = (_, _) => RaiseChanged()
            };
            _blockViews.Add(table);
            return table;
        }

        if (model.Kind == MarkdownBlockKind.ThematicBreak)
        {
            var rule = new Border
            {
                Height = 1,
                Margin = new Thickness(0, 8, 0, 8),
                Background = _blockOptions.SeparatorBrush
            };
            _blockViews.Add(rule);
            return rule;
        }

        var view = new MarkdownBlockView(model, isCode: model.Kind == MarkdownBlockKind.Code, _blockOptions)
        {
            TextCommitted = OnBlockTextCommitted,
            NavigateRequested = direction => FocusBlock(Math.Clamp(index + direction, 0, _models.Count - 1))
        };
        AttachAutoPair(view);
        view.RunFontSize = model.Kind switch
        {
            MarkdownBlockKind.Heading => model.HeadingLevel switch
            {
                1 => 26d,
                2 => 22d,
                3 => 19d,
                4 => 17d,
                5 => 16d,
                _ => 15d
            },
            MarkdownBlockKind.Code => 13d,
            _ => 15d
        };

        var root = DecorateBlock(model, view);
        _blockViews.Add(root);
        return root;
    }

    private Control DecorateBlock(MarkdownBlockModel model, MarkdownBlockView view)
    {
        switch (model.Kind)
        {
            case MarkdownBlockKind.ListItem:
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    Margin = new Thickness(model.IndentLevel * 18, 0, 0, 0)
                };

                if (model.IsTask)
                {
                    var box = new CheckBox
                    {
                        IsChecked = model.IsChecked,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 6, 0)
                    };
                    box.IsCheckedChanged += (_, _) =>
                    {
                        model.IsChecked = box.IsChecked == true;
                        view.NotifyModelChanged();
                    };
                    row.Children.Add(box);
                }
                else
                {
                    row.Children.Add(new TextBlock
                    {
                        Text = model.OrderedNumber > 0 ? $"{model.OrderedNumber}. " : "• ",
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = _blockOptions.MutedBrush,
                        MinWidth = 20
                    });
                }

                Grid.SetColumn(view, 1);
                row.Children.Add(view);
                return row;

            case MarkdownBlockKind.Quote:
                return new Border
                {
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    BorderBrush = _blockOptions.AccentBrush,
                    Padding = new Thickness(10, 2, 0, 2),
                    Child = view
                };

            case MarkdownBlockKind.Heading:
                return new Border
                {
                    Margin = new Thickness(0, model.HeadingLevel <= 2 ? 8 : 4, 0, 2),
                    Child = view
                };

            case MarkdownBlockKind.Code:
                return new Border
                {
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(10, 8, 10, 8),
                    CornerRadius = new CornerRadius(6),
                    Background = _blockOptions.CodeBackgroundBrush,
                    Child = view
                };

            default:
                return view;
        }
    }


    #endregion

    #region 编辑事件

    private void OnBlockTextCommitted(MarkdownBlockView view, string text)
    {
        RaiseChanged();
    }

    private void AttachAutoPair(MarkdownBlockView view)
    {
        view.AddHandler(InputElement.KeyDownEvent, OnBlockKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnBlockKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not MarkdownBlockView view || !_autoPairEnabled)
        {
            return;
        }

        if (e.Key == Key.Back && e.KeyModifiers == KeyModifiers.None && view.ActiveEditor is { } backBox)
        {
            var result = TextAutoPair.HandleBackspace(backBox.Text ?? string.Empty, backBox.SelectionStart, backBox.SelectionEnd - backBox.SelectionStart);
            if (result is { } change)
            {
                backBox.Text = (backBox.Text ?? string.Empty).Remove(change.Start, change.Length).Insert(change.Start, change.Text);
                backBox.CaretIndex = change.CaretOffset;
                e.Handled = true;
            }
        }
    }

    private void RaiseChanged()
    {
        if (_suppressChanged)
        {
            return;
        }

        MarkdownChanged?.Invoke(this, Text);
        RaiseSelectionChanged();
    }

    private void RaiseSelectionChanged()
    {
        var index = _activeIndex < 0 ? 0 : _activeIndex;
        var (column, lineCount) = (_activeIndex < 0 || _activeIndex >= _models.Count)
            ? (1, Math.Max(1, _models.Count))
            : (1, _models.Count);
        SelectionChanged?.Invoke(this, new MarkdownCaretEventArgs(index + 1, column, lineCount));
    }

    #endregion

    #region 动作

    private void ApplyAction(MarkdownEditorAction action)
    {
        var activeEditor = (_activeIndex >= 0 && _activeIndex < _blockViews.Count)
            ? FindActiveEditor(_blockViews[_activeIndex])
            : null;

        switch (action)
        {
            case MarkdownEditorAction.Undo:
                activeEditor?.Undo();
                break;
            case MarkdownEditorAction.Redo:
                activeEditor?.Redo();
                break;
            case MarkdownEditorAction.Cut:
                activeEditor?.Cut();
                break;
            case MarkdownEditorAction.Copy:
                activeEditor?.Copy();
                break;
            case MarkdownEditorAction.Paste:
                activeEditor?.Paste();
                break;
            case MarkdownEditorAction.SelectAll:
                activeEditor?.SelectAll();
                break;
            case MarkdownEditorAction.Bold:
                WrapActive("**", "**");
                break;
            case MarkdownEditorAction.Italic:
                WrapActive("*", "*");
                break;
            case MarkdownEditorAction.InlineCode:
                WrapActive("`", "`");
                break;
            case MarkdownEditorAction.Link:
                WrapActive("[", $"]({_options.LinkUrlPlaceholder})");
                break;
            case MarkdownEditorAction.Image:
                WrapActive("![", $"]({_options.ImageTargetPlaceholder})");
                break;
            case MarkdownEditorAction.Paragraph:
                ConvertActiveBlock(MarkdownBlockKind.Paragraph, 0);
                break;
            case MarkdownEditorAction.Heading1:
            case MarkdownEditorAction.Heading2:
            case MarkdownEditorAction.Heading3:
            case MarkdownEditorAction.Heading4:
            case MarkdownEditorAction.Heading5:
            case MarkdownEditorAction.Heading6:
                ConvertActiveBlock(MarkdownBlockKind.Heading, (int)action - (int)MarkdownEditorAction.Heading1 + 1);
                break;
            case MarkdownEditorAction.Quote:
                ConvertActiveBlock(MarkdownBlockKind.Quote, 0);
                break;
            case MarkdownEditorAction.UnorderedList:
                ConvertActiveBlock(MarkdownBlockKind.ListItem, 0);
                break;
            case MarkdownEditorAction.TaskList:
                ConvertActiveBlock(MarkdownBlockKind.ListItem, 0, isTask: true);
                break;
            case MarkdownEditorAction.CodeFence:
                InsertBlock(new MarkdownBlockModel { Kind = MarkdownBlockKind.Code, IsFenced = true, Language = _options.CodeFenceLanguage });
                break;
            case MarkdownEditorAction.Table:
                InsertBlock(new MarkdownBlockModel
                {
                    Kind = MarkdownBlockKind.Table,
                    HasHeader = true,
                    Alignments = [TableCellAlignment.None, TableCellAlignment.None],
                    Cells = [["", ""], ["", ""]]
                });
                break;
            case MarkdownEditorAction.MathBlock:
                InsertBlock(new MarkdownBlockModel { Kind = MarkdownBlockKind.Raw, Raw = "$$\n\n$$" });
                break;
            case MarkdownEditorAction.HorizontalRule:
                InsertBlock(new MarkdownBlockModel { Kind = MarkdownBlockKind.ThematicBreak });
                break;
            case MarkdownEditorAction.Indent:
                ChangeIndent(1);
                break;
            case MarkdownEditorAction.Outdent:
                ChangeIndent(-1);
                break;
            case MarkdownEditorAction.FocusEditor:
                FocusEditor();
                break;
        }
    }

    private void WrapActive(string prefix, string suffix)
    {
        if (_activeIndex < 0 || _activeIndex >= _blockViews.Count)
        {
            return;
        }

        if (FindActiveEditor(_blockViews[_activeIndex]) is not { } box)
        {
            return;
        }

        var text = box.Text ?? string.Empty;
        var start = Math.Clamp(box.SelectionStart, 0, text.Length);
        var selectionEnd = Math.Clamp(box.SelectionEnd, start, text.Length);
        var length = selectionEnd - start;
        var selected = length > 0
            ? text.Substring(start, length)
            : prefix == "**" ? _localizer.Get(MarkdownEditorText.BoldPlaceholder) : string.Empty;
        var replacement = prefix + selected + suffix;
        box.Text = text[..start] + replacement + text[(selectionEnd)..];
        box.CaretIndex = start + replacement.Length;
        box.SelectionStart = start + prefix.Length;
        box.SelectionEnd = box.SelectionStart + selected.Length;
        if (_models[_activeIndex].Kind is MarkdownBlockKind.Paragraph or MarkdownBlockKind.Heading or MarkdownBlockKind.ListItem or MarkdownBlockKind.Quote)
        {
            _models[_activeIndex].Text = box.Text ?? string.Empty;
        }

        RaiseChanged();
    }

    private void ConvertActiveBlock(MarkdownBlockKind kind, int headingLevel, bool isTask = false)
    {
        if (_activeIndex < 0 || _activeIndex >= _models.Count)
        {
            return;
        }

        var model = _models[_activeIndex];
        model.Kind = kind;
        if (kind == MarkdownBlockKind.Heading)
        {
            model.HeadingLevel = headingLevel;
        }

        if (kind == MarkdownBlockKind.ListItem)
        {
            model.IsTask = isTask;
        }

        RebuildBlocks();
        RaiseChanged();
    }

    private void ChangeIndent(int delta)
    {
        if (_activeIndex < 0 || _activeIndex >= _models.Count || _models[_activeIndex].Kind != MarkdownBlockKind.ListItem)
        {
            return;
        }

        _models[_activeIndex].IndentLevel = Math.Max(0, _models[_activeIndex].IndentLevel + delta);
        RebuildBlocks();
        RaiseChanged();
    }

    private void InsertBlock(MarkdownBlockModel model)
    {
        var index = _activeIndex < 0 ? _models.Count : Math.Min(_activeIndex + 1, _models.Count);
        _models.Insert(index, model);
        RebuildBlocks();
        RaiseChanged();
        FocusBlock(index);
    }

    private static TextBox? FindActiveEditor(Control control) => control switch
    {
        MarkdownBlockView view => view.ActiveEditor,
        Grid { Children: { Count: > 1 } children } => children.OfType<MarkdownBlockView>().FirstOrDefault()?.ActiveEditor,
        Border { Child: MarkdownBlockView inner } => inner.ActiveEditor,
        _ => null
    };

    #endregion

    #region 视觉

    private void ApplyThemedVisuals()
    {
        var background = ResolveBrush(EditorBackgroundKey, DefaultEditorBackgroundKey);
        var foreground = ResolveBrush(EditorForegroundKey, DefaultEditorForegroundKey) ?? Brushes.Black;
        var muted = ResolveBrush(MarkdownEditorPalette.QuoteKey, MarkdownEditorPalette.QuoteKey);
        var accent = ResolveBrush(MarkdownEditorPalette.AccentKey, MarkdownEditorPalette.AccentKey);
        var code = ResolveBrush(MarkdownEditorPalette.CodeKey, MarkdownEditorPalette.CodeKey);
        var link = ResolveBrush(MarkdownEditorPalette.LinkKey, MarkdownEditorPalette.LinkKey);
        var codeBackground = ResolveBrush(
            MarkdownEditorPalette.CurrentLineBackgroundKey,
            MarkdownEditorPalette.CurrentLineBackgroundKey);
        var separator = ResolveBrush(MarkdownEditorPalette.SeparatorKey, MarkdownEditorPalette.SeparatorKey);

        _container.Background = background;
        Foreground = foreground;

        // 宿主令牌变化后，已建好的块视图要按新色重建，否则只有容器换色。
        if (_blockOptions.Apply(foreground, muted, accent, code, link, codeBackground, separator)
            && _models.Count > 0)
        {
            RebuildBlocks();
        }
    }

    private IBrush? ResolveBrush(string key, string fallbackKey)
    {
        if (Application.Current is { } app
            && app.TryGetResource(key, ActualThemeVariant, out var resource)
            && resource is IBrush brush)
        {
            return brush;
        }

        return MarkdownEditorPalette.Resolve(key, ActualThemeVariant)
               ?? MarkdownEditorPalette.Resolve(fallbackKey, ActualThemeVariant);
    }

    #endregion
}
