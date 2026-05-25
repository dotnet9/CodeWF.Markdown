namespace CodeWF.Markdown;

/// <summary>
/// HTML content prepared for rich clipboard copy.
/// </summary>
public sealed record MarkdownHtmlCopyContent(string Text, string Html);
