namespace CodeWF.Markdown.Controls;

/// <summary>
/// 虚拟化窗口计算：根据块高度、非虚拟化标记与视口区间，决定哪些块需要物化。
/// 抽成纯函数（不依赖 Avalonia），便于单元测试与在宿主中复用同一套判定。
/// </summary>
internal static class MarkdownVirtualizationWindow
{
	/// <summary>
	/// 计算每块是否需要物化。视口区间用 <see cref="double.NegativeInfinity"/> /
	/// <see cref="double.PositiveInfinity"/> 表示「不裁剪，全部物化」。
	/// </summary>
	public static bool[] ResolveRealization(
		IReadOnlyList<double> heights,
		IReadOnlyList<bool> pinned,
		double spacing,
		double windowTop,
		double windowBottom)
	{
		var result = new bool[heights.Count];
		var noWindow = double.IsNegativeInfinity(windowTop) || double.IsPositiveInfinity(windowBottom);
		var y = 0d;
		for (var i = 0; i < heights.Count; i++)
		{
			var height = Math.Max(0, heights[i]);
			result[i] = noWindow
						|| (i < pinned.Count && pinned[i])
						|| (y + height >= windowTop && y <= windowBottom);
			y += height + spacing;
		}

		return result;
	}
}
