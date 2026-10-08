using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using CodeWF.Markdown.Controls;

namespace CodeWF.Markdown.Editor.Controls.Wysiwyg;

/// <summary>
/// 一个块的就地编辑视图：默认渲染为富文本（只读显示，可选中），
/// 点入后切换为源码编辑（<see cref="TextBox"/>），失焦或按 Esc 回渲染，
/// 按 Tab 进入下一个块。文本变化通过 <see cref="TextCommitted"/> 回抛给宿主视图。
/// </summary>
internal sealed class MarkdownBlockView : UserControl
{
    private readonly MarkdownRichTextView _richText = new();
    private readonly TextBox _editor = new();
    private readonly Border _root;
    private readonly MarkdownViewer _preview = new() { DocumentBottomPadding = 0 };
    private readonly bool _isCode;
    private readonly bool _isTableCell;
    private readonly MarkdownBlockViewOptions _options;
    private double _runFontSize = 15;

    private bool _editing;
    private bool _syncingText;

    public MarkdownBlockView(MarkdownBlockModel model, bool isCode, MarkdownBlockViewOptions options, bool isTableCell = false)
    {
        Model = model;
        _isCode = isCode;
        _isTableCell = isTableCell;
        _options = options;

        _editor.AcceptsReturn = true;
        _editor.TextWrapping = TextWrapping.Wrap;
        _editor.FontFamily = isCode ? MarkdownWysiwygPalette.CodeFontFamily : FontFamily.Default;
        _editor.FontSize = _runFontSize;
        _editor.Foreground = options.TextBrush;
        _editor.CaretBrush = options.AccentBrush;
        _editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        _editor.Text = ModelText;
        _editor.TextChanged += (_, _) =>
        {
            if (!_editing || _syncingText) return;
            var text = _editor.Text ?? string.Empty;
            if (string.Equals(ModelText, text, StringComparison.Ordinal)) return;
            ModelText = text;
            TextCommitted?.Invoke(this, ModelText);
        };
        _editor.LostFocus += (_, _) => CommitAndRender();
        _editor.KeyDown += OnEditorKeyDown;

        _root = new Border { Child = _richText };
        Content = _root;

        // 先接管渲染块的点击，避免 SelectableTextBlock 在冒泡前后抢回焦点，
        // 导致刚打开的编辑框立即触发 LostFocus 并消失。
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        UpdateRichText();
    }

    /// <summary>块内容变化的回调（参数为新文本）。</summary>
    public Action<MarkdownBlockView, string>? TextCommitted { get; set; }

    /// <summary>请求把焦点移到相邻块（参数为相对方向：-1 上一块，+1 下一块）。</summary>
    public Action<int>? NavigateRequested { get; set; }

    public Action<MarkdownBlockView>? EditingStarted { get; set; }

    public Action<MarkdownBlockView, int>? SplitRequested { get; set; }

    public MarkdownBlockModel Model { get; }

    private bool _renderQuoteAsParagraph;
    internal bool RenderQuoteAsParagraph
    {
        get => _renderQuoteAsParagraph;
        set { _renderQuoteAsParagraph = value; UpdateRichText(); }
    }

    /// <summary>正文字号；标题与代码块按类型设置。</summary>
    public double RunFontSize
    {
        get => _runFontSize;
        set
        {
            _runFontSize = value;
            _editor.FontSize = value;
            UpdateRichText();
        }
    }

    /// <summary>外部修改模型后刷新显示并通知宿主。</summary>
    public void NotifyModelChanged()
    {
        RefreshFromModel();
        TextCommitted?.Invoke(this, Model.Text);
    }

    public bool IsEditing => _editing;

    public void InsertText(string text)
    {
        if (!_editing) BeginEditAtEnd();
        var start = Math.Min(_editor.SelectionStart, _editor.SelectionEnd);
        var length = Math.Abs(_editor.SelectionEnd - _editor.SelectionStart);
        _editor.Text = CurrentText.Remove(start, length).Insert(start, text);
        _editor.CaretIndex = start + text.Length;
    }

    /// <summary>正在编辑时的底层文本框（用于格式化动作），否则为 null。</summary>
    internal TextBox? ActiveEditor => _editing ? _editor : null;

    /// <summary>当前编辑文本（非编辑态返回模型文本）。</summary>
    internal string CurrentText => _editing ? _editor.Text ?? string.Empty : ModelText;

    private string ModelText
    {
        get => Model.Kind == MarkdownBlockKind.Raw ? Model.Raw : Model.Text;
        set { if (Model.Kind == MarkdownBlockKind.Raw) Model.Raw = value; else Model.Text = value; }
    }

    /// <summary>进入编辑态并把光标放到行尾（等价于点入该块）。</summary>
    public void BeginEditAtEnd() => BeginEdit(ModelText.Length);

    /// <summary>进入编辑态，并把光标放到指定偏移。</summary>
    public void BeginEdit(int caretIndex)
    {
        if (_editing)
        {
            _editor.CaretIndex = Math.Clamp(caretIndex, 0, _editor.Text?.Length ?? 0);
            _editor.Focus();
            return;
        }

        _syncingText = true;
        _editor.Text = ModelText;
        _syncingText = false;
        _editing = true;
        _root.Child = _editor;
        _editor.Focus();
        _editor.CaretIndex = Math.Clamp(caretIndex, 0, _editor.Text?.Length ?? 0);
        EditingStarted?.Invoke(this);
    }

    /// <summary>回渲染（提交文本）。</summary>
    public void CommitAndRender()
    {
        if (!_editing)
        {
            return;
        }

        _editing = false;
        var text = _editor.Text ?? string.Empty;
        if (!string.Equals(text, ModelText, StringComparison.Ordinal))
        {
            ModelText = text;
            TextCommitted?.Invoke(this, text);
        }
        UpdateRichText();
    }

    /// <summary>外部替换内容（例如预览回写）时刷新显示。</summary>
    public void RefreshFromModel()
    {
        if (_editing)
        {
            return;
        }

        UpdateRichText();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_editing || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var offset = ReferenceEquals(_root.Child, _richText)
            ? MarkdownTextRunParser.GetSourceOffset(ModelText, _richText.GetTextOffset(e.GetPosition(_richText)))
            : ModelText.Length;
        if (_root.Child == _preview && e.Source is Visual source)
        {
            var text = source as SelectableTextBlock ?? source.GetVisualAncestors().OfType<SelectableTextBlock>().FirstOrDefault();
            if (text?.TextLayout is { } layout)
            {
                var visible = layout.HitTestPoint(e.GetPosition(text)).TextPosition;
                offset = _isCode || Model.Kind == MarkdownBlockKind.Raw ? visible
                    : MarkdownTextRunParser.GetSourceOffset(ModelText, visible);
            }
        }
        BeginEdit(offset);
        e.Handled = true;
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _editing = false;
                UpdateRichText();
                e.Handled = true;
                break;

            case Key.Enter when !_isCode && Model.Kind != MarkdownBlockKind.Raw && e.KeyModifiers == KeyModifiers.None:
                e.Handled = true;
                var caret = _editor.CaretIndex;
                CommitAndRender();
                SplitRequested?.Invoke(this, caret);
                break;

            case Key.Enter when e.KeyModifiers == KeyModifiers.Shift:
                var start = Math.Min(_editor.SelectionStart, _editor.SelectionEnd);
                var length = Math.Abs(_editor.SelectionEnd - _editor.SelectionStart);
                _editor.Text = CurrentText.Remove(start, length).Insert(start, _isCode ? "\n" : "  \n");
                _editor.CaretIndex = start + (_isCode ? 1 : 3);
                e.Handled = true;
                break;

            case Key.Tab:
                e.Handled = true;
                CommitAndRender();
                NavigateRequested?.Invoke(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1);
                break;
        }
    }

    private void UpdateRichText()
    {
        var useViewer = !_isTableCell || ModelText.Contains("![", StringComparison.Ordinal) || ModelText.Contains('$');
        if (useViewer)
        {
            _preview.ImageBasePath = _options.ImageBasePath;
            _preview.TypographyTheme = _options.TypographyTheme;
            _preview.TypographySize = _options.TypographySize;
            _preview.Markdown = Model.Kind == MarkdownBlockKind.ListItem || RenderQuoteAsParagraph
                ? Model.Text : MarkdownBlockParser.WriteCanonical([Model]);
            if (!_editing) _root.Child = _preview;
            return;
        }
        _richText.BindPalette(_options);
        _richText.FontWeight = Model.HeadingLevel > 0 ? FontWeight.Bold : FontWeight.Normal;
        _richText.RunFontSize = _runFontSize;
        _richText.SetRuns(_isCode
            ? MarkdownTextRunParser.Plain(Model.Text, code: true)
            : MarkdownTextRunParser.Parse(Model.Text));
        if (!_editing) _root.Child = _richText;
    }
}

/// <summary>所见即所得视图里一个表格块的编辑视图（每个单元格就地编辑）。</summary>
internal sealed class MarkdownTableView : UserControl
{
    private readonly Grid _grid = new();
    private readonly List<List<MarkdownBlockView>> _cells = [];
    private readonly MarkdownBlockModel _model;
    private readonly MarkdownBlockViewOptions _options;

    public MarkdownTableView(MarkdownBlockModel model, MarkdownBlockViewOptions options)
    {
        _model = model;
        _options = options;
        Rebuild();
        Content = new Border
        {
            BorderBrush = _options.SeparatorBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = _grid
        };
        var menu = new ContextMenu();
        var labels = new[] { "添加行", "删除末行", "添加列", "删除末列" };
        for (var action = 0; action < labels.Length; action++)
        {
            var capturedAction = action;
            var item = new MenuItem { Header = labels[action] };
            item.Click += (_, _) => StructureChangeRequested?.Invoke(this, capturedAction);
            menu.Items.Add(item);
        }
        ContextMenu = menu;
    }

    /// <summary>单元格内容变化回调。</summary>
    public Action<MarkdownTableView>? CellsChanged { get; set; }

    /// <summary>请求增删行列（参数为 0=加行 1=删行 2=加列 3=删列）。</summary>
    public Action<MarkdownTableView, int>? StructureChangeRequested { get; set; }

    public MarkdownBlockModel Model => _model;

    public void RefreshFromModel() => Rebuild();

    private void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        _cells.Clear();

        var rows = _model.Cells;
        var columnCount = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
        if (columnCount == 0)
        {
            return;
        }

        for (var column = 0; column < columnCount; column++)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var row = 0; row < rows.Count; row++)
        {
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var rowBoxes = new List<MarkdownBlockView>();
            for (var column = 0; column < columnCount; column++)
            {
                var value = column < rows[row].Count ? rows[row][column] : string.Empty;
                var isHeader = row == 0 && _model.HasHeader;
                var cellModel = new MarkdownBlockModel { Kind = MarkdownBlockKind.Paragraph, Text = value,
                    HeadingLevel = isHeader ? 6 : 0 };
                var box = new MarkdownBlockView(cellModel, false, _options, isTableCell: true) { RunFontSize = _options.FontSize };
                var capturedRow = row;
                var capturedColumn = column;
                box.TextCommitted = (_, text) => CommitCell(capturedRow, capturedColumn, text);
                box.SplitRequested = (_, _) => MoveFocus(capturedRow + 1, capturedColumn);
                box.NavigateRequested = direction =>
                {
                    var next = capturedRow * columnCount + capturedColumn + direction;
                    MoveFocus(next / columnCount, next % columnCount);
                };
                var cell = new Border { Padding = new Thickness(10, 8), Child = box,
                    BorderBrush = _options.SeparatorBrush, BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = isHeader ? _options.TableHeaderBackgroundBrush ?? _options.CodeBackgroundBrush : Brushes.Transparent };
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, column);
                _grid.Children.Add(cell);
                rowBoxes.Add(box);
            }

            _cells.Add(rowBoxes);
        }
    }

    private void CommitCell(int row, int column, string value)
    {
        if (_model.Cells.Count <= row || _model.Cells[row].Count <= column)
        {
            return;
        }

        if (string.Equals(_model.Cells[row][column], value, StringComparison.Ordinal))
        {
            return;
        }

        _model.Cells[row][column] = value;
        CellsChanged?.Invoke(this);
    }

    private void MoveFocus(int row, int column)
    {
        if (row >= 0 && row < _cells.Count && column >= 0 && column < _cells[row].Count)
        {
            _cells[row][column].BeginEditAtEnd();
        }
    }
}
