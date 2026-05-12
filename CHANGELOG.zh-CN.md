# 更新日志

## 12.0.2.6 - 2026-05-12

- 😄[新增]-`MarkdownViewer` 新增 `TypographyTheme` 与 `TypographySize`，支持单个 Viewer 独立覆盖排版主题和尺寸。
- 😄[新增]-`MarkdownThemes` 新增 `TypographySize`，并提供紧凑型排版资源，用于收紧字号、行高和块间距。
- 🔨[优化]-示例应用调整为 Tab 结构，新增多 Viewer 排版演示，支持全局设置和单个 Viewer 设置联动。
- 🔴[修复]-修复继承排版资源时复用已有父级 `ResourceDictionary` 导致的运行期异常。
- 🔨[优化]-更新 Markdown 包版本和依赖基线，配合新的排版配置能力发布。

## 12.0.2.5 - 2026-05-09

- 🔨[优化]-将 Markdown 包和示例应用的多语言资源从 `Lang.Avalonia.Resx` 替换为 `Lang.Avalonia.Json`。
- 🔨[优化]-JSON 语言资源复制到输出目录 `I18n` 并随 NuGet content files 分发，AOT 发布后语言切换可正常工作。
- 🔨[优化]-更新强类型语言键生成模板和示例启动注册，统一使用 JSON 语言资源。

## 12.0.2.4 - 2026-05-08

- 🔨[优化]-将 CodeWF.Markdown 包和示例从原 `CodeWF.AvaloniaControls` 仓库拆分为独立仓库。
- 🔨[优化]-保留完整 Markdown 控件、主题、示例和测试项目。
- 🔨[优化]-移除已废弃的低依赖版本包、配套主题和示例应用。
- 🔨[优化]-更新解决方案、打包脚本、发布脚本、README 文件和仓库协作说明，仅保留完整 Markdown 包线。
- 😄[新增]-新增简体中文更新日志，用于记录仓库级发布变更。
