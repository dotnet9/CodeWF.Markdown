extern alias FullThemes;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

using CodeWF.Markdown.Controls;

using Xunit;

using MarkdownThemes = FullThemes::CodeWF.Markdown.Themes.MarkdownThemes;
using MarkdownTypographySizes = FullThemes::CodeWF.Markdown.Themes.MarkdownTypographySizes;
using MarkdownTypographyThemeRegistry = FullThemes::CodeWF.Markdown.Themes.MarkdownTypographyThemeRegistry;
using MarkdownTypographyThemes = FullThemes::CodeWF.Markdown.Themes.MarkdownTypographyThemes;

namespace CodeWF.Markdown.Tests.Themes;

public sealed class MarkdownThemesResourceTests
{
	[Fact]
	public void MarkdownTypographyThemes_DoesNotExposeThemeList()
	{
		Assert.Null(typeof(MarkdownTypographyThemes).GetProperty("All"));
	}

	[Fact]
	public void OverrideTypographyResources_WhenViewerOverridesOnlySize_DoesNotReuseInheritedDictionary()
	{
		var parent = new Border();
		var viewer = new MarkdownViewer();
		parent.Child = viewer;

		var exception = Record.Exception(() =>
		{
			MarkdownThemes.OverrideTypographyResources(parent, MarkdownTypographyThemes.Basic, MarkdownTypographySizes.Small);
			viewer.TypographySize = MarkdownTypographySizes.Normal;
			MarkdownThemes.OverrideTypographyResources(parent, MarkdownTypographyThemes.Simple, MarkdownTypographySizes.Small);
		});

		Assert.Null(exception);
		Assert.NotEmpty(viewer.Resources.MergedDictionaries);
	}

	[Fact]
	public void OverrideTypographyResources_WhenAppliedToContainer_DoesNotExposeSemiPaletteKeys()
	{
		var parent = new Border();

		MarkdownThemes.OverrideTypographyResources(parent, MarkdownTypographyThemes.GeekBlack, MarkdownTypographySizes.Normal);

		Assert.True(parent.Resources.TryGetResource(MarkdownStyleKeys.TypographyBaseResourcesResource, ThemeVariant.Light, out _));
		Assert.True(parent.Resources.TryGetResource(MarkdownStyleKeys.ParagraphFontSizeResource, ThemeVariant.Light, out _));
		Assert.False(parent.Resources.TryGetResource(MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out _));
		Assert.False(parent.Resources.TryGetResource(MarkdownStyleKeys.QuoteBackgroundBrushResource, ThemeVariant.Light, out _));
	}

	[Fact]
	public void OverrideTypographyResources_WhenAppliedToContainer_KeepsPaletteOnViewerOnly()
	{
		var parent = new Border();
		var viewer = new MarkdownViewer();
		parent.Child = viewer;

		MarkdownThemes.OverrideTypographyResources(parent, MarkdownTypographyThemes.GeekBlack, MarkdownTypographySizes.Normal);

		Assert.False(parent.Resources.TryGetResource(MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out _));
		Assert.True(viewer.Resources.TryGetResource(MarkdownStyleKeys.AccentBrushResource, ThemeVariant.Light, out var accentBrush));
		Assert.IsType<SolidColorBrush>(accentBrush);
	}

	[Theory]
	[InlineData(MarkdownTypographyThemes.Simple, "#3E64FF", "#F6F8FA", "#F6F7F9", "#F6F7F9")]
	[InlineData(MarkdownTypographyThemes.OrangeHeart, "#EF7060", "#F6F8FA", "#FFF3F0", "#FFF3F0")]
	public void CreateExportStyle_WhenThemeUsesCommonResources_ResolvesIncludedBaseResources(
		string themeName,
		string accentColor,
		string codeBackgroundColor,
		string inlineCodeBackgroundColor,
		string quoteBackgroundColor)
	{
		var style = MarkdownThemes.CreateExportStyle(themeName);

		Assert.Equal(accentColor, style.LinkColor);
		Assert.Equal(codeBackgroundColor, style.CodeBackgroundColor);
		Assert.Equal(inlineCodeBackgroundColor, style.InlineCodeBackgroundColor);
		Assert.Equal(quoteBackgroundColor, style.QuoteBackgroundColor);
	}

	[Fact]
	public void CreateExportStyle_WhenWeChatFormatTheme_ResolvesDocumentPalette()
	{
		var style = MarkdownThemes.CreateExportStyle(MarkdownTypographyThemes.WeChatFormat);

		Assert.Contains(MarkdownTypographyThemes.WeChatFormat, MarkdownTypographyThemeRegistry.ThemeNames);
		Assert.Equal("#3F3F3F", style.BodyColor);
		Assert.Equal("#3F3F3F", style.HeadingColor);
		Assert.Equal("#FF3502", style.LinkColor);
		Assert.Equal("#F8F5EC", style.InlineCodeBackgroundColor);
		Assert.Equal("#9E9E9E", style.QuoteBorderColor);
		Assert.Equal("#1A9E9E9E", style.QuoteBackgroundColor);
		Assert.Equal(1.625d, style.LineHeightRatio);
	}

	[Fact]
	public void CreateExportStyle_WhenCustomThemeRegistered_UsesThemeResources()
	{
		var themeName = $"UnitTestTheme{Guid.NewGuid():N}";
		MarkdownTypographyThemeRegistry.Register(
			themeName,
			() => new ResourceDictionary
			{
				[MarkdownStyleKeys.TextBrushResource] = new SolidColorBrush(Color.Parse("#123456")),
				[MarkdownStyleKeys.AccentBrushResource] = new SolidColorBrush(Color.Parse("#0E88EB")),
				[MarkdownStyleKeys.ParagraphFontSizeResource] = 18d,
				[MarkdownStyleKeys.ParagraphLineHeightResource] = 30d,
				[MarkdownStyleKeys.Heading1FontSizeResource] = 36d,
				[MarkdownStyleKeys.CodeBlockFontSizeResource] = 14d
			});

		var style = MarkdownThemes.CreateExportStyle(themeName);

		Assert.Contains(themeName, MarkdownTypographyThemeRegistry.ThemeNames);
		Assert.Equal(18d, style.BodyFontSize);
		Assert.Equal(36d, style.Heading1FontSize);
		Assert.Equal(14d, style.CodeFontSize);
		Assert.Equal(1.667d, style.LineHeightRatio);
		Assert.Equal("#123456", style.BodyColor);
		Assert.Equal("#0E88EB", style.LinkColor);
	}
}
