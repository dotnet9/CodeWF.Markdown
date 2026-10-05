using Avalonia.Controls;
using Markdig.Syntax;

namespace CodeWF.Markdown.Rendering;

/// <summary>
/// 单种 Markdown 块的渲染器。TryRender 返回 null 表示不受理，
/// 管线继续交给下一个渲染器或 Viewer 内置的兜底转换。
/// </summary>
internal interface IMarkdownBlockRenderer
{
    Control? TryRender(Block block, string? sourceMarkdown, IMarkdownRenderContext context);
}

/// <summary>
/// 按注册顺序试渲染的块级管线；渲染器无状态，可跨 Viewer 实例共享。
/// </summary>
internal sealed class MarkdownBlockRendererPipeline
{
    private readonly List<IMarkdownBlockRenderer> _renderers = [];

    public void Register(IMarkdownBlockRenderer renderer)
    {
        _renderers.Add(renderer);
    }

    public Control? Render(Block block, string? sourceMarkdown, IMarkdownRenderContext context)
    {
        foreach (var renderer in _renderers)
        {
            if (renderer.TryRender(block, sourceMarkdown, context) is { } control)
            {
                return control;
            }
        }

        return null;
    }
}
