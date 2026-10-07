using CodeWF.Markdown.Editor.Services;

using Xunit;

namespace CodeWF.Markdown.Tests.Editor;

/// <summary>
/// 下沉到 CodeWF.Markdown.Editor 的纯逻辑测试（不依赖 Avalonia 平台）：
/// 智能换行、模板服务与本地化回退。
/// </summary>
public sealed class MarkdownEditorLogicTests
{
    [Fact]
    public void SmartNewLine_ContinuesUnorderedList()
    {
        const string text = "- first";
        var change = MarkdownSmartNewLine.CreateChange(text, text.Length, 0);

        Assert.Equal(text.Length, change.Start);
        Assert.Equal(0, change.Length);
        Assert.Equal("\n- ", change.Text);
    }

    [Fact]
    public void SmartNewLine_IncrementsOrderedListNumber()
    {
        const string text = "1. first";
        var change = MarkdownSmartNewLine.CreateChange(text, text.Length, 0);

        Assert.Equal(text.Length, change.Start);
        Assert.Equal("\n2. ", change.Text);
    }

    [Fact]
    public void SmartNewLine_EndsListOnEmptyItem()
    {
        const string text = "- first\n- ";
        var change = MarkdownSmartNewLine.CreateChange(text, text.Length, 0);

        // 空列表项上按 Enter 结束列表：删掉前缀，只留换行。
        Assert.Equal(8, change.Start);
        Assert.Equal(2, change.Length);
        Assert.Equal("\n", change.Text);
    }

    [Fact]
    public void InvariantLocalizer_ReturnsNonEmptyTextForEveryKey()
    {
        var localizer = new InvariantMarkdownEditorLocalizer();

        foreach (var key in Enum.GetValues<MarkdownEditorText>())
        {
            Assert.False(string.IsNullOrWhiteSpace(localizer.Get(key)), $"missing text for {key}");
        }
    }

    [Fact]
    public void InvariantLocalizer_FormatFillsArguments()
    {
        var localizer = new InvariantMarkdownEditorLocalizer();

        var text = localizer.Format(MarkdownEditorText.SearchMatchCountFormat, 3, "abc", 4);

        Assert.Contains("3", text, StringComparison.Ordinal);
        Assert.Contains("abc", text, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionEnum_CoversHostMenuActions()
    {
        // 宿主（Vex）右键菜单依赖这些动作存在，改名会直接破坏宿主映射。
        MarkdownEditorAction[] required =
        [
            MarkdownEditorAction.Undo,
            MarkdownEditorAction.Redo,
            MarkdownEditorAction.Cut,
            MarkdownEditorAction.Copy,
            MarkdownEditorAction.Paste,
            MarkdownEditorAction.SelectAll,
            MarkdownEditorAction.CopyPlainText,
            MarkdownEditorAction.Bold,
            MarkdownEditorAction.Italic,
            MarkdownEditorAction.InlineCode,
            MarkdownEditorAction.Link,
            MarkdownEditorAction.Image,
            MarkdownEditorAction.ClearFormatting,
            MarkdownEditorAction.Heading1,
            MarkdownEditorAction.Quote,
            MarkdownEditorAction.UnorderedList,
            MarkdownEditorAction.TaskList,
            MarkdownEditorAction.CodeFence,
            MarkdownEditorAction.Table,
            MarkdownEditorAction.MathBlock,
            MarkdownEditorAction.HorizontalRule,
            MarkdownEditorAction.SmartNewLine,
            MarkdownEditorAction.Indent,
            MarkdownEditorAction.Outdent,
            MarkdownEditorAction.FocusEditor
        ];

        foreach (var action in required)
        {
            Assert.True(Enum.IsDefined(action), $"{action} is not defined");
        }
    }

    [Fact]
    public void TemplateService_UsesOptionsForLinkAndImageTargets()
    {
        var options = new MarkdownEditorOptions
        {
            LinkUrlPlaceholder = "https://codewf.com",
            ImageTargetPlaceholder = "assets/a.png"
        };
        var templates = new MarkdownEditorTemplateService(new InvariantMarkdownEditorLocalizer(), options);

        Assert.Equal("https://codewf.com", templates.LinkUrlPlaceholder);
        Assert.Equal("![alt text](assets/a.png)", templates.ImageInsertion);
        Assert.Contains("| --- |", templates.TableInsertion, StringComparison.Ordinal);
    }
}
