// Mermaid 渲染手法移植自 MarkView.Avalonia.Mermaid（MIT, Copyright (c) Nicolas Musset）：
// Mermaider 输出的 SVG 使用 CSS 自定义属性（var(--_xxx)），而 SkiaSharp 不实现 CSS
// 级联会静默忽略 var()，因此渲染后需按 Mermaider 的 color-mix 公式把变量内联为具体色值。

using System.Text;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Svg.Skia;
using Avalonia.VisualTree;

using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Rendering;

using Markdig.Syntax;

using Mermaider;

using MermaidRenderOptions = Mermaider.Models.RenderOptions;

namespace CodeWF.Markdown.Mermaid;

/// <summary>
/// ```mermaid 围栏代码块渲染器：Mermaider（纯 .NET，无 JavaScript）解析渲染为 SVG，
/// 后台线程出图并内联 CSS 变量，跟随明暗主题重渲，失败回落显示源码。
/// </summary>
public sealed class MermaidBlockRenderer : IMarkdownBlockRenderer
{
    private static readonly Regex ThemeChangeProperty = new("^ActualThemeVariant$", RegexOptions.Compiled);

    public Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        if (block is not FencedCodeBlock fenced
            || !string.Equals(fenced.Info?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return CreateMermaidBlock(ExtractSource(fenced));
    }

    private static Control CreateMermaidBlock(string source)
    {
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var border = new Border { Child = image };
        border.Classes.Add("MdMermaid");

        CancellationTokenSource? cts = null;

        // 主题切换时 SVG 里的颜色是烘焙进文本的，必须重渲。
        void OnThemeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!ThemeChangeProperty.IsMatch(e.Property.Name))
            {
                return;
            }

            _ = ApplyThemeAsync();
        }

        Application.Current?.PropertyChanged += OnThemeChanged;

        // ScrollViewer 会给子项传递无穷可用宽度：钳制 MaxWidth 到视口宽度并随尺寸变化更新。
        image.AttachedToVisualTree += (_, _) =>
        {
            var scrollViewer = image.FindAncestorOfType<ScrollViewer>();
            if (scrollViewer is null)
            {
                return;
            }

            scrollViewer.SizeChanged += OnSizeChanged;
            image.DetachedFromLogicalTree += (_, _) =>
            {
                scrollViewer.SizeChanged -= OnSizeChanged;
                Application.Current?.PropertyChanged -= OnThemeChanged;
                cts?.Cancel();
                cts?.Dispose();
            };
            Update();

            void Update()
            {
                var width = scrollViewer.Viewport.Width;
                if (width > 0)
                {
                    image.MaxWidth = Math.Min(width, 800);
                }
            }

            void OnSizeChanged(object? sender, SizeChangedEventArgs e) => Update();
        };

        _ = ApplyThemeAsync();

        // Mermaid 解析渲染与 SkiaSharp SVG 加载都是 CPU 密集操作，放后台线程；
        // CTS 用于主题快速切换时取消在途渲染。
        async Task ApplyThemeAsync()
        {
            cts?.Cancel();
            cts?.Dispose();
            var localCts = cts = new CancellationTokenSource();
            var token = localCts.Token;

            var options = GetRenderOptions();

            try
            {
                var svgSource = await Task.Run(() =>
                {
                    var svg = MermaidRenderer.RenderSvg(source, options);
                    svg = InlineCssVariables(svg, options);
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
                    return SvgSource.LoadFromStream(stream);
                }, token);

                if (!token.IsCancellationRequested)
                {
                    image.Source = new SvgImage { Source = svgSource };
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (!token.IsCancellationRequested)
                {
                    var panel = new StackPanel { Spacing = 4 };
                    panel.Children.Add(new TextBlock { Text = $"Mermaid render error: {exception.Message}" });
                    panel.Children.Add(new TextBlock { Text = source, TextWrapping = TextWrapping.Wrap });
                    border.Child = panel;
                    border.Classes.Clear();
                    border.Classes.Add("MdMermaidFallback");
                }
            }
        }

        return border;
    }

    /// <summary>
    /// 按当前主题变体构造渲染选项；颜色来自 CodeWFMarkdownMermaid* 画刷资源
    /// （排版主题可覆盖），字面量仅作资源缺失时的兜底。
    /// </summary>
    private static MermaidRenderOptions GetRenderOptions()
    {
        var app = Application.Current;
        var isDark = app?.ActualThemeVariant == ThemeVariant.Dark;
        var theme = app?.ActualThemeVariant ?? ThemeVariant.Light;

        return new MermaidRenderOptions
        {
            Bg = ResolveHex(app, theme, "CodeWFMarkdownMermaidBackground", isDark ? "#18181B" : "#FFFFFF"),
            Fg = ResolveHex(app, theme, "CodeWFMarkdownMermaidForeground", isDark ? "#FAFAFA" : "#27272A"),
            Accent = ResolveHex(app, theme, "CodeWFMarkdownMermaidAccent", isDark ? "#60A5FA" : "#3B82F6"),
            Transparent = false
        };
    }

    private static string ResolveHex(Application? app, ThemeVariant theme, string resourceKey, string fallback)
    {
        if (app != null
            && app.TryGetResource(resourceKey, theme, out var resource)
            && resource is ISolidColorBrush brush)
        {
            var color = brush.Color;
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return fallback;
    }

    private readonly record struct Rgb(byte R, byte G, byte B);

    /// <summary>
    /// 把 Mermaider 的 CSS 自定义属性引用（var(--_xxx)）替换为计算后的 hex 色值，
    /// 公式镜像自 Mermaider 样式块中的 color-mix 定义。
    /// </summary>
    private static string InlineCssVariables(string svg, MermaidRenderOptions options)
    {
        var bg = Parse(options.Bg ?? "#FFFFFF");
        var fg = Parse(options.Fg ?? "#27272A");
        var acc = Parse(options.Accent ?? "#3b82f6");

        var vars = new (string Token, Rgb Color)[]
        {
            ("var(--_text)", fg),
            ("var(--_text-sec)", Mix(fg, 55, bg)),
            ("var(--_text-muted)", Mix(fg, 35, bg)),
            ("var(--_text-faint)", Mix(fg, 20, bg)),
            ("var(--_line)", Mix(fg, 32, bg)),
            ("var(--_arrow)", acc),
            ("var(--_node-fill)", Mix(fg, 4, bg)),
            ("var(--_node-stroke)", Mix(fg, 22, bg)),
            ("var(--_group-fill)", bg),
            ("var(--_group-hdr)", Mix(fg, 4, bg)),
            ("var(--_group-stroke)", Mix(fg, 10, bg)),
            ("var(--_inner-stroke)", Mix(fg, 10, bg)),
            ("var(--_key-badge)", Mix(fg, 8, bg)),
            ("var(--_accent-fill)", Mix(acc, 8, bg)),
            ("var(--_accent-stroke)", Mix(acc, 20, bg)),
            ("var(--_accent-text)", Mix(acc, 65, bg))
        };

        var builder = new StringBuilder(svg);
        foreach (var (token, color) in vars)
        {
            builder.Replace(token, Hex(color));
        }

        builder.Replace("background:var(--bg)", $"background:{Hex(bg)}");

        return builder.ToString();

        static Rgb Parse(string hex)
        {
            hex = hex.TrimStart('#');
            return new Rgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }

        // color-mix(in srgb, a N%, b) —— sRGB 空间线性插值
        static Rgb Mix(Rgb a, int aPercent, Rgb b)
        {
            int bPercent = 100 - aPercent;
            return new Rgb(
                (byte)(a.R * aPercent / 100 + b.R * bPercent / 100),
                (byte)(a.G * aPercent / 100 + b.G * bPercent / 100),
                (byte)(a.B * aPercent / 100 + b.B * bPercent / 100));
        }

        static string Hex(Rgb color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static string ExtractSource(FencedCodeBlock block)
    {
        if (block.Lines.Lines is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var line in block.Lines.Lines)
        {
            builder.AppendLine(line.ToString().TrimEnd());
        }

        return builder.ToString();
    }
}
