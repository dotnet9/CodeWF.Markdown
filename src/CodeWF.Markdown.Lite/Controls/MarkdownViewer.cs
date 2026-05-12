using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;
using AvaloniaInline = Avalonia.Controls.Documents.Inline;

namespace CodeWF.Markdown.Lite.Controls;

/// <summary>
/// Renders basic Markdown into an Avalonia control tree with only Avalonia and Markdig dependencies.
/// </summary>
[TemplatePart(DocumentHostPartName, typeof(Panel), IsRequired = true)]
public class MarkdownViewer : TemplatedControl
{
	private const string DocumentHostPartName = "PART_DocumentHost";
	private const string DefaultTypographyTheme = "Basic";
	private const string DefaultTypographySize = "Normal";

	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	private readonly List<IDisposable> _bindings = [];
	private Panel? _documentHost;
	private bool _isRenderQueued;

	public static readonly StyledProperty<string?> MarkdownProperty =
		AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(Markdown));

	public static readonly StyledProperty<string?> TypographyThemeProperty =
		AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(TypographyTheme));

	public static readonly StyledProperty<string?> TypographySizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, string?>(nameof(TypographySize));

	public static readonly StyledProperty<IBrush?> TextBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(TextBrush), Brushes.Black);

	public static readonly StyledProperty<IBrush?> MutedTextBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(MutedTextBrush), Brushes.Gray);

	public static readonly StyledProperty<IBrush?> AccentBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(AccentBrush), Brushes.DodgerBlue);

	public static readonly StyledProperty<IBrush?> AccentForegroundBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(AccentForegroundBrush), Brushes.White);

	public static readonly StyledProperty<IBrush?> BorderLineBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(BorderLineBrush), Brushes.LightGray);

	public static readonly StyledProperty<IBrush?> QuoteBackgroundBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(QuoteBackgroundBrush), Brushes.Transparent);

	public static readonly StyledProperty<IBrush?> CodeBackgroundBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(CodeBackgroundBrush), Brushes.Transparent);

	public static readonly StyledProperty<IBrush?> InlineCodeBackgroundBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(InlineCodeBackgroundBrush), Brushes.Transparent);

	public static readonly StyledProperty<IBrush?> TableHeaderBackgroundBrushProperty =
		AvaloniaProperty.Register<MarkdownViewer, IBrush?>(nameof(TableHeaderBackgroundBrush), Brushes.Transparent);

	public static readonly StyledProperty<double> ParagraphFontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(ParagraphFontSize), 16);

	public static readonly StyledProperty<double> ParagraphLineHeightProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(ParagraphLineHeight), 28);

	public static readonly StyledProperty<double> Heading1FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading1FontSize), 30);

	public static readonly StyledProperty<double> Heading2FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading2FontSize), 26);

	public static readonly StyledProperty<double> Heading3FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading3FontSize), 22);

	public static readonly StyledProperty<double> Heading4FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading4FontSize), 20);

	public static readonly StyledProperty<double> Heading5FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading5FontSize), 18);

	public static readonly StyledProperty<double> Heading6FontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(Heading6FontSize), 16);

	public static readonly StyledProperty<double> BlockSpacingProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(BlockSpacing), 8);

	public static readonly StyledProperty<double> DocumentBottomPaddingProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(DocumentBottomPadding), 64);

	public static readonly StyledProperty<Thickness> ParagraphMarginProperty =
		AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ParagraphMargin), new Thickness(0, 4, 0, 10));

	public static readonly StyledProperty<Thickness> HeadingMarginProperty =
		AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(HeadingMargin), new Thickness(0, 18, 0, 10));

	public static readonly StyledProperty<FontFamily> ContentFontFamilyProperty =
		AvaloniaProperty.Register<MarkdownViewer, FontFamily>(nameof(ContentFontFamily), FontFamily.Default);

	public static readonly StyledProperty<FontFamily> CodeFontFamilyProperty =
		AvaloniaProperty.Register<MarkdownViewer, FontFamily>(
			nameof(CodeFontFamily),
			new FontFamily("Consolas, Cascadia Mono, JetBrains Mono, monospace"));

	public static readonly StyledProperty<double> CodeBlockFontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeBlockFontSize), 13);

	public static readonly StyledProperty<double> CodeBlockLineHeightProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeBlockLineHeight), 20);

	public static readonly StyledProperty<double> CodeLanguageFontSizeProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(CodeLanguageFontSize), 12);

	public static readonly StyledProperty<double> UnorderedListMarkerWidthProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(UnorderedListMarkerWidth), 24);

	public static readonly StyledProperty<double> OrderedListMarkerMinWidthProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerMinWidth), 28);

	public static readonly StyledProperty<double> OrderedListMarkerCharacterWidthProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerCharacterWidth), 9);

	public static readonly StyledProperty<double> OrderedListMarkerExtraWidthProperty =
		AvaloniaProperty.Register<MarkdownViewer, double>(nameof(OrderedListMarkerExtraWidth), 6);

	public static readonly StyledProperty<Thickness> ListFirstParagraphMarginProperty =
		AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ListFirstParagraphMargin), new Thickness(0, 0, 0, 2));

	public static readonly StyledProperty<Thickness> ListNestedParagraphMarginProperty =
		AvaloniaProperty.Register<MarkdownViewer, Thickness>(nameof(ListNestedParagraphMargin), new Thickness(0, 2, 0, 2));

	static MarkdownViewer()
	{
		MarkdownProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument());
		TypographyThemeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument());
		TypographySizeProperty.Changed.AddClassHandler<MarkdownViewer>((viewer, _) => viewer.QueueRenderDocument());
	}

	public MarkdownViewer()
	{
		TextOptions.SetBaselinePixelAlignment(this, BaselinePixelAlignment.Aligned);
	}

	public string? Markdown
	{
		get => GetValue(MarkdownProperty);
		set => SetValue(MarkdownProperty, value);
	}

	public string? TypographyTheme
	{
		get => GetValue(TypographyThemeProperty) ?? DefaultTypographyTheme;
		set => SetValue(TypographyThemeProperty, value);
	}

	public string? TypographySize
	{
		get => GetValue(TypographySizeProperty) ?? DefaultTypographySize;
		set => SetValue(TypographySizeProperty, value);
	}

	public IBrush? TextBrush
	{
		get => GetValue(TextBrushProperty);
		set => SetValue(TextBrushProperty, value);
	}

	public IBrush? MutedTextBrush
	{
		get => GetValue(MutedTextBrushProperty);
		set => SetValue(MutedTextBrushProperty, value);
	}

	public IBrush? AccentBrush
	{
		get => GetValue(AccentBrushProperty);
		set => SetValue(AccentBrushProperty, value);
	}

	public IBrush? AccentForegroundBrush
	{
		get => GetValue(AccentForegroundBrushProperty);
		set => SetValue(AccentForegroundBrushProperty, value);
	}

	public IBrush? BorderLineBrush
	{
		get => GetValue(BorderLineBrushProperty);
		set => SetValue(BorderLineBrushProperty, value);
	}

	public IBrush? QuoteBackgroundBrush
	{
		get => GetValue(QuoteBackgroundBrushProperty);
		set => SetValue(QuoteBackgroundBrushProperty, value);
	}

	public IBrush? CodeBackgroundBrush
	{
		get => GetValue(CodeBackgroundBrushProperty);
		set => SetValue(CodeBackgroundBrushProperty, value);
	}

	public IBrush? InlineCodeBackgroundBrush
	{
		get => GetValue(InlineCodeBackgroundBrushProperty);
		set => SetValue(InlineCodeBackgroundBrushProperty, value);
	}

	public IBrush? TableHeaderBackgroundBrush
	{
		get => GetValue(TableHeaderBackgroundBrushProperty);
		set => SetValue(TableHeaderBackgroundBrushProperty, value);
	}

	public double ParagraphFontSize
	{
		get => GetValue(ParagraphFontSizeProperty);
		set => SetValue(ParagraphFontSizeProperty, value);
	}

	public double ParagraphLineHeight
	{
		get => GetValue(ParagraphLineHeightProperty);
		set => SetValue(ParagraphLineHeightProperty, value);
	}

	public double Heading1FontSize
	{
		get => GetValue(Heading1FontSizeProperty);
		set => SetValue(Heading1FontSizeProperty, value);
	}

	public double Heading2FontSize
	{
		get => GetValue(Heading2FontSizeProperty);
		set => SetValue(Heading2FontSizeProperty, value);
	}

	public double Heading3FontSize
	{
		get => GetValue(Heading3FontSizeProperty);
		set => SetValue(Heading3FontSizeProperty, value);
	}

	public double Heading4FontSize
	{
		get => GetValue(Heading4FontSizeProperty);
		set => SetValue(Heading4FontSizeProperty, value);
	}

	public double Heading5FontSize
	{
		get => GetValue(Heading5FontSizeProperty);
		set => SetValue(Heading5FontSizeProperty, value);
	}

	public double Heading6FontSize
	{
		get => GetValue(Heading6FontSizeProperty);
		set => SetValue(Heading6FontSizeProperty, value);
	}

	public double BlockSpacing
	{
		get => GetValue(BlockSpacingProperty);
		set => SetValue(BlockSpacingProperty, value);
	}

	public double DocumentBottomPadding
	{
		get => GetValue(DocumentBottomPaddingProperty);
		set => SetValue(DocumentBottomPaddingProperty, value);
	}

	public Thickness ParagraphMargin
	{
		get => GetValue(ParagraphMarginProperty);
		set => SetValue(ParagraphMarginProperty, value);
	}

	public Thickness HeadingMargin
	{
		get => GetValue(HeadingMarginProperty);
		set => SetValue(HeadingMarginProperty, value);
	}

	public FontFamily ContentFontFamily
	{
		get => GetValue(ContentFontFamilyProperty);
		set => SetValue(ContentFontFamilyProperty, value);
	}

	public FontFamily CodeFontFamily
	{
		get => GetValue(CodeFontFamilyProperty);
		set => SetValue(CodeFontFamilyProperty, value);
	}

	public double CodeBlockFontSize
	{
		get => GetValue(CodeBlockFontSizeProperty);
		set => SetValue(CodeBlockFontSizeProperty, value);
	}

	public double CodeBlockLineHeight
	{
		get => GetValue(CodeBlockLineHeightProperty);
		set => SetValue(CodeBlockLineHeightProperty, value);
	}

	public double CodeLanguageFontSize
	{
		get => GetValue(CodeLanguageFontSizeProperty);
		set => SetValue(CodeLanguageFontSizeProperty, value);
	}

	public double UnorderedListMarkerWidth
	{
		get => GetValue(UnorderedListMarkerWidthProperty);
		set => SetValue(UnorderedListMarkerWidthProperty, value);
	}

	public double OrderedListMarkerMinWidth
	{
		get => GetValue(OrderedListMarkerMinWidthProperty);
		set => SetValue(OrderedListMarkerMinWidthProperty, value);
	}

	public double OrderedListMarkerCharacterWidth
	{
		get => GetValue(OrderedListMarkerCharacterWidthProperty);
		set => SetValue(OrderedListMarkerCharacterWidthProperty, value);
	}

	public double OrderedListMarkerExtraWidth
	{
		get => GetValue(OrderedListMarkerExtraWidthProperty);
		set => SetValue(OrderedListMarkerExtraWidthProperty, value);
	}

	public Thickness ListFirstParagraphMargin
	{
		get => GetValue(ListFirstParagraphMarginProperty);
		set => SetValue(ListFirstParagraphMarginProperty, value);
	}

	public Thickness ListNestedParagraphMargin
	{
		get => GetValue(ListNestedParagraphMarginProperty);
		set => SetValue(ListNestedParagraphMarginProperty, value);
	}

	public string GetRenderedText()
	{
		return ExtractDocumentPlainText(ParseMarkdown());
	}

	public async Task CopyRenderedTextAsync()
	{
		if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
		{
			await clipboard.SetTextAsync(GetRenderedText());
		}
	}

	public void Rerender()
	{
		QueueRenderDocument();
	}

	public void RenderIncremental()
	{
		QueueRenderDocument();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_documentHost = e.NameScope.Find<Panel>(DocumentHostPartName);
		RenderDocument();
	}

	private void QueueRenderDocument()
	{
		if (_documentHost is null || _isRenderQueued)
		{
			return;
		}

		_isRenderQueued = true;
		Dispatcher.UIThread.Post(
			() =>
			{
				_isRenderQueued = false;
				RenderDocument();
			},
			DispatcherPriority.Background);
	}

	private void RenderDocument()
	{
		if (_documentHost is null)
		{
			return;
		}

		ClearBindings();
		_documentHost.Children.Clear();

		foreach (var block in ParseMarkdown())
		{
			var control = RenderBlock(block);
			if (control is not null)
			{
				_documentHost.Children.Add(control);
			}
		}
	}

	private MarkdownDocument ParseMarkdown()
	{
		return Markdig.Markdown.Parse(Markdown ?? string.Empty, Pipeline);
	}

	private Control? RenderBlock(Block block)
	{
		return block switch
		{
			HeadingBlock heading => RenderHeading(heading),
			ParagraphBlock paragraph when IsImageParagraph(paragraph) => RenderImageParagraph(paragraph),
			ParagraphBlock paragraph => RenderParagraph(paragraph),
			FencedCodeBlock fencedCode => RenderCodeBlock(fencedCode),
			CodeBlock codeBlock => RenderCodeBlock(codeBlock),
			ListBlock list => RenderList(list),
			QuoteBlock quote => RenderQuote(quote),
			ThematicBreakBlock => RenderThematicBreak(),
			Table table => RenderTable(table),
			HtmlBlock htmlBlock => RenderHtmlBlock(htmlBlock),
			LinkReferenceDefinitionGroup => null,
			LinkReferenceDefinition => null,
			_ => RenderUnknownBlock(block)
		};
	}

	private Control RenderHeading(HeadingBlock heading)
	{
		var textBlock = CreateSelectableText(
			MarkdownStyleKeys.Heading,
			MarkdownStyleKeys.GetHeadingClass(heading.Level));
		textBlock.FontWeight = FontWeight.Bold;
		BindTheme(textBlock, TextBlock.ForegroundProperty, heading.Level <= 2 ? AccentBrushProperty : TextBrushProperty);
		BindTheme(textBlock, TextElement.FontFamilyProperty, ContentFontFamilyProperty);
		BindTheme(textBlock, TextElement.FontSizeProperty, GetHeadingFontSizeProperty(heading.Level));
		foreach (var inline in ConvertInlines(heading.Inline))
		{
			textBlock.Inlines!.Add(inline);
		}

		var border = new Border
		{
			Child = textBlock
		};
		AddMarkdownClass(
			border,
			MarkdownStyleKeys.HeadingBorder,
			MarkdownStyleKeys.GetHeadingBorderClass(heading.Level));
		BindTheme(border, Border.BorderBrushProperty, AccentBrushProperty);
		BindTheme(border, Layoutable.MarginProperty, HeadingMarginProperty);
		return border;
	}

	private Control RenderParagraph(ParagraphBlock paragraph)
	{
		var textBlock = CreateSelectableText(MarkdownStyleKeys.Paragraph);
		BindText(textBlock, ParagraphFontSizeProperty);
		BindTheme(textBlock, TextBlock.LineHeightProperty, ParagraphLineHeightProperty);
		BindTheme(textBlock, Layoutable.MarginProperty, ParagraphMarginProperty);

		foreach (var inline in ConvertInlines(paragraph.Inline))
		{
			textBlock.Inlines!.Add(inline);
		}

		return textBlock;
	}

	private Control RenderCodeBlock(CodeBlock codeBlock)
	{
		var code = codeBlock.Lines.ToString().TrimEnd();
		var language = codeBlock is FencedCodeBlock fencedCode
			? (fencedCode.Info ?? string.Empty).Trim()
			: string.Empty;

		var content = new StackPanel();
		AddMarkdownClass(content, MarkdownStyleKeys.CodeBlockContent);

		var header = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		AddMarkdownClass(header, MarkdownStyleKeys.CodeBlockHeader);

		if (!string.IsNullOrWhiteSpace(language))
		{
			var languageText = new TextBlock
			{
				Text = language,
				VerticalAlignment = VerticalAlignment.Center
			};
			AddMarkdownClass(languageText, MarkdownStyleKeys.CodeLanguage);
			BindTheme(languageText, TextBlock.ForegroundProperty, MutedTextBrushProperty);
			BindTheme(languageText, TextElement.FontFamilyProperty, ContentFontFamilyProperty);
			BindTheme(languageText, TextElement.FontSizeProperty, CodeLanguageFontSizeProperty);
			header.Children.Add(languageText);
		}

		var copyButton = CreateCopyButton(code);
		Grid.SetColumn(copyButton, 1);
		header.Children.Add(copyButton);
		content.Children.Add(header);

		var codeText = new SelectableTextBlock
		{
			Text = code,
			TextWrapping = TextWrapping.NoWrap
		};
		AddMarkdownClass(codeText, MarkdownStyleKeys.CodeBlockText);
		TextOptions.SetBaselinePixelAlignment(codeText, BaselinePixelAlignment.Aligned);
		BindTheme(codeText, TextBlock.ForegroundProperty, TextBrushProperty);
		BindTheme(codeText, TextElement.FontFamilyProperty, CodeFontFamilyProperty);
		BindTheme(codeText, TextElement.FontSizeProperty, CodeBlockFontSizeProperty);
		BindTheme(codeText, TextBlock.LineHeightProperty, CodeBlockLineHeightProperty);

		var scrollViewer = new ScrollViewer
		{
			Content = codeText,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		AddMarkdownClass(scrollViewer, MarkdownStyleKeys.CodeBlockScrollViewer);
		content.Children.Add(scrollViewer);

		var border = new Border
		{
			Child = content
		};
		AddMarkdownClass(border, MarkdownStyleKeys.CodeBlock);
		BindTheme(border, Border.BackgroundProperty, CodeBackgroundBrushProperty);
		BindTheme(border, Border.BorderBrushProperty, BorderLineBrushProperty);
		return border;
	}

	private Control RenderList(ListBlock list)
	{
		var stack = new StackPanel();
		AddMarkdownClass(stack, MarkdownStyleKeys.List);

		var index = 1;
		foreach (var item in list.OfType<ListItemBlock>())
		{
			stack.Children.Add(RenderListItem(item, list.IsOrdered, index++));
		}

		return stack;
	}

	private Control RenderListItem(ListItemBlock item, bool isOrdered, int index)
	{
		Control marker;
		if (TryReadTaskState(item, out var isChecked))
		{
			marker = new CheckBox
			{
				IsChecked = isChecked,
				IsEnabled = false,
				Width = UnorderedListMarkerWidth
			};
			AddMarkdownClass(marker, MarkdownStyleKeys.TaskMarkerBox);
		}
		else
		{
			var markerText = CreateSelectableText(MarkdownStyleKeys.ListMarker);
			markerText.Text = isOrdered ? $"{index}." : "\u2022 ";
			markerText.TextAlignment = TextAlignment.Right;
			BindText(markerText, ParagraphFontSizeProperty);
			BindTheme(markerText, TextBlock.LineHeightProperty, ParagraphLineHeightProperty);
			BindTheme(markerText, Layoutable.MarginProperty, ListFirstParagraphMarginProperty);

			var markerWidth = isOrdered
				? Math.Max(OrderedListMarkerMinWidth, index.ToString().Length * OrderedListMarkerCharacterWidth + OrderedListMarkerExtraWidth)
				: UnorderedListMarkerWidth;
			markerText.Width = markerWidth;
			marker = markerText;
		}

		var content = new StackPanel();
		AddMarkdownClass(content, MarkdownStyleKeys.ListItemContent);
		foreach (var child in item)
		{
			if (RenderBlock(child) is { } childControl)
			{
				content.Children.Add(childControl);
			}
		}

		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(new GridLength(1, GridUnitType.Star))
			}
		};
		AddMarkdownClass(grid, MarkdownStyleKeys.ListItem);
		grid.Children.Add(marker);
		Grid.SetColumn(content, 1);
		grid.Children.Add(content);
		return grid;
	}

	private Control RenderImageParagraph(ParagraphBlock paragraph)
	{
		var image = paragraph.Inline?.FirstChild as LinkInline;
		var markdownImage = new MarkdownImage
		{
			Source = image?.Url,
			AltText = image is null ? string.Empty : ExtractImageText(image)
		};
		AddMarkdownClass(markdownImage, MarkdownStyleKeys.Image);
		return markdownImage;
	}

	private Control RenderQuote(QuoteBlock quote)
	{
		var content = new StackPanel();
		AddMarkdownClass(content, MarkdownStyleKeys.QuoteContent);
		foreach (var child in quote)
		{
			if (RenderBlock(child) is { } childControl)
			{
				content.Children.Add(childControl);
			}
		}

		var border = new Border
		{
			Child = content
		};
		AddMarkdownClass(border, MarkdownStyleKeys.Quote);
		BindTheme(border, Border.BackgroundProperty, QuoteBackgroundBrushProperty);
		BindTheme(border, Border.BorderBrushProperty, AccentBrushProperty);
		return border;
	}

	private Control RenderThematicBreak()
	{
		var border = new Border();
		AddMarkdownClass(border, MarkdownStyleKeys.ThematicBreak);
		BindTheme(border, Border.BackgroundProperty, BorderLineBrushProperty);
		return border;
	}

	private Control RenderTable(Table table)
	{
		var rows = table.OfType<TableRow>().ToList();
		var columnCount = Math.Max(1, rows.Select(row => row.Count).DefaultIfEmpty(1).Max());
		var grid = new Grid();
		AddMarkdownClass(grid, MarkdownStyleKeys.Table);

		for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
		}

		for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
		{
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			var row = rows[rowIndex];
			for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
			{
				if (columnIndex >= row.Count || row[columnIndex] is not TableCell cell)
				{
					continue;
				}

				var cellBorder = RenderTableCell(cell, row.IsHeader);
				Grid.SetRow(cellBorder, rowIndex);
				Grid.SetColumn(cellBorder, columnIndex);
				grid.Children.Add(cellBorder);
			}
		}

		var container = new Border
		{
			Child = grid,
			ClipToBounds = true
		};
		AddMarkdownClass(container, MarkdownStyleKeys.TableContainer);
		BindTheme(container, Border.BorderBrushProperty, BorderLineBrushProperty);
		return container;
	}

	private Border RenderTableCell(TableCell cell, bool isHeader)
	{
		var stack = new StackPanel();
		AddMarkdownClass(stack, MarkdownStyleKeys.TableCellContent);
		foreach (var block in cell)
		{
			if (RenderBlock(block) is { } child)
			{
				stack.Children.Add(child);
			}
		}

		var border = new Border
		{
			Child = stack
		};
		AddMarkdownClass(border, isHeader ? MarkdownStyleKeys.TableHeaderCell : MarkdownStyleKeys.TableCell);
		BindTheme(border, Border.BorderBrushProperty, BorderLineBrushProperty);
		if (isHeader)
		{
			BindTheme(border, Border.BackgroundProperty, TableHeaderBackgroundBrushProperty);
		}

		return border;
	}

	private Control RenderHtmlBlock(HtmlBlock htmlBlock)
	{
		var textBlock = CreateSelectableText(MarkdownStyleKeys.HtmlBlock);
		textBlock.Text = htmlBlock.Lines.ToString().TrimEnd();
		BindText(textBlock, ParagraphFontSizeProperty);
		BindTheme(textBlock, TextBlock.LineHeightProperty, ParagraphLineHeightProperty);
		BindTheme(textBlock, Layoutable.MarginProperty, ParagraphMarginProperty);
		return textBlock;
	}

	private Control RenderUnknownBlock(Block block)
	{
		var textBlock = CreateSelectableText(MarkdownStyleKeys.UnknownBlock);
		textBlock.Text = block.ToString() ?? string.Empty;
		BindText(textBlock, ParagraphFontSizeProperty);
		BindTheme(textBlock, TextBlock.LineHeightProperty, ParagraphLineHeightProperty);
		BindTheme(textBlock, Layoutable.MarginProperty, ParagraphMarginProperty);
		return textBlock;
	}

	private IEnumerable<AvaloniaInline> ConvertInlines(ContainerInline? container)
	{
		var child = container?.FirstChild;
		while (child is not null)
		{
			foreach (var inline in ConvertInline(child))
			{
				yield return inline;
			}

			child = child.NextSibling;
		}
	}

	private IEnumerable<AvaloniaInline> ConvertInline(Markdig.Syntax.Inlines.Inline inline)
	{
		switch (inline)
		{
			case LiteralInline literal:
				yield return CreateRun(literal.Content.ToString());
				break;
			case CodeInline code:
				yield return CreateInlineCode(code.Content);
				break;
			case LineBreakInline:
				yield return new LineBreak();
				break;
			case TaskList:
				break;
			case EmphasisInline emphasis:
				yield return CreateEmphasis(emphasis);
				break;
			case LinkInline { IsImage: true } image:
				yield return CreateRun(ExtractImageText(image));
				break;
			case LinkInline { IsImage: false } link:
				yield return CreateLink(link);
				break;
			case HtmlInline html:
				yield return CreateRun(html.Tag);
				break;
			case ContainerInline nested:
				foreach (var child in ConvertInlines(nested))
				{
					yield return child;
				}

				break;
			default:
				var text = inline.ToString() ?? string.Empty;
				if (!IsTypeNameFallback(text, inline.GetType()))
				{
					yield return CreateRun(text);
				}

				break;
		}
	}

	private Run CreateRun(string text)
	{
		return new Run(text);
	}

	private Run CreateInlineCode(string text)
	{
		var run = new Run(text);
		BindTheme(run, TextElement.ForegroundProperty, TextBrushProperty);
		BindTheme(run, TextElement.BackgroundProperty, InlineCodeBackgroundBrushProperty);
		BindTheme(run, TextElement.FontFamilyProperty, CodeFontFamilyProperty);
		return run;
	}

	private Span CreateEmphasis(EmphasisInline emphasis)
	{
		var span = new Span();
		if (emphasis.DelimiterCount >= 2)
		{
			span.FontWeight = FontWeight.SemiBold;
		}
		else
		{
			span.FontStyle = FontStyle.Italic;
		}

		foreach (var child in ConvertInlines(emphasis))
		{
			span.Inlines.Add(child);
		}

		return span;
	}

	private Span CreateLink(LinkInline link)
	{
		var span = new Span
		{
			TextDecorations = TextDecorations.Underline
		};
		AddMarkdownClass(span, MarkdownStyleKeys.Link);
		BindTheme(span, TextElement.ForegroundProperty, AccentBrushProperty);

		foreach (var child in ConvertInlines(link))
		{
			span.Inlines.Add(child);
		}

		if (span.Inlines.Count == 0 && !string.IsNullOrWhiteSpace(link.Url))
		{
			span.Inlines.Add(CreateRun(link.Url!));
		}

		return span;
	}

	private Button CreateCopyButton(string code)
	{
		var button = new Button
		{
			Content = "复制",
			Tag = code,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		AddMarkdownClass(button, MarkdownStyleKeys.CopyButton);
		button.Click += async (sender, _) =>
		{
			if (sender is Button { Tag: string text }
				&& TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
			{
				await clipboard.SetTextAsync(text);
			}
		};
		return button;
	}

	private SelectableTextBlock CreateSelectableText(params string[] classes)
	{
		var textBlock = new SelectableTextBlock
		{
			Inlines = new InlineCollection(),
			TextWrapping = TextWrapping.Wrap
		};
		AddMarkdownClass(textBlock, classes);
		TextOptions.SetBaselinePixelAlignment(textBlock, BaselinePixelAlignment.Aligned);
		return textBlock;
	}

	private void BindText(SelectableTextBlock textBlock, StyledProperty<double> fontSizeProperty)
	{
		BindTheme(textBlock, TextBlock.ForegroundProperty, TextBrushProperty);
		BindTheme(textBlock, TextElement.FontFamilyProperty, ContentFontFamilyProperty);
		BindTheme(textBlock, TextElement.FontSizeProperty, fontSizeProperty);
	}

	private ContextMenu CreateViewerContextMenu()
	{
		var copyRenderedTextItem = new MenuItem
		{
			Header = "Copy rendered text"
		};
		copyRenderedTextItem.Click += async (_, _) => await CopyRenderedTextAsync();
		return new ContextMenu { ItemsSource = new[] { copyRenderedTextItem } };
	}

	private ContextMenu CreateSelectableTextContextMenu(SelectableTextBlock textBlock)
	{
		var copySelectionItem = new MenuItem
		{
			Header = "Copy selected text"
		};
		copySelectionItem.Click += (_, _) => textBlock.Copy();

		var copyRenderedTextItem = new MenuItem
		{
			Header = "Copy rendered text"
		};
		copyRenderedTextItem.Click += async (_, _) => await CopyRenderedTextAsync();

		return new ContextMenu { ItemsSource = new[] { copySelectionItem, copyRenderedTextItem } };
	}

	private IDisposable BindTheme<T>(
		AvaloniaObject target,
		AvaloniaProperty<T> targetProperty,
		StyledProperty<T> sourceProperty)
	{
		var disposable = target.Bind(targetProperty, this.GetObservable(sourceProperty));
		_bindings.Add(disposable);
		return disposable;
	}

	private void ClearBindings()
	{
		foreach (var binding in _bindings)
		{
			binding.Dispose();
		}

		_bindings.Clear();
	}

	private static void AddMarkdownClass(Control control, params string[] classes)
	{
		foreach (var className in classes.Where(name => !string.IsNullOrWhiteSpace(name)))
		{
			control.Classes.Add(className);
		}
	}

	private static void AddMarkdownClass(Span span, params string[] classes)
	{
		foreach (var className in classes.Where(name => !string.IsNullOrWhiteSpace(name)))
		{
			span.Classes.Add(className);
		}
	}

	private static StyledProperty<double> GetHeadingFontSizeProperty(int level)
	{
		return level switch
		{
			1 => Heading1FontSizeProperty,
			2 => Heading2FontSizeProperty,
			3 => Heading3FontSizeProperty,
			4 => Heading4FontSizeProperty,
			5 => Heading5FontSizeProperty,
			_ => Heading6FontSizeProperty
		};
	}

	private static string ExtractDocumentPlainText(IEnumerable<Block> blocks)
	{
		var builder = new StringBuilder();
		foreach (var block in blocks)
		{
			var blockText = ExtractPlainText(block).TrimEnd();
			if (blockText.Length == 0)
			{
				continue;
			}

			if (builder.Length > 0)
			{
				builder.AppendLine();
				builder.AppendLine();
			}

			builder.Append(blockText);
		}

		return builder.ToString();
	}

	private static string ExtractPlainText(Block block)
	{
		return block switch
		{
			HeadingBlock heading => ExtractPlainText(heading.Inline),
			ParagraphBlock paragraph => ExtractPlainText(paragraph.Inline),
			CodeBlock codeBlock => codeBlock.Lines.ToString().TrimEnd(),
			ListBlock list => ExtractListPlainText(list),
			QuoteBlock quote => string.Join(
				Environment.NewLine,
				quote.Select(ExtractPlainText).Where(text => !string.IsNullOrWhiteSpace(text))),
			Table table => ExtractTablePlainText(table),
			ThematicBreakBlock => "---",
			HtmlBlock htmlBlock => htmlBlock.Lines.ToString().TrimEnd(),
			_ => IsTypeNameFallback(block.ToString() ?? string.Empty, block.GetType())
				? string.Empty
				: block.ToString() ?? string.Empty
		};
	}

	private static string ExtractListPlainText(ListBlock list)
	{
		var builder = new StringBuilder();
		var index = 1;
		foreach (var item in list.OfType<ListItemBlock>())
		{
			var marker = list.IsOrdered ? $"{index++}. " : "- ";
			var itemText = string.Join(
				Environment.NewLine,
				item.Select(ExtractPlainText).Where(text => !string.IsNullOrWhiteSpace(text)));
			if (itemText.Length == 0)
			{
				continue;
			}

			builder.Append(marker);
			builder.AppendLine(itemText.Replace(Environment.NewLine, Environment.NewLine + "  "));
		}

		return builder.ToString().TrimEnd();
	}

	private static string ExtractPlainText(ContainerInline? container)
	{
		if (container is null)
		{
			return string.Empty;
		}

		var parts = new List<string>();
		var child = container.FirstChild;
		while (child is not null)
		{
			parts.Add(child switch
			{
				LiteralInline literal => literal.Content.ToString(),
				CodeInline code => code.Content,
				LineBreakInline => Environment.NewLine,
				TaskList => string.Empty,
				LinkInline { IsImage: true } image => ExtractImageText(image),
				ContainerInline nested => ExtractPlainText(nested),
				HtmlInline html => html.Tag,
				_ => IsTypeNameFallback(child.ToString() ?? string.Empty, child.GetType())
					? string.Empty
					: child.ToString() ?? string.Empty
			});
			child = child.NextSibling;
		}

		return string.Concat(parts);
	}

	private static string ExtractImageText(LinkInline image)
	{
		var altText = ExtractPlainText((ContainerInline)image);
		if (!string.IsNullOrWhiteSpace(altText))
		{
			return altText;
		}

		return string.IsNullOrWhiteSpace(image.Url) ? "[image]" : image.Url!;
	}

	private static string ExtractTablePlainText(Table table)
	{
		var rows = new List<string>();
		foreach (var row in table.OfType<TableRow>())
		{
			var cells = row.OfType<TableCell>()
				.Select(cell => string.Join(" ", cell.Select(ExtractPlainText)).Trim())
				.ToArray();
			rows.Add(string.Join('\t', cells));
		}

		return string.Join(Environment.NewLine, rows);
	}

	private static bool IsImageParagraph(ParagraphBlock paragraph)
	{
		var first = paragraph.Inline?.FirstChild;
		if (first is not LinkInline { IsImage: true })
		{
			return false;
		}

		for (var sibling = first.NextSibling; sibling is not null; sibling = sibling.NextSibling)
		{
			if (sibling is LiteralInline literal && string.IsNullOrWhiteSpace(literal.Content.ToString()))
			{
				continue;
			}

			return false;
		}

		return true;
	}

	private static bool TryReadTaskState(ListItemBlock item, out bool isChecked)
	{
		isChecked = false;
		if (item.FirstOrDefault() is not ParagraphBlock paragraph)
		{
			return false;
		}

		if (paragraph.Inline?.FirstChild is TaskList taskList)
		{
			isChecked = taskList.Checked;
			return true;
		}

		if (paragraph.Inline?.FirstChild is not LiteralInline literal)
		{
			return false;
		}

		var text = literal.Content.ToString();
		if (text.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
		{
			isChecked = true;
			return true;
		}

		return text.StartsWith("[ ]", StringComparison.Ordinal);
	}

	private static bool IsTypeNameFallback(string text, Type type)
	{
		return string.Equals(text, type.FullName, StringComparison.Ordinal)
			   || string.Equals(text, type.Name, StringComparison.Ordinal);
	}
}
