using CodeWF.Markdown.Shared.Rendering;

using Markdig;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownDiffServiceTests
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	[Fact]
	public void Compare_WhenParagraphChanges_ReplacesOnlyChangedBlock()
	{
		var oldModel = MarkdownParser.Parse("# Title\n\nAlpha\n\nBeta", Pipeline);
		var newModel = MarkdownParser.Parse("# Title\n\nAlpha changed\n\nBeta", Pipeline);

		var diff = MarkdownDiffService.Compare(oldModel, newModel);

		Assert.False(diff.RequiresFullRender);
		Assert.Equal(1, diff.ReplaceStartIndex);
		Assert.Equal(2, diff.ReplaceEndIndex);
		Assert.Equal(1, diff.NewStartIndex);
		Assert.Equal(2, diff.NewEndIndex);
	}

	[Fact]
	public void Compare_WhenAppendingBlock_ReplacesOnlyTail()
	{
		var oldModel = MarkdownParser.Parse("# Title\n\nAlpha", Pipeline);
		var newModel = MarkdownParser.Parse("# Title\n\nAlpha\n\nBeta", Pipeline);

		var diff = MarkdownDiffService.Compare(oldModel, newModel);

		Assert.False(diff.RequiresFullRender);
		Assert.Equal(2, diff.ReplaceStartIndex);
		Assert.Equal(2, diff.ReplaceEndIndex);
		Assert.Equal(2, diff.NewStartIndex);
		Assert.Equal(3, diff.NewEndIndex);
	}

	[Fact]
	public void Compare_WhenTocAppears_RequiresFullRender()
	{
		var oldModel = MarkdownParser.Parse("# Title\n\nAlpha", Pipeline);
		var newModel = MarkdownParser.Parse("[TOC]\n\n# Title\n\nAlpha", Pipeline);

		var diff = MarkdownDiffService.Compare(oldModel, newModel);

		Assert.True(diff.RequiresFullRender);
	}

	[Fact]
	public void Compare_WhenFootnoteDefinitionChanges_RequiresFullRender()
	{
		var oldModel = MarkdownParser.Parse("Text with footnote[^a].\n\n[^a]: Old note", Pipeline);
		var newModel = MarkdownParser.Parse("Text with footnote[^a].\n\n[^a]: New note", Pipeline);

		var diff = MarkdownDiffService.Compare(oldModel, newModel);

		Assert.True(diff.RequiresFullRender);
	}

	[Fact]
	public void Parse_ProvidesPlainTextSpansForSelection()
	{
		var markdown = "# Title\n\n- One\n- Two\n\n```csharp\nvar x = 1;\n```";
		var model = MarkdownParser.Parse(markdown, Pipeline);

		Assert.Equal(markdown.Length, model.Source.Length);
		Assert.Contains("Title", model.PlainText);
		Assert.Contains("One", model.PlainText);
		Assert.Contains("var x = 1;", model.PlainText);
		Assert.All(model.Blocks.Where(block => block.PlainText.Length > 0), block =>
		{
			Assert.True(block.PlainTextSpan.End >= block.PlainTextSpan.Start);
			Assert.True(block.SourceSpan.End >= block.SourceSpan.Start);
		});
	}

	[Fact]
	public void Parse_IncludesImageTextForSelection()
	{
		var markdown = "Before ![Diagram alt](diagram.png) after\n\n![](fallback.png)";
		var model = MarkdownParser.Parse(markdown, Pipeline);

		Assert.Contains("Before Diagram alt after", model.PlainText);
		Assert.Contains("fallback.png", model.PlainText);
	}

	[Fact]
	public void Chemistry_ParsesFormulaIntoSelectableRuns()
	{
		Assert.True(MarkdownChemistry.TryParseLatex(@"\ce{H2SO4}", out var expression));

		Assert.Equal("H2SO4", expression.PlainText);
		Assert.Collection(
			expression.Inlines,
			inline =>
			{
				Assert.Equal(MarkdownChemInlineKind.Text, inline.Kind);
				Assert.Equal("H", inline.Text);
			},
			inline =>
			{
				Assert.Equal(MarkdownChemInlineKind.Subscript, inline.Kind);
				Assert.Equal("2", inline.Text);
			},
			inline =>
			{
				Assert.Equal(MarkdownChemInlineKind.Text, inline.Kind);
				Assert.Equal("SO", inline.Text);
			},
			inline =>
			{
				Assert.Equal(MarkdownChemInlineKind.Subscript, inline.Kind);
				Assert.Equal("4", inline.Text);
			});
	}

	[Fact]
	public void Chemistry_ParsesReactionArrowsAndCharges()
	{
		Assert.True(MarkdownChemistry.TryParseLatex(@"\ce{H+ + OH- -> H2O}", out var expression));

		Assert.Equal("H+ + OH- \u2192 H2O", expression.PlainText);
		Assert.Contains(expression.Inlines, inline =>
			inline.Kind == MarkdownChemInlineKind.Superscript && inline.Text == "+");
		Assert.Contains(expression.Inlines, inline =>
			inline.Kind == MarkdownChemInlineKind.Superscript && inline.Text == "-");
	}

	[Fact]
	public void Parse_UsesChemistryPlainTextForSelection()
	{
		var model = MarkdownParser.Parse(@"Water $\ce{H2O}$ and acid $\ce{H+}$.", Pipeline);

		Assert.Contains("Water H2O and acid H+.", model.PlainText);
	}

	[Fact]
	public void Parse_UsesChemistryBlockPlainTextForSelection()
	{
		var model = MarkdownParser.Parse("$$\n\\ce{H2 + O2 -> H2O}\n$$", Pipeline);

		Assert.Equal("H2 + O2 \u2192 H2O", model.PlainText);
		Assert.Equal(MarkdownBlockKind.Math, model.Blocks.Single().Kind);
	}
}
