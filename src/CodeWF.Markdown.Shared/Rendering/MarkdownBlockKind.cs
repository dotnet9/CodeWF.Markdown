using Markdig.Syntax;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Tables;

namespace CodeWF.Markdown.Shared.Rendering;

internal enum MarkdownBlockKind
{
	Unknown,
	Paragraph,
	Heading,
	Code,
	List,
	Quote,
	ThematicBreak,
	Table,
	Footnote,
	FootnoteGroup,
	Html,
	LinkReference,
	Toc,
	Math,
	Image,
	Slide
}

[Flags]
internal enum MarkdownDependencyFlags
{
	None = 0,
	Toc = 1,
	Footnote = 1 << 1,
	LinkReference = 1 << 2,
	OrderedList = 1 << 3,
	TableShape = 1 << 4,
	Global = Toc | Footnote | LinkReference
}

internal static class MarkdownBlockClassifier
{
	public static MarkdownBlockKind GetKind(Block block, string sourceText)
	{
		if (IsToc(block, sourceText))
		{
			return MarkdownBlockKind.Toc;
		}

		return block switch
		{
			HeadingBlock => MarkdownBlockKind.Heading,
			_ when IsMath(block, sourceText) => MarkdownBlockKind.Math,
			ParagraphBlock paragraph when IsImageParagraph(paragraph) => MarkdownBlockKind.Image,
			ParagraphBlock => MarkdownBlockKind.Paragraph,
			FencedCodeBlock => MarkdownBlockKind.Code,
			CodeBlock => MarkdownBlockKind.Code,
			ListBlock => MarkdownBlockKind.List,
			QuoteBlock => MarkdownBlockKind.Quote,
			ThematicBreakBlock => MarkdownBlockKind.ThematicBreak,
			Table => MarkdownBlockKind.Table,
			FootnoteGroup => MarkdownBlockKind.FootnoteGroup,
			Footnote => MarkdownBlockKind.Footnote,
			HtmlBlock => MarkdownBlockKind.Html,
			LinkReferenceDefinitionGroup => MarkdownBlockKind.LinkReference,
			LinkReferenceDefinition => MarkdownBlockKind.LinkReference,
			_ => MarkdownBlockKind.Unknown
		};
	}

	public static MarkdownDependencyFlags GetDependencyFlags(Block block, MarkdownBlockKind kind)
	{
		var flags = MarkdownDependencyFlags.None;
		if (kind == MarkdownBlockKind.Toc)
		{
			flags |= MarkdownDependencyFlags.Toc;
		}

		if (kind is MarkdownBlockKind.Footnote or MarkdownBlockKind.FootnoteGroup)
		{
			flags |= MarkdownDependencyFlags.Footnote;
		}

		if (kind == MarkdownBlockKind.LinkReference)
		{
			flags |= MarkdownDependencyFlags.LinkReference;
		}

		if (block is ListBlock { IsOrdered: true })
		{
			flags |= MarkdownDependencyFlags.OrderedList;
		}

		if (block is Table)
		{
			flags |= MarkdownDependencyFlags.TableShape;
		}

		return flags;
	}

	private static bool IsToc(Block block, string sourceText)
	{
		var typeName = block.GetType().Name;
		if (typeName.Contains("TableOfContents", StringComparison.OrdinalIgnoreCase)
			|| typeName.Equals("TocBlock", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return string.Equals(sourceText.Trim(), "[TOC]", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsImageParagraph(ParagraphBlock paragraph)
	{
		var first = paragraph.Inline?.FirstChild;
		if (first is not Markdig.Syntax.Inlines.LinkInline { IsImage: true })
		{
			return false;
		}

		for (var sibling = first.NextSibling; sibling is not null; sibling = sibling.NextSibling)
		{
			if (sibling is Markdig.Syntax.Inlines.LiteralInline literal
				&& string.IsNullOrWhiteSpace(literal.Content.ToString()))
			{
				continue;
			}

			return false;
		}

		return true;
	}

	private static bool IsMath(Block block, string sourceText)
	{
		if (block.GetType().Name.Contains("Math", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var trimmed = sourceText.Trim();
		return (trimmed.StartsWith("$$", StringComparison.Ordinal) && trimmed.EndsWith("$$", StringComparison.Ordinal))
			   || (trimmed.StartsWith(@"\[", StringComparison.Ordinal) && trimmed.EndsWith(@"\]", StringComparison.Ordinal));
	}
}
