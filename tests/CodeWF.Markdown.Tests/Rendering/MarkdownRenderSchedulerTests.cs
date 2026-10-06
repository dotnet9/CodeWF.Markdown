using CodeWF.Markdown.Rendering;
using CodeWF.Markdown.Shared.Rendering;

using Markdig;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownRenderSchedulerTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	private static MarkdownRenderScheduler CreateScheduler()
	{
		return new MarkdownRenderScheduler(text => MarkdownParser.Parse(text, Pipeline));
	}

	[Fact]
	public void CalculateDirtySpan_WhenTextIsUnchanged_ReturnsEmptySpan()
	{
		var span = MarkdownRenderScheduler.CalculateDirtySpan("abc", "abc");

		Assert.Equal(MarkdownTextSpan.Empty, span);
	}

	[Fact]
	public void CalculateDirtySpan_WhenTextIsInsertedInTheMiddle_ReturnsInsertedRange()
	{
		var span = MarkdownRenderScheduler.CalculateDirtySpan("abcd", "abXYcd");

		Assert.Equal(new MarkdownTextSpan(2, 4), span);
	}

	[Fact]
	public void CalculateDirtySpan_WhenTextIsDeleted_ReturnsEmptyRangeAtDeletionPoint()
	{
		var span = MarkdownRenderScheduler.CalculateDirtySpan("abXYcd", "abcd");

		Assert.Equal(2, span.Start);
		Assert.Equal(2, span.End);
	}

	[Fact]
	public void CalculateDirtySpan_WhenOldTextIsEmpty_SpansTheWholeNewText()
	{
		var span = MarkdownRenderScheduler.CalculateDirtySpan(null, "abc");

		Assert.Equal(new MarkdownTextSpan(0, 3), span);
	}

	[Fact]
	public void CalculateDirtySpan_WhenNewTextIsEmpty_CollapsesToTheStart()
	{
		var span = MarkdownRenderScheduler.CalculateDirtySpan("abc", null);

		Assert.Equal(MarkdownTextSpan.Empty, span);
	}

	[Fact]
	public void Queue_WhenCalledRepeatedlyInsideCoalesceWindow_ParsesOnce()
	{
		var scheduler = CreateScheduler();
		var completions = new List<MarkdownParseResult>();
		scheduler.Completed += (_, result) => completions.Add(result);

		scheduler.Queue("# A");
		scheduler.Queue("# AB");
		scheduler.Queue("# ABC");

		Assert.Equal(3, scheduler.Current.Version);
		Assert.Equal("# ABC", scheduler.Current.Text);

		var result = WaitForResult(completions);

		Assert.Single(completions);
		Assert.Equal("# ABC", result.Model.Source);
		Assert.Equal(3, result.Snapshot.Version);
		Assert.True(result.IsFirstParse);
		Assert.True(result.DirtySpanValid);
		Assert.Equal(scheduler.Current.Version, scheduler.ParsedVersion);
	}

	[Fact]
	public void Queue_WhenQueueIsCancelled_DoesNotParse()
	{
		var scheduler = CreateScheduler();
		var completions = new List<MarkdownParseResult>();
		scheduler.Completed += (_, result) => completions.Add(result);

		scheduler.Queue("# A");
		scheduler.Cancel();

		Thread.Sleep(250);

		Assert.Empty(completions);
		Assert.Equal(-1, scheduler.ParsedVersion);
	}

	[Fact]
	public void ParseNow_ParsesSynchronouslyAndReportsDuration()
	{
		var scheduler = CreateScheduler();

		var model = scheduler.ParseNow("# Title", out var duration);

		Assert.Equal("# Title", model.Source);
		Assert.True(duration >= TimeSpan.Zero);
		Assert.Equal(1, scheduler.ParsedVersion);
	}

	[Fact]
	public void Queue_WhenParseThrows_RaisesFailedAndKeepsVersion()
	{
		var scheduler = new MarkdownRenderScheduler(_ => throw new InvalidOperationException("boom"));
		Exception? failure = null;
		scheduler.Failed += (_, exception) => failure = exception;

		scheduler.Queue("# A");

		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (failure is null && DateTime.UtcNow < deadline)
		{
			Thread.Sleep(10);
		}

		Assert.IsType<InvalidOperationException>(failure);
		Assert.Equal(-1, scheduler.ParsedVersion);
	}

	private static MarkdownParseResult WaitForResult(List<MarkdownParseResult> completions)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (completions.Count == 0 && DateTime.UtcNow < deadline)
		{
			Thread.Sleep(10);
		}

		Assert.NotEmpty(completions);
		return completions[0];
	}
}
