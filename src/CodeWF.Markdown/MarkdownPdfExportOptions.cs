namespace CodeWF.Markdown;

/// <summary>
/// Controls PDF export metadata.
/// </summary>
public sealed record MarkdownPdfExportOptions(
	string DefaultHeading = "Markdown Document",
	string DefaultFileName = "document.md");
