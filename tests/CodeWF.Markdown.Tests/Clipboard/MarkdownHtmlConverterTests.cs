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

	[Fact]
	public void Html2Markdown_WhenInputIsPlainDiff_PreservesText()
	{
		var diff =
			"""
			diff --git a/demo.js b/demo.js
			index 2d3f110..78a2bc9 100644
			--- a/demo.js
			+++ b/demo.js
			@@ -2,8 +2,9 @@
			function sayHello() {
			-  console.log("Hello");
			+  console.log("Hello World");
			+  console.log("Git Diff Test");
			}

			const num = 10;
			- const str = "old text";
			+ const str = "new text";

			module.exports = { sayHello };
			""";

		var markdown = MarkdownHtmlConverter.Html2Markdown(diff);

		// 转换器会统一输出 LF，测试也显式按该契约比较，避免测试结果受源码文件的 CRLF/LF 影响。
		Assert.Equal(diff.ReplaceLineEndings("\n"), markdown);
	}

	[Fact]
	public void Html2Markdown_WhenInputIsPlainXml_PreservesText()
	{
		var xml = "<Project>\n\t<PropertyGroup>\n\t\t<Version>12.0.3.16</Version>\n\t\t<Authors>沙漠尽头的狼</Authors>\n\t</PropertyGroup>\n</Project>";

		var markdown = MarkdownHtmlConverter.Html2Markdown(xml);

		Assert.Equal(xml, markdown);
	}

	[Fact]
	public void Html2Markdown_WhenLayoutHtmlContainsEscapedCode_PreservesPlainTextShape()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""
			<div>&lt;Project&gt;</div>
			<div>&nbsp;&nbsp;&lt;PropertyGroup&gt;</div>
			<div>&nbsp;&nbsp;&nbsp;&nbsp;&lt;Version&gt;12.0.3.16&lt;/Version&gt;</div>
			<div>&nbsp;&nbsp;&lt;/PropertyGroup&gt;</div>
			<div>&lt;/Project&gt;</div>
			""");

		Assert.Equal(
			"<Project>\n  <PropertyGroup>\n    <Version>12.0.3.16</Version>\n  </PropertyGroup>\n</Project>",
			markdown);
	}

	[Fact]
	public void Html2Markdown_WhenLayoutHtmlContainsDiff_PreservesPlainTextShape()
	{
		var markdown = MarkdownHtmlConverter.Html2Markdown(
			"""
			<div>diff --git a/demo.js b/demo.js</div>
			<div>index 2d3f110..78a2bc9 100644</div>
			<div>--- a/demo.js</div>
			<div>+++ b/demo.js</div>
			<div>@@ -2,8 +2,9 @@</div>
			<div>function sayHello() {</div>
			<div>-&nbsp;&nbsp;console.log(&quot;Hello&quot;);</div>
			<div>+&nbsp;&nbsp;console.log(&quot;Hello World&quot;);</div>
			<div>}</div>
			""");

		Assert.Equal(
			"diff --git a/demo.js b/demo.js\nindex 2d3f110..78a2bc9 100644\n--- a/demo.js\n+++ b/demo.js\n@@ -2,8 +2,9 @@\nfunction sayHello() {\n-  console.log(\"Hello\");\n+  console.log(\"Hello World\");\n}",
			markdown);
	}
}
