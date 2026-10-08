using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;

using CodeWF.Markdown;
using CodeWF.Markdown.Shared.Rendering;
using Markdig;

using Avalonia.Media;

using CodeWF.Markdown.Sample.Themes;
using CodeWF.Markdown.Themes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Lang.Avalonia;

using Semi.Avalonia;

namespace CodeWF.Markdown.Sample.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private const string SampleTypographyThemeKey = "SampleInkGreen";
    private const string IncrementalStressHeading = "## 自动增量压力";
    private const string IncrementalInsertHeading = "## Markdown 中部插入演示";
    private const string IncrementalAppendHeading = "## Markdown 尾部追加演示";
    private const string IncrementalInsertAnchor = "中部插入锚点：";
    private const string AppThemeEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_APP_THEME";
    private const string TypographyThemeEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_TYPOGRAPHY";
    private const string MarkdownSampleEnvironmentVariable = "CODEWF_MARKDOWN_SAMPLE_FILE";

    private static readonly string[] IncrementalChineseFragments =
    [
        "会议纪要已补充验收口径，优先检查标题、列表和引用块的相邻间距",
        "产品说明新增灰度发布计划，并记录影响范围、负责人和回滚条件",
        "接口文档替换为最新字段说明，保留原有表格结构用于观察局部刷新",
        "排查记录追加复现步骤，重点确认中文长句在窄窗口中的自动换行",
        "变更日志改写为面向用户的描述，避免只更新孤立字符造成误判",
        "测试报告插入边界场景，覆盖代码块、任务列表和尾部滚动留白"
    ];

    private static readonly string[] IncrementalChineseTags =
    [
        "段落修订",
        "验收说明",
        "风险记录",
        "回归观察",
        "边界场景",
        "滚动校验"
    ];

    private static readonly TypographyThemeDefinition[] BuiltInTypographyThemes =
    [
        new(SampleL.TypographyThemeSimpleName, MarkdownTypographyThemes.Simple),
        new(SampleL.TypographyThemeOrangeHeartName, MarkdownTypographyThemes.OrangeHeart),
        new(SampleL.TypographyThemeInkBlackName, MarkdownTypographyThemes.InkBlack),
        new(SampleL.TypographyThemeTechnologyBlueName, MarkdownTypographyThemes.TechnologyBlue),
        new(SampleL.TypographyThemeFullStackBlueName, MarkdownTypographyThemes.FullStackBlue),
        new(SampleL.TypographyThemeLanQingName, MarkdownTypographyThemes.LanQing),
        new(SampleL.TypographyThemeColorfulPurpleName, MarkdownTypographyThemes.ColorfulPurple),
        new(SampleL.TypographyThemeTenderGreenName, MarkdownTypographyThemes.TenderGreen),
        new(SampleL.TypographyThemeYamabukiName, MarkdownTypographyThemes.Yamabuki),
        new(SampleL.TypographyThemeGeekBlackName, MarkdownTypographyThemes.GeekBlack),
        new(SampleL.TypographyThemeRedScarletName, MarkdownTypographyThemes.RedScarlet),
        new(SampleL.TypographyThemeVerdantName, MarkdownTypographyThemes.Verdant),
        new(SampleL.TypographyThemeCuteGreenName, MarkdownTypographyThemes.CuteGreen),
        new(SampleL.TypographyThemeBlueGlowName, MarkdownTypographyThemes.BlueGlow),
        new(SampleL.TypographyThemeRosePurpleName, MarkdownTypographyThemes.RosePurple),
        new(SampleL.TypographyThemeWeChatFormatName, MarkdownTypographyThemes.WeChatFormat),
    ];

    private readonly DispatcherTimer _incrementalStressTimer;
    private readonly string _markdownBasePath;
    private static readonly MarkdownPipeline ParseTimingPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    private IStorageProvider? _storageProvider;
    private IClipboard? _clipboard;
    private int _incrementalStressTick;
    private int _incrementalReplaceTick;
    private int _incrementalInsertTick;
    private int _incrementalAppendTick;
    private string? _incrementalStressMode;
    private SampleLanguage? _selectLanguage;

    public MainWindowViewModel()
    {
        MarkdownTypographyThemeRegistry.Register(SampleTypographyThemeKey, static () => new SampleTypographyThemeResources());

        _incrementalStressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _incrementalStressTimer.Tick += (_, _) => ApplyIncrementalStressTick();
        ToggleIncrementalStressCommand = new RelayCommand(ToggleIncrementalStress);
        StartIncrementalStressCommand = new RelayCommand<string?>(mode =>
        {
            StopIncrementalStress();
            _incrementalStressMode = mode;
            ToggleIncrementalStress();
        });
        StopIncrementalStressCommand = new RelayCommand(StopIncrementalStress);

        _markdownBasePath = ResolveMarkdownBasePath();
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
        Languages = CreateLanguages(["zh-CN", "zh-Hant", "en-US", "ja-JP"]);

        SelectedThemeVariant = FindThemeVariantOption(GetEnvironmentValue(AppThemeEnvironmentVariable))
                               ?? ThemeVariants[0];
        SelectedTypographyTheme = FindTypographyThemeChoice(TypographyThemes, GetEnvironmentValue(TypographyThemeEnvironmentVariable))
                                  ?? TypographyThemes.FirstOrDefault(theme => theme.Key == MarkdownTypographyThemes.Simple)
                                  ?? TypographyThemes.FirstOrDefault();
        FirstViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault();
        FirstViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault();
        SecondViewerSelectedTypographyTheme = ViewerTypographyThemeChoices.FirstOrDefault(theme => theme.Key == SampleTypographyThemeKey)
                                             ?? ViewerTypographyThemeChoices.FirstOrDefault();
        SecondViewerSelectedCompactLayout = ViewerCompactLayoutChoices.FirstOrDefault(layout => layout.Size == MarkdownTypographySizes.Small)
                                            ?? ViewerCompactLayoutChoices.FirstOrDefault();
        SelectedFile = FindMarkdownSampleFile(GetEnvironmentValue(MarkdownSampleEnvironmentVariable))
                       ?? MarkdownFiles.FirstOrDefault();
        SelectLanguage = Languages.FirstOrDefault(l => l.CultureName == I18nManager.Instance.Culture?.Name)
                         ?? Languages.FirstOrDefault();

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        ToggleDarkThemeCommand = new RelayCommand(ToggleDarkTheme);
        SetViewModeCommand = new RelayCommand<string?>(SetViewMode);
        ExportPngCommand = new AsyncRelayCommand<string?>(_ => ExportCoreAsync("png"));
        ExportPdfCommand = new AsyncRelayCommand<string?>(_ => ExportCoreAsync("pdf"));
        ExportWordCommand = new AsyncRelayCommand<string?>(_ => ExportCoreAsync("word"));
        CopySocialHtmlCommand = new AsyncRelayCommand<string?>(CopySocialHtmlAsync);
        VersionText = "v" + (typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3) ?? "14.0.0");
        UpdateStatistics();
    }

    public ObservableCollection<ThemeVariantOption> ThemeVariants { get; }

    public ObservableCollection<TypographyThemeChoice> TypographyThemes { get; }

    public ObservableCollection<TypographyThemeChoice> ViewerTypographyThemeChoices { get; }

    public ObservableCollection<CompactLayoutChoice> ViewerCompactLayoutChoices { get; }

    public ObservableCollection<MarkdownSampleFile> MarkdownFiles { get; }

    public List<SampleLanguage> Languages { get; }

    public RelayCommand ToggleIncrementalStressCommand { get; }

    public RelayCommand<string?> StartIncrementalStressCommand { get; }

    public RelayCommand StopIncrementalStressCommand { get; }

    public RelayCommand ToggleSidebarCommand { get; }

    public RelayCommand ToggleDarkThemeCommand { get; }

    public RelayCommand<string?> SetViewModeCommand { get; }

    public AsyncRelayCommand<string?> ExportPngCommand { get; }

    public AsyncRelayCommand<string?> ExportPdfCommand { get; }

    public AsyncRelayCommand<string?> ExportWordCommand { get; }

    public AsyncRelayCommand<string?> CopySocialHtmlCommand { get; }

    public string VersionText { get; }

    /// <summary>示例文档编码（状态栏展示，对应原型的 UTF-8）。</summary>
    public string CurrentEncodingText => "UTF-8";

    // ---- Quick Open ----
    public bool IsQuickOpenVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string QuickOpenFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(FilteredFiles));
            }
        }
    } = string.Empty;

    public IEnumerable<MarkdownSampleFile> FilteredFiles =>
        string.IsNullOrWhiteSpace(QuickOpenFilter)
            ? MarkdownFiles
            : MarkdownFiles.Where(f =>
                f.Name.Contains(QuickOpenFilter, StringComparison.OrdinalIgnoreCase)
                || f.Description.Contains(QuickOpenFilter, StringComparison.OrdinalIgnoreCase));

    public void ToggleQuickOpen()
    {
        IsQuickOpenVisible = !IsQuickOpenVisible;
        QuickOpenFilter = string.Empty;
    }

    public void OpenFilteredFile(MarkdownSampleFile? file)
    {
        IsQuickOpenVisible = false;
        if (file is not null)
        {
            SelectedFile = MarkdownFiles.FirstOrDefault(f => f.Name == file.Name);
        }
    }

    // ---- 浮动格式工具栏命令 ----
    public void ApplyFormat(string format)
    {
        // 由视图层调用：获取选区并包裹 Markdown 标记
        FormatRequested?.Invoke(this, format);
    }

    public event EventHandler<string>? FormatRequested;

    public bool IsSidebarCollapsed
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string ViewMode
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? "split"))
            {
                OnPropertyChanged(nameof(IsEditorVisible));
                OnPropertyChanged(nameof(IsPreviewVisible));
                OnPropertyChanged(nameof(IsPairVisible));
            }
        }
    } = "split";

    public bool IsEditorVisible => ViewMode is not ("preview" or "pair");

    public bool IsPreviewVisible => ViewMode is not ("edit" or "pair");

    public bool IsPairVisible => ViewMode == "pair";

    public string ThemeIconText
    {
        get;
        private set => SetProperty(ref field, value);
    } = "🌙";

    public string StatusMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = "就绪";

    public int WordCount
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public double ParseMilliseconds
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string SampleName => SelectedFile is { } file ? file.DisplayName + ".md" : string.Empty;

    public string? ImageBasePath => SelectedFile is { } file ? Path.GetDirectoryName(file.Path) : _markdownBasePath;

    public void SetViewMode(string? mode) => ViewMode = mode ?? "split";

    public void ConfigureHost(TopLevel? topLevel)
    {
        _storageProvider = topLevel?.StorageProvider;
        _clipboard = topLevel?.Clipboard;
    }

    private void ToggleDarkTheme()
    {
        var targetKey = SelectedThemeVariant?.Key == "dark" ? "light" : "dark";
        SelectedThemeVariant = FindThemeVariantOption(targetKey) ?? SelectedThemeVariant;
        ThemeIconText = targetKey == "dark" ? "☀️" : "🌙";
    }

    private async Task ExportCoreAsync(string? kind)
    {
        if (_storageProvider is null || string.IsNullOrWhiteSpace(Markdown))
        {
            return;
        }

        var (label, extension, exportKind) = kind switch
        {
            "png" => ("PNG 图片", "png", ExportKind.Png),
            "pdf" => ("PDF 文档", "pdf", ExportKind.Pdf),
            _ => ("Word 文档", "docx", ExportKind.Word)
        };

        var suggestedName = Path.GetFileNameWithoutExtension(
            SampleName is { Length: > 0 } ? SampleName : "demo") + "." + extension;
        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出" + label,
            SuggestedFileName = suggestedName,
            FileTypeChoices = [new FilePickerFileType(label) { Patterns = ["*." + extension] }]
        });
        if (file is null)
        {
            StatusMessage = "导出已取消";
            return;
        }

        try
        {
            StatusMessage = "正在导出" + label + "…";
            var savePath = file.Path.LocalPath;
            var themeName = SelectedTypographyTheme?.Key;
            var typographySize = CurrentTypographySize;
            await Task.Run(() => MarkdownDocumentExporter.ExportMarkdown(
                Markdown, exportKind, themeName, savePath, typographySize));
            StatusMessage = "已导出：" + Path.GetFileName(savePath);
        }
        catch (Exception exception)
        {
            StatusMessage = "导出失败：" + exception.Message;
        }
    }

    private async Task CopySocialHtmlAsync(string? target)
    {
        if (_clipboard is null)
        {
            return;
        }

        var targetName = target switch
        {
            "zhihu" => "知乎",
            "juejin" => "掘金",
            _ => "公众号"
        };
        var success = await _clipboard.TrySetMarkdownHtmlAsync(
            Markdown, SelectedTypographyTheme?.Key, target);
        StatusMessage = success ? "已复制" + targetName + "排版 HTML" : "复制失败：暂不支持该平台";
    }

    private void UpdateStatistics()
    {
        var text = Markdown ?? string.Empty;
        WordCount = CountWords(text);
        var stopwatch = Stopwatch.StartNew();
        MarkdownParser.Parse(text, ParseTimingPipeline);
        stopwatch.Stop();
        ParseMilliseconds = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1);
    }

    private static int CountWords(string text)
    {
        var count = 0;
        var inWord = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character) || char.IsPunctuation(character) || char.IsSymbol(character))
            {
                inWord = false;
                continue;
            }

            if (character >= 0x4E00 && character <= 0x9FFF)
            {
                count++;
                inWord = false;
            }
            else if (!inWord)
            {
                count++;
                inWord = true;
            }
        }

        return count;
    }

    public bool IsCompactLayout
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(CurrentTypographySize));
                OnPropertyChanged(nameof(FirstViewerTypographySize));
                OnPropertyChanged(nameof(SecondViewerTypographySize));
            }
        }
    }

    public TypographyThemeChoice? FirstViewerSelectedTypographyTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FirstViewerTypographyTheme));
            }
        }
    }

    public CompactLayoutChoice? FirstViewerSelectedCompactLayout
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(FirstViewerTypographySize));
            }
        }
    }

    public TypographyThemeChoice? SecondViewerSelectedTypographyTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(SecondViewerTypographyTheme));
            }
        }
    }

    public CompactLayoutChoice? SecondViewerSelectedCompactLayout
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(SecondViewerTypographySize));
            }
        }
    }

    public string? FirstViewerTypographyTheme => FirstViewerSelectedTypographyTheme?.Key ?? CurrentTypographyTheme;

    public string? FirstViewerTypographySize => FirstViewerSelectedCompactLayout?.Size ?? CurrentTypographySize;

    public string? SecondViewerTypographyTheme => SecondViewerSelectedTypographyTheme?.Key ?? CurrentTypographyTheme;

    public string? SecondViewerTypographySize => SecondViewerSelectedCompactLayout?.Size ?? CurrentTypographySize;

    public bool IsIncrementalStressRunning
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IncrementalStressButtonText));
            }
        }
    }

    public string IncrementalStressButtonText =>
        I18nManager.Instance.GetResource(IsIncrementalStressRunning
            ? SampleL.StopIncrementalDemo
            : SampleL.StartIncrementalDemo) ?? string.Empty;

    public string Markdown
    {
        get;
        set
        {
            if (SetProperty(ref field, value ?? string.Empty))
            {
                UpdateStatistics();
            }
        }
    } = string.Empty;

    public MarkdownSampleFile? SelectedFile
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                StopIncrementalStress();
                LoadMarkdown();
                OnPropertyChanged(nameof(SampleName));
                OnPropertyChanged(nameof(ImageBasePath));
            }
        }
    }

    public TypographyThemeChoice? SelectedTypographyTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(CurrentTypographyTheme));
                OnPropertyChanged(nameof(CurrentTypographyName));
                OnPropertyChanged(nameof(PreviewThemeText));
                OnPropertyChanged(nameof(FirstViewerTypographyTheme));
                OnPropertyChanged(nameof(SecondViewerTypographyTheme));
            }
        }
    }

    public ThemeVariantOption? SelectedThemeVariant
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null && Application.Current is { } app)
            {
                app.RequestedThemeVariant = value.ThemeVariant;
                ThemeIconText = value.Key == "dark" ? "☀️" : "🌙";
                OnPropertyChanged(nameof(PreviewThemeText));
            }
        }
    }

    public SampleLanguage? SelectLanguage
    {
        get => _selectLanguage;
        set
        {
            if (SetProperty(ref _selectLanguage, value) && value is not null)
            {
                I18nManager.Instance.Culture = new CultureInfo(value.CultureName);
                RefreshLocalizedTypographyChoices();
                OnPropertyChanged(nameof(IncrementalStressButtonText));
            }
        }
    }

    private void RefreshLocalizedTypographyChoices()
    {
        var selectedTypographyThemeKey = SelectedTypographyTheme?.Key;
        var firstViewerTypographyThemeKey = FirstViewerSelectedTypographyTheme?.Key;
        var secondViewerTypographyThemeKey = SecondViewerSelectedTypographyTheme?.Key;
        var firstViewerCompactLayoutSize = FirstViewerSelectedCompactLayout?.Size;
        var secondViewerCompactLayoutSize = SecondViewerSelectedCompactLayout?.Size;

        ReplaceItems(TypographyThemes, CreateTypographyThemes());
        ReplaceItems(ViewerTypographyThemeChoices, CreateViewerTypographyThemeChoices());
        ReplaceItems(ViewerCompactLayoutChoices, CreateCompactLayoutChoices());

        SelectedTypographyTheme = FindTypographyThemeChoice(TypographyThemes, selectedTypographyThemeKey)
                                  ?? TypographyThemes.FirstOrDefault(theme => theme.Key == MarkdownTypographyThemes.OrangeHeart)
                                  ?? TypographyThemes.FirstOrDefault();
        FirstViewerSelectedTypographyTheme = FindTypographyThemeChoice(ViewerTypographyThemeChoices, firstViewerTypographyThemeKey)
                                             ?? ViewerTypographyThemeChoices.FirstOrDefault();
        SecondViewerSelectedTypographyTheme = FindTypographyThemeChoice(ViewerTypographyThemeChoices, secondViewerTypographyThemeKey)
                                              ?? ViewerTypographyThemeChoices.FirstOrDefault(theme => theme.Key == SampleTypographyThemeKey)
                                              ?? ViewerTypographyThemeChoices.FirstOrDefault();
        FirstViewerSelectedCompactLayout = FindCompactLayoutChoice(ViewerCompactLayoutChoices, firstViewerCompactLayoutSize)
                                           ?? ViewerCompactLayoutChoices.FirstOrDefault();
        SecondViewerSelectedCompactLayout = FindCompactLayoutChoice(ViewerCompactLayoutChoices, secondViewerCompactLayoutSize)
                                            ?? ViewerCompactLayoutChoices.FirstOrDefault(layout => layout.Size == MarkdownTypographySizes.Small)
                                            ?? ViewerCompactLayoutChoices.FirstOrDefault();
    }

    private static IReadOnlyList<TypographyThemeChoice> CreateTypographyThemes() =>
    [
        .. BuiltInTypographyThemes.Select(CreateTypographyThemeChoice),
        new(GetResource(SampleL.TypographyThemeSampleInkGreenName), SampleTypographyThemeKey)
    ];

    private static IReadOnlyList<TypographyThemeChoice> CreateViewerTypographyThemeChoices() =>
    [
        new(GetResource(SampleL.TypographyThemeFollowUnifiedSettings), null),
        .. CreateTypographyThemes()
    ];

    private static IReadOnlyList<CompactLayoutChoice> CreateCompactLayoutChoices() =>
    [
        new(GetResource(SampleL.TypographyThemeFollowUnifiedSettings), null),
        new(GetResource(SampleL.TypographySizeNormalName), MarkdownTypographySizes.Normal),
        new(GetResource(SampleL.TypographySizeSmallName), MarkdownTypographySizes.Small)
    ];

    private static TypographyThemeChoice CreateTypographyThemeChoice(TypographyThemeDefinition definition) =>
        new(GetResource(definition.NameResourceKey), definition.Key);

    private static string GetResource(string resourceKey) =>
        I18nManager.Instance.GetResource(resourceKey) ?? resourceKey;

    private static TypographyThemeChoice? FindTypographyThemeChoice(IEnumerable<TypographyThemeChoice> choices, string? key) =>
        choices.FirstOrDefault(choice => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase));

    private static CompactLayoutChoice? FindCompactLayoutChoice(IEnumerable<CompactLayoutChoice> choices, string? size) =>
        choices.FirstOrDefault(choice => string.Equals(choice.Size, size, StringComparison.Ordinal));

    private ThemeVariantOption? FindThemeVariantOption(string? key) =>
        ThemeVariants.FirstOrDefault(theme => string.Equals(theme.Key, key, StringComparison.OrdinalIgnoreCase));

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

    private static string GetSampleDescription(string fileName) => Path.GetFileName(fileName) switch
    {
        var name when name.StartsWith("01") => "标题 · 表格 · 任务列表",
        var name when name.StartsWith("02") => "18 套排版主题",
        var name when name.StartsWith("03") => "TextMate 语法高亮",
        var name when name.StartsWith("04") => "嵌套列表 · 引用块",
        var name when name.StartsWith("05") => "SVG / GIF · 远程图片",
        var name when name.StartsWith("06") => "性能演示专用",
        _ => "Markdown 示例"
    };

    private static string? GetEnvironmentValue(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ReplaceItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private static List<SampleLanguage> CreateLanguages(IEnumerable<string> cultureNames)
    {
        return cultureNames
            .Where(cultureName => !string.IsNullOrWhiteSpace(cultureName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(CreateLanguage)
            .OrderBy(GetSortOrder)
            .ThenBy(language => language.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SampleLanguage CreateLanguage(string cultureName)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            return new SampleLanguage
            {
                CultureName = culture.Name,
                Language = culture.EnglishName,
                Description = culture.NativeName
            };
        }
        catch (CultureNotFoundException)
        {
            return new SampleLanguage
            {
                CultureName = cultureName,
                Language = cultureName,
                Description = cultureName
            };
        }
    }

    private static int GetSortOrder(SampleLanguage language) => language.CultureName switch
    {
        "zh-CN" => 0,
        "zh-Hant" => 1,
        "en-US" => 2,
        "ja-JP" => 3,
        _ => 4
    };

    private void ToggleIncrementalStress()
    {
        if (IsIncrementalStressRunning)
        {
            StopIncrementalStress();
            return;
        }

        IsIncrementalStressRunning = true;
        ApplyIncrementalStressTick();
        _incrementalStressTimer.Start();
    }

    private void StopIncrementalStress()
    {
        if (!IsIncrementalStressRunning)
        {
            return;
        }

        _incrementalStressTimer.Stop();
        IsIncrementalStressRunning = false;
    }

    private void ApplyIncrementalStressTick()
    {
        _incrementalStressTick++;
        var mode = _incrementalStressMode switch { "replace" => 0, "insert" => 1, "append" => 2, _ => (_incrementalStressTick - 1) % 3 };
        switch (mode)
        {
            case 0:
                _incrementalReplaceTick++;
                Markdown = UpsertIncrementalStressSection(Markdown, _incrementalReplaceTick);
                break;
            case 1:
                _incrementalInsertTick++;
                Markdown = InsertIncrementalMarkdownFragment(Markdown, _incrementalInsertTick);
                break;
            default:
                _incrementalAppendTick++;
                Markdown = AppendIncrementalMarkdownBlock(Markdown, _incrementalAppendTick);
                break;
        }
    }

    private void LoadMarkdown()
    {
        _incrementalStressTick = 0;
        _incrementalReplaceTick = 0;
        _incrementalInsertTick = 0;
        _incrementalAppendTick = 0;

        if (SelectedFile is null || !File.Exists(SelectedFile.Path))
        {
            Markdown = "# CodeWF.Markdown\n\n示例 Markdown 文件未找到。";
            return;
        }

        Markdown = File.ReadAllText(SelectedFile.Path);
    }

    private IReadOnlyList<MarkdownSampleFile> LoadMarkdownFiles()
    {
        if (!Directory.Exists(_markdownBasePath))
        {
            return [];
        }

        return Directory.GetFiles(_markdownBasePath, "*.md")
            .OrderBy(path => path)
            .Select(path => new MarkdownSampleFile(
                Path.GetFileName(path),
                path,
                GetSampleDescription(Path.GetFileName(path))))
            .ToList();
    }

    private static string UpsertIncrementalStressSection(string markdown, int tick)
    {
        var section = BuildIncrementalStressSection(tick);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return section;
        }

        var start = markdown.IndexOf(IncrementalStressHeading, StringComparison.Ordinal);
        if (start < 0)
        {
            var firstLineEnd = markdown.IndexOf('\n');
            var insertIndex = FindNextHeading(markdown, firstLineEnd < 0 ? markdown.Length : firstLineEnd + 1);
            return insertIndex < markdown.Length
                ? CombineMarkdown(markdown[..insertIndex], section, markdown[insertIndex..])
                : CombineMarkdown(markdown, section, string.Empty);
        }

        var end = FindNextHeading(markdown, start + IncrementalStressHeading.Length);
        return CombineMarkdown(markdown[..start], section, markdown[end..]);
    }

    private static string BuildIncrementalStressSection(int tick)
    {
        var phase = (tick % 4) switch
        {
            0 => "尾部追加",
            1 => "段落替换",
            2 => "表格修订",
            _ => "代码说明"
        };
        var fragment = BuildIncrementalChineseFragment(tick);
        var listItem = BuildIncrementalChineseFragment(tick + 2);
        var longLine = BuildIncrementalChineseLongLine(tick);

        return $$"""
               {{IncrementalStressHeading}}

               当前替换轮次：{{tick}}，阶段：{{phase}}，这一整段会被自动替换内容，用于验证局部修改刷新。计时器还会轮流模拟正文中部插入和文档尾部追加，三类操作每次只产生一个连续文本变更。自动输入片段：{{fragment}}。

               - 动态列表项：第 {{tick}} 次增量刷新，{{listItem}}。
               - 长文本换行：{{longLine}}

               ```json
               {
                 "轮次": {{tick}},
                 "阶段": "{{phase}}",
                 "说明": "{{fragment}}"
               }
               ```

               | 检查项 | 当前值 |
               | --- | --- |
               | 轮次 | {{tick}} |
               | 阶段 | {{phase}} |
               | 中文片段 | {{fragment}} |
               """;
    }

    private static string InsertIncrementalMarkdownFragment(string markdown, int tick)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return BuildIncrementalInsertSection(tick);
        }

        var anchorIndex = markdown.IndexOf(IncrementalInsertAnchor, StringComparison.Ordinal);
        if (anchorIndex < 0)
        {
            return InsertIncrementalInsertSection(markdown, tick);
        }

        var insertIndex = anchorIndex + IncrementalInsertAnchor.Length;
        var token = BuildIncrementalInsertToken(tick);
        return markdown.Insert(insertIndex, token);
    }

    private static string InsertIncrementalInsertSection(string markdown, int tick)
    {
        var section = BuildIncrementalInsertSection(tick);
        var stressStart = markdown.IndexOf(IncrementalStressHeading, StringComparison.Ordinal);
        if (stressStart >= 0)
        {
            var stressEnd = FindNextHeading(markdown, stressStart + IncrementalStressHeading.Length);
            return CombineMarkdown(markdown[..stressEnd], section, markdown[stressEnd..]);
        }

        var firstLineEnd = markdown.IndexOf('\n');
        var insertIndex = FindNextHeading(markdown, firstLineEnd < 0 ? markdown.Length : firstLineEnd + 1);
        return insertIndex < markdown.Length
            ? CombineMarkdown(markdown[..insertIndex], section, markdown[insertIndex..])
            : CombineMarkdown(markdown, section, string.Empty);
    }

    private static string BuildIncrementalInsertSection(int tick)
    {
        return $$"""
               {{IncrementalInsertHeading}}

               {{IncrementalInsertAnchor}}{{BuildIncrementalInsertToken(tick)}}这一段模拟人工在已有段落中间连续输入内容。每一轮只插入一小段 Markdown 行内文本，用于观察已渲染块的局部替换和后续块位置更新。
               """;
    }

    private static string BuildIncrementalInsertToken(int tick)
    {
        var fragment = BuildIncrementalChineseFragment(tick + 1);
        return $" **插入第 {tick:0000} 段：{fragment}**";
    }

    private static string AppendIncrementalMarkdownBlock(string markdown, int tick)
    {
        var appendBlock = markdown.Contains(IncrementalAppendHeading, StringComparison.Ordinal)
            ? BuildIncrementalAppendEntry(tick)
            : BuildIncrementalAppendSection(tick);

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return appendBlock;
        }

        return CombineMarkdown(markdown, appendBlock, string.Empty);
    }

    private static string BuildIncrementalAppendSection(int tick)
    {
        return CombineMarkdown(
            $$"""
            {{IncrementalAppendHeading}}

            这一节由“开始增量演示”按钮在文档尾部追加 Markdown 内容，文档长度会不断增加，用于观察新增块、尾部留白和滚动到底后的完整显示。
            """,
            BuildIncrementalAppendEntry(tick),
            string.Empty);
    }

    private static string BuildIncrementalAppendEntry(int tick)
    {
        var tag = BuildIncrementalChineseTag(tick);
        var fragment = BuildIncrementalChineseFragment(tick + 3);
        var detail = BuildIncrementalChineseLongLine(tick + 4);

        return $$"""
               ### 追加片段 {{tick:0000}}

               这是第 {{tick}} 次追加生成的 Markdown 段落，预览区应在文档长度持续增加时保持尾部可见。追加标记：`新增片段-{{tick:0000}}-{{tag}}`

               - 新增列表项：{{fragment}}。
               - 滚动观察：滚到底时应能看到本片段完整内容，以及文档尾部留白。

               > {{detail}}

               | 追加轮次 | 片段类型 | 中文长度 |
               | ---: | --- | ---: |
               | {{tick}} | {{tag}} | {{fragment.Length}} |
               """;
    }

    private static string BuildIncrementalChineseFragment(int tick)
    {
        return IncrementalChineseFragments[Math.Abs(tick) % IncrementalChineseFragments.Length];
    }

    private static string BuildIncrementalChineseTag(int tick)
    {
        return IncrementalChineseTags[Math.Abs(tick) % IncrementalChineseTags.Length];
    }

    private static string BuildIncrementalChineseLongLine(int tick)
    {
        var first = BuildIncrementalChineseFragment(tick);
        var second = BuildIncrementalChineseFragment(tick + 1);
        var tag = BuildIncrementalChineseTag(tick + 2);
        return $"{first}；{second}；本轮标记为“{tag}”，用于模拟真实中文 Markdown 文档里的连续长句、标点和语义变化。";
    }

    private static int FindNextHeading(string markdown, int startIndex)
    {
        var index = Math.Clamp(startIndex, 0, markdown.Length);
        if (index > 0)
        {
            var nextLine = markdown.IndexOf('\n', index - 1);
            if (nextLine < 0)
            {
                return markdown.Length;
            }

            index = nextLine + 1;
        }

        while (index < markdown.Length)
        {
            var lineStart = index;
            var lineEnd = markdown.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = markdown.Length;
            }

            var line = markdown[lineStart..lineEnd].TrimStart();
            if (line.StartsWith("# ", StringComparison.Ordinal) || line.StartsWith("## ", StringComparison.Ordinal))
            {
                return lineStart;
            }

            index = lineEnd + 1;
        }

        return markdown.Length;
    }

    private static string CombineMarkdown(string before, string section, string after)
    {
        var builder = new List<string>
        {
            before.TrimEnd(),
            section.Trim(),
            after.TrimStart()
        };

        return string.Join(Environment.NewLine + Environment.NewLine, builder.Where(part => part.Length > 0));
    }

    public string? CurrentTypographyTheme => SelectedTypographyTheme?.Key;

    /// <summary>排版主题显示名（状态栏芯片用）。</summary>
    public string CurrentTypographyName => SelectedTypographyTheme?.Key == MarkdownTypographyThemes.Simple
        ? "Simple" : SelectedTypographyTheme?.Name ?? string.Empty;

    public string CurrentTypographySize => IsCompactLayout
        ? MarkdownTypographySizes.Small
        : MarkdownTypographySizes.Normal;

    /// <summary>预览窗格头右侧信息（对应原型 #pvThemeName：排版主题 · 亮色/暗色）。</summary>
    public string PreviewThemeText =>
        $"{CurrentTypographyName} · {SelectedThemeVariant?.Name ?? string.Empty}".Trim();

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

    private sealed record TypographyThemeDefinition(string NameResourceKey, string Key);
}

public sealed record ThemeVariantOption(string Name, string Key, ThemeVariant ThemeVariant);

public sealed record TypographyThemeChoice(string Name, string? Key)
{
    public string DisplayName => Key == MarkdownTypographyThemes.Simple ? "Simple · 默认" : Name;
}

public sealed record CompactLayoutChoice(string Name, string? Size);

public sealed record MarkdownSampleFile(string Name, string Path, string Description)
{
    public string DisplayName => Name switch
    {
        "04-列表引用与HTML.md" => "列表与引用",
        "05-图片链接与长文.md" => "图片与链接",
        _ => System.Text.RegularExpressions.Regex.Replace(System.IO.Path.GetFileNameWithoutExtension(Name), @"^\d+-", string.Empty)
    };
}
