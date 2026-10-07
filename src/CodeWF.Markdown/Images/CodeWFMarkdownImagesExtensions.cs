using Avalonia.Layout;

using CodeWF.Markdown.Controls;
using CodeWF.Markdown.Rendering;

namespace CodeWF.Markdown.Images;

/// <summary>
/// 图片渲染能力接入扩展：注册后 Markdown 图片使用异步加载、SVG/GIF
/// 预览与点击放大；不注册时由 Core 降级为替代文本渲染。
/// </summary>
public static class CodeWFMarkdownImagesExtensions
{
    public static void UseImages() =>
        MarkdownViewer.RegisterImageControlFactory(static (context, source, altText) =>
        {
            var image = new MarkdownImage
            {
                Source = source,
                AltText = altText,
                ImageBasePath = context.ImageBasePath
            };
            context.AddMarkdownClass(image, MarkdownStyleKeys.Image);
            return image;
        });
}
