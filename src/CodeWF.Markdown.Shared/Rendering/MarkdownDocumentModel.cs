using Markdig.Syntax;

namespace CodeWF.Markdown.Shared.Rendering;

internal readonly record struct MarkdownTextSpan(int Start, int End)
{
	public int Length => Math.Max(0, End - Start);

	public static MarkdownTextSpan Empty { get; } = new(0, 0);
}

internal sealed record MarkdownDocumentBlock(
	Block SyntaxBlock,
	MarkdownTextSpan SourceSpan,
	MarkdownTextSpan PlainTextSpan,
	MarkdownBlockKind Kind,
	MarkdownDependencyFlags DependencyFlags,
	ulong ContentHash,
	string PlainText,
	string SourceText)
{
	public bool HasGlobalDependency => (DependencyFlags & MarkdownDependencyFlags.Global) != 0;
}

internal sealed record MarkdownDocumentModel(
	string Source,
	IReadOnlyList<MarkdownDocumentBlock> Blocks,
	string PlainText,
	MarkdownDependencyFlags DependencyFlags)
{
	public static MarkdownDocumentModel Empty { get; } = new(string.Empty, [], string.Empty, MarkdownDependencyFlags.None);
}
