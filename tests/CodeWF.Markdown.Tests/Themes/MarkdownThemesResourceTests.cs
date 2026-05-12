using Avalonia.Controls;

using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Themes;

using Xunit;

namespace CodeWF.Markdown.Tests.Themes;

public sealed class MarkdownThemesResourceTests
{
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
}
