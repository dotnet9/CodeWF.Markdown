# CodeWF.Markdown

基于 Avalonia 12 的 Markdown 渲染控件、排版主题和可运行示例。该仓库从 `CodeWF.AvaloniaControls` 拆分而来，只保留 Markdown 相关代码与文档。


更新日志：[UpdateLog.md](UpdateLog.md)

| 名称 | NuGet | 下载量 |
| --- | --- | --- |
| CodeWF.Markdown | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) |
| CodeWF.Markdown.Themes | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) |

## 仓库规范

- 当前版本：`12.0.4.3`，版本号统一维护在根目录 `Directory.Build.props` 的 `<Version>` 节点。
- NuGet 包项目统一支持 `net8.0;net10.0`；Demo、App、测试与内部应用项目统一使用 `net11.0` / `net11.0-windows`。
- 根目录 `logo.svg`、`logo.png`、`logo.ico` 是唯一图标源，子工程只通过 MSBuild `Link` 引用，不维护图标副本。
- 运行时帮助、Markdown 示例、内置备忘录、设计说明等业务文档按功能保留；仓库级入口文档使用根目录 `README.md` 和 `UpdateLog.md`。

## 包线说明

- `CodeWF.Markdown`：完整 MarkdownViewer，支持常见 Markdown 元素、代码高亮、图片预览、SVG/图片、数学渲染扩展、多语言资源和增量渲染。
- `CodeWF.Markdown.Themes`：`CodeWF.Markdown` 的默认控件模板和多套排版主题。

## 图片加载与导出辅助能力

`CodeWF.Markdown` 也提供可复用的 Markdown 图片工具，方便宿主应用把 Markdown 导出为可离线分发的文件。`MarkdownImageSourceLoader` 支持加载 `data:image`、本地路径、`file://` 和 HTTP(S) 图片，相对路径会按当前 Markdown 文档路径解析，并尝试 URL 解码后的文件名。`MarkdownImageRasterizer` 可把已加载的 SVG、GIF 首帧和其他位图格式转换为静态 PNG 字节，PDF、PNG、Word 或其他导出链路可以直接嵌入图片，不必重复实现预览控件里的图片加载逻辑。

`MarkdownDocumentExporter` 为宿主应用提供一行调用的 PNG/PDF/Word 导出能力：

```csharp
MarkdownDocumentExporter.ExportMarkdown(
    markdown,
    ExportKind.Pdf,
    "Simple",
    "article.pdf");

MarkdownDocumentExporter.ExportFile(
    @"C:\docs\article.md",
    ExportKind.Word,
    MarkdownTypographyThemes.Simple,
    "article.docx");

var document = new MarkdownExportDocument(markdown, filePath, fileName);
MarkdownDocumentExporter.Export(document, ExportKind.Png, "article.png");
```

内置 PNG/PDF/Word 导出器会复用公共图片加载与栅格化能力。Word 输出会把图片写入 `word/media`；PDF 输出会写入可选择文本，包含用于复制粘贴的 Unicode 文本映射，并把 Markdown 图片作为 PDF 图片内容嵌入，不再把整页压平成单张位图。

## 富 HTML 剪贴板辅助能力

`MarkdownHtmlClipboard` 和 `MarkdownHtmlClipboardExtensions` 为宿主应用提供可复用的富 HTML 剪贴板载荷，适合把 Markdown 渲染后的 HTML 复制到微信公众号、知乎、稀土掘金等网页编辑器。它会同时写入 `text/plain`、`text/html`、macOS `public.html` 和 Windows `HTML Format`；Windows 载荷使用带正确片段偏移的 UTF-8 CF_HTML 字节，避免 Chromium 系编辑器把带样式 HTML 当作普通文本显示。

Avalonia 剪贴板扩展的简单调用只需要当前 Markdown、排版主题和目标平台：

```csharp
await clipboard.TrySetMarkdownHtmlAsync(
    markdown,
    MarkdownTypographyThemes.Simple,
    "wechat",
    MarkdownTypographySizes.Small);

await clipboard.SetMarkdownHtmlAsync(
    markdown,
    MarkdownExportStyle.Resolve("Simple", "Small"),
    CopyKind.Zhihu);
```

内置目标包括 `CopyKind.Wechat`、`CopyKind.Zhihu` 和 `CopyKind.Juejin`；字符串目标名由 `MarkdownSocialCopyProfiles` 解析，方便宿主应用直接复用菜单命令参数。基于 Markdown 字符串复制时，相对图片按当前工作目录解析；基于 Markdown 文件生成内容时，相对图片可按文件路径解析。后续新增发布平台时，应用可传入自定义 `MarkdownSocialCopyProfile`，继续复用同一套 CF_HTML 剪贴板写入能力。

粘贴方向可使用 `MarkdownHtmlClipboard.Html2Markdown(htmlContent)`，把从网页复制到剪贴板的 HTML 转为 Markdown，覆盖标题、段落、链接、图片、列表、引用、代码块和表格等常见结构。转换器内置在 `CodeWF.Markdown` 中，不额外引入第三方包。

```csharp
var markdown = MarkdownHtmlClipboard.Html2Markdown(htmlContent);
```

## 安装

```powershell
Install-Package CodeWF.Markdown
Install-Package CodeWF.Markdown.Themes
```

## 使用方式

在 `App.axaml` 引入主题包：

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:markdown="https://codewf.com">
    <Application.Styles>
        <FluentTheme />
        <markdown:MarkdownThemes />
    </Application.Styles>
</Application>
```

可以在 `MarkdownThemes` 上设置全局默认，也可以在 `MarkdownViewer` 上设置单个 Viewer 覆盖。`TypographyTheme` 和 `TypographySize` 可不填，默认是 `Basic` 和 `Normal`。

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:md="https://codewf.com">
    <ScrollViewer
        HorizontalScrollBarVisibility="Disabled"
        VerticalScrollBarVisibility="Auto">
        <md:MarkdownViewer
            Markdown="{Binding Markdown}"
            TypographyTheme="Simple"
            TypographySize="Small" />
    </ScrollViewer>
</UserControl>
```

示例工程包含实时编辑、样例文档加载、排版主题切换和增量渲染压力测试。

## 扩展个性化排版主题

内置主题名继续使用 `MarkdownTypographyThemes.Simple` 这样的字符串常量，而不是改成 enum，是为了让宿主应用可以注册自己的主题 Key。自定义主题复用内置主题同一套资源 Key：

```csharp
MarkdownTypographyThemeRegistry.Register(
    "MyCompanyBlue",
    () => new ResourceDictionary
    {
        [MarkdownStyleKeys.TextBrushResource] = new SolidColorBrush(Color.Parse("#1F2937")),
        [MarkdownStyleKeys.MutedTextBrushResource] = new SolidColorBrush(Color.Parse("#64748B")),
        [MarkdownStyleKeys.AccentBrushResource] = new SolidColorBrush(Color.Parse("#0E88EB")),
        [MarkdownStyleKeys.BorderBrushResource] = new SolidColorBrush(Color.Parse("#BFDBFE")),
        [MarkdownStyleKeys.ParagraphFontSizeResource] = 16d,
        [MarkdownStyleKeys.ParagraphLineHeightResource] = 28d,
        [MarkdownStyleKeys.Heading1FontSizeResource] = 32d,
        [MarkdownStyleKeys.CodeBlockFontSizeResource] = 13d
    });

MarkdownThemes.OverrideTypographyResources(
    Application.Current!,
    "MyCompanyBlue",
    MarkdownTypographySizes.Normal);

var exportStyle = MarkdownThemes.CreateExportStyle("MyCompanyBlue");
MarkdownDocumentExporter.ExportMarkdown(markdown, ExportKind.Pdf, exportStyle, "article.pdf");
```

最简单的导出和自媒体复制 API 会通过 `MarkdownExportStyle.Resolve` 解析内置主题名和排版尺寸。如果应用需要完全接管导出外观，也可以直接构造并传入 `MarkdownExportStyle`。如果应用已有自己的 XAML 资源字典，可以注册 `() => new MyCompanyMarkdownResources()`，并在需要共享自定义资源时通过 `MarkdownThemes.CreateExportStyle(...)` 生成导出样式，让预览、PNG/PDF/Word 导出和自媒体复制 HTML 都从同一套排版资源解析样式。

## 仓库结构

- `src/CodeWF.Markdown`：完整 MarkdownViewer 类库
- `src/CodeWF.Markdown.Themes`：完整版本控件模板和排版主题
- `src/CodeWF.Markdown.Sample`：完整版本示例工程
- `tests/CodeWF.Markdown.Tests`：渲染和差异服务测试
- `CodeWF.Markdown.slnx`：Markdown 类库、示例和测试的解决方案视图

## 构建

```powershell
dotnet restore CodeWF.Markdown.slnx
dotnet build CodeWF.Markdown.slnx --no-restore
```

打包 NuGet：

```powershell
.\pack.bat
```

发布示例工程到 `win-x64` 和 `linux-x64`：

```powershell
.\publish_Markdown.bat
```

## 许可证

MIT，详见 [LICENSE](LICENSE)。

## 第三方开源组件审计

检查时间：2026-05-23。检查范围包括 NuGet 元数据、恢复后的 `project.assets.json`、NuGet.org 信息以及上游源码/许可证链接。优先接受 MIT / Apache-2.0 / BSD。

本次整改：

- 将 `Semi.Avalonia.AvaloniaEdit` 替换为开源 `Avalonia.AvaloniaEdit`。
- 示例工程移除 `AvaloniaEditSemiTheme`，编辑器渲染改为使用开源 AvaloniaEdit 控件和本仓库自己的 Markdown 主题。

| 包 | 协议 | 源码/项目地址 | 结论 |
| --- | --- | --- | --- |
| `AnimatedImage.Avalonia` | Apache-2.0 | https://github.com/whistyun/AnimatedImage | 通过 |
| `Avalonia` / `Avalonia.Desktop` / `Avalonia.Fonts.Inter` / `Avalonia.Themes.Fluent` | MIT | https://github.com/AvaloniaUI/Avalonia | 通过 |
| `Avalonia.AvaloniaEdit` | MIT | https://github.com/AvaloniaUI/AvaloniaEdit | 通过 |
| `CommunityToolkit.Mvvm` | MIT | https://github.com/CommunityToolkit/dotnet | 通过 |
| `Lang.Avalonia.Json` | MIT | https://github.com/dotnet9/Lang.Avalonia | 自研开源包 |
| `Markdig` | BSD-2-Clause | https://github.com/xoofx/markdig | 通过 |
| `Semi.Avalonia` | MIT | https://github.com/irihitech/Semi.Avalonia | 通过，仅示例使用开源主体包 |
| `Svg.Controls.Skia.Avalonia` / `Svg.Skia` | MIT | https://github.com/wieslawsoltes/Svg.Skia | 通过 |
| `Sylinko.CSharpMath.Avalonia` | MIT | https://github.com/Sylinko/CSharpMath.Avalonia | 通过 |
| `TextMateSharp` / `TextMateSharp.Grammars` | MIT | https://github.com/danipen/TextMateSharp | 通过 |
| `VC-LTL` | EPL-2.0 | https://github.com/Chuyu-Team/VC-LTL5 | 源码开放，按“非优先但可追溯”规则通过 |
| `YY-Thunks` | MIT | https://github.com/Chuyu-Team/YY-Thunks | 通过 |
| `Microsoft.NET.Test.Sdk` | MIT | https://github.com/microsoft/vstest | 测试依赖，通过 |
| `xunit` / `xunit.runner.visualstudio` | Apache-2.0 | https://github.com/xunit/xunit | 测试依赖，通过 |

传递依赖检查结论：Avalonia、AnimatedImage、SkiaSharp、Svg.Skia、CSharpMath、TextMateSharp 等链路均有公开源码，许可证为 MIT 或 BSD-style。有效项目文件中不再包含 `Semi.Avalonia.AvaloniaEdit`。
