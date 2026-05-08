using System.Net.Http;
using System.Text;
using System.Threading;

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

namespace CodeWF.Markdown.Lite.Controls;

[TemplatePart(ContentHostPartName, typeof(ContentControl), IsRequired = true)]
public class MarkdownImage : TemplatedControl
{
    private const string ContentHostPartName = "PART_ContentHost";
    private const double DefaultMaxImageWidth = 900;
    private const double DefaultMaxImageHeight = 520;

    private static readonly HttpClient HttpClient = new();

    private ContentControl? _contentHost;
    private Bitmap? _bitmap;
    private byte[]? _imageBytes;
    private string? _fileName;
    private long _loadVersion;
    private CancellationTokenSource? _loadCts;
    private Point? _pressedPoint;
    private bool _isPointerDragging;

    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<MarkdownImage, string?>(nameof(Source));

    public static readonly StyledProperty<string?> AltTextProperty =
        AvaloniaProperty.Register<MarkdownImage, string?>(nameof(AltText));

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

    static MarkdownImage()
    {
        SourceProperty.Changed.AddClassHandler<MarkdownImage>((image, _) => image.QueueLoad());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentHost = e.NameScope.Find<ContentControl>(ContentHostPartName);
        QueueLoad();
    }

    private void QueueLoad()
    {
        var source = Source?.Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            _loadCts?.Cancel();
            if (_bitmap is not null)
            {
                _bitmap.Dispose();
                _bitmap = null;
            }
            _imageBytes = null;
            _fileName = null;
            SetContent(null);
            return;
        }

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var version = Interlocked.Increment(ref _loadVersion);
        _ = LoadAsync(source, version, token);
    }

    private async Task LoadAsync(string source, long version, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var loadResult = await LoadBytesAsync(source, token);
            token.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(loadResult.Bytes);
            var bitmap = new Bitmap(stream);
            var fileName = loadResult.FileName;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (version != _loadVersion || token.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    return;
                }

                var oldBitmap = _bitmap;
                _bitmap = bitmap;
                _imageBytes = loadResult.Bytes;
                _fileName = fileName;
                oldBitmap?.Dispose();

                SetContent(CreateBitmapContent(bitmap));
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (FileNotFoundException ex)
        {
            await ShowFallbackAsync(
                version,
                string.Format("文件不存在：{0}", ex.FileName ?? source));
        }
        catch
        {
            await ShowFallbackAsync(
                version,
                string.Format("图片加载失败：{0}", AltText ?? source));
        }
    }

    private async Task<ImageLoadResult> LoadBytesAsync(string source, CancellationToken token)
    {
        if (TryReadDataUri(source, out var dataUriBytes))
        {
            return new ImageLoadResult(
                dataUriBytes,
                ResolveFileName(source));
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme is "http" or "https")
            {
                using var response = await HttpClient.GetAsync(uri, token);
                response.EnsureSuccessStatusCode();
                token.ThrowIfCancellationRequested();
                var bytes = await response.Content.ReadAsByteArrayAsync(token);
                return new ImageLoadResult(
                    bytes,
                    ResolveFileName(source));
            }

            if (uri.IsFile)
            {
                return await LoadLocalBytesAsync(new LocalImagePath(uri.LocalPath, uri.LocalPath));
            }
        }

        return await LoadLocalBytesAsync(ResolveLocalPath(source));
    }

    private static async Task<ImageLoadResult> LoadLocalBytesAsync(LocalImagePath localPath)
    {
        if (!File.Exists(localPath.Path))
        {
            throw new FileNotFoundException("Markdown image file was not found.", localPath.DisplayPath);
        }

        var bytes = await File.ReadAllBytesAsync(localPath.Path);
        return new ImageLoadResult(bytes, ResolveFileName(localPath.Path));
    }

    private static LocalImagePath ResolveLocalPath(string source)
    {
        if (Path.IsPathRooted(source))
        {
            return new LocalImagePath(Path.GetFullPath(source), source);
        }

        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, source.Replace('/', Path.DirectorySeparatorChar)));
        return new LocalImagePath(path, path);
    }

    private static bool TryReadDataUri(string source, out byte[] bytes)
    {
        bytes = [];

        if (!source.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = source.IndexOf(',');
        if (commaIndex < 0)
        {
            return false;
        }

        var metadata = source[..commaIndex];
        var payload = source[(commaIndex + 1)..];
        bytes = metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase)
            ? Convert.FromBase64String(payload)
            : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
        return true;
    }

    private static bool IsDataUri(string source)
    {
        return source.StartsWith("data:image", StringComparison.OrdinalIgnoreCase);
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

        var title = !string.IsNullOrWhiteSpace(AltText) ? AltText : Source;
        var window = new MarkdownImagePreviewWindow(
            _bitmap,
            _imageBytes,
            _fileName ?? "markdown-image.png",
            title);
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    private static string ResolveFileName(string source)
    {
        var fileName = "markdown-image";
        if (IsDataUri(source))
        {
            fileName += ResolveDataUriExtension(source);
        }
        else if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            fileName = Path.GetFileName(uri.IsFile ? uri.LocalPath : uri.LocalPath);
        }
        else
        {
            fileName = Path.GetFileName(source);
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "markdown-image.png";
        }
        else if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
        {
            fileName += ".png";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
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

    private static string ResolveDataUriExtension(string source)
    {
        var separatorIndex = source.IndexOf(';');
        if (separatorIndex <= "data:image/".Length)
        {
            return ".png";
        }

        return source["data:image/".Length..separatorIndex].ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "webp" => ".webp",
            "bmp" => ".bmp",
            "gif" => ".gif",
            _ => ".png"
        };
    }

    private async Task ShowFallbackAsync(long version, string text)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (version != _loadVersion)
            {
                return;
            }

            _bitmap = null;
            _imageBytes = null;
            _fileName = null;

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
        });
    }

    private sealed record ImageLoadResult(byte[] Bytes, string FileName);

    private sealed record LocalImagePath(string Path, string DisplayPath);

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
