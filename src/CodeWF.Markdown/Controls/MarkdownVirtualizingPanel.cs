using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CodeWF.Markdown.Shared.Rendering;

namespace CodeWF.Markdown.Controls;

/// <summary>
/// 文档虚拟化宿主：块数量超过阈值后，只把「视口 ± overscan」范围内的块控件挂到可视树，
/// 其余块保留已测量的高度占位，滚动离屏即释放，避免长文档一次性挂载全部控件。
/// <para>
/// 与普通面板的差异：子项索引与块的文档序号一一对应，即使某块当前未物化，
/// 索引也不会错位（<see cref="InsertBlock"/> / <see cref="RemoveBlockAt"/> 按块序号操作）。
/// </para>
/// <para>
/// 关闭 <see cref="EnableVirtualization"/> 或块数量低于 <see cref="VirtualizationThreshold"/> 时，
/// 行为与普通垂直面板完全一致，宿主可据此在异常场景退回非虚拟化宿主。
/// </para>
/// </summary>
public sealed class MarkdownVirtualizingPanel : Panel
{
    private sealed class BlockEntry
    {
        public BlockEntry(Control control, MarkdownBlockKind kind)
        {
            Control = control;
            Kind = kind;
            Pinned = kind is MarkdownBlockKind.Code
                or MarkdownBlockKind.Table
                or MarkdownBlockKind.Image;
        }

        public Control Control { get; }

        public MarkdownBlockKind Kind { get; }

        /// <summary>大块（代码块 / Mermaid / 图片 / 表格）始终物化，避免测量与滚动抖动。</summary>
        public bool Pinned { get; }

        public bool Realized { get; set; } = true;

        public bool Measured { get; set; }

        public double Height { get; set; }
    }

    private readonly List<BlockEntry> _entries = [];
    private ScrollViewer? _subscribedScrollHost;
    private double _lastWidth = double.NaN;
    private double[] _heightBuffer = [];
    private bool[] _pinnedBuffer = [];

    public static readonly StyledProperty<bool> EnableVirtualizationProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, bool>(nameof(EnableVirtualization), true);

    /// <summary>启用虚拟化的最小块数量；低于该值按普通面板处理，避免小文档付出额外开销。</summary>
    public static readonly StyledProperty<int> VirtualizationThresholdProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, int>(nameof(VirtualizationThreshold), 40);

    /// <summary>视口上下额外物化的屏数。</summary>
    public static readonly StyledProperty<double> OverscanViewportsProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, double>(nameof(OverscanViewports), 2d);

    /// <summary>滚动宿主；未显式指定时自动向上查找。</summary>
    public static readonly StyledProperty<ScrollViewer?> ScrollHostProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, ScrollViewer?>(nameof(ScrollHost));

    /// <summary>块间距，与模板的 BlockSpacing 对齐。</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, double>(nameof(Spacing));

    /// <summary>尚未测量过的块使用的估算高度（经验值，量测后回填）。</summary>
    public static readonly StyledProperty<double> EstimatedBlockHeightProperty =
        AvaloniaProperty.Register<MarkdownVirtualizingPanel, double>(nameof(EstimatedBlockHeight), 24d);

    public bool EnableVirtualization
    {
        get => GetValue(EnableVirtualizationProperty);
        set => SetValue(EnableVirtualizationProperty, value);
    }

    public int VirtualizationThreshold
    {
        get => GetValue(VirtualizationThresholdProperty);
        set => SetValue(VirtualizationThresholdProperty, value);
    }

    public double OverscanViewports
    {
        get => GetValue(OverscanViewportsProperty);
        set => SetValue(OverscanViewportsProperty, value);
    }

    public ScrollViewer? ScrollHost
    {
        get => GetValue(ScrollHostProperty);
        set => SetValue(ScrollHostProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double EstimatedBlockHeight
    {
        get => GetValue(EstimatedBlockHeightProperty);
        set => SetValue(EstimatedBlockHeightProperty, value);
    }

    /// <summary>当前块数量（含未物化的块）。</summary>
    public int BlockCount => _entries.Count;

    /// <summary>当前是否实际处于虚拟化状态（开关打开且块数超过阈值）。</summary>
    public bool IsVirtualizing => EnableVirtualization && _entries.Count >= Math.Max(1, VirtualizationThreshold);

    /// <summary>已物化（挂在可视树上）的块数量，供诊断与测试使用。</summary>
    public int RealizedBlockCount => Children.Count;

    static MarkdownVirtualizingPanel()
    {
        AffectsMeasure<MarkdownVirtualizingPanel>(
            EnableVirtualizationProperty,
            VirtualizationThresholdProperty,
            OverscanViewportsProperty,
            EstimatedBlockHeightProperty,
            SpacingProperty);
        AffectsArrange<MarkdownVirtualizingPanel>(SpacingProperty);
    }

    public void AddBlock(Control control, MarkdownBlockKind kind) =>
        InsertBlock(_entries.Count, control, kind);

    public void InsertBlock(int index, Control control, MarkdownBlockKind kind)
    {
        var entry = new BlockEntry(control, kind);
        index = Math.Clamp(index, 0, _entries.Count);
        _entries.Insert(index, entry);
        Children.Insert(RealizedIndexBefore(index), control);
        entry.Realized = true;
        InvalidateMeasure();
    }

    public void RemoveBlockAt(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return;
        }

        var entry = _entries[index];
        if (entry.Realized)
        {
            Children.Remove(entry.Control);
            entry.Realized = false;
        }

        _entries.RemoveAt(index);
        InvalidateMeasure();
    }

    public void ClearBlocks()
    {
        _entries.Clear();
        Children.Clear();
        InvalidateMeasure();
    }

    /// <summary>取块控件；未物化时返回 null。</summary>
    public Control? GetRealizedControl(int index) =>
        index >= 0 && index < _entries.Count && _entries[index].Realized ? _entries[index].Control : null;

    /// <summary>
    /// 请求物化指定块（偏移映射等需要精确 Bounds 的场景），返回是否已物化。
    /// </summary>
    public bool RealizeBlock(int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return false;
        }

        var entry = _entries[index];
        if (entry.Realized)
        {
            return true;
        }

        entry.Realized = true;
        Children.Insert(RealizedIndexBefore(index), entry.Control);
        InvalidateMeasure();
        return true;
    }

    /// <summary>取块在文档坐标中的顶边（未物化的块用已测量/估算高度推算）。</summary>
    public bool TryGetBlockTop(int index, out double top)
    {
        top = 0;
        if (index < 0 || index >= _entries.Count)
        {
            return false;
        }

        for (var i = 0; i < index; i++)
        {
            top += ResolveHeight(_entries[i]) + Spacing;
        }

        return true;
    }

    /// <summary>块高度：已量测优先，其次估算。</summary>
    public double GetBlockHeight(int index) =>
        index >= 0 && index < _entries.Count ? ResolveHeight(_entries[index]) : 0d;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : _lastWidth;
        if (!double.IsFinite(width))
        {
            width = 0;
        }

        var virtualizing = IsVirtualizing;
        var (windowTop, windowBottom) = ResolveVisibleWindow(virtualizing, availableSize);
        var realization = MarkdownVirtualizationWindow.ResolveRealization(
            FillHeightBuffer(), FillPinnedBuffer(), Spacing, windowTop, windowBottom);

        double y = 0;
        var maxWidth = 0d;
        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            SetRealized(entry, realization[i]);

            if (entry.Realized)
            {
                entry.Control.Measure(new Size(width, double.PositiveInfinity));
                var desired = entry.Control.DesiredSize;
                entry.Height = desired.Height;
                entry.Measured = true;
                maxWidth = Math.Max(maxWidth, desired.Width);
            }

            y += ResolveHeight(entry) + Spacing;
        }

        if (_entries.Count > 0)
        {
            y -= Spacing;
        }

        _lastWidth = width;
        return new Size(maxWidth, Math.Max(0, y));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double y = 0;
        var childIndex = 0;
        foreach (var entry in _entries)
        {
            if (entry.Realized)
            {
                if (childIndex < Children.Count)
                {
                    var height = entry.Measured ? entry.Height : entry.Control.DesiredSize.Height;
                    Children[childIndex].Arrange(new Rect(0, y, finalSize.Width, Math.Max(0, height)));
                }

                childIndex++;
            }

            y += ResolveHeight(entry) + Spacing;
        }

        return finalSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeScrollHost();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeScrollHost();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollHostProperty)
        {
            SubscribeScrollHost();
        }
    }

    private void SubscribeScrollHost()
    {
        var host = ScrollHost ?? this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
        if (ReferenceEquals(host, _subscribedScrollHost))
        {
            return;
        }

        UnsubscribeScrollHost();
        _subscribedScrollHost = host;
        if (host is not null)
        {
            host.ScrollChanged += OnScrollHostChanged;
        }
    }

    private void UnsubscribeScrollHost()
    {
        if (_subscribedScrollHost is not null)
        {
            _subscribedScrollHost.ScrollChanged -= OnScrollHostChanged;
            _subscribedScrollHost = null;
        }
    }

    private void OnScrollHostChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (IsVirtualizing)
        {
            InvalidateMeasure();
        }
    }

    private double[] FillHeightBuffer()
    {
        if (_heightBuffer.Length != _entries.Count)
        {
            _heightBuffer = new double[_entries.Count];
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            _heightBuffer[i] = ResolveHeight(_entries[i]);
        }

        return _heightBuffer;
    }

    private bool[] FillPinnedBuffer()
    {
        if (_pinnedBuffer.Length != _entries.Count)
        {
            _pinnedBuffer = new bool[_entries.Count];
        }

        for (var i = 0; i < _entries.Count; i++)
        {
            _pinnedBuffer[i] = _entries[i].Pinned;
        }

        return _pinnedBuffer;
    }

    private (double Top, double Bottom) ResolveVisibleWindow(bool virtualizing, Size availableSize)
    {
        if (!virtualizing)
        {
            return (double.NegativeInfinity, double.PositiveInfinity);
        }

        var host = _subscribedScrollHost ?? ScrollHost;
        if (host is null)
        {
            return (double.NegativeInfinity, double.PositiveInfinity);
        }

        var viewportHeight = host.Viewport.Height;
        if (!double.IsFinite(viewportHeight) || viewportHeight <= 0)
        {
            viewportHeight = double.IsFinite(availableSize.Height) && availableSize.Height > 0
                ? availableSize.Height
                : 720;
        }

        var margin = Math.Max(0, OverscanViewports) * viewportHeight;
        return (host.Offset.Y - margin, host.Offset.Y + viewportHeight + margin);
    }

    private void SetRealized(BlockEntry entry, bool realized)
    {
        if (entry.Realized == realized)
        {
            return;
        }

        if (realized)
        {
            entry.Realized = true;
            Children.Insert(RealizedIndexBefore(_entries.IndexOf(entry)), entry.Control);
        }
        else
        {
            if (entry.Control.Bounds.Height > 0)
            {
                entry.Height = entry.Control.Bounds.Height;
                entry.Measured = true;
            }

            Children.Remove(entry.Control);
            entry.Realized = false;
        }
    }

    private int RealizedIndexBefore(int entryIndex)
    {
        var count = 0;
        for (var i = 0; i < entryIndex && i < _entries.Count; i++)
        {
            if (_entries[i].Realized)
            {
                count++;
            }
        }

        return count;
    }

    private double ResolveHeight(BlockEntry entry) => entry.Measured ? entry.Height : Math.Max(0, EstimatedBlockHeight);
}
