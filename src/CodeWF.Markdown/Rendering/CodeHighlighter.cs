using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using CodeWF.Markdown;

using Lang.Avalonia;

using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

using AvaloniaFontStyle = Avalonia.Media.FontStyle;
using TextMateFontStyle = TextMateSharp.Themes.FontStyle;

namespace CodeWF.Markdown.Rendering;

internal static class CodeHighlighter
{
    private const int MaxCacheSize = 32;
    private static readonly Dictionary<(string Language, ThemeName Theme), (IGrammar? Grammar, Theme Theme)> Cache = new();
    private static readonly Queue<(string Language, ThemeName Theme)> CacheOrder = new();
    private static readonly Dictionary<HighlightCacheKey, HighlightedLine[]> HighlightCache = new();
    private static readonly Queue<HighlightCacheKey> HighlightCacheOrder = new();

    public static Control Render(
        string code,
        string language,
        bool isDark,
        FontFamily fontFamily,
        double fontSize,
        double lineHeight,
        Func<bool>? hasGlobalSelection = null,
        Func<Task>? copyGlobalSelectionAsync = null)
    {
        var themeName = isDark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        var (grammar, theme) = GetOrCreateGrammar(NormalizeLanguage(language), themeName);

        var textBlock = new SelectableTextBlock
        {
            Inlines = new InlineCollection(),
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = fontFamily,
            FontSize = fontSize,
            LineHeight = lineHeight,
            Margin = new Thickness(0, 0, 0, 6),
            SelectionBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xCC, 0x2F, 0x6F, 0xD6)),
            SelectionForegroundBrush = Brushes.White
        };
        textBlock.Classes.Add(MarkdownStyleKeys.CodeBlockText);
        TextOptions.SetBaselinePixelAlignment(textBlock, BaselinePixelAlignment.Aligned);
        textBlock.ContextMenu = CreateCopyContextMenu(textBlock, hasGlobalSelection, copyGlobalSelectionAsync);

        if (grammar is null)
        {
            textBlock.Inlines.Add(new Run(code) { Foreground = isDark ? Brushes.White : Brushes.Black });
            return Wrap(textBlock);
        }

        foreach (var line in GetOrCreateHighlightedLines(code, NormalizeLanguage(language), themeName, grammar, theme, isDark))
        {
            foreach (var token in line.Tokens)
            {
                textBlock.Inlines.Add(token.CreateRun());
            }

            textBlock.Inlines.Add(new LineBreak());
        }

        return Wrap(textBlock);
    }

    private static Control Wrap(Control content)
    {
        if (content is SelectableTextBlock textBlock)
        {
            var lineBreakCount = textBlock.Inlines?.OfType<LineBreak>().Count() ?? 0;
            var plainText = textBlock.Inlines is null
                ? string.Empty
                : string.Concat(textBlock.Inlines.OfType<Run>().Select(run => run.Text ?? string.Empty));
            var lineCount = lineBreakCount > 0
                ? lineBreakCount
                : Math.Max(1, plainText.Replace("\r\n", "\n").Split('\n').Length);
            var lineNumbers = new SelectableTextBlock
            {
                Text = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount).Select(i => i.ToString())),
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                LineHeight = textBlock.LineHeight,
                Foreground = Brushes.Gray,
                Opacity = 0.72,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 12, 6),
                IsHitTestVisible = false
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star))
                }
            };
            grid.Children.Add(lineNumbers);
            Grid.SetColumn(content, 1);
            grid.Children.Add(content);
            content = grid;
        }

        var scrollViewer = new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        scrollViewer.Classes.Add(MarkdownStyleKeys.CodeBlockScrollViewer);
        return scrollViewer;
    }

    private static ContextMenu CreateCopyContextMenu(
        SelectableTextBlock textBlock,
        Func<bool>? hasGlobalSelection,
        Func<Task>? copyGlobalSelectionAsync)
    {
        var copySelectionItem = new MenuItem
        {
            Header = I18nManager.Instance.GetResource(MarkdownL.CopyRenderedText)
        };
        copySelectionItem.Click += async (_, _) =>
        {
            if (hasGlobalSelection?.Invoke() == true && copyGlobalSelectionAsync is not null)
            {
                await copyGlobalSelectionAsync();
            }
            else
            {
                textBlock.Copy();
            }
        };
        return new ContextMenu { ItemsSource = new[] { copySelectionItem } };
    }

    private static (IGrammar? Grammar, Theme Theme) GetOrCreateGrammar(string language, ThemeName themeName)
    {
        var key = (language, themeName);
        if (Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (Cache.Count >= MaxCacheSize)
        {
            var oldest = CacheOrder.Dequeue();
            Cache.Remove(oldest);
        }

        var options = new RegistryOptions(themeName);
        var registry = new Registry(options);
        var theme = registry.GetTheme();
        var scope = options.GetScopeByLanguageId(language);
        var grammar = string.IsNullOrWhiteSpace(scope) ? null : registry.LoadGrammar(scope);
        grammar ??= registry.LoadGrammar(options.GetScopeByLanguageId("log"));

        var value = (grammar, theme);
        Cache[key] = value;
        CacheOrder.Enqueue(key);
        return value;
    }

    private static HighlightedLine[] GetOrCreateHighlightedLines(
        string code,
        string language,
        ThemeName themeName,
        IGrammar grammar,
        Theme theme,
        bool isDark)
    {
        var key = new HighlightCacheKey(code, language, themeName);
        if (HighlightCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (HighlightCache.Count >= MaxCacheSize)
        {
            var oldest = HighlightCacheOrder.Dequeue();
            HighlightCache.Remove(oldest);
        }

        var highlightedLines = Tokenize(code, grammar, theme, isDark);
        HighlightCache[key] = highlightedLines;
        HighlightCacheOrder.Enqueue(key);
        return highlightedLines;
    }

    private static HighlightedLine[] Tokenize(string code, IGrammar grammar, Theme theme, bool isDark)
    {
        var lines = new List<HighlightedLine>();
        IStateStack? ruleStack = null;
        foreach (var line in code.Replace("\r\n", "\n").Split('\n'))
        {
            var result = grammar.TokenizeLine(line, ruleStack, TimeSpan.FromSeconds(2));
            ruleStack = result.RuleStack;
            var tokens = new List<HighlightedToken>();

            foreach (var token in result.Tokens)
            {
                var start = Math.Min(token.StartIndex, line.Length);
                var end = Math.Min(token.EndIndex, line.Length);
                if (end <= start)
                {
                    continue;
                }

                tokens.Add(CreateHighlightedToken(line[start..end], token.Scopes, theme, isDark));
            }

            lines.Add(new HighlightedLine(tokens));
        }

        return lines.ToArray();
    }

    private static Run CreateRun(string text, IEnumerable<string> scopes, Theme theme, bool isDark)
    {
        return CreateHighlightedToken(text, scopes, theme, isDark).CreateRun();
    }

    private static HighlightedToken CreateHighlightedToken(string text, IEnumerable<string> scopes, Theme theme, bool isDark)
    {
        var foregroundId = -1;
        var backgroundId = -1;
        var fontStyle = TextMateFontStyle.NotSet;

        foreach (var rule in theme.Match(scopes.ToList()))
        {
            if (foregroundId == -1 && rule.foreground > 0)
            {
                foregroundId = rule.foreground;
            }

            if (backgroundId == -1 && rule.background > 0)
            {
                backgroundId = rule.background;
            }

            if (rule.fontStyle > 0)
            {
                fontStyle |= (TextMateFontStyle)rule.fontStyle;
            }
        }

        return new HighlightedToken(
            text,
            foregroundId == -1
                ? (isDark ? Brushes.White : Brushes.Black)
                : new ImmutableSolidColorBrush(ParseColor(theme.GetColor(foregroundId))),
            backgroundId == -1
                ? null
                : new ImmutableSolidColorBrush(ParseColor(theme.GetColor(backgroundId))),
            (fontStyle & TextMateFontStyle.Bold) != 0 ? FontWeight.Bold : FontWeight.Normal,
            (fontStyle & TextMateFontStyle.Italic) != 0 ? AvaloniaFontStyle.Italic : AvaloniaFontStyle.Normal);
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "text";
        }

        return language.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant() switch
        {
            "csharp" => "c#",
            "cs" => "c#",
            "js" => "javascript",
            "ts" => "typescript",
            "ps" => "powershell",
            "pwsh" => "powershell",
            var value => value
        };
    }

    private static Color ParseColor(string hex)
    {
        var value = hex.TrimStart('#');
        if (value.Length == 8)
        {
            return Color.FromArgb(
                byte.Parse(value[..2], NumberStyles.HexNumber),
                byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber),
                byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber),
                byte.Parse(value.Substring(6, 2), NumberStyles.HexNumber));
        }

        return Color.FromRgb(
            byte.Parse(value[..2], NumberStyles.HexNumber),
            byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber),
            byte.Parse(value.Substring(4, 2), NumberStyles.HexNumber));
    }

    private sealed record HighlightedLine(IReadOnlyList<HighlightedToken> Tokens);

    private sealed record HighlightedToken(
        string Text,
        IBrush? Foreground,
        IBrush? Background,
        FontWeight FontWeight,
        AvaloniaFontStyle FontStyle)
    {
        public Run CreateRun()
        {
            return new Run
            {
                Text = Text,
                Foreground = Foreground,
                Background = Background,
                FontWeight = FontWeight,
                FontStyle = FontStyle
            };
        }
    }

    private readonly record struct HighlightCacheKey(string Code, string Language, ThemeName Theme);
}
