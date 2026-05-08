namespace CodeWF.Markdown.Shared.Rendering;

internal readonly record struct MarkdownRenderDiff(
	bool RequiresFullRender,
	int ReplaceStartIndex,
	int ReplaceEndIndex,
	int NewStartIndex,
	int NewEndIndex)
{
	public int OldRemoveCount => Math.Max(0, ReplaceEndIndex - ReplaceStartIndex);
	public int NewInsertCount => Math.Max(0, NewEndIndex - NewStartIndex);

	public static MarkdownRenderDiff Full { get; } = new(true, 0, 0, 0, 0);
	public static MarkdownRenderDiff NoChange { get; } = new(false, 0, 0, 0, 0);
}

internal static class MarkdownDiffService
{
	private const int LargeDocumentThreshold = 4096;

	public static MarkdownRenderDiff Compare(MarkdownDocumentModel oldModel, MarkdownDocumentModel newModel)
	{
		if (ReferenceEquals(oldModel, newModel) || oldModel.Source == newModel.Source)
		{
			return MarkdownRenderDiff.NoChange;
		}

		if (oldModel.Blocks.Count == 0 || newModel.Blocks.Count == 0)
		{
			return MarkdownRenderDiff.Full;
		}

		if (ShouldFullRenderByTextChange(oldModel.Source, newModel.Source))
		{
			return MarkdownRenderDiff.Full;
		}

		if (HasGlobalDependencyRisk(oldModel, newModel))
		{
			return MarkdownRenderDiff.Full;
		}

		var prefix = 0;
		while (prefix < oldModel.Blocks.Count
			   && prefix < newModel.Blocks.Count
			   && CanReuse(oldModel.Blocks[prefix], newModel.Blocks[prefix]))
		{
			prefix++;
		}

		var oldSuffix = oldModel.Blocks.Count - 1;
		var newSuffix = newModel.Blocks.Count - 1;
		while (oldSuffix >= prefix
			   && newSuffix >= prefix
			   && CanReuse(oldModel.Blocks[oldSuffix], newModel.Blocks[newSuffix]))
		{
			oldSuffix--;
			newSuffix--;
		}

		var oldChangedCount = oldSuffix - prefix + 1;
		var newChangedCount = newSuffix - prefix + 1;
		if (oldChangedCount < 0 && newChangedCount < 0)
		{
			return MarkdownRenderDiff.NoChange;
		}

		if (ShouldFullRenderByBlockChange(oldModel.Blocks.Count, newModel.Blocks.Count, oldChangedCount, newChangedCount))
		{
			return MarkdownRenderDiff.Full;
		}

		return new MarkdownRenderDiff(
			false,
			prefix,
			oldSuffix + 1,
			prefix,
			newSuffix + 1);
	}

	public static bool CanReuse(MarkdownDocumentBlock oldBlock, MarkdownDocumentBlock newBlock)
	{
		return oldBlock.ContentHash == newBlock.ContentHash
			   && oldBlock.Kind == newBlock.Kind
			   && oldBlock.DependencyFlags == newBlock.DependencyFlags;
	}

	private static bool HasGlobalDependencyRisk(MarkdownDocumentModel oldModel, MarkdownDocumentModel newModel)
	{
		if ((oldModel.DependencyFlags & MarkdownDependencyFlags.Global)
			!= (newModel.DependencyFlags & MarkdownDependencyFlags.Global))
		{
			return true;
		}

		if ((oldModel.DependencyFlags & MarkdownDependencyFlags.Global) == 0)
		{
			return false;
		}

		var oldGlobalHash = CombineGlobalHash(oldModel);
		var newGlobalHash = CombineGlobalHash(newModel);
		return oldGlobalHash != newGlobalHash;
	}

	private static ulong CombineGlobalHash(MarkdownDocumentModel model)
	{
		var hash = 14695981039346656037UL;
		foreach (var block in model.Blocks.Where(block => block.HasGlobalDependency))
		{
			hash ^= block.ContentHash;
			hash *= 1099511628211UL;
		}

		return hash;
	}

	private static bool ShouldFullRenderByBlockChange(int oldCount, int newCount, int oldChangedCount, int newChangedCount)
	{
		var maxCount = Math.Max(oldCount, newCount);
		if (maxCount <= 0)
		{
			return true;
		}

		var changedCount = Math.Max(oldChangedCount, newChangedCount);
		return maxCount > 80 && changedCount > Math.Max(80, maxCount * 9 / 10);
	}

	private static bool ShouldFullRenderByTextChange(string oldText, string newText)
	{
		var change = CalculateTextChange(oldText, newText);
		var preservedLength = change.OldStart + oldText.Length - change.OldEnd;
		if (oldText.Length > 0 && preservedLength < oldText.Length / 2)
		{
			return true;
		}

		var newChangedLength = change.NewEnd - change.NewStart;
		return newText.Length > LargeDocumentThreshold
			   && newChangedLength > Math.Max(LargeDocumentThreshold, newText.Length * 9 / 10);
	}

	private static TextChange CalculateTextChange(string oldText, string newText)
	{
		var prefixLength = 0;
		var minLength = Math.Min(oldText.Length, newText.Length);
		while (prefixLength < minLength && oldText[prefixLength] == newText[prefixLength])
		{
			prefixLength++;
		}

		var suffixLength = 0;
		while (suffixLength < oldText.Length - prefixLength
			   && suffixLength < newText.Length - prefixLength
			   && oldText[oldText.Length - suffixLength - 1] == newText[newText.Length - suffixLength - 1])
		{
			suffixLength++;
		}

		return new TextChange(prefixLength, oldText.Length - suffixLength, prefixLength, newText.Length - suffixLength);
	}

	private readonly record struct TextChange(int OldStart, int OldEnd, int NewStart, int NewEnd);
}
