using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

using CodeWF.Markdown.Lite;
using CodeWF.Markdown.Lite.Controls;
using CodeWF.Markdown.Lite.Themes.Themes;

namespace CodeWF.Markdown.Lite.Themes;

/// <summary>
/// Markdown 控件的默认样式入口，并负责把指定排版主题资源合并到目标资源字典。
/// </summary>
public class MarkdownThemes : Styles
{
    private static readonly IReadOnlyDictionary<string, Func<ResourceDictionary>> ThemeResourceFactories =
        new Dictionary<string, Func<ResourceDictionary>>(StringComparer.OrdinalIgnoreCase)
    {
        [MarkdownTypographyThemes.Basic] = static () => new BasicTypographyResources(),
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
        MarkdownStyleKeys.TypographyThemeResource,
        MarkdownStyleKeys.TypographySizeResource,
        MarkdownStyleKeys.TypographyBaseResourcesResource,
    ];

    private static readonly ConditionalWeakTable<IResourceDictionary, AppliedTypographyResources> AppliedResources = new();

    private string? _typographyTheme = MarkdownTypographyThemes.Basic;
    private string? _typographySize = MarkdownTypographySizes.Normal;

    static MarkdownThemes()
    {
        MarkdownViewer.TypographyThemeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => ApplyViewerTypographyResources(viewer));
        MarkdownViewer.TypographySizeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => ApplyViewerTypographyResources(viewer));
    }

    public MarkdownThemes()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyTypographyResources(Resources, _typographyTheme, _typographySize);
    }

    /// <summary>
    /// 全局默认排版主题。默认 Basic；单个 MarkdownViewer 可通过同名属性覆盖。
    /// </summary>
    public string? TypographyTheme
    {
        get => _typographyTheme;
        set
        {
            _typographyTheme = value;
            ApplyTypographyResources(Resources, value, _typographySize);
        }
    }

    /// <summary>
    /// 排版尺寸。默认 Normal；设置为 Small 时会在当前排版主题上叠加紧凑字号、行高和间距资源。
    /// </summary>
    public string? TypographySize
    {
        get => _typographySize;
        set
        {
            _typographySize = value;
            ApplyTypographyResources(Resources, _typographyTheme, value);
        }
    }

    /// <summary>
    /// 覆盖应用级 Markdown 排版资源，适合全局主题切换。
    /// </summary>
    public static void OverrideTypographyResources(Application application, string? typographyTheme)
    {
        OverrideTypographyResources(application, typographyTheme, MarkdownTypographySizes.Normal);
    }

    public static void OverrideTypographyResources(Application application, string? typographyTheme, string? typographySize)
    {
        ApplyTypographyResources(application.Resources, typographyTheme, typographySize);
        RefreshMarkdownViewerTypographyResources(application);
    }

    /// <summary>
    /// 覆盖指定控件或窗口的 Markdown 排版资源，适合局部预览区切换主题。
    /// </summary>
    public static void OverrideTypographyResources(StyledElement element, string? typographyTheme)
    {
        OverrideTypographyResources(element, typographyTheme, MarkdownTypographySizes.Normal);
    }

    public static void OverrideTypographyResources(StyledElement element, string? typographyTheme, string? typographySize)
    {
        ApplyTypographyResources(element.Resources, typographyTheme, typographySize);
        RefreshMarkdownViewerTypographyResources(element);
    }

    public static void OverrideTypographyResources(Application application, ResourceDictionary typographyResources)
    {
        OverrideTypographyResources(application, typographyResources, MarkdownTypographySizes.Normal);
    }

    public static void OverrideTypographyResources(Application application, ResourceDictionary typographyResources, string? typographySize)
    {
        ApplyTypographyResources(application.Resources, CreateSizedTypographyResources(typographyResources, typographySize));
        RefreshMarkdownViewerTypographyResources(application);
    }

    public static void OverrideTypographyResources(StyledElement element, ResourceDictionary typographyResources)
    {
        OverrideTypographyResources(element, typographyResources, MarkdownTypographySizes.Normal);
    }

    public static void OverrideTypographyResources(StyledElement element, ResourceDictionary typographyResources, string? typographySize)
    {
        ApplyTypographyResources(element.Resources, CreateSizedTypographyResources(typographyResources, typographySize));
        RefreshMarkdownViewerTypographyResources(element);
    }

    public static void ApplyTypographyResources(IResourceDictionary targetResources, string? typographyTheme)
    {
        ApplyTypographyResources(targetResources, CreateTypographyResources(typographyTheme));
    }

    public static void ApplyTypographyResources(IResourceDictionary targetResources, string? typographyTheme, string? typographySize)
    {
        ApplyTypographyResources(targetResources, CreateTypographyResources(typographyTheme, typographySize));
    }

    public static void ApplyTypographyResources(IResourceDictionary targetResources, ResourceDictionary typographyResources)
    {
        RemoveTypographyResources(targetResources);

        var applied = AppliedResources.GetOrCreateValue(targetResources);
        targetResources.MergedDictionaries.Add(typographyResources);
        applied.Resources = typographyResources;
    }

    public static ResourceDictionary CreateTypographyResources(string? typographyTheme)
    {
        return CreateTypographyResources(typographyTheme, MarkdownTypographySizes.Normal);
    }

    public static ResourceDictionary CreateTypographyResources(string? typographyTheme, string? typographySize)
    {
        var normalizedTheme = NormalizeTypographyTheme(typographyTheme);
        var resources = LoadTypographyResources(normalizedTheme);
        return CreateSizedTypographyResources(resources, typographySize, normalizedTheme);
    }

    private static ResourceDictionary LoadTypographyResources(string? typographyTheme)
    {
        return !string.IsNullOrWhiteSpace(typographyTheme)
               && ThemeResourceFactories.TryGetValue(typographyTheme.Trim(), out var factory)
            ? factory()
            : ThemeResourceFactories[MarkdownTypographyThemes.Basic]();
    }

    private static void ApplyViewerTypographyResources(MarkdownViewer viewer)
    {
        var typographyTheme = GetConfiguredTypographyTheme(viewer);
        var typographySize = GetConfiguredTypographySize(viewer);

        RemoveTypographyResources(viewer.Resources);

        if (typographyTheme is null && typographySize is null)
        {
            return;
        }

        var inheritedSize = GetInheritedTypographySize(viewer);
        var targetSize = typographySize ?? inheritedSize;
        var typographyResources = typographyTheme is null
            ? CreateInheritedSizeResources(viewer, targetSize)
            : CreateTypographyResources(typographyTheme, targetSize);

        ApplyTypographyResources(viewer.Resources, typographyResources);
    }

    private static string? GetConfiguredTypographyTheme(MarkdownViewer viewer)
    {
        var typographyTheme = viewer.GetValue(MarkdownViewer.TypographyThemeProperty);
        return string.IsNullOrWhiteSpace(typographyTheme) ? null : typographyTheme.Trim();
    }

    private static string? GetConfiguredTypographySize(MarkdownViewer viewer)
    {
        var typographySize = viewer.GetValue(MarkdownViewer.TypographySizeProperty);
        return string.IsNullOrWhiteSpace(typographySize) ? null : typographySize.Trim();
    }

    private static void RemoveTypographyResources(IResourceDictionary targetResources)
    {
        if (AppliedResources.TryGetValue(targetResources, out var applied)
            && applied.Resources is not null)
        {
            targetResources.MergedDictionaries.Remove(applied.Resources);
            applied.Resources = null;
        }

        foreach (var key in TypographyResourceKeys)
        {
            targetResources.Remove(key);
        }
    }

    private static ResourceDictionary CreateInheritedSizeResources(MarkdownViewer viewer, string typographySize)
    {
        var normalizedSize = NormalizeTypographySize(typographySize);
        var sizeResources = new ResourceDictionary
        {
            [MarkdownStyleKeys.TypographySizeResource] = normalizedSize
        };

        if (IsSmallTypographySize(normalizedSize))
        {
            foreach (var compactResource in new CompactTypographyResources())
            {
                sizeResources[compactResource.Key] = compactResource.Value;
            }

            return sizeResources;
        }

        CopyTypographySizeResources(sizeResources, new CommonTypographyResources(), viewer.ActualThemeVariant);

        if (TryGetInheritedTypographyBaseResources(viewer, out var baseResources))
        {
            CopyTypographySizeResources(sizeResources, baseResources, viewer.ActualThemeVariant);
        }

        return sizeResources;
    }

    private static string GetInheritedTypographySize(StyledElement element)
    {
        return TryFindStringResource(element, MarkdownStyleKeys.TypographySizeResource)
            ?? MarkdownTypographySizes.Normal;
    }

    private static string? TryFindStringResource(StyledElement element, string key)
    {
        return element.TryFindResource(key, element.ActualThemeVariant, out var value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text)
            ? text
            : null;
    }

    private static bool TryGetInheritedTypographyBaseResources(StyledElement element, out ResourceDictionary resources)
    {
        if (element.TryFindResource(
                MarkdownStyleKeys.TypographyBaseResourcesResource,
                element.ActualThemeVariant,
                out var value)
            && value is ResourceDictionary resourceDictionary)
        {
            resources = resourceDictionary;
            return true;
        }

        resources = null!;
        return false;
    }

    private static string NormalizeTypographyTheme(string? typographyTheme)
    {
        return !string.IsNullOrWhiteSpace(typographyTheme)
            && ThemeResourceFactories.ContainsKey(typographyTheme.Trim())
            ? typographyTheme.Trim()
            : MarkdownTypographyThemes.Basic;
    }

    private static string NormalizeTypographySize(string? typographySize)
    {
        return IsSmallTypographySize(typographySize)
            ? MarkdownTypographySizes.Small
            : MarkdownTypographySizes.Normal;
    }

    private static bool IsSmallTypographySize(string? typographySize)
    {
        return string.Equals(typographySize?.Trim(), MarkdownTypographySizes.Small, StringComparison.OrdinalIgnoreCase);
    }

    private static ResourceDictionary CreateSizedTypographyResources(ResourceDictionary typographyResources, string? typographySize)
    {
        return CreateSizedTypographyResources(typographyResources, typographySize, null);
    }

    private static ResourceDictionary CreateSizedTypographyResources(
        ResourceDictionary typographyResources,
        string? typographySize,
        string? typographyTheme)
    {
        var normalizedSize = NormalizeTypographySize(typographySize);
        var sizedResources = new ResourceDictionary();
        sizedResources.MergedDictionaries.Add(new CommonTypographyResources());
        sizedResources.MergedDictionaries.Add(typographyResources);

        if (IsSmallTypographySize(normalizedSize))
        {
            foreach (var compactResource in new CompactTypographyResources())
            {
                sizedResources[compactResource.Key] = compactResource.Value;
            }
        }

        sizedResources[MarkdownStyleKeys.TypographyThemeResource] = typographyTheme ?? string.Empty;
        sizedResources[MarkdownStyleKeys.TypographySizeResource] = normalizedSize;
        sizedResources[MarkdownStyleKeys.TypographyBaseResourcesResource] = typographyResources;

        return sizedResources;
    }

    private static void CopyTypographySizeResources(
        ResourceDictionary targetResources,
        ResourceDictionary sourceResources,
        ThemeVariant themeVariant)
    {
        foreach (var compactResource in new CompactTypographyResources())
        {
            if (sourceResources.TryGetResource(compactResource.Key, themeVariant, out var value))
            {
                targetResources[compactResource.Key] = value;
            }
        }
    }

    private static void RefreshMarkdownViewerTypographyResources(Application application)
    {
        switch (application.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                foreach (var window in desktop.Windows)
                {
                    RefreshMarkdownViewerTypographyResources(window);
                }

                break;
            case ISingleViewApplicationLifetime singleView when singleView.MainView is StyledElement root:
                RefreshMarkdownViewerTypographyResources(root);
                break;
        }
    }

    private static void RefreshMarkdownViewerTypographyResources(StyledElement element)
    {
        var viewers = new HashSet<MarkdownViewer>();

        if (element is MarkdownViewer viewer)
        {
            viewers.Add(viewer);
        }

        if (element is ILogical logical)
        {
            foreach (var childViewer in logical.GetLogicalDescendants().OfType<MarkdownViewer>())
            {
                viewers.Add(childViewer);
            }
        }

        if (element is Visual visual)
        {
            foreach (var childViewer in visual.GetVisualDescendants().OfType<MarkdownViewer>())
            {
                viewers.Add(childViewer);
            }
        }

        foreach (var childViewer in viewers)
        {
            ApplyViewerTypographyResources(childViewer);
        }
    }

    private sealed class AppliedTypographyResources
    {
        public ResourceDictionary? Resources { get; set; }
    }
}

