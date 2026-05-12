# CodeWF.Markdown

基于 Avalonia 12 的 Markdown 渲染控件、排版主题和可运行示例。该仓库从 `CodeWF.AvaloniaControls` 拆分而来，只保留 Markdown 相关代码与文档。

[English](README.md) | 简体中文

更新日志：[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

| 名称 | NuGet | 下载量 |
| --- | --- | --- |
| CodeWF.Markdown | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.svg)](https://www.nuget.org/packages/CodeWF.Markdown/) |
| CodeWF.Markdown.Themes | [![NuGet](https://img.shields.io/nuget/v/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) | [![NuGet](https://img.shields.io/nuget/dt/CodeWF.Markdown.Themes.svg)](https://www.nuget.org/packages/CodeWF.Markdown.Themes/) |

## 包线说明

- `CodeWF.Markdown`：完整 MarkdownViewer，支持常见 Markdown 元素、代码高亮、图片预览、SVG/图片、数学渲染扩展、多语言资源和增量渲染。
- `CodeWF.Markdown.Themes`：`CodeWF.Markdown` 的默认控件模板和多套排版主题。

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
