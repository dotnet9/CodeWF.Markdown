using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CodeWF.Markdown.Rendering;
using CodeWF.Markdown.Shared.Rendering;

namespace CodeWF.Markdown.Controls;

/// <summary>
/// MarkdownViewer 的能力 API 部分：后台解析调度、阅读位置存取、远程图片刷新与
/// 任务列表勾选回写。宿主通过这些公共 API 替代自行实现（反射探测滚动位置、
/// URL 追参强制刷新、源码正则改写等）。
/// </summary>
public partial class MarkdownViewer
{
    // 渲染调度：文本变更只登记快照，合并窗口后在后台线程解析，过期结果丢弃。
    private readonly MarkdownRenderScheduler _scheduler;
    private MarkdownRenderMode _lastRenderMode = MarkdownRenderMode.Full;
    private TimeSpan _lastParseDuration;
    private int _taskMarkerOffset = -1;

    /// <summary>
    /// 指定块控件当前是否已物化（虚拟化宿主下未进入视口的块不挂载控件）；
    /// 宿主可用它区分「未物化」与「不存在」。
    /// </summary>
    public bool IsBlockRealized(int blockIndex) =>
        _virtualizingHost is null || _virtualizingHost.GetRealizedControl(blockIndex) is not null;

    /// <summary>
    /// 已物化的块数量；未启用虚拟化时等于块总数，供诊断与测试使用。
    /// </summary>
    public int RealizedBlockCount => _virtualizingHost?.RealizedBlockCount ?? _renderedBlocks.Count;

    /// <summary>
    /// 阅读位置快照：已渲染块序号与本块内的相对进度（0-1）。
    /// 块序号在文档重排后仍可定位，避免用像素值导致跳位。
    /// </summary>
    public readonly record struct MarkdownReadingPosition(int BlockIndex, double BlockRatio, double DocumentRatio);

    /// <summary>
    /// 服务该 Viewer 的滚动宿主；未显式指定时自动向上查找 <see cref="ScrollViewer"/>。
    /// </summary>
    public ScrollViewer? ScrollHost { get; set; }

    /// <summary>
    /// 任务列表勾选回写事件：宿主将 <see cref="MarkdownTaskWriteResult.Markdown"/>
    /// 按变更区间应用到编辑器，并保持撤销栈。
    /// </summary>
    public event EventHandler<MarkdownTaskWriteResult>? TaskWriteBackRequested;

    /// <summary>
    /// 最近一次渲染采用的模式（增量 / 全量），供宿主展示与诊断。
    /// </summary>
    public MarkdownRenderMode LastRenderMode => _lastRenderMode;

    /// <summary>
    /// 最近一次解析耗时，供宿主状态栏与性能面板展示。
    /// </summary>
    public TimeSpan LastParseDuration => _lastParseDuration;

    /// <summary>
    /// 基于当前 Markdown 的文档模型（含大纲、统计等增量解析入口）；
    /// 后台解析结果尚未到达时返回最近一次渲染使用的模型。
    /// </summary>
    public MarkdownDocumentModel CurrentModel => _renderedModel;

    /// <summary>
    /// 采集当前阅读位置；文档未渲染或滚动宿主不可用时返回 null。
    /// </summary>
    public MarkdownReadingPosition? SaveReadingPosition()
    {
        if (_renderedBlocks.Count == 0 || _documentHost is null)
        {
            return null;
        }

        var host = ResolveScrollHost();
        if (host is null)
        {
            return new MarkdownReadingPosition(0, 0, 0);
        }

        var offset = host.Offset.Y;
        var viewportHeight = Math.Max(1, host.Viewport.Height);
        var documentHeight = Math.Max(viewportHeight, host.Extent.Height);
        var probeOffset = offset + viewportHeight / 2;

        for (var i = 0; i < _renderedBlocks.Count; i++)
        {
            var control = _renderedBlocks[i].Control;
            if (control.TranslatePoint(new Point(0, 0), _documentHost) is not { } origin)
            {
                continue;
            }

            var top = origin.Y - offset;
            var height = Math.Max(1, control.Bounds.Height);
            if (probeOffset > top + height)
            {
                continue;
            }

            var ratioInBlock = Math.Clamp((probeOffset - top) / height, 0d, 1d);
            var documentRatio = Math.Clamp(probeOffset / documentHeight, 0d, 1d);
            return new MarkdownReadingPosition(i, ratioInBlock, documentRatio);
        }

        var lastIndex = _renderedBlocks.Count - 1;
        return new MarkdownReadingPosition(lastIndex, 1, Math.Clamp(probeOffset / documentHeight, 0d, 1d));
    }

    /// <summary>
    /// 恢复到指定阅读位置；定位块尚未布局时返回 false，宿主可在布局完成后重试。
    /// </summary>
    public bool RestoreReadingPosition(MarkdownReadingPosition? position)
    {
        if (position is not { } value || _renderedBlocks.Count == 0 || _documentHost is null)
        {
            return false;
        }

        var host = ResolveScrollHost();
        if (host is null || host.Extent.Height <= 0)
        {
            return false;
        }

        var index = Math.Clamp(value.BlockIndex, 0, _renderedBlocks.Count - 1);
        if (_renderedBlocks[index].Control.TranslatePoint(new Point(0, 0), _documentHost) is not { } origin)
        {
            return false;
        }

        var viewportHeight = Math.Max(1, host.Viewport.Height);
        var maxOffset = Math.Max(0, host.Extent.Height - viewportHeight);
        var target = origin.Y + _renderedBlocks[index].Control.Bounds.Height * value.BlockRatio - viewportHeight / 2;
        host.Offset = new Vector(host.Offset.X, Math.Clamp(target, 0, maxOffset));
        return true;
    }

    /// <summary>
    /// 失效远程图片缓存并重建受影响的图片块；本地文件与 data URI 保持不变。
    /// </summary>
    public void RefreshRemoteImages()
    {
        if (!TryRefreshRemoteImageBlocks())
        {
            QueueRenderDocument(MarkdownRenderMode.Full);
        }
    }

    /// <summary>
    /// 取用最近一次任务勾选回写对应的源码偏移（无待处理时返回 -1）。
    /// </summary>
    public int ConsumePendingTaskWriteBackOffset()
    {
        var offset = _taskMarkerOffset;
        _taskMarkerOffset = -1;
        return offset;
    }

    private MarkdownDocumentModel ParseMarkdownWithTiming(string text)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var model = MarkdownParser.Parse(text, Pipeline);
        _lastParseDuration = System.Diagnostics.Stopwatch.GetElapsedTime(start);
        return model;
    }

    /// <summary>
    /// 属性变更后的渲染调度：全量模式立即重建；增量模式只登记快照，
    /// 由合并窗口与后台解析在结果到达时再做局部替换。
    /// </summary>
    private void ScheduleRender(MarkdownRenderMode mode)
    {
        if (_documentHost is null)
        {
            return;
        }

        var text = Markdown ?? string.Empty;
        if (mode == MarkdownRenderMode.Full)
        {
            RenderDocumentFull(text);
            return;
        }

        if (string.Equals(text, _renderedMarkdown, StringComparison.Ordinal))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text) || _renderedBlocks.Count == 0)
        {
            RenderDocumentFull(text);
            return;
        }

        _scheduler.Queue(text);
    }

    private void OnSchedulerParseCompleted(object? sender, MarkdownParseResult result)
    {
        if (_documentHost is null)
        {
            return;
        }

        if (!string.Equals(result.Snapshot.Text, Markdown ?? string.Empty, StringComparison.Ordinal))
        {
            // 解析期间文本又变了：丢弃过期结果，等待下一次合并窗口。
            return;
        }

        _lastParseDuration = result.Duration;
        var dirtySpan = result.DirtySpanValid ? result.DirtySpan : (MarkdownTextSpan?)null;
        if (!result.IsFirstParse && TryRenderIncremental(result.Snapshot.Text, result.Model, dirtySpan))
        {
            return;
        }

        RenderDocumentFull(result.Snapshot.Text, result.Model);
    }

    private void OnSchedulerParseFailed(object? sender, Exception exception)
    {
        // 解析失败保持上一次渲染结果，不抛出到 UI 线程。
        System.Diagnostics.Debug.WriteLine($"Markdown parse failed: {exception}");
    }

    private ScrollViewer? ResolveScrollHost()
    {
        if (ScrollHost is not null)
        {
            return ScrollHost;
        }

        return this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
    }

    private bool TryGetSourceOffsetBoundsNow(int sourceOffset, out Rect bounds)
    {
        bounds = default;
        if (_documentHost is null || _renderedBlocks.Count == 0)
        {
            return false;
        }

        var renderedBlock = FindRenderedBlockBySourceOffset(sourceOffset);
        if (renderedBlock is null)
        {
            return false;
        }

        // 虚拟化宿主下目标块可能未物化：先物化再量测，保证偏移映射给出精确 Bounds。
        _virtualizingHost?.RealizeBlock(_renderedBlocks.IndexOf(renderedBlock));

        if (renderedBlock.Control.TranslatePoint(new Point(0, 0), this) is not { } topLeft)
        {
            return false;
        }

        bounds = new Rect(topLeft, renderedBlock.Control.Bounds.Size);
        return true;
    }

    private void OnTaskMarkerClick(CheckBox checkBox, bool isChecked)
    {
        if (checkBox.Tag is not int markerStart)
        {
            return;
        }

        var result = MarkdownTaskListWriter.SetTaskState(Markdown, markerStart, isChecked);
        if (result is null)
        {
            return;
        }

        _taskMarkerOffset = markerStart;
        TaskWriteBackRequested?.Invoke(this, result);
    }

    private void AttachTaskMarkerInteraction(CheckBox checkBox, int markerStart)
    {
        checkBox.Tag = markerStart;
        checkBox.IsHitTestVisible = true;
        checkBox.Click += OnTaskMarkerClickHandler;
        _currentBlockDisposables.Add(new TaskMarkerBinding(this, checkBox));
    }

    private void OnTaskMarkerClickHandler(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            OnTaskMarkerClick(checkBox, checkBox.IsChecked == true);
        }
    }

    private static bool IsTaskMarkerInteractive(bool isTask, bool allowInteractiveTasks)
    {
        return isTask && allowInteractiveTasks;
    }

    bool Rendering.IMarkdownRenderContext.TaskMarkersInteractive => true;

    void Rendering.IMarkdownRenderContext.AttachTaskMarkerInteraction(CheckBox checkBox, int markerStart) =>
        AttachTaskMarkerInteraction(checkBox, markerStart);

    private bool TryRefreshRemoteImageBlocks()
    {
        if (_documentHost is null || _renderedBlocks.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < _renderedBlocks.Count; i++)
        {
            var block = _renderedBlocks[i];
            if (!block.HasRemoteImage)
            {
                continue;
            }

            if (i >= _renderedModel.Blocks.Count)
            {
                return false;
            }

            var modelBlock = _renderedModel.Blocks[i];
            var control = ConvertBlock(modelBlock.SyntaxBlock, _renderedMarkdown);
            if (control is null)
            {
                return false;
            }

            var blockDisposables = ExtractCurrentBlockDisposables(_currentBlockDisposables.Count);
            var replacement = RenderedBlock.FromModel(
                modelBlock,
                control,
                blockDisposables,
                ContainsRemoteImage(modelBlock),
                TryGetTaskMarkerOffset(_renderedMarkdown, modelBlock));
            block.Cleanup();
            ReplaceBlockControl(i, control, block.Kind);
            _renderedBlocks[i] = replacement;
        }

        ResetSelectionState();
        RefreshSelectionBlocks();
        InvalidateDocumentLayout();
        return true;
    }

    private static int TryGetTaskMarkerOffset(string? markdown, MarkdownDocumentBlock modelBlock)
    {
        return MarkdownTaskListWriter.TryLocateTaskMarker(
            markdown,
            modelBlock.SourceSpan.Start,
            modelBlock.SourceSpan.End,
            out var markerStart,
            out _)
            ? markerStart
            : -1;
    }

    /// <summary>
    /// 块内是否包含远程图片：命中时 <see cref="RefreshRemoteImages"/> 会重建该块。
    /// 近似判定（"![" 与 http(s) 同现）只会导致多做一次重建，不会漏刷新。
    /// </summary>
    internal static bool ContainsRemoteImage(MarkdownDocumentBlock block)
    {
        var source = block.SourceText;
        if (source.Length == 0 || !source.Contains("![", StringComparison.Ordinal))
        {
            return false;
        }

        return source.Contains("http://", StringComparison.OrdinalIgnoreCase)
               || source.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TaskMarkerBinding : IDisposable
    {
        private readonly MarkdownViewer _viewer;
        private readonly CheckBox _checkBox;

        public TaskMarkerBinding(MarkdownViewer viewer, CheckBox checkBox)
        {
            _viewer = viewer;
            _checkBox = checkBox;
        }

        public void Dispose()
        {
            _checkBox.Click -= _viewer.OnTaskMarkerClickHandler;
            _checkBox.Tag = null;
        }
    }
}
