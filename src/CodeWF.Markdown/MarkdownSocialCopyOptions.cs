namespace CodeWF.Markdown;

/// <summary>
/// Options used when rendering Markdown for rich social-editor clipboard HTML.
/// </summary>
public sealed record MarkdownSocialCopyOptions
{
	public string? Language { get; init; }

	public string? Title { get; init; }

	public bool IncludeFragmentMarkers { get; init; } = true;

	public string? ToolName { get; init; }

	public string? SuffixFormat { get; init; }

	public string Website { get; init; } = "https://codewf.com";
}
