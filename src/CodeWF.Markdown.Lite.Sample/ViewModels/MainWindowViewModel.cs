using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Styling;

using CodeWF.Markdown.Lite.Sample.Themes;
using CodeWF.Markdown.Themes;

using Semi.Avalonia;

namespace CodeWF.Markdown.Lite.Sample.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
	private const string SampleTypographyThemeKey = "SampleInkGreen";
	private const string AppThemeEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_APP_THEME";
	private const string TypographyThemeEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_TYPOGRAPHY";
	private const string MarkdownSampleEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_FILE";

	private MarkdownSampleFile? _selectedFile;
	private TypographyThemeChoice? _selectedTypographyTheme;
	private ThemeVariantOption? _selectedThemeVariant;
	private bool _isCompactLayout;
	private string _markdown = string.Empty;
	private TypographyThemeChoice? _firstViewerSelectedTypographyTheme;
	private CompactLayoutChoice? _firstViewerSelectedCompactLayout;
	private TypographyThemeChoice? _secondViewerSelectedTypographyTheme;
	private CompactLayoutChoice? _secondViewerSelectedCompactLayout;

	private static readonly TypographyThemeChoice[] BuiltInTypographyThemes =
	[
		new("简", MarkdownTypographyThemes.Simple),
		new("橙心", MarkdownTypographyThemes.OrangeHeart),
		new("墨黑", MarkdownTypographyThemes.InkBlack),
		new("科技蓝", MarkdownTypographyThemes.TechnologyBlue),
		new("全栈蓝", MarkdownTypographyThemes.FullStackBlue),
		new("兰青", MarkdownTypographyThemes.LanQing),
		new("姹紫", MarkdownTypographyThemes.ColorfulPurple),
		new("嫩青", MarkdownTypographyThemes.TenderGreen),
		new("山吹", MarkdownTypographyThemes.Yamabuki),
		new("极客黑", MarkdownTypographyThemes.GeekBlack),
		new("红绯", MarkdownTypographyThemes.RedScarlet),
		new("绿意", MarkdownTypographyThemes.Verdant),
		new("萌绿", MarkdownTypographyThemes.CuteGreen),
		new("蓝莹", MarkdownTypographyThemes.BlueGlow),
		new("蔷薇紫", MarkdownTypographyThemes.RosePurple),
		new("微信公众号", MarkdownTypographyThemes.WeChatFormat),
	];

	public MainWindowViewModel()
	{
		MarkdownTypographyThemeRegistry.Register(SampleTypographyThemeKey, static () => new SampleTypographyThemeResources());

		ThemeVariants =
		[
			new("浅色", "light", ThemeVariant.Light),
			new("深色", "dark", ThemeVariant.Dark),
			new("水生", "aquatic", SemiTheme.Aquatic),
			new("沙漠", "desert", SemiTheme.Desert),
			new("暮色", "dusk", SemiTheme.Dusk),
			new("夜空", "night-sky", SemiTheme.NightSky)
		];
		TypographyThemes = new ObservableCollection<TypographyThemeChoice>(CreateTypographyThemes());
		ViewerTypographyThemeChoices = new ObservableCollection<TypographyThemeChoice>(CreateViewerTypographyThemeChoices());
		ViewerCompactLayoutChoices = new ObservableCollection<CompactLayoutChoice>(CreateCompactLayoutChoices());
		MarkdownFiles = new ObservableCollection<MarkdownSampleFile>(LoadMarkdownFiles());

		SelectedThemeVariant = FindThemeVariantOption(GetEnvironmentValue(AppThemeEnvironmentVariable))
							   ?? ThemeVariants[0];
		SelectedTypographyTheme = FindTypographyThemeChoice(TypographyThemes, GetEnvironmentValue(TypographyThemeEnvironmentVariable))
								  ?? TypographyThemes.FirstOrDefault(theme => theme.Key == MarkdownTypographyThemes.OrangeHeart)
								  ?? TypographyThemes.FirstOrDefault();
		FirstViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault();
		FirstViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault();
		SecondViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault(theme => theme.Key == SampleTypographyThemeKey)
											 ?? ViewerTypographyThemeChoices.FirstOrDefault();
		SecondViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault(layout => layout.Size == MarkdownTypographySizes.Small)
											?? ViewerCompactLayoutChoices.FirstOrDefault();
		SelectedFile = FindMarkdownSampleFile(GetEnvironmentValue(MarkdownSampleEnvironmentVariable))
					   ?? MarkdownFiles.FirstOrDefault();
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public ObservableCollection<ThemeVariantOption> ThemeVariants { get; }

	public ObservableCollection<TypographyThemeChoice> TypographyThemes { get; }

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
				OnPropertyChanged(nameof(FirstViewerTypographySize));
				OnPropertyChanged(nameof(SecondViewerTypographySize));
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

	public string? FirstViewerTypographyTheme => FirstViewerSelectedTypographyTheme?.Key ?? CurrentTypographyTheme;

	public string? FirstViewerTypographySize => FirstViewerSelectedCompactLayout?.Size ?? CurrentTypographySize;

	public string? SecondViewerTypographyTheme => SecondViewerSelectedTypographyTheme?.Key ?? CurrentTypographyTheme;

	public string? SecondViewerTypographySize => SecondViewerSelectedCompactLayout?.Size ?? CurrentTypographySize;

	public string? CurrentTypographyTheme => SelectedTypographyTheme?.Key;

	public string CurrentTypographySize => IsCompactLayout
		? MarkdownTypographySizes.Small
		: MarkdownTypographySizes.Normal;

	public string Markdown
	{
		get => _markdown;
		set => SetProperty(ref _markdown, value ?? string.Empty);
	}

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

	public TypographyThemeChoice? SelectedTypographyTheme
	{
		get => _selectedTypographyTheme;
		set
		{
			if (SetProperty(ref _selectedTypographyTheme, value))
			{
				OnPropertyChanged(nameof(CurrentTypographyTheme));
				OnPropertyChanged(nameof(FirstViewerTypographyTheme));
				OnPropertyChanged(nameof(SecondViewerTypographyTheme));
			}
		}
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

		var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "MarkdownSamples"));
		return Directory.Exists(sourcePath) ? sourcePath : outputPath;
	}

	private ThemeVariantOption? FindThemeVariantOption(string? key) =>
		ThemeVariants.FirstOrDefault(theme => string.Equals(theme.Key, key, StringComparison.OrdinalIgnoreCase));

	private static IReadOnlyList<TypographyThemeChoice> CreateTypographyThemes() =>
	[
		.. BuiltInTypographyThemes,
		new("示例：青墨绿", SampleTypographyThemeKey)
	];

	private static IReadOnlyList<TypographyThemeChoice> CreateViewerTypographyThemeChoices() =>
	[
		new("跟随统一设置", null),
		.. CreateTypographyThemes()
	];

	private static IReadOnlyList<CompactLayoutChoice> CreateCompactLayoutChoices() =>
	[
		new("跟随统一设置", null),
		new("正常", MarkdownTypographySizes.Normal),
		new("紧凑", MarkdownTypographySizes.Small)
	];

	private static TypographyThemeChoice? FindTypographyThemeChoice(IEnumerable<TypographyThemeChoice> choices, string? key) =>
		choices.FirstOrDefault(choice => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase));

	private MarkdownSampleFile? FindMarkdownSampleFile(string? nameOrPath)
	{
		if (string.IsNullOrWhiteSpace(nameOrPath))
		{
			return null;
		}

		return MarkdownFiles.FirstOrDefault(file =>
			string.Equals(file.Name, nameOrPath, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(Path.GetFileName(file.Path), nameOrPath, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(file.Path, nameOrPath, StringComparison.OrdinalIgnoreCase));
	}

	private static string? GetEnvironmentValue(string variableName)
	{
		var value = Environment.GetEnvironmentVariable(variableName);
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

public sealed record ThemeVariantOption(string Name, string Key, ThemeVariant ThemeVariant);

public sealed record TypographyThemeChoice(string Name, string? Key);

public sealed record CompactLayoutChoice(string Name, string? Size);

public sealed record MarkdownSampleFile(string Name, string Path);
