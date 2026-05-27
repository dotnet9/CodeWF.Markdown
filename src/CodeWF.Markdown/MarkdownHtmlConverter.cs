using System.Net;
using System.Text;

namespace CodeWF.Markdown;

/// <summary>
/// Converts HTML copied from browsers or rich editors into Markdown text.
/// </summary>
public static class MarkdownHtmlConverter
{
	private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
	{
		"area",
		"base",
		"br",
		"col",
		"embed",
		"hr",
		"img",
		"input",
		"link",
		"meta",
		"param",
		"source",
		"track",
		"wbr"
	};

	private static readonly HashSet<string> BlockElements = new(StringComparer.OrdinalIgnoreCase)
	{
		"address",
		"article",
		"aside",
		"blockquote",
		"body",
		"dd",
		"details",
		"div",
		"dl",
		"dt",
		"fieldset",
		"figcaption",
		"figure",
		"footer",
		"form",
		"h1",
		"h2",
		"h3",
		"h4",
		"h5",
		"h6",
		"header",
		"hr",
		"html",
		"li",
		"main",
		"nav",
		"ol",
		"p",
		"pre",
		"section",
		"table",
		"tbody",
		"td",
		"tfoot",
		"th",
		"thead",
		"tr",
		"ul"
	};

	private static readonly HashSet<string> IgnoredElements = new(StringComparer.OrdinalIgnoreCase)
	{
		"base",
		"head",
		"link",
		"meta",
		"noscript",
		"script",
		"style",
		"svg",
		"title"
	};

	/// <summary>
	/// Converts an HTML fragment or document into Markdown.
	/// </summary>
	public static string Html2Markdown(string htmlContent)
	{
		ArgumentNullException.ThrowIfNull(htmlContent);

		var fragment = ExtractClipboardFragment(htmlContent);
		if (string.IsNullOrWhiteSpace(fragment))
		{
			return string.Empty;
		}

		var root = HtmlParser.Parse(fragment);
		var markdown = HasBlockChild(root)
			? ConvertBlocks(root.Children, 0)
			: ConvertInlineChildren(root.Children);
		return NormalizeMarkdown(markdown);
	}

	private static string ExtractClipboardFragment(string htmlContent)
	{
		var start = htmlContent.IndexOf(MarkdownHtmlClipboard.StartFragmentMarker, StringComparison.Ordinal);
		var end = htmlContent.IndexOf(MarkdownHtmlClipboard.EndFragmentMarker, StringComparison.Ordinal);
		if (start >= 0 && end > start)
		{
			return htmlContent[(start + MarkdownHtmlClipboard.StartFragmentMarker.Length)..end];
		}

		if (TryExtractWindowsClipboardFragment(htmlContent, out var fragment))
		{
			return fragment;
		}

		return htmlContent;
	}

	private static bool TryExtractWindowsClipboardFragment(string htmlContent, out string fragment)
	{
		fragment = string.Empty;
		if (!htmlContent.StartsWith("Version:", StringComparison.OrdinalIgnoreCase)
		    || !TryReadClipboardOffset(htmlContent, "StartFragment", out var startFragment)
		    || !TryReadClipboardOffset(htmlContent, "EndFragment", out var endFragment)
		    || endFragment <= startFragment)
		{
			return false;
		}

		var bytes = Encoding.UTF8.GetBytes(htmlContent);
		if (startFragment < 0 || endFragment > bytes.Length)
		{
			return false;
		}

		fragment = Encoding.UTF8.GetString(bytes, startFragment, endFragment - startFragment);
		return true;
	}

	private static bool TryReadClipboardOffset(string htmlContent, string name, out int offset)
	{
		offset = 0;
		var prefix = name + ":";
		var index = htmlContent.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
		if (index < 0)
		{
			return false;
		}

		index += prefix.Length;
		var end = htmlContent.IndexOfAny(['\r', '\n'], index);
		if (end < 0)
		{
			end = htmlContent.Length;
		}

		return int.TryParse(htmlContent.AsSpan(index, end - index), out offset);
	}

	private static string ConvertBlocks(IReadOnlyList<HtmlNode> nodes, int listDepth)
	{
		var parts = new List<string>();
		foreach (var node in nodes)
		{
			var markdown = ConvertBlock(node, listDepth).Trim();
			if (markdown.Length > 0)
			{
				parts.Add(markdown);
			}
		}

		return string.Join("\n\n", parts);
	}

	private static string ConvertBlock(HtmlNode node, int listDepth)
	{
		if (node.IsText)
		{
			return CollapseInlineWhitespace(WebUtility.HtmlDecode(node.Text)).Trim();
		}

		if (IgnoredElements.Contains(node.Name))
		{
			return string.Empty;
		}

		return node.Name switch
		{
			"html" or "body" => ConvertBlocks(node.Children, listDepth),
			"h1" => CreateHeading(node, 1),
			"h2" => CreateHeading(node, 2),
			"h3" => CreateHeading(node, 3),
			"h4" => CreateHeading(node, 4),
			"h5" => CreateHeading(node, 5),
			"h6" => CreateHeading(node, 6),
			"p" => ConvertInlineChildren(node.Children).Trim(),
			"br" => "\n",
			"hr" => "---",
			"pre" => ConvertPreBlock(node),
			"blockquote" => ConvertBlockQuote(node, listDepth),
			"ul" => ConvertList(node, ordered: false, listDepth),
			"ol" => ConvertList(node, ordered: true, listDepth),
			"table" => ConvertTable(node),
			"thead" or "tbody" or "tfoot" => ConvertBlocks(node.Children, listDepth),
			"tr" => string.Empty,
			"li" => ConvertListItem(node, listDepth),
			"div" or "section" or "article" or "main" or "header" or "footer" or "aside" or "nav"
				=> HasBlockChild(node) ? ConvertBlocks(node.Children, listDepth) : ConvertInlineChildren(node.Children).Trim(),
			_ => HasBlockChild(node) ? ConvertBlocks(node.Children, listDepth) : ConvertInline(node).Trim()
		};
	}

	private static string CreateHeading(HtmlNode node, int level)
	{
		var text = ConvertInlineChildren(node.Children).Trim();
		return text.Length == 0 ? string.Empty : $"{new string('#', level)} {text}";
	}

	private static string ConvertPreBlock(HtmlNode node)
	{
		var codeNode = FindFirstChild(node, "code");
		var language = codeNode is null ? string.Empty : GetCodeLanguage(codeNode);
		var code = WebUtility.HtmlDecode(GetInnerText(codeNode ?? node))
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace('\r', '\n')
			.Trim('\n');

		return $"```{language}\n{code}\n```";
	}

	private static string GetCodeLanguage(HtmlNode codeNode)
	{
		if (!codeNode.Attributes.TryGetValue("class", out var className))
		{
			return string.Empty;
		}

		foreach (var item in className.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (item.StartsWith("language-", StringComparison.OrdinalIgnoreCase))
			{
				return item["language-".Length..];
			}

			if (item.StartsWith("lang-", StringComparison.OrdinalIgnoreCase))
			{
				return item["lang-".Length..];
			}
		}

		return string.Empty;
	}

	private static string ConvertBlockQuote(HtmlNode node, int listDepth)
	{
		var content = ConvertBlocks(node.Children, listDepth).Trim();
		if (content.Length == 0)
		{
			return string.Empty;
		}

		var lines = content.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			lines[i] = lines[i].Length == 0 ? ">" : $"> {lines[i]}";
		}

		return string.Join('\n', lines);
	}

	private static string ConvertList(HtmlNode node, bool ordered, int listDepth)
	{
		var items = node.Children
			.Where(child => !child.IsText && string.Equals(child.Name, "li", StringComparison.OrdinalIgnoreCase))
			.ToArray();
		if (items.Length == 0)
		{
			return string.Empty;
		}

		var index = ReadStartIndex(node);
		var builder = new StringBuilder();
		foreach (var item in items)
		{
			var itemMarkdown = ConvertListItem(item, listDepth + 1).Trim();
			if (itemMarkdown.Length == 0)
			{
				index++;
				continue;
			}

			var marker = ordered ? $"{index}. " : "- ";
			AppendListItem(builder, itemMarkdown, marker, listDepth);
			index++;
		}

		return builder.ToString().TrimEnd();
	}

	private static int ReadStartIndex(HtmlNode node)
	{
		return node.Attributes.TryGetValue("start", out var start)
		       && int.TryParse(start, out var value)
		       && value > 0
			? value
			: 1;
	}

	private static void AppendListItem(StringBuilder builder, string itemMarkdown, string marker, int listDepth)
	{
		var indent = new string(' ', listDepth * 2);
		var continuationIndent = indent + new string(' ', marker.Length);
		var lines = itemMarkdown.Split('\n');
		builder.Append(indent);
		builder.Append(marker);
		builder.AppendLine(lines[0]);
		for (var i = 1; i < lines.Length; i++)
		{
			if (lines[i].Length == 0)
			{
				builder.AppendLine();
			}
			else
			{
				builder.Append(continuationIndent);
				builder.AppendLine(lines[i]);
			}
		}
	}

	private static string ConvertListItem(HtmlNode node, int listDepth)
	{
		var parts = new List<string>();
		var inlineBuilder = new StringBuilder();
		foreach (var child in node.Children)
		{
			if (child.IsText || !BlockElements.Contains(child.Name))
			{
				inlineBuilder.Append(ConvertInline(child));
				continue;
			}

			FlushInlineListItem(parts, inlineBuilder);
			var markdown = child.Name is "ul" or "ol"
				? ConvertBlock(child, listDepth).Trim()
				: ConvertBlock(child, listDepth).Trim();
			if (markdown.Length > 0)
			{
				parts.Add(markdown);
			}
		}

		FlushInlineListItem(parts, inlineBuilder);
		return string.Join('\n', parts);
	}

	private static void FlushInlineListItem(ICollection<string> parts, StringBuilder inlineBuilder)
	{
		var inlineText = CleanupInlineText(inlineBuilder.ToString()).Trim();
		if (inlineText.Length > 0)
		{
			parts.Add(inlineText);
		}

		inlineBuilder.Clear();
	}

	private static string ConvertTable(HtmlNode node)
	{
		var rows = CollectRows(node).ToArray();
		if (rows.Length == 0)
		{
			return string.Empty;
		}

		var convertedRows = rows
			.Select(row => row.Children
				.Where(cell => !cell.IsText && (cell.Name is "td" or "th"))
				.Select(ConvertTableCell)
				.ToArray())
			.Where(row => row.Length > 0)
			.ToArray();

		if (convertedRows.Length == 0)
		{
			return string.Empty;
		}

		var columnCount = convertedRows.Max(row => row.Length);
		var builder = new StringBuilder();
		AppendTableRow(builder, PadRow(convertedRows[0], columnCount));
		AppendTableRow(builder, Enumerable.Repeat("---", columnCount).ToArray());
		foreach (var row in convertedRows.Skip(1))
		{
			AppendTableRow(builder, PadRow(row, columnCount));
		}

		return builder.ToString().TrimEnd();
	}

	private static IEnumerable<HtmlNode> CollectRows(HtmlNode node)
	{
		foreach (var child in node.Children)
		{
			if (child.IsText)
			{
				continue;
			}

			if (string.Equals(child.Name, "tr", StringComparison.OrdinalIgnoreCase))
			{
				yield return child;
				continue;
			}

			foreach (var row in CollectRows(child))
			{
				yield return row;
			}
		}
	}

	private static string ConvertTableCell(HtmlNode cell)
	{
		var text = HasBlockChild(cell)
			? ConvertBlocks(cell.Children, 0)
			: ConvertInlineChildren(cell.Children);
		return text
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace('\r', '\n')
			.Replace("\n", "<br>", StringComparison.Ordinal)
			.Replace("|", "\\|", StringComparison.Ordinal)
			.Trim();
	}

	private static string[] PadRow(string[] row, int columnCount)
	{
		if (row.Length == columnCount)
		{
			return row;
		}

		var padded = new string[columnCount];
		Array.Copy(row, padded, row.Length);
		for (var i = row.Length; i < padded.Length; i++)
		{
			padded[i] = string.Empty;
		}

		return padded;
	}

	private static void AppendTableRow(StringBuilder builder, IReadOnlyList<string> row)
	{
		builder.Append("| ");
		builder.Append(string.Join(" | ", row));
		builder.AppendLine(" |");
	}

	private static string ConvertInline(HtmlNode node)
	{
		if (node.IsText)
		{
			return CollapseInlineWhitespace(WebUtility.HtmlDecode(node.Text));
		}

		if (IgnoredElements.Contains(node.Name))
		{
			return string.Empty;
		}

		return node.Name switch
		{
			"br" => "\n",
			"strong" or "b" => WrapInline("**", ConvertInlineChildren(node.Children)),
			"em" or "i" => WrapInline("*", ConvertInlineChildren(node.Children)),
			"del" or "s" or "strike" => WrapInline("~~", ConvertInlineChildren(node.Children)),
			"code" => CreateInlineCode(ConvertInlineChildren(node.Children)),
			"a" => ConvertLink(node),
			"img" => ConvertImage(node),
			"input" => ConvertInput(node),
			"p" => ConvertInlineChildren(node.Children),
			_ => ConvertInlineChildren(node.Children)
		};
	}

	private static string ConvertInlineChildren(IReadOnlyList<HtmlNode> nodes)
	{
		var builder = new StringBuilder();
		foreach (var node in nodes)
		{
			builder.Append(ConvertInline(node));
		}

		return CleanupInlineText(builder.ToString());
	}

	private static string WrapInline(string marker, string content)
	{
		content = content.Trim();
		return content.Length == 0 ? string.Empty : $"{marker}{content}{marker}";
	}

	private static string CreateInlineCode(string content)
	{
		content = content.Trim();
		if (content.Length == 0)
		{
			return string.Empty;
		}

		var marker = content.Contains('`', StringComparison.Ordinal) ? "``" : "`";
		var padding = marker.Length > 1 ? " " : string.Empty;
		return $"{marker}{padding}{content}{padding}{marker}";
	}

	private static string ConvertLink(HtmlNode node)
	{
		var text = ConvertInlineChildren(node.Children).Trim();
		if (!node.Attributes.TryGetValue("href", out var href) || string.IsNullOrWhiteSpace(href))
		{
			return text;
		}

		href = WebUtility.HtmlDecode(href).Trim();
		if (text.Length == 0)
		{
			text = href;
		}

		return $"[{EscapeLinkText(text)}]({EscapeLinkTarget(href)})";
	}

	private static string ConvertImage(HtmlNode node)
	{
		if (!node.Attributes.TryGetValue("src", out var src) || string.IsNullOrWhiteSpace(src))
		{
			return string.Empty;
		}

		node.Attributes.TryGetValue("alt", out var alt);
		return $"![{EscapeLinkText(WebUtility.HtmlDecode(alt ?? string.Empty).Trim())}]({EscapeLinkTarget(WebUtility.HtmlDecode(src).Trim())})";
	}

	private static string ConvertInput(HtmlNode node)
	{
		if (!node.Attributes.TryGetValue("type", out var type)
		    || !type.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}

		var isChecked = node.Attributes.ContainsKey("checked");
		return isChecked ? "[x] " : "[ ] ";
	}

	private static string EscapeLinkText(string text)
	{
		return text
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("[", "\\[", StringComparison.Ordinal)
			.Replace("]", "\\]", StringComparison.Ordinal);
	}

	private static string EscapeLinkTarget(string target)
	{
		return target.Replace(")", "%29", StringComparison.Ordinal);
	}

	private static string GetInnerText(HtmlNode node)
	{
		if (node.IsText)
		{
			return node.Text;
		}

		var builder = new StringBuilder();
		foreach (var child in node.Children)
		{
			if (!IgnoredElements.Contains(child.Name))
			{
				builder.Append(GetInnerText(child));
			}
		}

		return builder.ToString();
	}

	private static HtmlNode? FindFirstChild(HtmlNode node, string name)
	{
		foreach (var child in node.Children)
		{
			if (!child.IsText && string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}

		return null;
	}

	private static bool HasBlockChild(HtmlNode node)
	{
		return node.Children.Any(child => !child.IsText && BlockElements.Contains(child.Name));
	}

	private static string CollapseInlineWhitespace(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		var builder = new StringBuilder(text.Length);
		var inWhitespace = false;
		foreach (var c in text)
		{
			if (c == '\r' || c == '\n' || c == '\t' || c == ' ')
			{
				if (!inWhitespace)
				{
					builder.Append(' ');
					inWhitespace = true;
				}

				continue;
			}

			builder.Append(c);
			inWhitespace = false;
		}

		return builder.ToString();
	}

	private static string CleanupInlineText(string text)
	{
		return text
			.Replace(" \n", "\n", StringComparison.Ordinal)
			.Replace("\n ", "\n", StringComparison.Ordinal);
	}

	private static string NormalizeMarkdown(string markdown)
	{
		var normalized = markdown
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace('\r', '\n')
			.Trim();

		while (normalized.Contains("\n\n\n", StringComparison.Ordinal))
		{
			normalized = normalized.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
		}

		return normalized;
	}

	private sealed class HtmlNode
	{
		private HtmlNode(string name, string text, Dictionary<string, string>? attributes)
		{
			Name = name;
			Text = text;
			Attributes = attributes ?? [];
		}

		public string Name { get; }

		public string Text { get; }

		public Dictionary<string, string> Attributes { get; }

		public List<HtmlNode> Children { get; } = [];

		public bool IsText => Name.Length == 0;

		public static HtmlNode Element(string name, Dictionary<string, string>? attributes = null)
		{
			return new HtmlNode(name.ToLowerInvariant(), string.Empty, attributes);
		}

		public static HtmlNode TextNode(string text)
		{
			return new HtmlNode(string.Empty, text, null);
		}
	}

	private static class HtmlParser
	{
		public static HtmlNode Parse(string html)
		{
			var root = HtmlNode.Element("#document");
			var stack = new Stack<HtmlNode>();
			stack.Push(root);
			var index = 0;
			while (index < html.Length)
			{
				if (html[index] != '<')
				{
					var nextTag = html.IndexOf('<', index);
					if (nextTag < 0)
					{
						nextTag = html.Length;
					}

					AddText(stack.Peek(), html[index..nextTag]);
					index = nextTag;
					continue;
				}

				if (StartsWith(html, index, "<!--"))
				{
					var commentEnd = html.IndexOf("-->", index + 4, StringComparison.Ordinal);
					index = commentEnd < 0 ? html.Length : commentEnd + 3;
					continue;
				}

				var tagEnd = FindTagEnd(html, index + 1);
				if (tagEnd < 0)
				{
					AddText(stack.Peek(), html[index..]);
					break;
				}

				var tagContent = html.Substring(index + 1, tagEnd - index - 1).Trim();
				index = tagEnd + 1;
				if (tagContent.Length == 0)
				{
					continue;
				}

				if (tagContent[0] is '!' or '?')
				{
					continue;
				}

				if (tagContent[0] == '/')
				{
					CloseElement(stack, ReadTagName(tagContent.AsSpan(1)));
					continue;
				}

				var isSelfClosing = tagContent.EndsWith("/", StringComparison.Ordinal);
				if (isSelfClosing)
				{
					tagContent = tagContent[..^1].TrimEnd();
				}

				var name = ReadTagName(tagContent.AsSpan());
				if (name.Length == 0)
				{
					continue;
				}

				if (IgnoredElements.Contains(name) && !VoidElements.Contains(name))
				{
					index = SkipElementContent(html, index, name);
					continue;
				}

				AutoClose(stack, name);
				var attributes = ParseAttributes(tagContent.AsSpan(name.Length));
				var node = HtmlNode.Element(name, attributes);
				stack.Peek().Children.Add(node);
				if (!isSelfClosing && !VoidElements.Contains(name))
				{
					stack.Push(node);
				}
			}

			return root;
		}

		private static void AddText(HtmlNode parent, string text)
		{
			if (text.Length > 0)
			{
				parent.Children.Add(HtmlNode.TextNode(text));
			}
		}

		private static int FindTagEnd(string html, int start)
		{
			var quote = '\0';
			for (var i = start; i < html.Length; i++)
			{
				var c = html[i];
				if (quote != '\0')
				{
					if (c == quote)
					{
						quote = '\0';
					}

					continue;
				}

				if (c is '"' or '\'')
				{
					quote = c;
					continue;
				}

				if (c == '>')
				{
					return i;
				}
			}

			return -1;
		}

		private static string ReadTagName(ReadOnlySpan<char> content)
		{
			var length = 0;
			while (length < content.Length)
			{
				var c = content[length];
				if (!char.IsLetterOrDigit(c) && c != '-' && c != ':')
				{
					break;
				}

				length++;
			}

			return length == 0 ? string.Empty : content[..length].ToString().ToLowerInvariant();
		}

		private static Dictionary<string, string> ParseAttributes(ReadOnlySpan<char> content)
		{
			var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var index = 0;
			while (index < content.Length)
			{
				while (index < content.Length && char.IsWhiteSpace(content[index]))
				{
					index++;
				}

				var nameStart = index;
				while (index < content.Length && IsAttributeNameCharacter(content[index]))
				{
					index++;
				}

				if (index == nameStart)
				{
					index++;
					continue;
				}

				var name = content[nameStart..index].ToString();
				while (index < content.Length && char.IsWhiteSpace(content[index]))
				{
					index++;
				}

				var value = string.Empty;
				if (index < content.Length && content[index] == '=')
				{
					index++;
					while (index < content.Length && char.IsWhiteSpace(content[index]))
					{
						index++;
					}

					value = ReadAttributeValue(content, ref index);
				}

				attributes[name] = WebUtility.HtmlDecode(value);
			}

			return attributes;
		}

		private static string ReadAttributeValue(ReadOnlySpan<char> content, ref int index)
		{
			if (index >= content.Length)
			{
				return string.Empty;
			}

			if (content[index] == '"' || content[index] == '\'')
			{
				var quote = content[index];
				index++;
				var start = index;
				while (index < content.Length && content[index] != quote)
				{
					index++;
				}

				var value = content[start..index].ToString();
				if (index < content.Length)
				{
					index++;
				}

				return value;
			}

			var valueStart = index;
			while (index < content.Length && !char.IsWhiteSpace(content[index]))
			{
				index++;
			}

			return content[valueStart..index].ToString();
		}

		private static bool IsAttributeNameCharacter(char c)
		{
			return char.IsLetterOrDigit(c) || c is '-' or '_' or ':' or '.';
		}

		private static void CloseElement(Stack<HtmlNode> stack, string name)
		{
			if (name.Length == 0)
			{
				return;
			}

			while (stack.Count > 1)
			{
				var current = stack.Pop();
				if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}
		}

		private static void AutoClose(Stack<HtmlNode> stack, string name)
		{
			if (stack.Count <= 1)
			{
				return;
			}

			var current = stack.Peek().Name;
			if ((name is "p" && current is "p")
			    || (name is "li" && current is "li")
			    || (name is "tr" && current is "tr")
			    || ((name is "td" or "th") && (current is "td" or "th")))
			{
				stack.Pop();
			}
		}

		private static int SkipElementContent(string html, int index, string name)
		{
			var closing = "</" + name;
			var closeStart = html.IndexOf(closing, index, StringComparison.OrdinalIgnoreCase);
			if (closeStart < 0)
			{
				return index;
			}

			var closeEnd = FindTagEnd(html, closeStart + 2);
			return closeEnd < 0 ? html.Length : closeEnd + 1;
		}

		private static bool StartsWith(string text, int index, string value)
		{
			return text.AsSpan(index).StartsWith(value, StringComparison.Ordinal);
		}
	}
}
