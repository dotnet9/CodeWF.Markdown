# 更新日志

## 12.0.3.1 - 2026-05-16

- 😄[新增]-新增内部 `MarkdownMathView` 用于公式渲染，使数学公式前景色跟随当前 Markdown 主题。
- 🔨[优化]-更新完整版和 Lite 示例应用中的横向滑动图片 Markdown，改用 CodeWF 截图示例。
- 🔨[优化]-为示例应用补充 Markdown、主题、SVG 及相关程序集的裁剪保留配置，改善裁剪发布兼容性。
- 🔨[优化]-更新 `publishbase.bat`，按运行时和项目名输出到稳定发布目录，并在缺少预期可执行文件时失败退出。
- 🔨[优化]-将共享包版本提升到 `12.0.3.1`，并通过根构建属性统一 Markdown 包版本配置。
- 🔨[优化]-更新 SVG 与运行时辅助依赖基线，包括 `Svg.Controls.Skia.Avalonia`、`Svg.Skia` 和 `YY-Thunks`。

## 12.0.2.7 - 2026-05-13

- 😄[新增]-新增 `CodeWF.Markdown.Lite`，提供基础 Markdown Viewer，直接包引用仅包含 `Avalonia` 和 `Markdig`。
- 🔨[优化]-Lite 渲染支持常用标题、段落、列表、任务列表、引用、表格、位图图片、纯文本代码块和复制按钮。
- 😄[新增]-新增 `CodeWF.Markdown.Lite.Themes`，模板和排版主题资源与 `CodeWF.Markdown.Themes` 保持一致，仅引用 Lite Viewer 程序集。
- 😄[新增]-新增 `CodeWF.Markdown.Lite.Sample`，以简体中文保留编辑预览和多预览主题演示，移除多语言切换和 AvaloniaEdit 依赖。
- 🔴[修复]-修复 Lite 行内文本继承问题，标题字号等排版资源现在可正确生效。
- 🔴[修复]-修复完整版增量渲染中块级主题绑定未及时释放的问题，连续切换 Markdown 文件时旧控件引用可正常清理。
- 🔨[优化]-改进完整版与 Lite 的图片清理逻辑，图片被替换或控件离开可视树时会取消未完成加载并释放位图。
- 🔨[优化]-调整完整版图片预览窗口，使预览窗口独立持有位图，Markdown 切换时 Viewer 图片资源可安全释放。
- 🔨[优化]-已对完整版和 Lite 示例应用执行重复 Markdown 切换与滚动压力测试，清理后未发现内存溢出或 CPU 飙高。
- 🔨[优化]-更新解决方案、打包脚本和发布脚本，纳入 Lite 包线。
- 🔨[优化]-删除各工程目录下的 `CHANGELOG.md`，后续统一维护根目录更新日志。

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
