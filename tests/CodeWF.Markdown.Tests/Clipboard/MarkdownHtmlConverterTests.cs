using System.Text;

using Xunit;

namespace CodeWF.Markdown.Tests.Clipboard;

public sealed class MarkdownHtmlConverterTests
{
	[Fact]
	public void Html2Markdown_WhenHtmlContainsCommonArticleElements_ConvertsToMarkdown()
	{
		var markdown = MarkdownHtmlClipboard.Html2Markdown(
			"""
			<article>
				<h1>Hello <em>World</em></h1>
				<p>Visit <a href="https://example.com">Example</a> and <strong>bold</strong>.</p>
				<ul><li>First</li><li>Second</li></ul>
				<pre><code class="language-csharp">Console.WriteLine(&quot;Hi&quot;);</code></pre>
			</article>
			""");

		Assert.Contains("# Hello *World*", markdown);
		Assert.Contains("Visit [Example](https://example.com) and **bold**.", markdown);
		Assert.Contains("- First", markdown);
		Assert.Contains("- Second", markdown);
		Assert.Contains("```csharp\nConsole.WriteLine(\"Hi\");\n```", markdown);
	}

	[Fact]
	public void Html2Markdown_WhenHtmlContainsTableAndImage_ConvertsMarkdownTableAndImage()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""
			<table>
				<tr><th>Name</th><th>Value</th></tr>
				<tr><td>Logo</td><td><img alt="CodeWF" src="https://example.com/logo.png"></td></tr>
			</table>
			""");

		Assert.Contains("| Name | Value |", markdown);
		Assert.Contains("| --- | --- |", markdown);
		Assert.Contains("| Logo | ![CodeWF](https://example.com/logo.png) |", markdown);
	}

	[Fact]
	public void Html2Markdown_WhenHtmlContainsInlineOnlyFragment_KeepsSingleParagraph()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""<span>Hello</span> <strong>World</strong>""");

		Assert.Equal("Hello **World**", markdown);
	}

	[Fact]
	public void Html2Markdown_WhenListItemContainsInlineChildren_KeepsOneListItemLine()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""<ul><li><a href="https://example.com">Example</a> item</li></ul>""");

		Assert.Equal("- [Example](https://example.com) item", markdown);
	}

	[Fact]
	public void Html2Markdown_WhenHtmlUsesClipboardFragment_ConvertsOnlyFragment()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""
			<!doctype html><html><body>before<!--StartFragment--><h2>Fragment</h2><p>正文</p><!--EndFragment-->after</body></html>
			""");

		Assert.Equal("## Fragment\n\n正文", markdown);
	}

	[Fact]
	public void Html2Markdown_WhenInputIsWindowsClipboardHtml_UsesUtf8FragmentOffsets()
	{
		var payload = MarkdownHtmlClipboard.BuildWindowsClipboardHtml(
			"<!doctype html><html><body>前<!--StartFragment--><p>未命名</p><!--EndFragment-->后</body></html>");
		var bytes = Encoding.UTF8.GetBytes(payload);

		var markdown = MarkdownHtmlConverter.Html2Markdown(Encoding.UTF8.GetString(bytes));

		Assert.Equal("未命名", markdown);
	}
}
