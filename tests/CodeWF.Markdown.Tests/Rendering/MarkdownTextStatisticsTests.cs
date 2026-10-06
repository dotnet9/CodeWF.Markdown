using CodeWF.Markdown.Shared.Rendering;

using Markdig;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownTextStatisticsTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	[Fact]
	public void Calculate_WhenDocumentIsEmpty_ReturnsEmpty()
	{
		var statistics = MarkdownTextStatisticsCalculator.Calculate(MarkdownParser.Parse(string.Empty, Pipeline));

		Assert.Equal(MarkdownTextStatistics.Empty, statistics);
	}

	[Fact]
	public void Calculate_WhenDocumentIsNull_ReturnsEmpty()
	{
		Assert.Equal(MarkdownTextStatistics.Empty, MarkdownTextStatisticsCalculator.Calculate(null));
	}

	[Fact]
	public void Calculate_CountsWordsLinesParagraphsAndHeadings()
	{
		var statistics = MarkdownTextStatisticsCalculator.Calculate(
			MarkdownParser.Parse("# Title\n\nAlpha\n\nBeta", Pipeline));

		Assert.Equal(3, statistics.Words);
		Assert.Equal(5, statistics.Lines);
		Assert.Equal(2, statistics.Paragraphs);
		Assert.Equal(1, statistics.Headings);
		Assert.Equal(1, statistics.ReadingMinutes);
	}

	[Fact]
	public void Calculate_WhenTextIsPureCjk_CountsEveryCharacterAsOneWord()
	{
		var statistics = MarkdownTextStatisticsCalculator.Calculate(
			MarkdownParser.Parse("你好世界", Pipeline));

		Assert.Equal(4, statistics.Words);
		Assert.Equal(4, statistics.Characters);
		Assert.Equal(1, statistics.Paragraphs);
	}

	[Fact]
	public void Calculate_WhenListHasItems_CountsOneParagraphPerItem()
	{
		var statistics = MarkdownTextStatisticsCalculator.Calculate(
			MarkdownParser.Parse("# H\n\n- a\n- b\n- c", Pipeline));

		Assert.Equal(3, statistics.Paragraphs);
		Assert.Equal(1, statistics.Headings);
	}

	[Fact]
	public void Calculate_WhenDocumentIsLong_ReportsAtLeastOneMinute()
	{
		var markdown = string.Join("\n\n", Enumerable.Range(0, 500).Select(index => $"word{index}"));

		var statistics = MarkdownTextStatisticsCalculator.Calculate(MarkdownParser.Parse(markdown, Pipeline));

		Assert.Equal(500, statistics.Words);
		Assert.Equal(
			(int)Math.Ceiling(500 / MarkdownTextStatisticsCalculator.WordsPerMinute),
			statistics.ReadingMinutes);
	}
}
