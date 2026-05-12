using Avalonia.Controls;

using Xunit;

using LiteMarkdownThemes = CodeWF.Markdown.Lite.Themes.MarkdownThemes;
using LiteMarkdownTypographySizes = CodeWF.Markdown.Lite.Themes.MarkdownTypographySizes;
using LiteMarkdownTypographyThemes = CodeWF.Markdown.Lite.Themes.MarkdownTypographyThemes;
using LiteMarkdownViewer = CodeWF.Markdown.Lite.Controls.MarkdownViewer;

namespace CodeWF.Markdown.Tests.Themes;

public sealed class LiteMarkdownThemesResourceTests
{
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
