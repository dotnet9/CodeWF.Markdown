using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CodeWF.Markdown.MathRendering;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

/// <summary>
/// 数学公式可见性回归测试。
/// <para>
/// 背景：官方 CSharpMath.Avalonia 12.0.0 的画布在 Avalonia 12.1.3 下只会画出分数线，
/// 其余字形全部缺失（<c>StreamGeometryContext</c> 填充失效）。本包改用自实现的画布适配后，
/// 这里以离屏渲染断言「公式确实产生了非空像素」。
/// </para>
/// </summary>
[Collection("AvaloniaPlatform")]
public sealed class MarkdownMathViewTests
{
	private const int Width = 320;
	private const int Height = 120;

	private static readonly object SetupLock = new();
	private static bool _initialized;

	[Theory]
	[InlineData("x^2+y^2")]
	[InlineData(@"\frac{a}{b}")]
	[InlineData(@"\sqrt{x^2+1}")]
	[InlineData(@"\mathrm{C}\mathrm{O}_{2}")]
	public void Render_WhenFormulaIsValid_DrawsGlyphPixels(string latex)
	{
		EnsureAvaloniaInitialized();

		var painted = RenderMath(latex);

		// 分式横线约 88 像素：只画横线即为缺陷复现，因此要求显著多于它。
		Assert.True(painted > 200, $"公式 “{latex}” 仅绘制了 {painted} 个像素，疑似字形丢失。");
	}

	[Fact]
	public void Measure_WhenFormulaIsValid_ReturnsPositiveSize()
	{
		EnsureAvaloniaInitialized();

		var size = Dispatcher.UIThread.Invoke(() =>
		{
			var view = new MarkdownMathView { LaTeX = "x^2+y^2", FontSize = 20 };
			view.Measure(new Size(Width, Height));
			return view.DesiredSize;
		});

		Assert.True(size.Width > 1);
		Assert.True(size.Height > 1);
	}

	private static int RenderMath(string latex)
	{
		return Dispatcher.UIThread.Invoke(() =>
		{
			var view = new MarkdownMathView
			{
				LaTeX = latex,
				FontSize = 40,
				Foreground = Brushes.Black
			};
			view.Measure(new Size(Width, Height));
			view.Arrange(new Rect(0, 0, Width, Height));

			using var bitmap = new RenderTargetBitmap(new PixelSize(Width, Height), new Vector(96, 96));
			bitmap.Render(view);

			var stride = Width * 4;
			var pixels = new byte[stride * Height];
			var handle = System.Runtime.InteropServices.GCHandle.Alloc(
				pixels,
				System.Runtime.InteropServices.GCHandleType.Pinned);
			try
			{
				bitmap.CopyPixels(new PixelRect(0, 0, Width, Height), handle.AddrOfPinnedObject(), pixels.Length, stride);
			}
			finally
			{
				handle.Free();
			}

			var painted = 0;
			for (var i = 0; i + 3 < pixels.Length; i += 4)
			{
				if (pixels[i + 3] > 8 && (pixels[i + 2] < 240 || pixels[i + 1] < 240 || pixels[i] < 240))
				{
					painted++;
				}
			}

			return painted;
		});
	}

	private static void EnsureAvaloniaInitialized()
	{
		lock (SetupLock)
		{
			if (_initialized)
			{
				return;
			}

			if (Application.Current is null)
			{
				AppBuilder.Configure<Application>()
					.UseSkia()
					.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
					.SetupWithoutStarting();
			}

			_initialized = true;
		}
	}
}
