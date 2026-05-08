using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CodeWF.Markdown.Shared.Rendering;

internal sealed record MarkdownSelectionBlock(
	Control Control,
	MarkdownTextSpan PlainTextSpan,
	string PlainText);

internal sealed class MarkdownSelectionController
{
	private const string SelectedBlockClass = "MdSelectedBlock";
	private readonly List<MarkdownSelectionBlock> _blocks = [];
	private Visual? _coordinateRoot;
	private int? _anchorIndex;
	private int? _activeIndex;
	private bool _hasVisualSelection;

	public bool HasSelection => _hasVisualSelection
								&& _anchorIndex.HasValue
								&& _activeIndex.HasValue
								&& SelectedText.Length > 0;

	public string SelectedText
	{
		get
		{
			if (!_hasVisualSelection || !_anchorIndex.HasValue || !_activeIndex.HasValue || _blocks.Count == 0)
			{
				return string.Empty;
			}

			var (start, end) = GetOrderedIndexes();
			var parts = _blocks
				.Skip(start)
				.Take(end - start + 1)
				.Select(block => block.PlainText)
				.Where(text => !string.IsNullOrEmpty(text));
			return string.Join(Environment.NewLine + Environment.NewLine, parts);
		}
	}

	public void SetBlocks(IEnumerable<MarkdownSelectionBlock> blocks, Visual? coordinateRoot = null)
	{
		ClearVisualState();
		_blocks.Clear();
		_blocks.AddRange(blocks.Where(block => block.PlainTextSpan.Length > 0));
		_coordinateRoot = coordinateRoot;
		_anchorIndex = null;
		_activeIndex = null;
		_hasVisualSelection = false;
	}

	public bool Begin(Point point)
	{
		var index = HitTestBlock(point);
		if (index < 0)
		{
			Clear();
			return false;
		}

		ClearVisualState();
		_anchorIndex = index;
		_activeIndex = index;
		return true;
	}

	public bool Extend(Point point)
	{
		if (!_anchorIndex.HasValue)
		{
			return false;
		}

		var index = ResolveExtensionIndex(point);
		if (index < 0)
		{
			return false;
		}

		if (_activeIndex == index)
		{
			return false;
		}

		_activeIndex = index;
		ApplyVisualState();
		return true;
	}

	public void CommitClickWithoutDrag()
	{
		Clear();
	}

	public void Clear()
	{
		ClearVisualState();
		_anchorIndex = null;
		_activeIndex = null;
		_hasVisualSelection = false;
	}

	private int HitTestBlock(Point documentPoint)
	{
		for (var i = 0; i < _blocks.Count; i++)
		{
			var bounds = GetDocumentBounds(_blocks[i].Control);
			if (documentPoint.Y >= bounds.Top && documentPoint.Y <= bounds.Bottom)
			{
				return i;
			}
		}

		return -1;
	}

	private Rect GetDocumentBounds(Control control)
	{
		if (_coordinateRoot is not null
			&& control.TranslatePoint(new Point(0, 0), _coordinateRoot) is { } origin)
		{
			return new Rect(origin, control.Bounds.Size);
		}

		return control.Bounds;
	}

	private int ResolveExtensionIndex(Point documentPoint)
	{
		return HitTestBlock(documentPoint);
	}

	private (int Start, int End) GetOrderedIndexes()
	{
		var anchor = _anchorIndex.GetValueOrDefault();
		var active = _activeIndex.GetValueOrDefault();
		return anchor <= active ? (anchor, active) : (active, anchor);
	}

	private void ApplyVisualState()
	{
		ClearVisualState();
		if (!_anchorIndex.HasValue || !_activeIndex.HasValue)
		{
			return;
		}

		var (start, end) = GetOrderedIndexes();
		for (var i = start; i <= end; i++)
		{
			_blocks[i].Control.Classes.Add(SelectedBlockClass);
		}

		_hasVisualSelection = true;
	}

	private void ClearVisualState()
	{
		foreach (var block in _blocks)
		{
			block.Control.Classes.Remove(SelectedBlockClass);
		}

		_hasVisualSelection = false;
	}
}

internal readonly record struct MarkdownPointerSelectionState(Point StartPoint)
{
	public bool IsDragging(Point currentPoint)
	{
		return Math.Abs(currentPoint.X - StartPoint.X) > 4
			   || Math.Abs(currentPoint.Y - StartPoint.Y) > 4;
	}
}
