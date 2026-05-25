using System.Net;
using System.Net.Sockets;
using System.Text;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

public sealed class MarkdownImageSourceLoaderTests
{
	[Fact]
	public void Load_WhenDataImageUri_ReturnsBytesAndImageKind()
	{
		var result = MarkdownImageSourceLoader.Load("data:image/gif;base64,R0lGODlh");

		Assert.True(result.IsGif);
		Assert.Equal("markdown-image.gif", result.FileName);
		Assert.Equal(Encoding.ASCII.GetBytes("GIF89a"), result.Bytes);
	}

	[Fact]
	public async Task LoadAsync_WhenRelativePathIsUrlEncoded_UsesImageBasePathDirectory()
	{
		var root = Path.Combine(Path.GetTempPath(), "CodeWFMarkdownImageTests", Guid.NewGuid().ToString("N"));
		var imageDirectory = Path.Combine(root, "images");
		var markdownPath = Path.Combine(root, "article.md");
		var imagePath = Path.Combine(imageDirectory, "logo mark.svg");
		Directory.CreateDirectory(imageDirectory);

		try
		{
			var svg = """<svg xmlns="http://www.w3.org/2000/svg" width="8" height="8"><rect width="8" height="8"/></svg>""";
			await File.WriteAllTextAsync(markdownPath, "# Article");
			await File.WriteAllTextAsync(imagePath, svg);

			var result = await MarkdownImageSourceLoader.LoadAsync("images/logo%20mark.svg", markdownPath);

			Assert.True(result.IsSvg);
			Assert.Equal("logo mark.svg", result.FileName);
			Assert.Equal(imagePath, result.LocalPath);
			Assert.Equal(svg, Encoding.UTF8.GetString(result.Bytes));
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, recursive: true);
			}
		}
	}

	[Fact]
	public async Task LoadAsync_WhenHttpImageUrl_ReturnsRemoteBytes()
	{
		var payload = Encoding.ASCII.GetBytes("GIF89a");
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var endpoint = (IPEndPoint)listener.LocalEndpoint;
		var serverTask = ServeSingleImageAsync(listener, payload, "image/gif");

		try
		{
			var result = await MarkdownImageSourceLoader.LoadAsync($"http://127.0.0.1:{endpoint.Port}/image.gif");

			Assert.True(result.IsGif);
			Assert.Equal("image.gif", result.FileName);
			Assert.Equal(payload, result.Bytes);
		}
		finally
		{
			listener.Stop();
			await serverTask;
		}
	}

	[Fact]
	public void RenderToPngBytes_WhenGifImage_ReturnsStaticPng()
	{
		var gif = Convert.FromBase64String("R0lGODlhAQABAPAAAP///wAAACH5BAAAAAAALAAAAAABAAEAAAICRAEAOw==");
		var source = new MarkdownImageSource(gif, "pixel.gif", IsSvg: false, IsGif: true, LocalPath: null);

		var png = MarkdownImageRasterizer.RenderToPngBytes(source);

		Assert.True(png.Length > 8);
		Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
	}

	[Fact]
	public void RenderToPngBytes_WhenSvgHasFilterAttribute_ReturnsPng()
	{
		var svg = """
			<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12">
			  <defs><filter id="shadow"><feDropShadow dx="1" dy="1" stdDeviation="1"/></filter></defs>
			  <rect width="12" height="12" fill="#2563eb" filter="url(#shadow)"/>
			</svg>
			""";

		var png = MarkdownSvgRasterizer.RenderToPngBytes(Encoding.UTF8.GetBytes(svg));

		Assert.True(png.Length > 8);
		Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4]);
	}

	private static async Task ServeSingleImageAsync(TcpListener listener, byte[] payload, string contentType)
	{
		try
		{
			using var client = await listener.AcceptTcpClientAsync();
			await using var stream = client.GetStream();
			await ReadRequestHeadersAsync(stream);

			var headers = Encoding.ASCII.GetBytes(
				$"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n");
			await stream.WriteAsync(headers);
			await stream.WriteAsync(payload);
		}
		catch (ObjectDisposedException)
		{
		}
		catch (SocketException)
		{
		}
	}

	private static async Task ReadRequestHeadersAsync(Stream stream)
	{
		var buffer = new byte[256];
		var received = new MemoryStream();
		while (true)
		{
			var read = await stream.ReadAsync(buffer);
			if (read == 0)
			{
				return;
			}

			received.Write(buffer, 0, read);
			if (Encoding.ASCII.GetString(received.ToArray()).Contains("\r\n\r\n", StringComparison.Ordinal))
			{
				return;
			}
		}
	}
}
