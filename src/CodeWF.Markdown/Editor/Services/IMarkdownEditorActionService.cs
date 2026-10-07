using AvaloniaEdit;

namespace CodeWF.Markdown.Editor.Services;

/// <summary>
/// Markdown 编辑器动作服务：把「用户动作」翻译为对 AvaloniaEdit 的文本改写。
/// 事件发布与同步节流由控制器统一处理，本类只负责动作 → 变更的映射。
/// </summary>
public interface IMarkdownEditorActionService
{
    Task ExecuteAsync(TextEditor editor, MarkdownEditorAction action, Action<Action> runTextMutation);
}
