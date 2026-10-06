using CodeWF.Markdown;

using Xunit;

namespace CodeWF.Markdown.Tests.Export;

public sealed class MarkdownHtmlExporterTests
{
	[Fact]
	public void RenderDocument_WhenDocumentWrapperIsRequested_WritesCompleteHtmlDocument()
	{
		var html = MarkdownHtmlExporter.RenderDocument(
			CreateDocument(),
			MarkdownExportStyle.Resolve(null, null),
			new MarkdownHtmlDocumentOptions { Title = "HTML 导出" });

		Assert.StartsWith("<!doctype html>", html);
		Assert.Contains("<title>HTML 导出</title>", html);
		Assert.Contains("<h1", html);
		Assert.Contains("导出正文", html);
		Assert.Contains("</html>", html);
	}

	[Fact]
	public void RenderBody_ReturnsStyleBoundFragmentWithoutDocumentWrapper()
	{
		var html = MarkdownHtmlExporter.RenderBody(CreateDocument(), MarkdownExportStyle.Resolve(null, null));

		Assert.DoesNotContain("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("<html", html, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("导出正文", html);
	}

	[Fact]
	public void RenderDocument_WhenWrapperIsDisabled_ReturnsOnlyRenderedFragment()
	{
		var html = MarkdownHtmlExporter.RenderDocument(
			CreateDocument(),
			MarkdownExportStyle.Resolve(null, null),
			new MarkdownHtmlDocumentOptions { IncludeDocumentWrapper = false });

		Assert.DoesNotContain("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("导出正文", html);
	}

	[Fact]
	public void RenderBody_WhenFragmentMarkersAreRequested_KeepsThemAroundBody()
	{
		var html = MarkdownHtmlExporter.RenderBody(
			CreateDocument(),
			MarkdownExportStyle.Resolve(null, null),
			new MarkdownHtmlDocumentOptions { IncludeFragmentMarkers = true });

		Assert.Contains(MarkdownHtmlClipboard.StartFragmentMarker, html);
		Assert.Contains(MarkdownHtmlClipboard.EndFragmentMarker, html);
	}

	[Fact]
	public void CreateContent_WithFragmentMarkers_IncludesMarkersAndPlainTextFallback()
	{
		var content = MarkdownHtmlExporter.CreateContent(
			CreateDocument(),
			new MarkdownHtmlDocumentOptions { IncludeFragmentMarkers = true });

		Assert.Contains(MarkdownHtmlClipboard.StartFragmentMarker, content.Html);
		Assert.Contains(MarkdownHtmlClipboard.EndFragmentMarker, content.Html);
		Assert.Contains("导出正文", content.Text);
		Assert.DoesNotContain("<h1", content.Text, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ExportMarkdown_WhenCalled_WritesUtf8WithoutBom()
	{
		var path = Path.Combine(Path.GetTempPath(), $"codewf-markdown-html-{Guid.NewGuid():N}.html");
		try
		{
			MarkdownHtmlExporter.ExportMarkdown(
				"# 中文标题\n\n正文 `code`。",
				path,
				options: new MarkdownHtmlDocumentOptions { Title = "中文标题" });

			var bytes = File.ReadAllBytes(path);
			Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
			var html = System.Text.Encoding.UTF8.GetString(bytes);
			Assert.Contains("中文标题", html);
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
	public void Export_ThroughExporterFacade_WritesHtmlForExportKind()
	{
		var path = Path.Combine(Path.GetTempPath(), $"codewf-markdown-html-{Guid.NewGuid():N}.html");
		try
		{
			MarkdownDocumentExporter.ExportMarkdown(
				"# Facade HTML",
				ExportKind.Html,
				MarkdownExportStyle.Resolve(null, null),
				path);

			Assert.Contains("Facade HTML", File.ReadAllText(path));
		}
		finally
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}

	private static MarkdownExportDocument CreateDocument()
	{
		return new MarkdownExportDocument("# 标题\n\n导出正文", fileName: "export.md");
	}
}
