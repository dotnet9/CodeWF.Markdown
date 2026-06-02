extern alias LiteThemes;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using Xunit;

using LiteMarkdownThemes = LiteThemes::CodeWF.Markdown.Lite.Themes.MarkdownThemes;
using LiteMarkdownTypographySizes = LiteThemes::CodeWF.Markdown.Themes.MarkdownTypographySizes;
using LiteMarkdownTypographyThemes = LiteThemes::CodeWF.Markdown.Themes.MarkdownTypographyThemes;
using LiteMarkdownViewer = CodeWF.Markdown.Lite.Controls.MarkdownViewer;

namespace CodeWF.Markdown.Tests.Themes;

public sealed class LiteMarkdownThemesResourceTests
{
	[Fact]
	public void LiteMarkdownTypographyThemes_DoesNotExposeThemeList()
	{
		Assert.Null(typeof(LiteMarkdownTypographyThemes).GetProperty("All"));
	}

	[Fact]
	public void OverrideTypographyResources_WhenLiteViewerOverridesOnlySize_DoesNotReuseInheritedDictionary()
	{
		var parent = new Border();
		var viewer = new LiteMarkdownViewer();
		parent.Child = viewer;

		var exception = Record.Exception(() =>
		{
			LiteMarkdownThemes.OverrideTypographyResources(parent, LiteMarkdownTypographyThemes.Basic, LiteMarkdownTypographySizes.Small);
			viewer.TypographySize = LiteMarkdownTypographySizes.Normal;
			LiteMarkdownThemes.OverrideTypographyResources(parent, LiteMarkdownTypographyThemes.Simple, LiteMarkdownTypographySizes.Small);
		});

		Assert.Null(exception);
		Assert.NotEmpty(viewer.Resources.MergedDictionaries);
	}

	[Fact]
	public void OverrideTypographyResources_WhenAppliedToLiteContainer_DoesNotExposeSemiPaletteKeys()
	{
		var parent = new Border();

		LiteMarkdownThemes.OverrideTypographyResources(parent, LiteMarkdownTypographyThemes.GeekBlack, LiteMarkdownTypographySizes.Normal);

		Assert.True(parent.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.TypographyBaseResourcesResource, ThemeVariant.Light, out _));
		Assert.True(parent.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.ParagraphFontSizeResource, ThemeVariant.Light, out _));
		Assert.False(parent.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out _));
		Assert.False(parent.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.QuoteBackgroundBrushResource, ThemeVariant.Light, out _));
	}

	[Fact]
	public void OverrideTypographyResources_WhenAppliedToLiteContainer_KeepsPaletteOnViewerOnly()
	{
		var parent = new Border();
		var viewer = new LiteMarkdownViewer();
		parent.Child = viewer;

		LiteMarkdownThemes.OverrideTypographyResources(parent, LiteMarkdownTypographyThemes.GeekBlack, LiteMarkdownTypographySizes.Normal);

		Assert.False(parent.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out _));
		Assert.True(viewer.Resources.TryGetResource(CodeWF.Markdown.Lite.MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out var accentBrush));
		Assert.IsType<SolidColorBrush>(accentBrush);
	}

	[Fact]
	public void ApplyTypographyResources_WhenWeChatFormatTheme_RegistersLiteResources()
	{
		var resources = new ResourceDictionary();

		var exception = Record.Exception(() =>
			LiteMarkdownThemes.ApplyTypographyResources(resources, LiteMarkdownTypographyThemes.WeChatFormat));

		Assert.Null(exception);
		Assert.NotEmpty(resources.MergedDictionaries);
	}

	[Fact]
	public void GetRenderedText_WhenBasicMarkdown_ReturnsPlainText()
	{
		var viewer = new LiteMarkdownViewer
		{
			Markdown = "# Title\n\n- One\n- Two\n\n```text\nplain code\n```"
		};

		var renderedText = viewer.GetRenderedText();

		Assert.Contains("Title", renderedText);
		Assert.Contains("- One", renderedText);
		Assert.Contains("plain code", renderedText);
	}
}
