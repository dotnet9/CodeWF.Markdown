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

Checked on 2026-05-20 with NuGet metadata, restored `project.assets.json`, and upstream source/license links. MIT / Apache-2.0 / BSD are preferred.

Remediation:

- Replaced `Semi.Avalonia.AvaloniaEdit` with the open-source `Avalonia.AvaloniaEdit` package.
- Removed `AvaloniaEditSemiTheme` from the full sample app; editor rendering now relies on the open AvaloniaEdit control and the repository's own Markdown themes.

| Package | License | Source | Status |
| --- | --- | --- | --- |
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

Transitive dependencies from Avalonia, SkiaSharp, Svg.Skia, CSharpMath, and TextMateSharp were checked and are source-open under MIT/BSD-style licenses. Active project files no longer contain `Semi.Avalonia.AvaloniaEdit`.
