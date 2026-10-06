using Markdig.Syntax;

namespace CodeWF.Markdown.Shared.Rendering;

/// <summary>
/// 大纲条目：标题层级（1-6）、标题纯文本与 1 起始的源码行号。
/// </summary>
public sealed record MarkdownOutlineItem(int Level, string Title, int Line);

/// <summary>
/// 从已解析文档模型提取标题大纲。基于 Markdig AST，
/// 代码块/HTML 块内的 "#" 不会被误判为标题。
/// </summary>
public static class MarkdownOutlineExtractor
{
	public static IReadOnlyList<MarkdownOutlineItem> Extract(MarkdownDocumentModel? model)
	{
		if (model is null || model.Blocks.Count == 0)
		{
			return [];
		}

		var items = new List<MarkdownOutlineItem>();
		var source = model.Source;
		foreach (var block in model.Blocks)
		{
			if (block.Kind != MarkdownBlockKind.Heading || block.SyntaxBlock is not HeadingBlock heading)
			{
				continue;
			}

			var title = heading.Inline is null
				? string.Empty
				: MarkdownPlainTextExtractor.ExtractPlainText(heading.Inline).Trim();
			if (title.Length == 0)
			{
				continue;
			}

			items.Add(new MarkdownOutlineItem(
				Math.Clamp(heading.Level, 1, 6),
				title,
				GetLine(source, block.SourceSpan.Start)));
		}

		return items;
	}

	/// <summary>
	/// 返回第一个标题的文本，用于导出/打印的默认标题；无标题时返回 null。
	/// </summary>
	public static string? FindFirstHeading(MarkdownDocumentModel? model)
	{
		var items = Extract(model);
		return items.Count == 0 ? null : items[0].Title;
	}

	private static int GetLine(string source, int offset)
	{
		var line = 1;
		var limit = Math.Clamp(offset, 0, source.Length);
		for (var i = 0; i < limit; i++)
		{
			if (source[i] == '\n')
			{
				line++;
			}
		}

		return line;
	}
}
