# CodeWF.Markdown

Avalonia Markdown viewer controls, typography themes, and a runnable sample app split from `CodeWF.AvaloniaControls` into a standalone repository.

[简体中文](README.zh-CN.md) | English

Changelog: [English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

| Package | NuGet | Downloads |
| --- | --- | --- |
| CodeWF.Markdown | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) |
| CodeWF.Markdown.Themes | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) |

## Packages

- `CodeWF.Markdown`: full Markdown viewer with common Markdown elements, code highlighting, image preview, SVG/image support, math rendering hooks, localization, and incremental rendering.
- `CodeWF.Markdown.Themes`: default templates and typography themes for `CodeWF.Markdown`.

## Image Loading And Export Helpers

`CodeWF.Markdown` also exposes shared Markdown image utilities for host applications that export Markdown to offline files. `MarkdownImageSourceLoader` loads `data:image` URIs, local paths, `file://` URIs, and HTTP(S) images, resolving relative paths against the current Markdown document path and trying URL-decoded filenames. `MarkdownImageRasterizer` converts loaded SVG, GIF first frames, and other bitmap formats to static PNG bytes so PDF, PNG, Word, or other export pipelines can embed images without reimplementing viewer-specific loading logic.

`MarkdownDocumentExporter` provides one-call export helpers for host applications:

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

The built-in PNG/PDF/Word exporters reuse the shared image loader and rasterizer. Word output embeds image parts under `word/media`, while image-based PDF output renders the document with resolved images before writing PDF pages.

## Rich HTML Clipboard Helpers

`MarkdownHtmlClipboard` and `MarkdownHtmlClipboardExtensions` create reusable rich HTML clipboard payloads for host applications that copy Markdown-rendered HTML into web editors such as WeChat Official Account, Zhihu, and Juejin. They write `text/plain`, `text/html`, macOS `public.html`, and Windows `HTML Format` data; the Windows payload is UTF-8 CF_HTML bytes with correct fragment offsets, so Chromium-based editors can paste styled HTML instead of showing the raw markup as plain text.

The simple Avalonia clipboard path only needs the Markdown text, active typography theme, and target platform:

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

Built-in targets are `CopyKind.Wechat`, `CopyKind.Zhihu`, and `CopyKind.Juejin`; string target names are resolved by `MarkdownSocialCopyProfiles` so host applications can keep lightweight menu command parameters. Markdown string copy resolves relative images from the current working directory. File-based content creation can resolve relative images from the Markdown file path. Applications can pass a custom `MarkdownSocialCopyProfile` for new publishing targets while still reusing the same CF_HTML clipboard writer.

## Installation

```powershell
Install-Package CodeWF.Markdown
Install-Package CodeWF.Markdown.Themes
```

## Usage

Add the theme package in `App.axaml`:

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

Set `TypographyTheme` and `TypographySize` on `MarkdownThemes` for app defaults, or on `MarkdownViewer` for per-viewer overrides. Omitted values default to `Basic` and `Normal`.

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

The sample app shows live editing, file loading, theme switching, and incremental rendering stress scenarios.

## Custom Typography Themes

Built-in theme names stay as string constants such as `MarkdownTypographyThemes.Simple` instead of an enum because host applications can register their own theme keys. A custom theme can reuse the same resource keys used by the built-in themes:

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

The simplest export and social-copy APIs resolve built-in theme names through `MarkdownExportStyle.Resolve`, including typography size. Applications that need complete control can still build and pass a `MarkdownExportStyle` directly. Applications that keep custom XAML resource dictionaries can register `() => new MyCompanyMarkdownResources()` and create an export style with `MarkdownThemes.CreateExportStyle(...)` when they want preview, PNG/PDF/Word export, and social-copy HTML styling to share the same custom resource dictionary.

## Repository Layout

- `src/CodeWF.Markdown`: full Markdown viewer package
- `src/CodeWF.Markdown.Themes`: full viewer templates and typography themes
- `src/CodeWF.Markdown.Sample`: full viewer sample app
- `tests/CodeWF.Markdown.Tests`: rendering and diff service tests
- `CodeWF.Markdown.slnx`: solution view for Markdown projects, sample, and tests

## Build

```powershell
dotnet restore CodeWF.Markdown.slnx
dotnet build CodeWF.Markdown.slnx --no-restore
```

To create NuGet packages:

```powershell
.\pack.bat
```

To publish the sample app for `win-x64` and `linux-x64`:

```powershell
.\publish_Markdown.bat
```

## License

MIT. See [LICENSE](LICENSE).

## Third-Party Open Source Audit

Checked on 2026-05-23 with NuGet metadata, restored `project.assets.json`, and upstream source/license links. MIT / Apache-2.0 / BSD are preferred.

Remediation:

- Replaced `Semi.Avalonia.AvaloniaEdit` with the open-source `Avalonia.AvaloniaEdit` package.
- Removed `AvaloniaEditSemiTheme` from the full sample app; editor rendering now relies on the open AvaloniaEdit control and the repository's own Markdown themes.

| Package | License | Source | Status |
| --- | --- | --- | --- |
| `AnimatedImage.Avalonia` | Apache-2.0 | https://github.com/whistyun/AnimatedImage | Approved |
| `Avalonia` / `Avalonia.Desktop` / `Avalonia.Fonts.Inter` / `Avalonia.Themes.Fluent` | MIT | https://github.com/AvaloniaUI/Avalonia | Approved |
| `Avalonia.AvaloniaEdit` | MIT | https://github.com/AvaloniaUI/AvaloniaEdit | Approved |
| `CommunityToolkit.Mvvm` | MIT | https://github.com/CommunityToolkit/dotnet | Approved |
| `Lang.Avalonia.Json` | MIT | https://github.com/dotnet9/Lang.Avalonia | Approved |
| `Markdig` | BSD-2-Clause | https://github.com/xoofx/markdig | Approved |
| `Semi.Avalonia` | MIT | https://github.com/irihitech/Semi.Avalonia | Approved, only the open core package is used by the sample |
| `Svg.Controls.Skia.Avalonia` / `Svg.Skia` | MIT | https://github.com/wieslawsoltes/Svg.Skia | Approved |
| `Sylinko.CSharpMath.Avalonia` | MIT | https://github.com/Sylinko/CSharpMath.Avalonia | Approved |
| `TextMateSharp` / `TextMateSharp.Grammars` | MIT | https://github.com/danipen/TextMateSharp | Approved |
| `VC-LTL` | EPL-2.0 | https://github.com/Chuyu-Team/VC-LTL5 | Source-open; approved under the source-traceable non-preferred license rule |
| `YY-Thunks` | MIT | https://github.com/Chuyu-Team/YY-Thunks | Approved |
| `Microsoft.NET.Test.Sdk` | MIT | https://github.com/microsoft/vstest | Approved, test-only |
| `xunit` / `xunit.runner.visualstudio` | Apache-2.0 | https://github.com/xunit/xunit | Approved, test-only |

Transitive dependencies from Avalonia, AnimatedImage, SkiaSharp, Svg.Skia, CSharpMath, and TextMateSharp were checked and are source-open under MIT/BSD-style licenses. Active project files no longer contain `Semi.Avalonia.AvaloniaEdit`.
