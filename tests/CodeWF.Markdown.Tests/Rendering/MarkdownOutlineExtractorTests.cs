using CodeWF.Markdown.Shared.Rendering;

using Markdig;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownOutlineExtractorTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	[Fact]
	public void Extract_ReturnsLevelsTitlesAndLines()
	{
		var model = MarkdownParser.Parse("# A\n\n## B\n\n### C", Pipeline);

		var items = MarkdownOutlineExtractor.Extract(model);

		Assert.Equal(
			[
				new MarkdownOutlineItem(1, "A", 1),
				new MarkdownOutlineItem(2, "B", 3),
				new MarkdownOutlineItem(3, "C", 5)
			],
			items);
	}

	[Fact]
	public void Extract_WhenHeadingMarkersAreInsideCodeFence_IgnoresThem()
	{
		var model = MarkdownParser.Parse("```\n# not a heading\n```\n\n# Real", Pipeline);

		var items = MarkdownOutlineExtractor.Extract(model);

		var item = Assert.Single(items);
		Assert.Equal("Real", item.Title);
		Assert.Equal(1, item.Level);
		Assert.Equal(5, item.Line);
	}

	[Fact]
	public void Extract_WhenHeadingContainsInlineMarkup_ReturnsPlainTitle()
	{
		var model = MarkdownParser.Parse("## Use `Code` and [link](https://example.com)", Pipeline);

		var item = Assert.Single(MarkdownOutlineExtractor.Extract(model));

		Assert.Equal("Use Code and link", item.Title);
	}

	[Fact]
	public void Extract_WhenModelIsNull_ReturnsEmpty()
	{
		Assert.Empty(MarkdownOutlineExtractor.Extract(null));
	}

	[Fact]
	public void FindFirstHeading_ReturnsFirstTitleOrNull()
	{
		Assert.Equal("First", MarkdownOutlineExtractor.FindFirstHeading(
			MarkdownParser.Parse("# First\n\n# Second", Pipeline)));
		Assert.Null(MarkdownOutlineExtractor.FindFirstHeading(
			MarkdownParser.Parse("no headings here", Pipeline)));
	}
}
