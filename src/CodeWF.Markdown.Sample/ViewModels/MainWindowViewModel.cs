using System.Collections.ObjectModel;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

using CodeWF.Markdown.Sample.Themes;
using CodeWF.Markdown.Themes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Lang.Avalonia;

namespace CodeWF.Markdown.Sample.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private const string SampleTypographyThemeKey = "SampleInkGreen";
    private const string IncrementalStressHeading = "## 自动增量压力";
    private const string IncrementalInsertHeading = "## Markdown 中部插入演示";
    private const string IncrementalAppendHeading = "## Markdown 尾部追加演示";
    private const string IncrementalInsertAnchor = "中部插入锚点：";

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

    private readonly DispatcherTimer _incrementalStressTimer;
    private readonly string _markdownBasePath;
    private int _incrementalStressTick;
    private int _incrementalReplaceTick;
    private int _incrementalInsertTick;
    private int _incrementalAppendTick;
    private SampleLanguage? _selectLanguage;

    public MainWindowViewModel()
    {
        _incrementalStressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _incrementalStressTimer.Tick += (_, _) => ApplyIncrementalStressTick();
        ToggleIncrementalStressCommand = new RelayCommand(ToggleIncrementalStress);

        _markdownBasePath = ResolveMarkdownBasePath();
        ThemeVariants =
        [
            new("浅色", ThemeVariant.Light),
            new("深色", ThemeVariant.Dark),
        ];
        TypographyThemes = new ObservableCollection<MarkdownTypographyTheme>(
            MarkdownTypographyThemes.All.Append(new MarkdownTypographyTheme("示例：青墨绿", SampleTypographyThemeKey)));
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
        Languages = CreateLanguages(["zh-CN", "zh-Hant", "en-US", "ja-JP"]);

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
        SelectLanguage = Languages.FirstOrDefault(l => l.CultureName == I18nManager.Instance.Culture?.Name)
                         ?? Languages.FirstOrDefault();
    }

    public ObservableCollection<ThemeVariantOption> ThemeVariants { get; }

    public ObservableCollection<MarkdownTypographyTheme> TypographyThemes { get; }

    public ObservableCollection<TypographyThemeChoice> ViewerTypographyThemeChoices { get; }

    public ObservableCollection<CompactLayoutChoice> ViewerCompactLayoutChoices { get; }

    public ObservableCollection<MarkdownSampleFile> MarkdownFiles { get; }

    public List<SampleLanguage> Languages { get; }

    public RelayCommand ToggleIncrementalStressCommand { get; }

    public bool IsCompactLayout
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(CurrentTypographySize));
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

    public string? FirstViewerTypographyTheme => FirstViewerSelectedTypographyTheme?.Key;

    public string? FirstViewerTypographySize => FirstViewerSelectedCompactLayout?.Size;

    public string? SecondViewerTypographyTheme => SecondViewerSelectedTypographyTheme?.Key;

    public string? SecondViewerTypographySize => SecondViewerSelectedCompactLayout?.Size;

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
        set => SetProperty(ref field, value ?? string.Empty);
    } = string.Empty;

    public string FirstViewerMarkdown { get; } = """
        # 方案摘要

        这一份预览默认跟随上方统一排版设置。切换 TabControl 上方的排版主题或紧凑布局时，如果本 Viewer 的主题或尺寸选项保持“跟随统一设置”，这里会同步变化。

        ## 重点

        - 单个 Viewer 可以只接管主题或尺寸。
        - 未接管的配置继续从外层 `MarkdownThemes` 资源读取。
        - 表格、列表、引用和代码块都参与尺寸对比。

        | 项目 | 状态 | 说明 |
        | --- | --- | --- |
        | 全局主题 | 跟随 | 由 TabControl 资源提供 |
        | 局部主题 | 可选 | 设置后只影响当前 Viewer |
        | 紧凑布局 | 可选 | Small 会收紧字号和间距 |

        ```csharp
        markdownViewer.TypographyTheme = null;
        markdownViewer.TypographySize = MarkdownTypographySizes.Small;
        ```
        """;

    public string SecondViewerMarkdown { get; } = """
        # 评审记录

        这一份预览初始使用自己的排版主题和紧凑尺寸。它适合观察同一个页面里两个 `MarkdownViewer` 采用不同阅读密度时，资源隔离是否稳定。

        > 局部设置会写入当前 MarkdownViewer 的资源范围，不会污染同级 Viewer。

        ## 清单

        1. 标题层级保持清晰。
        2. 引用块背景和强调色来自当前主题。
        3. 代码块字号、按钮尺寸和表格间距会随尺寸变化。

        ```json
        {
          "viewer": "second",
          "theme": "local",
          "size": "small"
        }
        ```
        """;

    public MarkdownSampleFile? SelectedFile
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                StopIncrementalStress();
                LoadMarkdown();
            }
        }
    }

    public MarkdownTypographyTheme? SelectedTypographyTheme
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ThemeVariantOption? SelectedThemeVariant
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value is not null && Application.Current is { } app)
            {
                app.RequestedThemeVariant = value.ThemeVariant;
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
                OnPropertyChanged(nameof(IncrementalStressButtonText));
            }
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

    public void ApplyTypographyResourcesTo(StyledElement element)
    {
        if (SelectedTypographyTheme?.Key == SampleTypographyThemeKey)
        {
            var sampleResources = LoadSampleTypographyResources();
            MarkdownThemes.OverrideTypographyResources(element, sampleResources, CurrentTypographySize);
            return;
        }

        MarkdownThemes.OverrideTypographyResources(element, SelectedTypographyTheme?.Key, CurrentTypographySize);
    }

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
        switch ((_incrementalStressTick - 1) % 3)
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
            .Select(path => new MarkdownSampleFile(Path.GetFileName(path), path))
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

    private static ResourceDictionary LoadSampleTypographyResources()
    {
        return new SampleTypographyThemeResources();
    }

    public string CurrentTypographySize => IsCompactLayout
        ? MarkdownTypographySizes.Small
        : MarkdownTypographySizes.Normal;

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
}

public sealed record ThemeVariantOption(string Name, ThemeVariant ThemeVariant);

public sealed record TypographyThemeChoice(string Name, string? Key);

public sealed record CompactLayoutChoice(string Name, string? Size);

public sealed record MarkdownSampleFile(string Name, string Path);
