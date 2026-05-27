using System.Reflection;
using System.Runtime.CompilerServices;

using Avalonia.Controls.Documents;
using Avalonia.Media;

using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Xunit;

using AvaloniaInline = Avalonia.Controls.Documents.Inline;
using FullMarkdownViewer = CodeWF.Markdown.Controls.MarkdownViewer;
using LiteMarkdownViewer = CodeWF.Markdown.Lite.Controls.MarkdownViewer;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownInlineStyleTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ConvertInlines_WhenParagraphHasMixedEmphasis_KeepsStylesScopedToInlineRuns(bool useLiteViewer)
	{
		var viewer = CreateViewer(useLiteViewer);
		var expectedBoldWeight = useLiteViewer ? FontWeight.SemiBold : FontWeight.Bold;

		var boldInlines = ConvertParagraphInlines(viewer, "**中华**人民共和国");
		var boldSpan = Assert.IsType<Span>(boldInlines[0]);
		var boldRuns = FlattenRuns(boldInlines);
		Assert.NotEqual(expectedBoldWeight, boldSpan.FontWeight);
		Assert.Equal("中华", boldRuns[0].Text);
		Assert.Equal(expectedBoldWeight, boldRuns[0].FontWeight);
		Assert.Equal("人民共和国", boldRuns[1].Text);
		Assert.NotEqual(expectedBoldWeight, boldRuns[1].FontWeight);

		var italicInlines = ConvertParagraphInlines(viewer, "中华*人民*共和国");
		var italicSpan = Assert.IsType<Span>(italicInlines[1]);
		var italicRuns = FlattenRuns(italicInlines);
		Assert.NotEqual(FontStyle.Italic, italicSpan.FontStyle);
		Assert.Equal("中华", italicRuns[0].Text);
		Assert.NotEqual(FontStyle.Italic, italicRuns[0].FontStyle);
		Assert.Equal("人民", italicRuns[1].Text);
		Assert.Equal(FontStyle.Italic, italicRuns[1].FontStyle);
		Assert.Equal("共和国", italicRuns[2].Text);
		Assert.NotEqual(FontStyle.Italic, italicRuns[2].FontStyle);
	}

	[Fact]
	public void ConvertInlines_WhenFullViewerHasMixedStrikethrough_KeepsDecorationScopedToInlineRuns()
	{
		var viewer = CreateViewer(useLiteViewer: false);

		var inlines = ConvertParagraphInlines(viewer, "中华~~人民~~共和国");
		var strikeSpan = Assert.IsType<Span>(inlines[1]);
		var runs = FlattenRuns(inlines);

		Assert.Null(strikeSpan.TextDecorations);
		Assert.Equal("中华", runs[0].Text);
		Assert.Null(runs[0].TextDecorations);
		Assert.Equal("人民", runs[1].Text);
		Assert.Same(TextDecorations.Strikethrough, runs[1].TextDecorations);
		Assert.Equal("共和国", runs[2].Text);
		Assert.Null(runs[2].TextDecorations);
	}

	private static IReadOnlyList<AvaloniaInline> ConvertParagraphInlines(object viewer, string markdown)
	{
		var document = Markdig.Markdown.Parse(markdown, Pipeline);
		var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(document));
		var method = viewer.GetType()
			.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
			.Single(method =>
				method.Name == "ConvertInlines"
				&& method.GetParameters().Length is 1 or 2
				&& method.GetParameters()[0].ParameterType == typeof(ContainerInline));
		var parameters = method.GetParameters().Length == 2
			? new object?[] { paragraph.Inline, false }
			: [paragraph.Inline];

		return ((IEnumerable<AvaloniaInline>)method.Invoke(viewer, parameters)!).ToList();
	}

	private static object CreateViewer(bool useLiteViewer)
	{
		var viewerType = useLiteViewer
			? typeof(LiteMarkdownViewer)
			: typeof(FullMarkdownViewer);
		return RuntimeHelpers.GetUninitializedObject(viewerType);
	}

	private static IReadOnlyList<Run> FlattenRuns(IEnumerable<AvaloniaInline> inlines)
	{
		var runs = new List<Run>();
		foreach (var inline in inlines)
		{
			CollectRuns(inline, runs);
		}

		return runs;
	}

	private static void CollectRuns(AvaloniaInline inline, ICollection<Run> runs)
	{
		if (inline is Run run)
		{
			runs.Add(run);
			return;
		}

		if (inline is Span span)
		{
			foreach (var child in span.Inlines)
			{
				CollectRuns(child, runs);
			}
		}
	}
}
