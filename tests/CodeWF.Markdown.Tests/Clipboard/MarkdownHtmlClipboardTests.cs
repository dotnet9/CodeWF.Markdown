using System.Globalization;
using System.Text;

using Xunit;

namespace CodeWF.Markdown.Tests.Clipboard;

public sealed class MarkdownHtmlClipboardTests
{
	[Fact]
	public void NormalizeHtmlForClipboard_WhenFragmentHasNoDocument_WrapsWithMarkers()
	{
		var normalized = MarkdownHtmlClipboard.NormalizeHtmlForClipboard("<section id=\"vex\">未命名</section>");

		Assert.Contains("<html>", normalized);
		Assert.Contains(MarkdownHtmlClipboard.StartFragmentMarker, normalized);
		Assert.Contains("<section id=\"vex\">未命名</section>", normalized);
		Assert.Contains(MarkdownHtmlClipboard.EndFragmentMarker, normalized);
	}

	[Fact]
	public void NormalizeHtmlForClipboard_WhenDocumentHasBody_InsertsMarkersInsideBody()
	{
		var normalized = MarkdownHtmlClipboard.NormalizeHtmlForClipboard("<!doctype html><html><head></head><body><section id=\"vex\">A</section></body></html>");

		var bodyStart = normalized.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
		var startFragment = normalized.IndexOf(MarkdownHtmlClipboard.StartFragmentMarker, StringComparison.Ordinal);
		var section = normalized.IndexOf("<section", StringComparison.Ordinal);
		var endFragment = normalized.IndexOf(MarkdownHtmlClipboard.EndFragmentMarker, StringComparison.Ordinal);
		var bodyEnd = normalized.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);

		Assert.True(bodyStart < startFragment);
		Assert.True(startFragment < section);
		Assert.True(section < endFragment);
		Assert.True(endFragment < bodyEnd);
	}

	[Fact]
	public void BuildWindowsClipboardHtml_UsesUtf8ByteOffsets()
	{
		var html = "<!doctype html><html><body>前<!--StartFragment--><p>未命名</p><!--EndFragment-->后</body></html>";
		var payload = MarkdownHtmlClipboard.BuildWindowsClipboardHtml(html);
		var bytes = Encoding.UTF8.GetBytes(payload);
		var startHtml = ReadOffset(payload, "StartHTML");
		var endHtml = ReadOffset(payload, "EndHTML");
		var startFragment = ReadOffset(payload, "StartFragment");
		var endFragment = ReadOffset(payload, "EndFragment");

		Assert.Equal(bytes.Length, endHtml);
		Assert.Equal("<!do", Encoding.UTF8.GetString(bytes, startHtml, 4));
		Assert.Equal("<p>未命名</p>", Encoding.UTF8.GetString(bytes, startFragment, endFragment - startFragment));
	}

	[Fact]
	public void BuildWindowsClipboardHtmlBytes_ReturnsUtf8Payload()
	{
		var bytes = MarkdownHtmlClipboard.BuildWindowsClipboardHtmlBytes("<section>未命名</section>");
		var payload = Encoding.UTF8.GetString(bytes);

		Assert.StartsWith("Version:1.0\r\n", payload);
		Assert.Contains("StartFragment:", payload);
		Assert.Contains("<section>未命名</section>", payload);
	}

	private static int ReadOffset(string payload, string name)
	{
		var prefix = $"{name}:";
		var line = payload
			.Split("\r\n", StringSplitOptions.None)
			.Single(line => line.StartsWith(prefix, StringComparison.Ordinal));
		return int.Parse(line[prefix.Length..], CultureInfo.InvariantCulture);
	}
}
