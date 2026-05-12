using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Styling;

using CodeWF.Markdown.Lite.Themes;

namespace CodeWF.Markdown.Lite.Sample.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
	private MarkdownSampleFile? _selectedFile;
	private MarkdownTypographyTheme? _selectedTypographyTheme;
	private ThemeVariantOption? _selectedThemeVariant;
	private bool _isCompactLayout;
	private string _markdown = string.Empty;
	private TypographyThemeChoice? _firstViewerSelectedTypographyTheme;
	private CompactLayoutChoice? _firstViewerSelectedCompactLayout;
	private TypographyThemeChoice? _secondViewerSelectedTypographyTheme;
	private CompactLayoutChoice? _secondViewerSelectedCompactLayout;

	public MainWindowViewModel()
	{
		ThemeVariants =
		[
			new("浅色", ThemeVariant.Light),
			new("深色", ThemeVariant.Dark)
		];
		TypographyThemes = new ObservableCollection<MarkdownTypographyTheme>(MarkdownTypographyThemes.All);
		ViewerTypographyThemeChoices = new ObservableCollection<TypographyThemeChoice>(
		[
			new("跟随统一设置", null),
			.. MarkdownTypographyThemes.All.Select(theme => new TypographyThemeChoice(theme.Name, theme.Key))
		]);
		ViewerCompactLayoutChoices = new ObservableCollection<CompactLayoutChoice>(
		[
			new("跟随统一设置", null),
			new("正常", MarkdownTypographySizes.Normal),
			new("紧凑", MarkdownTypographySizes.Small)
		]);
		MarkdownFiles = new ObservableCollection<MarkdownSampleFile>(LoadMarkdownFiles());

		SelectedThemeVariant = ThemeVariants[0];
		SelectedTypographyTheme = TypographyThemes.FirstOrDefault(theme => theme.Key == MarkdownTypographyThemes.OrangeHeart)
								  ?? TypographyThemes.FirstOrDefault();
		FirstViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault();
		FirstViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault();
		SecondViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault(theme => theme.Key == MarkdownTypographyThemes.Simple)
											 ?? ViewerTypographyThemeChoices.FirstOrDefault();
		SecondViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault(layout => layout.Size == MarkdownTypographySizes.Small)
											?? ViewerCompactLayoutChoices.FirstOrDefault();
		SelectedFile = MarkdownFiles.FirstOrDefault();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<ThemeVariantOption> ThemeVariants { get; }

	public ObservableCollection<MarkdownTypographyTheme> TypographyThemes { get; }

	public ObservableCollection<TypographyThemeChoice> ViewerTypographyThemeChoices { get; }

	public ObservableCollection<CompactLayoutChoice> ViewerCompactLayoutChoices { get; }

	public ObservableCollection<MarkdownSampleFile> MarkdownFiles { get; }

	public bool IsCompactLayout
	{
		get => _isCompactLayout;
		set
		{
			if (SetProperty(ref _isCompactLayout, value))
			{
				OnPropertyChanged(nameof(CurrentTypographySize));
			}
		}
	}

	public TypographyThemeChoice? FirstViewerSelectedTypographyTheme
	{
		get => _firstViewerSelectedTypographyTheme;
		set
		{
			if (SetProperty(ref _firstViewerSelectedTypographyTheme, value))
			{
				OnPropertyChanged(nameof(FirstViewerTypographyTheme));
			}
		}
	}

	public CompactLayoutChoice? FirstViewerSelectedCompactLayout
	{
		get => _firstViewerSelectedCompactLayout;
		set
		{
			if (SetProperty(ref _firstViewerSelectedCompactLayout, value))
			{
				OnPropertyChanged(nameof(FirstViewerTypographySize));
			}
		}
	}

	public TypographyThemeChoice? SecondViewerSelectedTypographyTheme
	{
		get => _secondViewerSelectedTypographyTheme;
		set
		{
			if (SetProperty(ref _secondViewerSelectedTypographyTheme, value))
			{
				OnPropertyChanged(nameof(SecondViewerTypographyTheme));
			}
		}
	}

	public CompactLayoutChoice? SecondViewerSelectedCompactLayout
	{
		get => _secondViewerSelectedCompactLayout;
		set
		{
			if (SetProperty(ref _secondViewerSelectedCompactLayout, value))
			{
				OnPropertyChanged(nameof(SecondViewerTypographySize));
			}
		}
	}

	public string? FirstViewerTypographyTheme => FirstViewerSelectedTypographyTheme?.Key;

	public string? FirstViewerTypographySize => FirstViewerSelectedCompactLayout?.Size;

	public string? SecondViewerTypographyTheme => SecondViewerSelectedTypographyTheme?.Key;

	public string? SecondViewerTypographySize => SecondViewerSelectedCompactLayout?.Size;

	public string CurrentTypographySize => IsCompactLayout
		? MarkdownTypographySizes.Small
		: MarkdownTypographySizes.Normal;

	public string Markdown
	{
		get => _markdown;
		set => SetProperty(ref _markdown, value ?? string.Empty);
	}

	public string FirstViewerMarkdown { get; } = """
		# 方案摘要

		这一份预览默认跟随上方统一排版设置。切换应用主题、排版主题或紧凑布局时，如果本预览区的主题或尺寸选项保持“跟随统一设置”，这里会同步变化。

		## 重点

		- Lite 控件保留标题、段落、列表、引用、表格、图片和代码块等常用 Markdown 渲染。
		- 代码块使用纯文本显示，不依赖 TextMateSharp。
		- SVG、数学公式和多语言文案不在 Lite 包内，避免引入额外依赖。

		```csharp
		markdownViewer.TypographySize = MarkdownTypographySizes.Small;
		```
		""";

	public string SecondViewerMarkdown { get; } = """
		# 局部覆盖

		这一份预览初始使用自己的排版主题和紧凑尺寸，适合和全局预览做对比。

		> 局部排版资源会写入当前 MarkdownViewer 的资源范围，不会污染同级预览区，也不需要引用完整版 Markdown 包。

		1. 为任意预览区选择不同主题。
		2. 切换上方紧凑布局。
		3. 确认浅色和深色主题下基础 Markdown 都保持可读。
		""";

	public MarkdownSampleFile? SelectedFile
	{
		get => _selectedFile;
		set
		{
			if (SetProperty(ref _selectedFile, value))
			{
				LoadMarkdown();
			}
		}
	}

	public MarkdownTypographyTheme? SelectedTypographyTheme
	{
		get => _selectedTypographyTheme;
		set => SetProperty(ref _selectedTypographyTheme, value);
	}

	public ThemeVariantOption? SelectedThemeVariant
	{
		get => _selectedThemeVariant;
		set
		{
			if (SetProperty(ref _selectedThemeVariant, value) && value is not null && Application.Current is { } app)
			{
				app.RequestedThemeVariant = value.ThemeVariant;
			}
		}
	}

	public void ApplyTypographyResourcesTo(StyledElement element)
	{
		MarkdownThemes.OverrideTypographyResources(element, SelectedTypographyTheme?.Key, CurrentTypographySize);
	}

	private void LoadMarkdown()
	{
		if (SelectedFile is null || !File.Exists(SelectedFile.Path))
		{
			Markdown = "# CodeWF.Markdown.Lite\n\n未找到示例 Markdown 文件。";
			return;
		}

		Markdown = File.ReadAllText(SelectedFile.Path);
	}

	private static IReadOnlyList<MarkdownSampleFile> LoadMarkdownFiles()
	{
		var markdownBasePath = ResolveMarkdownBasePath();
		if (!Directory.Exists(markdownBasePath))
		{
			return [];
		}

		return Directory.GetFiles(markdownBasePath, "*.md")
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.Select(path => new MarkdownSampleFile(Path.GetFileName(path), path))
			.ToList();
	}

	private static string ResolveMarkdownBasePath()
	{
		var outputPath = Path.Combine(AppContext.BaseDirectory, "MarkdownSamples");
		if (Directory.Exists(outputPath))
		{
			return outputPath;
		}

		var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "MarkdownSamples"));
		return Directory.Exists(sourcePath) ? sourcePath : outputPath;
	}

	private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed record ThemeVariantOption(string Name, ThemeVariant ThemeVariant);

public sealed record TypographyThemeChoice(string Name, string? Key);

public sealed record CompactLayoutChoice(string Name, string? Size);

public sealed record MarkdownSampleFile(string Name, string Path);
