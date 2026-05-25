using System.Threading;

using AnimatedImage.Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Lang.Avalonia;

namespace CodeWF.Markdown.Controls;

[TemplatePart(ContentHostPartName, typeof(ContentControl), IsRequired = true)]
public class MarkdownImage : TemplatedControl
{
    private const string ContentHostPartName = "PART_ContentHost";
    private const double DefaultMaxImageWidth = 900;
    private const double DefaultMaxImageHeight = 520;
    private const int MaxImageByteCacheSize = 64;

    private static readonly Dictionary<string, MarkdownImageSource> ImageByteCache = new(StringComparer.Ordinal);
    private static readonly Queue<string> ImageByteCacheOrder = new();
    private static readonly object ImageByteCacheGate = new();

    private ContentControl? _contentHost;
    private Bitmap? _bitmap;
    private MemoryStream? _animatedStream;
    private byte[]? _imageBytes;
    private string? _fileName;
    private bool _isSvg;
    private bool _isGif;
    private long _loadVersion;
    private CancellationTokenSource? _loadCts;
    private Point? _pressedPoint;
    private bool _isPointerDragging;

    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<MarkdownImage, string?>(nameof(Source));

    public static readonly StyledProperty<string?> AltTextProperty =
        AvaloniaProperty.Register<MarkdownImage, string?>(nameof(AltText));

    public static readonly StyledProperty<string?> ImageBasePathProperty =
        AvaloniaProperty.Register<MarkdownImage, string?>(nameof(ImageBasePath));

    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string? AltText
    {
        get => GetValue(AltTextProperty);
        set => SetValue(AltTextProperty, value);
    }

    public string? ImageBasePath
    {
        get => GetValue(ImageBasePathProperty);
        set => SetValue(ImageBasePathProperty, value);
    }

    static MarkdownImage()
    {
        SourceProperty.Changed.AddClassHandler<MarkdownImage>((image, _) => image.QueueLoad());
        ImageBasePathProperty.Changed.AddClassHandler<MarkdownImage>((image, _) => image.QueueLoad());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentHost = e.NameScope.Find<ContentControl>(ContentHostPartName);
        QueueLoad();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Interlocked.Increment(ref _loadVersion);
        CancelCurrentLoad();
        ClearImageState();
        base.OnDetachedFromVisualTree(e);
    }

    private void QueueLoad()
    {
        var source = Source?.Trim();
        var version = Interlocked.Increment(ref _loadVersion);
        CancelCurrentLoad();

        if (string.IsNullOrWhiteSpace(source))
        {
            ClearImageState();
            return;
        }

        var loadCts = new CancellationTokenSource();
        _loadCts = loadCts;
        _ = LoadAsync(source, version, loadCts);
    }

    private void CancelCurrentLoad()
    {
        var loadCts = _loadCts;
        _loadCts = null;
        loadCts?.Cancel();
    }

    private void ClearImageState()
    {
        var oldBitmap = _bitmap;
        var oldAnimatedStream = _animatedStream;
        _bitmap = null;
        _animatedStream = null;
        _imageBytes = null;
        _fileName = null;
        _isSvg = false;
        _isGif = false;
        _pressedPoint = null;
        _isPointerDragging = false;

        SetContent(null);
        oldBitmap?.Dispose();
        oldAnimatedStream?.Dispose();
    }

    private async Task LoadAsync(string source, long version, CancellationTokenSource loadCts)
    {
        var token = loadCts.Token;

        try
        {
            token.ThrowIfCancellationRequested();
            var loadResult = await LoadBytesAsync(source, token);
            token.ThrowIfCancellationRequested();
            var previewBytes = loadResult.IsSvg || loadResult.IsGif
                ? MarkdownImageRasterizer.RenderToPngBytes(loadResult)
                : loadResult.Bytes;
            token.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(previewBytes);
            var bitmap = new Bitmap(stream);
            var fileName = loadResult.FileName;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (version != _loadVersion || token.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    return;
                }

                MemoryStream? animatedStream = null;
                var content = loadResult.IsSvg
                    ? CreateSvgContent(bitmap)
                    : loadResult.IsGif
                        ? CreateAnimatedGifContent(loadResult.Bytes, bitmap, out animatedStream)
                        : CreateBitmapContent(bitmap);

                var oldBitmap = _bitmap;
                var oldAnimatedStream = _animatedStream;
                _bitmap = bitmap;
                _animatedStream = animatedStream;
                _imageBytes = loadResult.Bytes;
                _fileName = fileName;
                _isSvg = loadResult.IsSvg;
                _isGif = loadResult.IsGif;

                SetContent(content);
                oldBitmap?.Dispose();
                oldAnimatedStream?.Dispose();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (FileNotFoundException ex)
        {
            ;
            await ShowFallbackAsync(
                version,
                string.Format(I18nManager.Instance.GetResource(CodeWF.MarkdownL.ImageFileNotFound), ex.FileName ?? source));
        }
        catch
        {
            await ShowFallbackAsync(
                version,
                string.Format(I18nManager.Instance.GetResource(CodeWF.MarkdownL.ImageLoadFailed), AltText ?? source));
        }
        finally
        {
            if (ReferenceEquals(_loadCts, loadCts))
            {
                _loadCts = null;
            }

            loadCts.Dispose();
        }
    }

    private async Task<MarkdownImageSource> LoadBytesAsync(string source, CancellationToken token)
    {
        var cacheKey = MarkdownImageSourceLoader.CreateCacheKey(source, ImageBasePath);
        if (TryGetCachedImageBytes(cacheKey, out var cached))
        {
            return cached;
        }

        var result = await MarkdownImageSourceLoader.LoadAsync(source, ImageBasePath, token);
        AddImageBytesToCache(cacheKey, result);
        return result;
    }

    private static bool TryGetCachedImageBytes(string source, out MarkdownImageSource result)
    {
        lock (ImageByteCacheGate)
        {
            return ImageByteCache.TryGetValue(source, out result!);
        }
    }

    private static void AddImageBytesToCache(string source, MarkdownImageSource result)
    {
        lock (ImageByteCacheGate)
        {
            if (ImageByteCache.ContainsKey(source))
            {
                ImageByteCache[source] = result;
                return;
            }

            if (ImageByteCache.Count >= MaxImageByteCacheSize)
            {
                var oldest = ImageByteCacheOrder.Dequeue();
                ImageByteCache.Remove(oldest);
            }

            ImageByteCache[source] = result;
            ImageByteCacheOrder.Enqueue(source);
        }
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        _pressedPoint = e.GetPosition(this);
        _isPointerDragging = false;
    }

    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedPoint is not { } pressedPoint)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        if (Math.Abs(currentPoint.X - pressedPoint.X) > 4 || Math.Abs(currentPoint.Y - pressedPoint.Y) > 4)
        {
            _isPointerDragging = true;
        }
    }

    private void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var shouldOpen = e.InitialPressMouseButton == MouseButton.Left && !_isPointerDragging;
        _pressedPoint = null;
        _isPointerDragging = false;

        if (shouldOpen)
        {
            OpenPreviewWindow();
            e.Handled = true;
        }
    }

    private void OpenPreviewWindow()
    {
        if (_bitmap is null || _imageBytes is null)
        {
            return;
        }

        Bitmap previewBitmap;
        try
        {
            var previewBytes = _isSvg || _isGif
                ? MarkdownImageRasterizer.RenderToPngBytes(new MarkdownImageSource(
                    _imageBytes,
                    _fileName ?? "markdown-image.png",
                    _isSvg,
                    _isGif,
                    null))
                : _imageBytes;
            using var previewStream = new MemoryStream(previewBytes);
            previewBitmap = new Bitmap(previewStream);
        }
        catch
        {
            return;
        }

        var title = !string.IsNullOrWhiteSpace(AltText) ? AltText : Source;
        var window = new MarkdownImagePreviewWindow(
            previewBitmap,
            _imageBytes,
            _fileName ?? (_isSvg ? "markdown-image.svg" : _isGif ? "markdown-image.gif" : "markdown-image.png"),
            title,
            _isSvg,
            _isGif);
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    private Control CreateBitmapContent(Bitmap bitmap)
    {
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ApplyImageChrome(image, bitmap);
        AttachImageClick(image);
        return image;
    }

    private Control CreateSvgContent(Bitmap previewBitmap)
    {
        return CreateBitmapContent(previewBitmap);
    }

    private Control CreateAnimatedGifContent(byte[] gifBytes, Bitmap previewBitmap, out MemoryStream animatedStream)
    {
        animatedStream = new MemoryStream(gifBytes, writable: false);
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ImageBehavior.SetAnimatedSource(image, new AnimatedImageSourceStream(animatedStream));
        ImageBehavior.SetRepeatBehavior(image, RepeatBehavior.Forever);
        ApplyImageChrome(image, previewBitmap);
        AttachImageClick(image);
        return image;
    }

    private void ApplyImageChrome(Control control, Bitmap bitmap)
    {
        control.Classes.Add(MarkdownStyleKeys.ImageContent);
        control.MaxWidth = DefaultMaxImageWidth;
        control.MaxHeight = DefaultMaxImageHeight;
        control.Margin = new Thickness(0, 6, 0, 10);

        var (width, height) = CalculateDisplaySize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        control.Width = width;
        control.Height = height;
    }

    private (double Width, double Height) CalculateDisplaySize(double imageWidth, double imageHeight)
    {
        imageWidth = Math.Max(1, imageWidth);
        imageHeight = Math.Max(1, imageHeight);

        if (!double.IsNaN(Width) && Width > 0 && !double.IsNaN(Height) && Height > 0)
        {
            var scale = Math.Min(Width / imageWidth, Height / imageHeight);
            return (imageWidth * scale, imageHeight * scale);
        }

        if (!double.IsNaN(Width) && Width > 0)
        {
            var scale = Width / imageWidth;
            return (Width, imageHeight * scale);
        }

        if (!double.IsNaN(Height) && Height > 0)
        {
            var scale = Height / imageHeight;
            return (imageWidth * scale, Height);
        }

        var maxWidth = double.IsNaN(MaxWidth) || double.IsInfinity(MaxWidth) || MaxWidth <= 0
            ? DefaultMaxImageWidth
            : MaxWidth;
        var maxHeight = double.IsNaN(MaxHeight) || double.IsInfinity(MaxHeight) || MaxHeight <= 0
            ? DefaultMaxImageHeight
            : MaxHeight;
        var maxScale = Math.Min(1, Math.Min(maxWidth / imageWidth, maxHeight / imageHeight));
        return (imageWidth * maxScale, imageHeight * maxScale);
    }

    private void AttachImageClick(Control control)
    {
        control.AddHandler(
            InputElement.PointerPressedEvent,
            OnImagePointerPressed,
            RoutingStrategies.Bubble);
        control.AddHandler(
            InputElement.PointerMovedEvent,
            OnImagePointerMoved,
            RoutingStrategies.Bubble);
        control.AddHandler(
            InputElement.PointerReleasedEvent,
            OnImagePointerReleased,
            RoutingStrategies.Bubble);
    }

    private async Task ShowFallbackAsync(long version, string text)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (version != _loadVersion)
            {
                return;
            }

            var oldBitmap = _bitmap;
            var oldAnimatedStream = _animatedStream;
            _bitmap = null;
            _animatedStream = null;
            _imageBytes = null;
            _fileName = null;
            _isSvg = false;
            _isGif = false;

            var fallback = new Border
            {
                Child = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            fallback.Classes.Add(MarkdownStyleKeys.ImageFallback);
            if (fallback.Child is TextBlock textBlock)
            {
                textBlock.Classes.Add(MarkdownStyleKeys.ImageFallbackText);
            }

            SetContent(fallback);
            oldBitmap?.Dispose();
            oldAnimatedStream?.Dispose();
        });
    }

    private void SetContent(Control? content)
    {
        if (_contentHost is not null)
        {
            _contentHost.Content = content;
            _contentHost.InvalidateMeasure();
        }

        InvalidateMeasure();
        InvalidateArrange();
    }
}
