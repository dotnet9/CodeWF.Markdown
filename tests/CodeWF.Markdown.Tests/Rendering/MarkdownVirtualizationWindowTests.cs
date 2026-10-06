using CodeWF.Markdown.Controls;

using Xunit;

namespace CodeWF.Markdown.Tests.Rendering;

/// <summary>
/// 虚拟化窗口判定（纯逻辑，不依赖 Avalonia 平台）：
/// 视口窗口内的块物化、外部块不物化、非虚拟化块始终物化。
/// </summary>
public sealed class MarkdownVirtualizationWindowTests
{
	private const double BlockHeight = 20;
	private const int BlockCount = 220;

	[Fact]
	public void ResolveRealization_WhenNoWindow_RealizesEveryBlock()
	{
		var result = MarkdownVirtualizationWindow.ResolveRealization(
			Heights(), Pinned(), 0, double.NegativeInfinity, double.PositiveInfinity);

		Assert.Equal(BlockCount, result.Count(realized => realized));
	}

	[Fact]
	public void ResolveRealization_WhenWindowCoversViewport_RealizesOnlyVisibleBlocks()
	{
		// 视口 0-200：0-10 命中（第 10 块顶边 200）。
		var result = MarkdownVirtualizationWindow.ResolveRealization(Heights(), Pinned(), 0, 0, 200);

		Assert.True(result[0]);
		Assert.True(result[10]);
		Assert.False(result[11]);
		Assert.False(result[BlockCount - 1]);
		Assert.Equal(11, result.Count(realized => realized));
	}

	[Fact]
	public void ResolveRealization_WhenOverscanMarginIsApplied_ExtendsRealizedRange()
	{
		// 视口 200 + 上下各 2 屏（400）→ 窗口 -400..600，覆盖到第 30 块（顶边 600）。
		var result = MarkdownVirtualizationWindow.ResolveRealization(Heights(), Pinned(), 0, -400, 600);

		Assert.Equal(31, result.Count(realized => realized));
		Assert.True(result[30]);
		Assert.False(result[31]);
	}

	[Fact]
	public void ResolveRealization_WhenBlockIsPinnedOutOfViewport_KeepsItRealized()
	{
		var pinned = new bool[BlockCount];
		pinned[BlockCount - 1] = true;

		var result = MarkdownVirtualizationWindow.ResolveRealization(Heights(), pinned, 0, 0, 200);

		Assert.True(result[BlockCount - 1]);
		Assert.Equal(12, result.Count(realized => realized));
	}

	[Fact]
	public void ResolveRealization_WhenScrolledPastTop_RealizesWindowAroundOffset()
	{
		// 滚动到 y=4000：窗口 4000..4200 覆盖第 199-210 块。
		var result = MarkdownVirtualizationWindow.ResolveRealization(Heights(), Pinned(), 0, 4000, 4200);

		Assert.False(result[0]);
		Assert.False(result[198]);
		Assert.True(result[199]);
		Assert.True(result[210]);
		Assert.False(result[211]);
	}

	[Fact]
	public void ResolveRealization_WithSpacing_AccountsForBlockGap()
	{
		var heights = Enumerable.Repeat(BlockHeight, 10).ToArray();

		// 间距 4：第 6 块顶边 = 6 * 24 = 144，窗口 0-150 覆盖到第 6 块。
		var result = MarkdownVirtualizationWindow.ResolveRealization(heights, new bool[10], 4, 0, 150);

		Assert.True(result[6]);
		Assert.False(result[7]);
	}

	private static double[] Heights() => Enumerable.Repeat(BlockHeight, BlockCount).ToArray();

	private static bool[] Pinned() => new bool[BlockCount];
}
