using SkiaSharp;

namespace CodeWF.Markdown;

/// <summary>
/// Converts loaded Markdown image bytes into static PNG preview bytes.
/// </summary>
public static class MarkdownImageRasterizer
{
	public static byte[] RenderToPngBytes(MarkdownImageSource imageSource)
	{
		ArgumentNullException.ThrowIfNull(imageSource);

		return imageSource.IsSvg
			? MarkdownSvgRasterizer.RenderToPngBytes(imageSource.Bytes)
			: imageSource.IsGif
				? RenderBitmapToPngBytes(imageSource.Bytes)
				: imageSource.Bytes;
	}

	private static byte[] RenderBitmapToPngBytes(byte[] bytes)
	{
		using var sourceData = SKData.CreateCopy(bytes);
		using var codec = SKCodec.Create(sourceData);
		if (codec is null)
		{
			throw new InvalidDataException("Markdown image could not be decoded.");
		}

		var sourceInfo = codec.Info;
		var imageInfo = new SKImageInfo(
			sourceInfo.Width,
			sourceInfo.Height,
			SKColorType.Rgba8888,
			SKAlphaType.Premul);
		using var bitmap = new SKBitmap(imageInfo);
		var result = codec.GetPixels(imageInfo, bitmap.GetPixels());
		if (result is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
		{
			throw new InvalidDataException("Markdown image could not be decoded.");
		}

		using var image = SKImage.FromBitmap(bitmap);
		using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
		return encoded?.ToArray() ?? throw new InvalidDataException("Markdown image could not be encoded.");
	}
}
