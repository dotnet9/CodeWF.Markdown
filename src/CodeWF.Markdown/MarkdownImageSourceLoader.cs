using System.Text;

namespace CodeWF.Markdown;

/// <summary>
/// Loads Markdown image sources from data URIs, local files, file URIs, and HTTP(S) URLs.
/// </summary>
public static class MarkdownImageSourceLoader
{
	public const long DefaultMaxRemoteImageBytes = 32L * 1024 * 1024;

	private static readonly HttpClient HttpClient = CreateHttpClient();

	public static MarkdownImageSource Load(
		string? source,
		string? imageBasePath = null,
		long maxRemoteImageBytes = DefaultMaxRemoteImageBytes)
	{
		var normalizedSource = NormalizeSource(source);
		if (TryReadDataUri(normalizedSource, out var dataUriBytes, out var dataUriIsSvg, out var dataUriIsGif))
		{
			return new MarkdownImageSource(
				dataUriBytes,
				ResolveFileName(normalizedSource),
				dataUriIsSvg || IsSvgBytes(dataUriBytes),
				dataUriIsGif || IsGifBytes(dataUriBytes),
				null);
		}

		if (Uri.TryCreate(normalizedSource, UriKind.Absolute, out var uri))
		{
			if (uri.Scheme is "http" or "https")
			{
				var bytes = ReadRemoteBytes(uri, maxRemoteImageBytes);
				return new MarkdownImageSource(
					bytes,
					ResolveFileName(normalizedSource),
					IsSvgPath(uri.LocalPath) || IsSvgBytes(bytes),
					IsGifPath(uri.LocalPath) || IsGifBytes(bytes),
					null);
			}

			if (uri.IsFile)
			{
				return LoadLocalFile(uri.LocalPath, uri.LocalPath);
			}
		}

		var localPath = ResolveLocalPath(normalizedSource, imageBasePath);
		return LoadLocalFile(localPath.Path, localPath.DisplayPath);
	}

	public static async Task<MarkdownImageSource> LoadAsync(
		string? source,
		string? imageBasePath = null,
		CancellationToken cancellationToken = default,
		long maxRemoteImageBytes = DefaultMaxRemoteImageBytes)
	{
		var normalizedSource = NormalizeSource(source);
		if (TryReadDataUri(normalizedSource, out var dataUriBytes, out var dataUriIsSvg, out var dataUriIsGif))
		{
			return new MarkdownImageSource(
				dataUriBytes,
				ResolveFileName(normalizedSource),
				dataUriIsSvg || IsSvgBytes(dataUriBytes),
				dataUriIsGif || IsGifBytes(dataUriBytes),
				null);
		}

		if (Uri.TryCreate(normalizedSource, UriKind.Absolute, out var uri))
		{
			if (uri.Scheme is "http" or "https")
			{
				var bytes = await ReadRemoteBytesAsync(uri, maxRemoteImageBytes, cancellationToken);
				return new MarkdownImageSource(
					bytes,
					ResolveFileName(normalizedSource),
					IsSvgPath(uri.LocalPath) || IsSvgBytes(bytes),
					IsGifPath(uri.LocalPath) || IsGifBytes(bytes),
					null);
			}

			if (uri.IsFile)
			{
				return await LoadLocalFileAsync(uri.LocalPath, uri.LocalPath, cancellationToken);
			}
		}

		var localPath = ResolveLocalPath(normalizedSource, imageBasePath);
		return await LoadLocalFileAsync(localPath.Path, localPath.DisplayPath, cancellationToken);
	}

	internal static string CreateCacheKey(string source, string? imageBasePath)
	{
		var normalizedSource = NormalizeSource(source);
		if (IsDataUri(normalizedSource)
			|| Uri.TryCreate(normalizedSource, UriKind.Absolute, out _)
			|| Path.IsPathRooted(normalizedSource))
		{
			return normalizedSource;
		}

		return $"{ResolveImageBaseDirectory(imageBasePath) ?? AppContext.BaseDirectory}|{normalizedSource}";
	}

	private static HttpClient CreateHttpClient()
	{
		var client = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(30)
		};
		client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeWF.Markdown/12");
		return client;
	}

	private static string NormalizeSource(string? source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			throw new ArgumentException("Markdown image source cannot be empty.", nameof(source));
		}

		return source.Trim();
	}

	private static MarkdownLocalImagePath ResolveLocalPath(string source, string? imageBasePath)
	{
		foreach (var candidate in EnumerateLocalPathCandidates(source))
		{
			if (Path.IsPathRooted(candidate))
			{
				return new MarkdownLocalImagePath(Path.GetFullPath(candidate), candidate);
			}
		}

		var baseDirectory = ResolveImageBaseDirectory(imageBasePath) ?? AppContext.BaseDirectory;
		foreach (var candidate in EnumerateLocalPathCandidates(source))
		{
			var path = Path.GetFullPath(Path.Combine(baseDirectory, candidate));
			if (File.Exists(path))
			{
				return new MarkdownLocalImagePath(path, path);
			}
		}

		var fallback = Path.GetFullPath(Path.Combine(baseDirectory, source.Replace('/', Path.DirectorySeparatorChar)));
		return new MarkdownLocalImagePath(fallback, fallback);
	}

	private static IEnumerable<string> EnumerateLocalPathCandidates(string source)
	{
		var normalized = source.Replace('/', Path.DirectorySeparatorChar);
		yield return normalized;

		var decoded = DecodeLocalImageUrl(normalized);
		if (!string.Equals(decoded, normalized, StringComparison.Ordinal))
		{
			yield return decoded;
		}
	}

	private static string DecodeLocalImageUrl(string source)
	{
		try
		{
			return Uri.UnescapeDataString(source);
		}
		catch (UriFormatException)
		{
			return source;
		}
	}

	private static string? ResolveImageBaseDirectory(string? imageBasePath)
	{
		var basePath = imageBasePath?.Trim();
		if (string.IsNullOrWhiteSpace(basePath))
		{
			return null;
		}

		try
		{
			if (Uri.TryCreate(basePath, UriKind.Absolute, out var uri) && uri.IsFile)
			{
				return ResolveExistingDirectory(uri.LocalPath);
			}

			return ResolveExistingDirectory(basePath);
		}
		catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
		{
			return null;
		}
	}

	private static string? ResolveExistingDirectory(string path)
	{
		var fullPath = Path.GetFullPath(path);
		if (Directory.Exists(fullPath))
		{
			return fullPath;
		}

		var directory = Path.GetDirectoryName(fullPath);
		return string.IsNullOrWhiteSpace(directory) ? null : directory;
	}

	private static MarkdownImageSource LoadLocalFile(string path, string displayPath)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException("Markdown image file was not found.", displayPath);
		}

		var bytes = File.ReadAllBytes(path);
		return new MarkdownImageSource(
			bytes,
			ResolveFileName(path),
			IsSvgPath(path) || IsSvgBytes(bytes),
			IsGifPath(path) || IsGifBytes(bytes),
			path);
	}

	private static async Task<MarkdownImageSource> LoadLocalFileAsync(string path, string displayPath, CancellationToken cancellationToken)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException("Markdown image file was not found.", displayPath);
		}

		var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
		return new MarkdownImageSource(
			bytes,
			ResolveFileName(path),
			IsSvgPath(path) || IsSvgBytes(bytes),
			IsGifPath(path) || IsGifBytes(bytes),
			path);
	}

	private static byte[] ReadRemoteBytes(Uri uri, long maxRemoteImageBytes)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		using var response = HttpClient.Send(request, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();
		using var source = response.Content.ReadAsStream();
		return ReadRemoteResponseBytes(source, response.Content.Headers.ContentLength, maxRemoteImageBytes);
	}

	private static async Task<byte[]> ReadRemoteBytesAsync(Uri uri, long maxRemoteImageBytes, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		response.EnsureSuccessStatusCode();
		await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
		return await ReadRemoteResponseBytesAsync(source, response.Content.Headers.ContentLength, maxRemoteImageBytes, cancellationToken);
	}

	private static byte[] ReadRemoteResponseBytes(Stream source, long? contentLength, long maxRemoteImageBytes)
	{
		ValidateRemoteLength(contentLength, maxRemoteImageBytes);
		using var memory = new MemoryStream(GetInitialRemoteImageCapacity(contentLength, maxRemoteImageBytes));
		var buffer = new byte[81920];
		int read;
		while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
		{
			ValidateRemoteLength(memory.Length + read, maxRemoteImageBytes);
			memory.Write(buffer, 0, read);
		}

		return memory.ToArray();
	}

	private static async Task<byte[]> ReadRemoteResponseBytesAsync(
		Stream source,
		long? contentLength,
		long maxRemoteImageBytes,
		CancellationToken cancellationToken)
	{
		ValidateRemoteLength(contentLength, maxRemoteImageBytes);
		using var memory = new MemoryStream(GetInitialRemoteImageCapacity(contentLength, maxRemoteImageBytes));
		var buffer = new byte[81920];
		int read;
		while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
		{
			ValidateRemoteLength(memory.Length + read, maxRemoteImageBytes);
			await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
		}

		return memory.ToArray();
	}

	private static void ValidateRemoteLength(long? contentLength, long maxRemoteImageBytes)
	{
		if (contentLength > maxRemoteImageBytes)
		{
			throw new InvalidDataException("Markdown image response is too large.");
		}
	}

	private static int GetInitialRemoteImageCapacity(long? contentLength, long maxRemoteImageBytes)
	{
		return contentLength is > 0 and <= int.MaxValue
			? (int)Math.Min(contentLength.Value, maxRemoteImageBytes)
			: 0;
	}

	private static bool TryReadDataUri(string source, out byte[] bytes, out bool isSvg, out bool isGif)
	{
		bytes = [];
		isSvg = false;
		isGif = false;

		if (!IsDataUri(source))
		{
			return false;
		}

		var commaIndex = source.IndexOf(',');
		if (commaIndex < 0)
		{
			return false;
		}

		var metadata = source[..commaIndex];
		var payload = source[(commaIndex + 1)..];
		isSvg = IsSvgMediaType(metadata);
		isGif = IsGifMediaType(metadata);
		bytes = metadata.Contains(";base64", StringComparison.OrdinalIgnoreCase)
			? Convert.FromBase64String(payload)
			: Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
		return true;
	}

	private static bool IsDataUri(string source)
	{
		return source.StartsWith("data:image", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSvgMediaType(string? mediaType)
	{
		return mediaType?.Contains("image/svg+xml", StringComparison.OrdinalIgnoreCase) == true;
	}

	private static bool IsSvgPath(string path)
	{
		return string.Equals(Path.GetExtension(path), ".svg", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSvgBytes(byte[] bytes)
	{
		var length = Math.Min(bytes.Length, 512);
		if (length == 0)
		{
			return false;
		}

		var prefix = Encoding.UTF8.GetString(bytes, 0, length).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
		return prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
			   || prefix.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
			   && prefix.Contains("<svg", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGifMediaType(string? mediaType)
	{
		return mediaType?.Contains("image/gif", StringComparison.OrdinalIgnoreCase) == true;
	}

	private static bool IsGifPath(string path)
	{
		return string.Equals(Path.GetExtension(path), ".gif", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGifBytes(byte[] bytes)
	{
		return bytes.Length >= 6
			   && bytes[0] == 'G'
			   && bytes[1] == 'I'
			   && bytes[2] == 'F'
			   && bytes[3] == '8'
			   && (bytes[4] == '7' || bytes[4] == '9')
			   && bytes[5] == 'a';
	}

	private static string ResolveFileName(string source)
	{
		var fileName = "markdown-image";
		if (IsDataUri(source))
		{
			fileName += ResolveDataUriExtension(source);
		}
		else if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			fileName = Path.GetFileName(uri.IsFile ? uri.LocalPath : uri.LocalPath);
		}
		else
		{
			fileName = Path.GetFileName(source);
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			fileName = "markdown-image.png";
		}
		else if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
		{
			fileName += ".png";
		}

		foreach (var invalidChar in Path.GetInvalidFileNameChars())
		{
			fileName = fileName.Replace(invalidChar, '_');
		}

		return fileName;
	}

	private static string ResolveDataUriExtension(string source)
	{
		var semicolonIndex = source.IndexOf(';');
		var commaIndex = source.IndexOf(',');
		var separatorIndex = semicolonIndex >= 0
			? semicolonIndex
			: commaIndex;
		if (separatorIndex <= "data:image/".Length)
		{
			return ".png";
		}

		return source["data:image/".Length..separatorIndex].ToLowerInvariant() switch
		{
			"svg+xml" => ".svg",
			"jpeg" => ".jpg",
			"jpg" => ".jpg",
			"webp" => ".webp",
			"bmp" => ".bmp",
			"gif" => ".gif",
			_ => ".png"
		};
	}

	private sealed record MarkdownLocalImagePath(string Path, string DisplayPath);
}
