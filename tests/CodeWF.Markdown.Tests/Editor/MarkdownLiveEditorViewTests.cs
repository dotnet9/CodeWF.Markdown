using CodeWF.Markdown.Editor.Controls;
using CodeWF.Markdown.Editor.Controls.Wysiwyg;

using CodeWF.Markdown.Tests.Rendering;

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
}
