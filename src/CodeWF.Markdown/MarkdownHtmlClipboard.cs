using System.Globalization;
using System.Text;

using Avalonia.Input;
using Avalonia.Input.Platform;

namespace CodeWF.Markdown;

/// <summary>
/// Creates rich HTML clipboard payloads that paste targets such as Chromium,
/// WeChat Official Account, Zhihu, and Juejin can recognize.
/// </summary>
public static class MarkdownHtmlClipboard
{
	public const string HtmlMimeFormatName = "text/html";
	public const string MacHtmlFormatName = "public.html";
	public const string WindowsHtmlFormatName = "HTML Format";
	public const string StartFragmentMarker = "<!--StartFragment-->";
	public const string EndFragmentMarker = "<!--EndFragment-->";

	public static readonly DataFormat<string> HtmlMimeFormat = DataFormat.CreateStringPlatformFormat(HtmlMimeFormatName);
	public static readonly DataFormat<string> MacHtmlFormat = DataFormat.CreateStringPlatformFormat(MacHtmlFormatName);
	public static readonly DataFormat<byte[]> WindowsHtmlFormat = DataFormat.CreateBytesPlatformFormat(WindowsHtmlFormatName);

	/// <summary>
	/// Writes HTML to the clipboard with native rich-text formats and a plain text fallback.
	/// </summary>
	public static async Task SetHtmlAsync(IClipboard clipboard, string html, string? text = null)
	{
		ArgumentNullException.ThrowIfNull(clipboard);

		await clipboard.SetDataAsync(CreateHtmlDataTransfer(html, text)).ConfigureAwait(false);
		await clipboard.FlushAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Builds an Avalonia data transfer object containing text/plain, text/html,
	/// macOS public.html, and Windows CF_HTML formats.
	/// </summary>
	public static DataTransfer CreateHtmlDataTransfer(string html, string? text = null)
	{
		ArgumentNullException.ThrowIfNull(html);

		var clipboardHtml = NormalizeHtmlForClipboard(html);
		var item = new DataTransferItem();
		item.SetText(text ?? html);
		item.Set(HtmlMimeFormat, clipboardHtml);
		item.Set(MacHtmlFormat, clipboardHtml);
		item.Set(WindowsHtmlFormat, BuildWindowsClipboardHtmlBytes(clipboardHtml));

		var transfer = new DataTransfer();
		transfer.Add(item);
		return transfer;
	}

	/// <summary>
	/// Ensures the HTML document contains CF_HTML fragment markers.
	/// </summary>
	public static string NormalizeHtmlForClipboard(string html)
	{
		ArgumentNullException.ThrowIfNull(html);

		if (HasValidFragmentMarkers(html))
		{
			return html;
		}

		if (TryInsertBodyFragmentMarkers(html, out var markedHtml))
		{
			return markedHtml;
		}

		return $$"""
			<!doctype html>
			<html>
			<body>
			{{StartFragmentMarker}}
			{{html}}
			{{EndFragmentMarker}}
			</body>
			</html>
			""";
	}

	/// <summary>
	/// Builds the Windows CF_HTML string. Offsets are UTF-8 byte offsets from the
	/// beginning of the payload, as required by the native clipboard format.
	/// </summary>
	public static string BuildWindowsClipboardHtml(string html)
	{
		ArgumentNullException.ThrowIfNull(html);

		var clipboardHtml = NormalizeHtmlForClipboard(html);
		var startMarkerIndex = clipboardHtml.IndexOf(StartFragmentMarker, StringComparison.Ordinal);
		var endMarkerIndex = clipboardHtml.IndexOf(EndFragmentMarker, StringComparison.Ordinal);
		if (startMarkerIndex < 0 || endMarkerIndex < 0 || endMarkerIndex < startMarkerIndex)
		{
			throw new InvalidOperationException("HTML clipboard content must contain a valid fragment marker pair.");
		}

		const string HeaderFormat = "Version:1.0\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\nStartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";

		var blankHeader = string.Format(CultureInfo.InvariantCulture, HeaderFormat, 0, 0, 0, 0);
		var startHtml = Encoding.UTF8.GetByteCount(blankHeader);
		var endHtml = startHtml + Encoding.UTF8.GetByteCount(clipboardHtml);
		var startFragment = startHtml + Encoding.UTF8.GetByteCount(clipboardHtml[..(startMarkerIndex + StartFragmentMarker.Length)]);
		var endFragment = startHtml + Encoding.UTF8.GetByteCount(clipboardHtml[..endMarkerIndex]);
		var header = string.Format(CultureInfo.InvariantCulture, HeaderFormat, startHtml, endHtml, startFragment, endFragment);
		return header + clipboardHtml;
	}

	/// <summary>
	/// Builds UTF-8 bytes for Windows CF_HTML. Windows HTML Format is a byte
	/// clipboard format, not a UTF-16 text format.
	/// </summary>
	public static byte[] BuildWindowsClipboardHtmlBytes(string html)
	{
		return Encoding.UTF8.GetBytes(BuildWindowsClipboardHtml(html));
	}

	private static bool HasValidFragmentMarkers(string html)
	{
		var startMarkerIndex = html.IndexOf(StartFragmentMarker, StringComparison.Ordinal);
		var endMarkerIndex = html.IndexOf(EndFragmentMarker, StringComparison.Ordinal);
		return startMarkerIndex >= 0 && endMarkerIndex > startMarkerIndex;
	}

	private static bool TryInsertBodyFragmentMarkers(string html, out string markedHtml)
	{
		markedHtml = string.Empty;

		var bodyStartIndex = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
		var bodyEndIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
		if (bodyStartIndex < 0 || bodyEndIndex <= bodyStartIndex)
		{
			return false;
		}

		var bodyOpenEndIndex = html.IndexOf('>', bodyStartIndex);
		if (bodyOpenEndIndex < 0 || bodyOpenEndIndex >= bodyEndIndex)
		{
			return false;
		}

		var builder = new StringBuilder(html.Length + StartFragmentMarker.Length + EndFragmentMarker.Length + 12);
		builder.Append(html, 0, bodyOpenEndIndex + 1);
		builder.AppendLine();
		builder.AppendLine(StartFragmentMarker);
		builder.Append(html, bodyOpenEndIndex + 1, bodyEndIndex - bodyOpenEndIndex - 1);
		builder.AppendLine();
		builder.AppendLine(EndFragmentMarker);
		builder.Append(html, bodyEndIndex, html.Length - bodyEndIndex);
		markedHtml = builder.ToString();
		return true;
	}
}
