using System.Runtime.CompilerServices;

using Avalonia.Metadata;

[assembly: XmlnsPrefix("https://codewf.com", "markdown")]

// 渲染引擎（CodeWF.Markdown.Lite）
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Controls")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Rendering")]

// 能力包（图片 / 数学 / Mermaid）与编辑器控件同属本包
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Images")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.MathRendering")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Mermaid")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Editor")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Editor.Controls")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Editor.Controls.Wysiwyg")]
[assembly: XmlnsDefinition("https://codewf.com", "CodeWF.Markdown.Editor.Services")]

[assembly: InternalsVisibleTo("CodeWF.Markdown.Tests")]
