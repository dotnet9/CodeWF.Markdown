using Avalonia.Controls;
using Avalonia.Media;

using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Themes;

using Xunit;

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
