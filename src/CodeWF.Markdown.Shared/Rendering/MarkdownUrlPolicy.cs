using System.Diagnostics;

namespace CodeWF.Markdown.Shared.Rendering;

/// <summary>
/// Applies the common external-link policy used by the Full and Lite viewers.
/// </summary>
internal static class MarkdownUrlPolicy
{
	public static bool IsAllowedExternalUri(string? url)
	{
		return Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
			&& uri.Scheme is "http" or "https" or "mailto";
	}

	public static void Open(string? url)
	{
		if (!IsAllowedExternalUri(url))
		{
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true });
		}
		catch
		{
			// Link activation must not interrupt Markdown rendering.
		}
	}
}
