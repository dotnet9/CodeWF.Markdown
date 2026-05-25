# 更新日志

## 12.0.3.8 - 2026-05-25

- 😄[新增]-新增 Markdown 图片源公共加载能力，支持 `data:image`、本地路径、`file://` 与 HTTP(S) URL，并按 `ImageBasePath` 处理 URL 解码后的相对路径回退。
- 😄[新增]-新增可复用的图片栅格化辅助能力，统一处理 SVG 预览、GIF 首帧静态 PNG 输出和位图转 PNG 规范化，便于导出链路嵌入图片而不重复维护预览控件逻辑。
- 🔨[优化]-完整版 `MarkdownImage` 控件改为复用公共图片加载与栅格化能力，同时保留实时预览中的 GIF 动画播放。
- 🔨[优化]-补充 `data:image`、URL 编码相对本地图、HTTP 图片加载、SVG 栅格化和 GIF 首帧 PNG 输出测试。

## 12.0.3.6 - 2026-05-23

- 😄[新增]-通过 `AnimatedImage.Avalonia` 支持 Markdown 图片块、行内图片和图片预览窗口中的 GIF 动画播放。
- 😄[新增]-`MarkdownViewer.ImageBasePath` 支持把相对图片路径按当前文档路径解析，不再落到程序基目录。
- 🔨[优化]-保留 SVG 动画控件路径，并继续使用栅格兜底计算尺寸与预览安全回退。
- 🔨[优化]-补充 GIF 动画依赖的裁剪保留配置和第三方开源审计说明。

## 12.0.3.2 - 2026-05-20

- 🔨[优化]-先合并远端最新 `12.0.3.1` 发布更新，再应用本地依赖调整。
- 🔨[优化]-示例应用将 `Semi.Avalonia.AvaloniaEdit` 替换为源码开放的 `Avalonia.AvaloniaEdit`。
- 🔨[优化]-移除完整版示例应用中的 `AvaloniaEditSemiTheme`，避免继续依赖 Semi AvaloniaEdit 主题包。
- 🔨[优化]-测试依赖升级到最新稳定版：`Microsoft.NET.Test.Sdk 18.5.1`、`xunit 2.9.3`、`xunit.runner.visualstudio 3.1.5`。
- 🔨[优化]-同步更新英文和简体中文开源依赖审计说明。

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
