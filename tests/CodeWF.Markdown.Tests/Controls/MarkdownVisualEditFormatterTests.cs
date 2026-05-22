using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Shared.Rendering;

using Xunit;

namespace CodeWF.Markdown.Tests.Controls;

public sealed class MarkdownVisualEditFormatterTests
{
	[Fact]
	public void Heading_RoundTripsWithOriginalLevel()
	{
		var editorText = MarkdownVisualEditFormatter.ToEditorText(
			MarkdownBlockKind.Heading,
			"### Old title",
			"Old title");

		var markdown = MarkdownVisualEditFormatter.FromEditorText(
			MarkdownBlockKind.Heading,
			"### Old title",
			"New title");

		Assert.Equal("Old title", editorText);
		Assert.Equal("### New title", markdown);
	}

	[Fact]
	public void Quote_RoundTripsEachLineWithQuotePrefix()
	{
		var editorText = MarkdownVisualEditFormatter.ToEditorText(
			MarkdownBlockKind.Quote,
			"> First\n> Second",
			"First\nSecond");

		var markdown = MarkdownVisualEditFormatter.FromEditorText(
			MarkdownBlockKind.Quote,
			"> First\n> Second",
			"Alpha\nBeta");

		Assert.Equal("First\nSecond", editorText);
		Assert.Equal("> Alpha\n> Beta", markdown);
	}

	[Fact]
	public void CodeBlock_PreservesFenceAndInfoString()
	{
		var source = "```csharp\nConsole.WriteLine(\"Old\");\n```";
		var editorText = MarkdownVisualEditFormatter.ToEditorText(
			MarkdownBlockKind.Code,
			source,
			"Console.WriteLine(\"Old\");");

		var markdown = MarkdownVisualEditFormatter.FromEditorText(
			MarkdownBlockKind.Code,
			source,
			"Console.WriteLine(\"New\");");

		Assert.Equal("Console.WriteLine(\"Old\");", editorText);
		Assert.Equal("```csharp\nConsole.WriteLine(\"New\");\n```", markdown);
	}

	[Fact]
	public void TaskList_StripsAndPreservesCheckedMarker()
	{
		var source = "- [x] Done\n- [x] Also done";
		var editorText = MarkdownVisualEditFormatter.ToEditorText(
			MarkdownBlockKind.List,
			source,
			"- Done\n- Also done");

		var markdown = MarkdownVisualEditFormatter.FromEditorText(
			MarkdownBlockKind.List,
			source,
			"Done\nStill done");

		Assert.Equal("Done\nAlso done", editorText);
		Assert.Equal("- [x] Done\n- [x] Still done", markdown);
	}
}
