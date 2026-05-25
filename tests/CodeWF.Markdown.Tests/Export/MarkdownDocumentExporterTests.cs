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
}
