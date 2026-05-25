using System.IO.Compression;
using Xunit;

namespace CodeWF.Markdown.Tests.Export;

public sealed class MarkdownDocumentExporterTests
{
	[Fact]
	public void ExportWord_WhenMarkdownContainsDataImage_EmbedsImagePart()
	{
		var path = Path.Combine(Path.GetTempPath(), $"codewf-markdown-export-{Guid.NewGuid():N}.docx");
		try
		{
			var document = new MarkdownExportDocument(
				"""
				# Export test

				![pixel](data:image/gif;base64,R0lGODlhAQABAPAAAP///wAAACH5BAAAAAAALAAAAAABAAEAAAICRAEAOw==)
				""",
				fileName: "export-test.md");

			MarkdownDocumentExporter.ExportWord(document, path);

			using var archive = ZipFile.OpenRead(path);
			Assert.NotNull(archive.GetEntry("word/document.xml"));
			Assert.NotNull(archive.GetEntry("word/_rels/document.xml.rels"));
			Assert.Contains(archive.Entries, entry => entry.FullName.StartsWith("word/media/image", StringComparison.Ordinal));
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void ExportMarkdown_WhenKindIsWord_WritesDocx()
	{
		var path = Path.Combine(Path.GetTempPath(), $"codewf-markdown-export-{Guid.NewGuid():N}.docx");
		try
		{
			MarkdownDocumentExporter.ExportMarkdown(
				"# Export facade",
				ExportKind.Word,
				MarkdownExportStyle.Resolve(null, null),
				path);

			using var archive = ZipFile.OpenRead(path);
			Assert.NotNull(archive.GetEntry("word/document.xml"));
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	[Fact]
	public void ExportFile_WhenKindIsWord_ReadsMarkdownFromDisk()
	{
		var markdownPath = Path.Combine(Path.GetTempPath(), $"codewf-markdown-export-{Guid.NewGuid():N}.md");
		var docxPath = Path.Combine(Path.GetTempPath(), $"codewf-markdown-export-{Guid.NewGuid():N}.docx");
		try
		{
			File.WriteAllText(markdownPath, "# Export file");

			MarkdownDocumentExporter.ExportFile(
				markdownPath,
				ExportKind.Word,
				MarkdownExportStyle.Resolve(null, null),
				docxPath);

			using var archive = ZipFile.OpenRead(docxPath);
			Assert.NotNull(archive.GetEntry("word/document.xml"));
		}
		finally
		{
			if (File.Exists(markdownPath))
			{
				File.Delete(markdownPath);
			}

			if (File.Exists(docxPath))
			{
				File.Delete(docxPath);
			}
		}
	}
}
