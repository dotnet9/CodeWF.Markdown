using AvaloniaEdit;

namespace CodeWF.Markdown.Editor.Services;

/// <summary>查找/替换结果回调：宿主用它更新查找栏文案与计数。</summary>
public delegate void MarkdownEditorSearchResultHandler(string message, int currentIndex, int totalCount);

/// <summary>查找/替换服务：正则、大小写与全词匹配，支持计数、下一个、替换当前与全部替换。</summary>
public interface IMarkdownEditorSearchService
{
    void Search(
        TextEditor editor,
        MarkdownEditorSearchCommand command,
        Action<Action> runTextMutation,
        Action publishTextChanged,
        MarkdownEditorSearchResultHandler report);
}
