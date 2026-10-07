using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

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
    private readonly string _sourceText;
    private readonly bool _isCode;
    private readonly MarkdownBlockViewOptions _options;
    private double _runFontSize = 15;

    private bool _editing;

    public MarkdownBlockView(MarkdownBlockModel model, bool isCode, MarkdownBlockViewOptions options)
    {
        Model = model;
        _isCode = isCode;
        _sourceText = model.Text;
        _options = options;

        _editor.AcceptsReturn = isCode;
        _editor.TextWrapping = TextWrapping.Wrap;
        _editor.FontFamily = isCode ? MarkdownWysiwygPalette.CodeFontFamily : FontFamily.Default;
        _editor.FontSize = _runFontSize;
        _editor.Foreground = options.TextBrush;
        _editor.CaretBrush = options.AccentBrush;
        _editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        _editor.Text = model.Text;
        _editor.LostFocus += (_, _) => CommitAndRender();
        _editor.KeyDown += OnEditorKeyDown;

        _root = new Border { Child = _richText };
        Content = _root;

        PointerPressed += OnPointerPressed;
        UpdateRichText();
    }

    /// <summary>块内容变化的回调（参数为新文本）。</summary>
    public Action<MarkdownBlockView, string>? TextCommitted { get; set; }

    /// <summary>请求把焦点移到相邻块（参数为相对方向：-1 上一块，+1 下一块）。</summary>
    public Action<int>? NavigateRequested { get; set; }

    public MarkdownBlockModel Model { get; }

    /// <summary>姝ｆ枃瀛楀彿锛堟爣棰?浠ｇ爜鍧楃敱瑙嗗浘鎸夊潡绫诲瀷璁剧疆锛夈€?/summary>
    public double RunFontSize
    {
        get => _runFontSize;
        set
        {
            _runFontSize = value;
            UpdateRichText();
        }
    }

    /// <summary>妯″瀷琚閮ㄤ慨鏀癸紙濡備换鍔″嬀閫夛級鍚庡埛鏂版樉绀恒€?/summary>
    public void NotifyModelChanged()
    {
        RefreshFromModel();
        TextCommitted?.Invoke(this, Model.Text);
    }

    public bool IsEditing => _editing;

    /// <summary>正在编辑时的底层文本框（用于格式化动作），否则为 null。</summary>
    internal TextBox? ActiveEditor => _editing ? _editor : null;

    /// <summary>当前编辑文本（非编辑态返回模型文本）。</summary>
    internal string CurrentText => _editing ? _editor.Text ?? string.Empty : Model.Text;

    /// <summary>进入编辑态并把光标放到行尾（等价于点入该块）。</summary>
    public void BeginEditAtEnd() => BeginEdit(_editor.Text?.Length ?? 0);

    /// <summary>进入编辑态，并把光标放到指定偏移。</summary>
    public void BeginEdit(int caretIndex)
    {
        if (_editing)
        {
            _editor.CaretIndex = Math.Clamp(caretIndex, 0, _editor.Text?.Length ?? 0);
            _editor.Focus();
            return;
        }

        _editing = true;
        _editor.Text = Model.Text;
        _root.Child = _editor;
        _editor.Focus();
        _editor.CaretIndex = Math.Clamp(caretIndex, 0, _editor.Text?.Length ?? 0);
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
        _root.Child = _richText;
        if (!string.Equals(text, Model.Text, StringComparison.Ordinal))
        {
            Model.Text = text;
            UpdateRichText();
            TextCommitted?.Invoke(this, text);
        }
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
        if (_editing)
        {
            return;
        }

        var point = e.GetPosition(_richText);
        var offset = _richText.GetTextOffset(point);
        BeginEdit(offset);
        e.Handled = true;
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _editing = false;
                var original = Model.Text;
                _root.Child = _richText;
                _editor.Text = original;
                UpdateRichText();
                e.Handled = true;
                break;

            case Key.Enter when !_isCode && !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                // 段落/标题内回车 = 结束本块编辑（换行请用 Shift+Enter）。
                e.Handled = true;
                CommitAndRender();
                NavigateRequested?.Invoke(1);
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
        _richText.BindPalette(_options);
        _richText.RunFontSize = _runFontSize;
        _richText.SetRuns(_isCode
            ? MarkdownTextRunParser.Plain(Model.Text, code: true)
            : MarkdownTextRunParser.Parse(Model.Text));
    }
}

/// <summary>所见即所得视图里一个表格块的编辑视图（每个单元格就地编辑）。</summary>
internal sealed class MarkdownTableView : UserControl
{
    private readonly Grid _grid = new();
    private readonly List<List<TextBox>> _cells = [];
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
            var rowBoxes = new List<TextBox>();
            for (var column = 0; column < columnCount; column++)
            {
                var value = column < rows[row].Count ? rows[row][column] : string.Empty;
                var isHeader = row == 0 && _model.HasHeader;
                var box = new TextBox
                {
                    Text = value,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                var capturedRow = row;
                var capturedColumn = column;
                box.LostFocus += (_, _) => CommitCell(capturedRow, capturedColumn, box.Text ?? string.Empty);
                box.KeyDown += (_, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        args.Handled = true;
                        CommitCell(capturedRow, capturedColumn, box.Text ?? string.Empty);
                        MoveFocus(capturedRow + 1, capturedColumn);
                    }
                };

                Grid.SetRow(box, row);
                Grid.SetColumn(box, column);
                _grid.Children.Add(box);
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
            _cells[row][column].Focus();
        }
    }
}
