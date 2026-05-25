using Avalonia.Media.Imaging;

namespace CodeWF.Markdown;

/// <summary>
/// One-stop export facade for Markdown documents.
/// </summary>
public static class MarkdownDocumentExporter
{
	public static RenderTargetBitmap RenderPng(MarkdownExportDocument document, MarkdownExportStyle? style = null)
	{
		ArgumentNullException.ThrowIfNull(document);

		return new MarkdownPngRenderer().Render(document, style);
	}

	public static void ExportPng(MarkdownExportDocument document, string path, MarkdownExportStyle? style = null)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		new MarkdownPngRenderer().Save(document, path, style);
	}

	public static void ExportPdf(
		MarkdownExportDocument document,
		string path,
		MarkdownExportStyle? style = null,
		MarkdownPdfExportOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		new MarkdownPdfRenderer().Render(document, path, style, options);
	}

	public static void ExportWord(MarkdownExportDocument document, string path, MarkdownExportStyle? style = null)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		MarkdownDocxExporter.Export(document, path, style ?? MarkdownExportStyle.Resolve(null, null));
	}
}

