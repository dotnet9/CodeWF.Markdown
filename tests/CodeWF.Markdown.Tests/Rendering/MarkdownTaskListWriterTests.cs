using CodeWF.Markdown.Shared.Rendering;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownTaskListWriterTests
{
	[Fact]
	public void FindTaskMarkers_ReturnsUncheckedAndCheckedItems()
	{
		var markdown = "- [ ] one\n- [x] two\n- [X] three\n- plain";

		var markers = MarkdownTaskListWriter.FindTaskMarkers(markdown);

		Assert.Collection(
			markers,
			marker =>
			{
				Assert.Equal(2, marker.Start);
				Assert.Equal(5, marker.End);
				Assert.False(marker.IsChecked);
				Assert.Equal(1, marker.Line);
			},
			marker =>
			{
				Assert.True(marker.IsChecked);
				Assert.Equal(2, marker.Line);
			},
			marker =>
			{
				Assert.True(marker.IsChecked);
				Assert.Equal(3, marker.Line);
			});
	}

	[Fact]
	public void FindTaskMarkers_WhenMarkerIsInsideCodeFence_IgnoresIt()
	{
		var markdown = "```\n- [ ] not a task\n```\n\n- [ ] real";

		var marker = Assert.Single(MarkdownTaskListWriter.FindTaskMarkers(markdown));

		Assert.Equal(5, marker.Line);
		Assert.False(marker.IsChecked);
	}

	[Fact]
	public void FindTaskMarkers_RecognizesNestedAndQuotedItems()
	{
		var markdown = "- [ ] a\n  - [x] b\n> - [ ] c\n1. [ ] d";

		var markers = MarkdownTaskListWriter.FindTaskMarkers(markdown);

		Assert.Equal(4, markers.Count);
		Assert.True(markers[1].IsChecked);
	}

	[Fact]
	public void SetTaskState_WhenStateChanges_ReturnsNewMarkdownAndChangedSpan()
	{
		var markdown = "- [ ] one\n- [ ] two";

		var result = MarkdownTaskListWriter.SetTaskState(markdown, markerStart: 2, isChecked: true);

		Assert.NotNull(result);
		Assert.Equal("- [x] one\n- [ ] two", result!.Markdown);
		Assert.Equal(new MarkdownTextSpan(2, 5), result.ChangedSpan);
		Assert.True(result.IsChecked);
	}

	[Fact]
	public void SetTaskState_WhenStateIsUnchanged_ReturnsNull()
	{
		Assert.Null(MarkdownTaskListWriter.SetTaskState("- [x] one", 2, isChecked: true));
	}

	[Fact]
	public void SetTaskState_WhenOffsetIsInTheMiddleOfAMarker_StillRewritesIt()
	{
		var result = MarkdownTaskListWriter.SetTaskStateAtOffset("- [ ] one", sourceOffset: 3, isChecked: true);

		Assert.NotNull(result);
		Assert.Equal("- [x] one", result!.Markdown);
	}

	[Fact]
	public void SetTaskStateInBlock_WhenRangeStartsAtTheTaskItem_RewritesThatItem()
	{
		var markdown = "# Title\n\n- [ ] keep\n- [x] toggle me";

		var result = MarkdownTaskListWriter.SetTaskStateInBlock(
			markdown,
			blockStart: 20,
			blockEnd: markdown.Length,
			isChecked: false);

		Assert.NotNull(result);
		Assert.Equal("# Title\n\n- [ ] keep\n- [ ] toggle me", result!.Markdown);
		Assert.Equal(new MarkdownTextSpan(22, 25), result.ChangedSpan);
	}

	[Fact]
	public void SetTaskStateInBlock_WhenBlockHasNoTaskItem_ReturnsNull()
	{
		Assert.Null(MarkdownTaskListWriter.SetTaskStateInBlock("- plain item", 0, 12, isChecked: true));
	}
}
