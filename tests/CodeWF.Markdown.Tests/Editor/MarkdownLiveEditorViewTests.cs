using CodeWF.Markdown.Editor.Controls;
using CodeWF.Markdown.Editor.Controls.Wysiwyg;

using CodeWF.Markdown.Tests.Rendering;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

using Xunit;

namespace CodeWF.Markdown.Tests.Editor;

/// <summary>
/// 单栏实时编辑（所见即所得）视图的行为测试（需要 Avalonia 平台）。
/// </summary>
[Collection("AvaloniaPlatform")]
public sealed class MarkdownLiveEditorViewTests
{
    private readonly AvaloniaPlatformFixture _platform;

    public MarkdownLiveEditorViewTests(AvaloniaPlatformFixture platform) => _platform = platform;

    [Fact]
    public void MultiParagraphQuote_SharesContainerAndKeepsBlockNavigation() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        var markdown = "> first\n>\n> second\n\n# following";
        view.SetText(markdown);
        Assert.Equal(markdown, view.Text);
        Assert.Equal(3, view.BlockCount);
        var quote = Assert.Single(view.GetLogicalDescendants().OfType<Border>(),
            border => border.Classes.Contains("MdLiveQuote"));
        Assert.Equal(2, Assert.IsType<StackPanel>(quote.Child).Children.Count);
        view.FocusBlock(1);
        Assert.Equal(1, view.ActiveBlockIndex);
        view.InsertText("!");
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("> second!", view.Text);
        Assert.Contains("> first", view.Text);
        Assert.Contains("# following", view.Text);
    });

    [Fact]
    public void SetText_DoesNotRaiseMarkdownChanged() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        var raised = 0;
        view.MarkdownChanged += (_, _) => raised++;

        view.SetText("# Title\n\npara");

        Assert.Equal(0, raised);
        Assert.Equal(2, view.BlockCount);
    });

    [Fact]
    public void Text_RoundTripsIntoBlockModels() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("- [ ] a\n\n# b");

        var text = view.Text;

        Assert.Contains("- [ ] a", text, StringComparison.Ordinal);
        Assert.Contains("# b", text, StringComparison.Ordinal);
    });

    [Fact]
    public void ApplyExternalEdit_SwapsDocumentWithoutRaising() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("first");
        var raised = 0;
        view.MarkdownChanged += (_, _) => raised++;

        view.ApplyExternalEdit("second", 0, 5);

        Assert.Equal(0, raised);
        Assert.Contains("second", view.Text, StringComparison.Ordinal);
    });

    [Fact]
    public void InsertText_RaisesMarkdownChanged() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("start");
        var payloads = new List<string>();
        view.MarkdownChanged += (_, text) => payloads.Add(text);

        view.InsertText("- appended");

        Assert.NotEmpty(payloads);
        Assert.Contains("- appended", payloads[^1], StringComparison.Ordinal);
    });

    [Fact]
    public void TableAndTaskBlocks_ExposeStructure() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("| a |\n| --- |\n| 1 |\n\n- [x] done");

        var text = view.Text;
        Assert.Equal(2, view.BlockCount);
        Assert.True(text.Contains("| a |", StringComparison.Ordinal), $"actual: {text}");
        Assert.True(text.Contains("- [x] done", StringComparison.Ordinal), $"actual: {text}");
    });

    [Theory]
    [InlineData("# title")]
    [InlineData("> quote")]
    [InlineData("```csharp\ncode\n```")]
    public void FocusBlock_DecoratedBlockCanBeEdited(string markdown) => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText(markdown);
        view.FocusBlock(0);
        var block = Assert.Single(view.GetLogicalDescendants().OfType<MarkdownBlockView>());
        Assert.True(block.IsEditing);
        block.ActiveEditor!.Text += " changed";
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("changed", view.Text);
    });

    [Fact]
    public void InsertText_ReplacesSelectionAtCaretWithoutRebuilding() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("# abcdef");
        view.FocusBlock(0);
        var block = Assert.Single(view.GetLogicalDescendants().OfType<MarkdownBlockView>());
        var editor = block.ActiveEditor!;
        editor.SelectionStart = 2;
        editor.SelectionEnd = 4;
        view.InsertText("中文");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("ab中文ef", editor.Text);
        Assert.Equal("# ab中文ef", view.Text);
        Assert.Same(editor, block.ActiveEditor);
    });

    [Fact]
    public void Enter_ContinuesOrderedListWithoutDuplicateNumber() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("1. a\n2. b\n3. c");
        view.FocusBlock(0);
        var block = view.GetLogicalDescendants().OfType<MarkdownBlockView>().First();
        block.ActiveEditor!.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
        Dispatcher.UIThread.RunJobs();
        var models = MarkdownBlockParser.Parse(view.Text);
        Assert.Equal(new[] { 1, 2, 3, 4 }, models.Select(model => model.OrderedNumber));
        Assert.Contains("3. b", view.Text);
        Assert.Contains("4. c", view.Text);
    });

    [Fact]
    public void Typing_ImmediatelyPublishesAndTaskTogglePersists() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("- [ ] a\n- [ ] b");
        var payloads = new List<string>();
        view.MarkdownChanged += (_, text) => payloads.Add(text);
        view.FocusBlock(0);
        var block = view.GetLogicalDescendants().OfType<MarkdownBlockView>().First();
        block.ActiveEditor!.Text = "edited";
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("edited", payloads[^1]);
        block.CommitAndRender();
        view.GetLogicalDescendants().OfType<CheckBox>().First().IsChecked = true;
        var models = MarkdownBlockParser.Parse(view.Text);
        Assert.True(models[0].IsChecked);
        Assert.Equal("edited", models[0].Text);
        Assert.False(models[1].IsChecked);
    });

    [Fact]
    public void ShiftEnter_InsertsHardBreakInSameBlock() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("first");
        view.FocusBlock(0);
        var editor = view.GetLogicalDescendants().OfType<MarkdownBlockView>().Single().ActiveEditor!;
        editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, KeyModifiers = KeyModifiers.Shift });
        editor.Text += "second";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, view.BlockCount);
        Assert.Equal("first  \nsecond", view.Text);
    });

    [Fact]
    public void Table_CellEditsNavigationAndMenuActionsReachDocument() => _platform.Run(() =>
    {
        var view = new MarkdownLiveEditorView();
        view.SetText("before\n\n| a | b |\n| :--- | ---: |\n| 1 | 2 |\n\nafter");
        var changed = 0;
        view.MarkdownChanged += (_, _) => changed++;
        var table = view.GetLogicalDescendants().OfType<MarkdownTableView>().Single();
        var cells = table.GetLogicalDescendants().OfType<MarkdownBlockView>().ToArray();
        cells[2].BeginEditAtEnd();
        cells[2].ActiveEditor!.Text = "**中文**";
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("| **中文** | 2 |", view.Text);
        cells[2].ActiveEditor!.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Tab });
        Assert.False(cells[2].IsEditing);
        Assert.True(cells[3].IsEditing);

        ((MenuItem)table.ContextMenu!.Items[0]!).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        ((MenuItem)table.ContextMenu.Items[2]!).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        var model = MarkdownBlockParser.Parse(view.Text).Single(block => block.Kind == MarkdownBlockKind.Table);
        Assert.Equal(3, model.Cells.Count);
        Assert.All(model.Cells, row => Assert.Equal(3, row.Count));
        Assert.Equal(TableCellAlignment.Left, model.Alignments[0]);
        Assert.Equal(TableCellAlignment.Right, model.Alignments[1]);
        Assert.StartsWith("before\n\n", view.Text);
        Assert.EndsWith("\n\nafter", view.Text);
        Assert.Equal(3, changed);
    });

    [Fact]
    public async Task Platform_RunFromDifferentWorkersUsesOneUiThread()
    {
        var firstThread = _platform.Run(() => Environment.CurrentManagedThreadId);
        var secondThread = await Task.Run(() => _platform.Run(() => Environment.CurrentManagedThreadId));
        Assert.Equal(firstThread, secondThread);
    }
}
