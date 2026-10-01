using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace CodeWF.Markdown.Shared.Rendering;

/// <summary>
///     修复 Avalonia 12.1 <see cref="Avalonia.Controls.SelectableTextBlock" /> 的选中渲染移位：
///     基类在构造选区前景色覆盖时，用控件级 FontSize 与默认 BaselineAlignment 重建
///     <see cref="TextRunProperties" />，行内代码、脚注等自定义字号或基线对齐的 Run
///     被选中后会按控件字号重新排版，出现文字变大下沉、背景块与文本装饰丢失的移位。
///     此子类只替换选中部分的前景色，完整保留 Run 原有的字号、字体、基线对齐、背景与文本装饰。
/// </summary>
internal class MarkdownSelectableTextBlock : Avalonia.Controls.SelectableTextBlock
{
    // 让针对 SelectableTextBlock 的主题样式（选区画刷、MdSelectedBlock 等）继续匹配子类。
    protected override Type StyleKeyOverride => typeof(Avalonia.Controls.SelectableTextBlock);

    protected override TextLayout CreateTextLayout(string? text)
    {
        var selectionStart = SelectionStart;
        var selectionEnd = SelectionEnd;
        var start = Math.Min(selectionStart, selectionEnd);
        var length = Math.Max(selectionStart, selectionEnd) - start;
        var textRuns = _textRuns;

        // LineSpacing 的段落属性设置器是 Avalonia 内部 API，子类无法复制；
        // 未设置时（默认 0）两种构造完全等价，设置了则回退基类行为。
        var lineSpacing = GetValue(LineSpacingProperty);
        if (length <= 0
            || SelectionForegroundBrush is null
            || textRuns is null
            || Math.Abs(lineSpacing) > 0.0001)
        {
            return base.CreateTextLayout(text);
        }

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var defaultProperties = new GenericTextRunProperties(
            typeface,
            FontSize,
            TextDecorations,
            Foreground,
            fontFeatures: FontFeatures);
        var paragraphProperties = new GenericTextParagraphProperties(
            FlowDirection, TextAlignment, true, false,
            defaultProperties, TextWrapping, LineHeight, 0, LetterSpacing);

        var textStyleOverrides = BuildSelectionOverrides(
            textRuns, start, length, SelectionForegroundBrush,
            typeface, FontSize, FontFeatures);

        if (textStyleOverrides is null)
        {
            return base.CreateTextLayout(text);
        }

        var textSource = new InlinesTextSource(textRuns, textStyleOverrides);
        var maxSize = GetMaxSizeFromConstraint();

        return new TextLayout(
            textSource,
            paragraphProperties,
            TextTrimming,
            maxSize.Width,
            maxSize.Height,
            MaxLines);
    }

    /// <summary>
    ///     为选中的文本片段构造只替换前景色的属性覆盖：字号、字体、基线对齐、背景与文本装饰
    ///     全部沿用 Run 原有属性，避免基类用控件级属性重建导致的选中渲染移位。
    /// </summary>
    internal static List<ValueSpan<TextRunProperties>>? BuildSelectionOverrides(
        IReadOnlyList<TextRun> textRuns,
        int start,
        int length,
        IBrush? selectionForegroundBrush,
        Typeface fallbackTypeface,
        double fallbackFontSize,
        FontFeatureCollection? fallbackFontFeatures)
    {
        if (length <= 0 || selectionForegroundBrush is null)
        {
            return null;
        }

        List<ValueSpan<TextRunProperties>>? textStyleOverrides = null;
        var accumulatedLength = 0;
        foreach (var textRun in textRuns)
        {
            var runLength = textRun.Text.Length;
            if (accumulatedLength + runLength <= start || accumulatedLength >= start + length)
            {
                accumulatedLength += runLength;
                continue;
            }

            var overlapStart = Math.Max(start, accumulatedLength);
            var overlapEnd = Math.Min(start + length, accumulatedLength + runLength);
            var overlapLength = overlapEnd - overlapStart;
            var properties = textRun.Properties;

            textStyleOverrides ??= [];
            textStyleOverrides.Add(
                new ValueSpan<TextRunProperties>(
                    overlapStart,
                    overlapLength,
                    new GenericTextRunProperties(
                        properties?.Typeface ?? fallbackTypeface,
                        properties?.FontRenderingEmSize ?? fallbackFontSize,
                        properties?.TextDecorations,
                        selectionForegroundBrush,
                        properties?.BackgroundBrush,
                        properties?.BaselineAlignment ?? BaselineAlignment.Baseline,
                        properties?.CultureInfo,
                        properties?.FontFeatures ?? fallbackFontFeatures)));

            accumulatedLength += runLength;
        }

        return textStyleOverrides;
    }
}
