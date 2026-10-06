using Avalonia.Media.Imaging;

namespace CodeWF.Markdown;

/// <summary>
/// One-stop export facade for Markdown documents.
/// </summary>
public static class MarkdownDocumentExporter
{
	public static void ExportMarkdown(
		string markdown,
		ExportKind kind,
		string savePath)
	{
		ExportMarkdown(markdown, kind, style: null, savePath);
	}

	public static void ExportMarkdown(
		string markdown,
		ExportKind kind,
		string? themeName,
		string savePath,
		string? typographySize = null,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		Export(
			new MarkdownExportDocument(markdown),
			kind,
			savePath,
			MarkdownExportStyle.Resolve(themeName, typographySize),
			pdfOptions);
	}

	public static void ExportMarkdown(
		string markdown,
		ExportKind kind,
		MarkdownExportStyle? style,
		string savePath,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		Export(new MarkdownExportDocument(markdown), kind, savePath, style, pdfOptions);
	}

	public static void ExportFile(
		string markdownFilePath,
		ExportKind kind,
		string savePath)
	{
		ExportFile(markdownFilePath, kind, style: null, savePath);
	}

	public static void ExportFile(
		string markdownFilePath,
		ExportKind kind,
		string? themeName,
		string savePath,
		string? typographySize = null,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		Export(
			CreateDocumentFromFile(markdownFilePath),
			kind,
			savePath,
			MarkdownExportStyle.Resolve(themeName, typographySize),
			pdfOptions);
	}

	public static void ExportFile(
		string markdownFilePath,
		ExportKind kind,
		MarkdownExportStyle? style,
		string savePath,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		Export(CreateDocumentFromFile(markdownFilePath), kind, savePath, style, pdfOptions);
	}

	public static void Export(
		MarkdownExportDocument document,
		ExportKind kind,
		string savePath,
		MarkdownExportStyle? style = null,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentException.ThrowIfNullOrWhiteSpace(savePath);

		switch (kind)
		{
			case ExportKind.Png:
				ExportPng(document, savePath, style);
				break;
			case ExportKind.Pdf:
				ExportPdf(document, savePath, style, pdfOptions);
				break;
			case ExportKind.Word:
				ExportWord(document, savePath, style);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Markdown export kind.");
		}
	}

	public static void Export(
		MarkdownExportDocument document,
		ExportKind kind,
		string? themeName,
		string savePath,
		string? typographySize = null,
		MarkdownPdfExportOptions? pdfOptions = null)
	{
		Export(document, kind, savePath, MarkdownExportStyle.Resolve(themeName, typographySize), pdfOptions);
	}

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

	private static MarkdownExportDocument CreateDocumentFromFile(string markdownFilePath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(markdownFilePath);

		return new MarkdownExportDocument(
			File.ReadAllText(markdownFilePath),
			markdownFilePath);
	}
}
