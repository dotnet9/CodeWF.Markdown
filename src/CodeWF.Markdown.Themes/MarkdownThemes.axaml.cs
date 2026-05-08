using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

using CodeWF.Markdown;
using CodeWF.Markdown.Themes.Themes;

namespace CodeWF.Markdown.Themes;

/// <summary>
/// Markdown 控件的默认样式入口，并负责把指定排版主题资源合并到目标资源字典。
/// </summary>
public class MarkdownThemes : Styles
{
    private static readonly IReadOnlyDictionary<string, Func<ResourceDictionary>> ThemeResourceFactories =
        new Dictionary<string, Func<ResourceDictionary>>(StringComparer.OrdinalIgnoreCase)
    {
        [MarkdownTypographyThemes.OrangeHeart] = static () => new OrangeHeartTypographyResources(),
        [MarkdownTypographyThemes.InkBlack] = static () => new InkBlackTypographyResources(),
        [MarkdownTypographyThemes.ColorfulPurple] = static () => new ColorfulPurpleTypographyResources(),
        [MarkdownTypographyThemes.TenderGreen] = static () => new TenderGreenTypographyResources(),
        [MarkdownTypographyThemes.Verdant] = static () => new VerdantTypographyResources(),
        [MarkdownTypographyThemes.RedScarlet] = static () => new RedScarletTypographyResources(),
        [MarkdownTypographyThemes.BlueGlow] = static () => new BlueGlowTypographyResources(),
        [MarkdownTypographyThemes.TechnologyBlue] = static () => new TechnologyBlueTypographyResources(),
        [MarkdownTypographyThemes.LanQing] = static () => new LanQingTypographyResources(),
        [MarkdownTypographyThemes.Yamabuki] = static () => new YamabukiTypographyResources(),
        [MarkdownTypographyThemes.FrontendPeak] = static () => new FrontendPeakTypographyResources(),
        [MarkdownTypographyThemes.GeekBlack] = static () => new GeekBlackTypographyResources(),
        [MarkdownTypographyThemes.Simple] = static () => new SimpleTypographyResources(),
        [MarkdownTypographyThemes.RosePurple] = static () => new RosePurpleTypographyResources(),
        [MarkdownTypographyThemes.CuteGreen] = static () => new CuteGreenTypographyResources(),
        [MarkdownTypographyThemes.FullStackBlue] = static () => new FullStackBlueTypographyResources(),
    };

    private static readonly string[] TypographyResourceKeys =
    [
        MarkdownStyleKeys.AccentBrushResource,
        MarkdownStyleKeys.QuoteBackgroundBrushResource,
        MarkdownStyleKeys.InlineCodeBackgroundBrushResource,
        MarkdownStyleKeys.TableHeaderBackgroundBrushResource,
        MarkdownStyleKeys.CodeBackgroundBrushResource,
        MarkdownStyleKeys.ParagraphFontSizeResource,
        MarkdownStyleKeys.ParagraphLineHeightResource,
        MarkdownStyleKeys.Heading1FontSizeResource,
        MarkdownStyleKeys.Heading2FontSizeResource,
        MarkdownStyleKeys.Heading3FontSizeResource,
        MarkdownStyleKeys.Heading4FontSizeResource,
        MarkdownStyleKeys.Heading5FontSizeResource,
        MarkdownStyleKeys.Heading6FontSizeResource,
    ];

    private static readonly ConditionalWeakTable<IResourceDictionary, AppliedTypographyResources> AppliedResources = new();

    private string? _typographyTheme = MarkdownTypographyThemes.Simple;

    public MarkdownThemes()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyTypographyResources(Resources, _typographyTheme);
    }

    public string? TypographyTheme
    {
        get => _typographyTheme;
        set
        {
            _typographyTheme = value;
            ApplyTypographyResources(Resources, value);
        }
    }

    /// <summary>
    /// 覆盖应用级 Markdown 排版资源，适合全局主题切换。
    /// </summary>
    public static void OverrideTypographyResources(Application application, string? typographyTheme)
    {
        ApplyTypographyResources(application.Resources, typographyTheme);
    }

    /// <summary>
    /// 覆盖指定控件或窗口的 Markdown 排版资源，适合局部预览区切换主题。
    /// </summary>
    public static void OverrideTypographyResources(StyledElement element, string? typographyTheme)
    {
        ApplyTypographyResources(element.Resources, typographyTheme);
    }

    public static void OverrideTypographyResources(Application application, ResourceDictionary typographyResources)
    {
        ApplyTypographyResources(application.Resources, typographyResources);
    }

    public static void OverrideTypographyResources(StyledElement element, ResourceDictionary typographyResources)
    {
        ApplyTypographyResources(element.Resources, typographyResources);
    }

    public static void ApplyTypographyResources(IResourceDictionary targetResources, string? typographyTheme)
    {
        ApplyTypographyResources(targetResources, LoadTypographyResources(typographyTheme));
    }

    public static void ApplyTypographyResources(IResourceDictionary targetResources, ResourceDictionary typographyResources)
    {
        var applied = AppliedResources.GetOrCreateValue(targetResources);
        if (applied.Resources is not null)
        {
            targetResources.MergedDictionaries.Remove(applied.Resources);
            applied.Resources = null;
        }

        foreach (var key in TypographyResourceKeys)
        {
            targetResources.Remove(key);
        }

        targetResources.MergedDictionaries.Add(typographyResources);
        applied.Resources = typographyResources;
    }

    private static ResourceDictionary LoadTypographyResources(string? typographyTheme)
    {
        return !string.IsNullOrWhiteSpace(typographyTheme)
               && ThemeResourceFactories.TryGetValue(typographyTheme.Trim(), out var factory)
            ? factory()
            : ThemeResourceFactories[MarkdownTypographyThemes.Simple]();
    }

    private sealed class AppliedTypographyResources
    {
        public ResourceDictionary? Resources { get; set; }
    }
}
