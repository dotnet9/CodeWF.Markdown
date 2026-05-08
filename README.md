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
        <markdown:MarkdownThemes TypographyTheme="Simple" />
    </Application.Styles>
</Application>
```

Use `MarkdownViewer` in a view:

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:md="https://codewf.com">
    <ScrollViewer
        HorizontalScrollBarVisibility="Disabled"
        VerticalScrollBarVisibility="Auto">
        <md:MarkdownViewer Markdown="{Binding Markdown}" />
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
