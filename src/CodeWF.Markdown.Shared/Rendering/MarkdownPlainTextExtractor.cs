using Markdig.Extensions.TaskLists;
using Markdig.Syntax.Inlines;

namespace CodeWF.Markdown.Shared.Rendering;

/// <summary>
/// 从 Markdig 内联树提取纯文本，供复制、选择与无渲染降级路径复用。
/// </summary>
public static class MarkdownPlainTextExtractor
{
	public static string ExtractPlainText(ContainerInline? container)
	{
		if (container is null)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		var child = container.FirstChild;
		while (child is not null)
		{
			parts.Add(child switch
			{
				LiteralInline literal => literal.Content.ToString(),
				CodeInline code => code.Content,
				LineBreakInline => Environment.NewLine,
				TaskList => string.Empty,
				LinkInline { IsImage: true } image => ExtractImageText(image),
				ContainerInline nested => ExtractPlainText(nested),
				_ when MarkdownChemistry.TryGetChemInlinePlainText(child, out var chemText) => chemText,
				_ => IsTypeNameFallback(child.ToString() ?? string.Empty, child.GetType())
					? string.Empty
					: MarkdownChemistry.ReplaceChemCommandsWithPlainText(child.ToString() ?? string.Empty)
			});
			child = child.NextSibling;
		}

		return string.Concat(parts);
	}

	public static string ExtractImageText(LinkInline image)
	{
		var altText = ExtractPlainText((ContainerInline)image);
		if (!string.IsNullOrWhiteSpace(altText))
		{
			return altText;
		}

		return string.IsNullOrWhiteSpace(image.Url) ? "[image]" : image.Url!;
	}

	/// <summary>
	/// Markdig 对未识别节点 ToString 会回退成类型名，这里识别该情形以避免把类型名当正文输出。
	/// </summary>
	public static bool IsTypeNameFallback(string text, Type type)
	{
		return string.Equals(text, type.FullName, StringComparison.Ordinal)
			   || string.Equals(text, type.Name, StringComparison.Ordinal);
	}
}
