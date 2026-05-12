using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
	private long _loadVersion;
	private CancellationTokenSource? _loadCts;

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
		_bitmap = null;
		SetContent(null);
		oldBitmap?.Dispose();
	}

	private async Task LoadAsync(string source, long version, CancellationTokenSource loadCts)
	{
		var token = loadCts.Token;

		try
		{
			var bytes = await LoadBytesAsync(source, token);
			token.ThrowIfCancellationRequested();

			await using var stream = new MemoryStream(bytes);
			var bitmap = new Bitmap(stream);
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				if (version != _loadVersion || token.IsCancellationRequested)
				{
					bitmap.Dispose();
					return;
				}

				var oldBitmap = _bitmap;
				_bitmap = bitmap;
				SetContent(CreateBitmapContent(bitmap));
				oldBitmap?.Dispose();
			});
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
			await ShowFallbackAsync(version, AltText ?? source);
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

	private static async Task<byte[]> LoadBytesAsync(string source, CancellationToken token)
	{
		if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			if (uri.Scheme is "http" or "https")
			{
				return await HttpClient.GetByteArrayAsync(uri, token);
			}

			if (uri.IsFile)
			{
				return await File.ReadAllBytesAsync(uri.LocalPath, token);
			}
		}

		var path = Path.IsPathRooted(source)
			? source
			: Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, source.Replace('/', Path.DirectorySeparatorChar)));
		return await File.ReadAllBytesAsync(path, token);
	}

	private Control CreateBitmapContent(Bitmap bitmap)
	{
		var image = new Image
		{
			Source = bitmap,
			Stretch = Stretch.Uniform,
			HorizontalAlignment = HorizontalAlignment.Left,
			MaxWidth = DefaultMaxImageWidth,
			MaxHeight = DefaultMaxImageHeight
		};
		image.Classes.Add(MarkdownStyleKeys.ImageContent);

		var (width, height) = CalculateDisplaySize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
		image.Width = width;
		image.Height = height;
		return image;
	}

	private (double Width, double Height) CalculateDisplaySize(double imageWidth, double imageHeight)
	{
		imageWidth = Math.Max(1, imageWidth);
		imageHeight = Math.Max(1, imageHeight);

		var maxWidth = double.IsNaN(MaxWidth) || double.IsInfinity(MaxWidth) || MaxWidth <= 0
			? DefaultMaxImageWidth
			: MaxWidth;
		var maxHeight = double.IsNaN(MaxHeight) || double.IsInfinity(MaxHeight) || MaxHeight <= 0
			? DefaultMaxImageHeight
			: MaxHeight;
		var maxScale = Math.Min(1, Math.Min(maxWidth / imageWidth, maxHeight / imageHeight));
		return (imageWidth * maxScale, imageHeight * maxScale);
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
			_bitmap = null;

			var fallbackText = new TextBlock
			{
				Text = text,
				TextWrapping = TextWrapping.Wrap
			};
			fallbackText.Classes.Add(MarkdownStyleKeys.ImageFallbackText);

			var fallback = new Border
			{
				Child = fallbackText
			};
			fallback.Classes.Add(MarkdownStyleKeys.ImageFallback);
			SetContent(fallback);
			oldBitmap?.Dispose();
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
