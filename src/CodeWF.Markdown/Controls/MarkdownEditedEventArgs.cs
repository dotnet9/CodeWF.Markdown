namespace CodeWF.Markdown.Controls;

/// <summary>
/// MarkdownViewer 可视化编辑提交后的新 Markdown 文本。
/// </summary>
public sealed class MarkdownEditedEventArgs : EventArgs
{
    public MarkdownEditedEventArgs(string markdown)
    {
        Markdown = markdown;
    }

    public string Markdown { get; }
}
