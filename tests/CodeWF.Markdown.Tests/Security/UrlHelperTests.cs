using CodeWF.Markdown.Helpers;
using Xunit;

namespace CodeWF.Markdown.Tests.Security;

public sealed class UrlHelperTests
{
	[Theory]
	[InlineData("https://example.com", true)]
	[InlineData("http://example.com/path", true)]
	[InlineData("mailto:user@example.com", true)]
	[InlineData("javascript:alert(1)", false)]
	[InlineData("file:///etc/passwd", false)]
	[InlineData("//example.com/path", false)]
	[InlineData("relative/path", false)]
	public void IsAllowedExternalUri_UsesSafeSchemePolicy(string url, bool expected)
	{
		Assert.Equal(expected, UrlHelper.IsAllowedExternalUri(url));
	}
}
