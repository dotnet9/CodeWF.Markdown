namespace CodeWF.Markdown.Tests.Clipboard;

using Xunit;

public sealed class MarkdownSocialCopyRendererTests
{
	[Theory]
	[InlineData("wechat", MarkdownSocialCopyFormat.Wechat, false)]
	[InlineData("weixin", MarkdownSocialCopyFormat.Wechat, false)]
	[InlineData("zhihu", MarkdownSocialCopyFormat.Mountain, false)]
	[InlineData("juejin", MarkdownSocialCopyFormat.Mountain, true)]
	public void TryResolve_WhenTargetNameIsBuiltIn_ReturnsProfile(
		string targetName,
		MarkdownSocialCopyFormat format,
		bool appendSuffix)
	{
		var resolved = MarkdownSocialCopyProfiles.TryResolve(targetName, out var profile);

		Assert.True(resolved);
		Assert.Equal(format, profile.Format);
		Assert.Equal(appendSuffix, profile.AppendSuffix);
	}

	[Fact]
	public void RenderMarkdown_WhenTargetIsWechat_UsesWechatHeadingTemplate()
	{
		var content = MarkdownHtmlClipboard.CreateHtmlCopyContent(
			"""
			# Title

			## Section

			Paragraph with `code`.
			""",
			CopyKind.Wechat,
			"Basic");

		Assert.Contains(MarkdownHtmlClipboard.StartFragmentMarker, content.Html);
		Assert.Contains("border-bottom: 2px solid", content.Html);
		Assert.DoesNotContain("mountain_2_20191028221337.png", content.Html);
		Assert.Contains("<section id=\"codewf-markdown\"", content.Text);
	}

	[Fact]
	public void TryCreateHtmlCopyContent_WhenTargetNameIsBuiltIn_ReturnsContent()
	{
		var resolved = MarkdownHtmlClipboard.TryCreateHtmlCopyContent(
			"## Section",
			"zhihu",
			out var content,
			"Basic");

		Assert.True(resolved);
		Assert.Contains("mountain_2_20191028221337.png", content.Html);
	}

	[Fact]
	public void TryCreateHtmlCopyContent_WhenTargetNameIsUnknown_ReturnsFalse()
	{
		var resolved = MarkdownHtmlClipboard.TryCreateHtmlCopyContent(
			"正文",
			"unknown-platform",
			out var content);

		Assert.False(resolved);
		Assert.Empty(content.Html);
		Assert.Empty(content.Text);
	}

	[Fact]
	public void RenderMarkdown_WhenTargetIsZhihu_UsesMountainHeadingTemplate()
	{
		var content = MarkdownSocialCopyRenderer.RenderMarkdown(
			"""
			## Section
			""",
			CopyKind.Zhihu);

		Assert.Contains("mountain_2_20191028221337.png", content.Html);
		Assert.DoesNotContain("vex-suffix-juejin-container", content.Html);
	}

	[Fact]
	public void RenderMarkdown_WhenTargetIsJuejin_AppendsSuffix()
	{
		var content = MarkdownSocialCopyRenderer.RenderMarkdown(
			"正文",
			CopyKind.Juejin);

		Assert.Contains("vex-suffix-juejin-container", content.Html);
		Assert.Contains("codewf.com", content.Html);
	}

	[Fact]
	public void RenderMarkdown_WhenOptionsProvideSuffixFormat_UsesLocalizedSuffixText()
	{
		var content = MarkdownSocialCopyRenderer.RenderMarkdown(
			"正文",
			CopyKind.Juejin,
			options: new MarkdownSocialCopyOptions
			{
				ToolName = "本地化编辑器",
				SuffixFormat = "本文使用 {0} 排版"
			});

		Assert.Contains("data-tool=\"本地化编辑器\"", content.Html);
		Assert.Contains("本文使用 <a href=\"https://codewf.com\"", content.Html);
		Assert.Contains("</a> 排版", content.Html);
	}

	[Fact]
	public void RenderMarkdown_WhenMarkdownContainsLocalImage_EmbedsDataUri()
	{
		var folder = Path.Combine(Path.GetTempPath(), $"codewf-social-copy-{Guid.NewGuid():N}");
		var markdownPath = Path.Combine(folder, "article.md");
		var imagePath = Path.Combine(folder, "pixel.png");
		try
		{
			Directory.CreateDirectory(folder);
			File.WriteAllBytes(imagePath, [0x89, 0x50, 0x4E, 0x47]);
			File.WriteAllText(markdownPath, "![pixel](pixel.png)");

			var content = MarkdownHtmlClipboard.CreateFileHtmlCopyContent(
				markdownPath,
				CopyKind.Wechat,
				MarkdownExportStyle.Resolve(null, null));

			Assert.Contains("src=\"data:image/png;base64,", content.Html);
		}
		finally
		{
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}
}
