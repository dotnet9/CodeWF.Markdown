# 更新日志


## 14.0.1-dev.20261008.10 (2026-10-08，联调开发版)

- 🐛[修复]-实时编辑保留未改动块的原始 Markdown、空白、行尾和复杂元素；有序列表、引用层级、链接样式、选区定位、列表续行与表格结构操作修正。
- 🐛[修复]-块编辑采用隧道路由接管左键点击，避免渲染文本抢回焦点；原地输入立即同步正文，多段引用共用容器并保留逐段编辑和导航。
- 🐛[修复]-Demo 注册完整渲染能力，实时块复用 MarkdownViewer；C# 高亮别名/默认字形、编辑器独立配色与 URL 颜色、Mermaid 深色变体和颜色表达式修正。
- 🎨[界面]-按 design/prototype.html 校对标题栏、216px 侧栏、44px 工具栏、32px 面板头、30px 状态栏、文件图标/选中竖条、视图按钮、默认排版与引用留白。
- 🔨[优化]-标题和引用默认装饰允许宿主按语义类名覆盖，Demo 与 Vex 可以使用各自原型的边框和间距。
- 🎨[界面]-源码字号保持 13px，行高按原型对齐为 22.1px；编辑器新增可配置 EditorLineHeight，基于字体实测高度调整行距；Demo 预览正文末尾补齐 48px 留白。
- 🧪[验证]-182 项 Release 测试通过；离屏覆盖明暗主题、1400/980 宽度、四模式、实际点击/输入/Esc、共享引用编辑与源码同步。规格和验证边界见 design/README.md。
- 🧹[维护]-移除临时核对报告，将有效规格与验证方法合入原型维护文档，保留教程、样例图片和验证代码。
- 📦[联调]-四包开发版仅本地打包，未创建正式标签/发布 NuGet；系统窗控/DPI/IME 等实机验证边界单独记录。

## 14.0.0 (2026-10-07)

- 💥[破坏性]-包线重组为 4 个包（按「是否引入第三方能力依赖」切分，基础与完整**共用同一份渲染实现**，不再有 Lite/完整两套冗余代码）：
  - `CodeWF.Markdown.Lite`：唯一渲染引擎（常规元素 + 代码块单色 + 图片替代文本 + 公式原文），零第三方能力依赖。
  - `CodeWF.Markdown`：在 Lite 之上提供代码高亮 / 数学公式 / Mermaid / 图片(GIF·SVG·预览) / PNG·PDF·Word·公众号 HTML 导出，**并含编辑器控件**。
  - `CodeWF.Markdown.Lite.Themes`：控件模板 + 全部排版令牌 + 18 套排版主题。
  - `CodeWF.Markdown.Themes`：完整包样式入口 `MarkdownFullThemes`（Lite.Themes + 图片能力外观）。
- 💥[破坏性]-原能力包 `CodeWF.Markdown.Highlighting` / `.Math` / `.Mermaid` / `.Images` / `.Export` 与 `CodeWF.Markdown.Editor` 停止发布，能力与编辑器并入 `CodeWF.Markdown`；能力扩展方法命名空间保持不变，调用代码零改动。
- 💥[破坏性]-所有 NuGet 包统一为 `net10.0` 单目标（Mermaid 依赖仅提供 net10 资产），net8 消费者需先升级目标框架。
- 🚀[新增]-编辑器并入 `CodeWF.Markdown`，并新增**单栏实时编辑（所见即所得）控件 `MarkdownLiveEditorView`**：每个块渲染为富文本（粗体/斜体/删除线/行内代码/链接），点入该块就地按源码编辑、离开即回渲染；表格直接渲染为表格并可编辑单元格，任务列表可直接勾选；与源码编辑器共享格式化动作、本地化与 `SetText`/`ApplyExternalEdit` 同步语义（载入不回抛 `MarkdownChanged`）。
- 🔨[优化]-编辑器视图逐块解析与回写（`MarkdownBlockParser` + `MarkdownTextRunParser`），解析/回写往返幂等，新增 21 项编辑器与所见即所得测试（累计 141 项全通过）。
- 🔨[优化]-测试基建：Avalonia headless 平台改为「首次调用线程初始化 + 串行测试」，彻底消除跨线程持有控件导致的随机失败。

## 13.1.1 (2026-10-07)

- 🐛[修复]-发布链路：v13.1.0 的 CI 在 Build 步骤失败（该 tag 与新依赖 `CodeWF.AvaloniaControls` 12.3.0 同批推送，runner 恢复时 nuget.org 尚未上架，NU1101），未产出 NuGet 包与 Release；本版依赖对齐到已上架的 12.3.1 并以 13.1.1 重新发布。
- 🧪[测试]-修复测试进程内 Avalonia 初始化线程不一致导致的随机失败（离线渲染用例与主题资源用例相互影响，单独跑过、全量跑挂）：新增 `AvaloniaPlatform` 集合夹具统一初始化，全部 109 项测试稳定通过。

## 13.1.0 (2026-10-07)

- 🚀[新增]-文档宿主改为虚拟化面板：块数达到阈值（默认 40，可调）后只物化视口 ± 2 屏内的块，代码块/表格/图片等大块始终物化；离屏块保留已测量高度占位，滚动离屏即释放控件，长文档不再一次性挂载全部控件。
- 🚀[新增]-`MarkdownViewer.EnableVirtualization` / `VirtualizationThreshold` 开关与阈值（异常场景可退回非虚拟化宿主）；新增 `RealizedBlockCount` / `IsBlockRealized` 供宿主与诊断观察物化状态。
- 🔨[优化]-偏移映射在虚拟化宿主下会先物化目标块再返回精确 Bounds。
- 🔨[优化]-虚拟化宿主改为复用 `CodeWF.AvaloniaControls` 的 `WindowedStackPanel`（同源实现只维护一份），库内不再自带裁剪面板与重复单测。
- 🔨[优化]-依赖 `CodeWF.AvaloniaControls` 对齐到已上架版本 12.3.1。


## 13.0.0.1 (2026-10-06)

- 🐛[修复]-公式字形可见性修复正式发版：13.0.0 包内仍是官方 CSharpMath 画布（Avalonia 12.1.3 下只剩分数线），本版把内置 Avalonia 12 兼容画布交付到 NuGet，`UseMath()` 接入后块级/行内公式字形完整显示。关联：dotnet9/Vex#3。

## 13.0.0.0 (2026-10-06)

### 大版本重构：能力包架构

- **包矩阵重组**：主包聚焦核心渲染（渲染器管线 + 插件接缝 + 公共文档模型），高亮、数学、Mermaid、图片、导出全部拆为可选能力包；`CodeWF.Markdown.Lite` / `Lite.Themes` 退役——轻量需求等待 Core 包，过渡期可暂留 12.x。
- **渲染器管线**：内置 11 个块级渲染器（特殊块/段落/数学/代码/列表/引用/表格/分割线/标题/脚注/HTML），`MarkdownViewer.RegisterBlockRenderer` 支持注册外部渲染器；内联转换引擎与链接交互拆为独立组件。
- **新能力包 CodeWF.Markdown.Mermaid**（net10.0+）：```mermaid 围栏代码块渲染为图表，基于 Mermaider 纯 .NET 实现（无 JavaScript），后台渲染 + 主题跟随 + 失败回落源码。
- **新能力包 CodeWF.Markdown.Export**：PNG / PDF / Word 导出与微信公众号、知乎、掘金剪贴板 HTML。
- **升级**：Markdig 1.4.0、Mermaider 0.14.1、Lang.Avalonia.Json 12.1.2.15、Microsoft.NET.Test.Sdk 18.10.1。
- **Demo 发布**：新增 publish-demo 工作流，为演示应用产出 win-x64（Inno Setup）/ linux-x64 / linux-arm64（deb）/ osx-x64 / osx-arm64（dmg）五平台安装包。
- **公式渲染修复**：官方 Sylinko.CSharpMath.Avalonia 12.0.0 的画布在 Avalonia 12.1.3 下填充失效（`StreamGeometryContext` 画不出图形、`StreamGeometry.Parse` 不渲染），公式只剩分数线；`CodeWF.Markdown.Math` 改为内置 Avalonia 12 兼容画布（自实现 `ICanvas` / `Path`，直接组装 `PathFigure` 绘制），公式字形恢复完整显示，并新增离屏渲染回归测试守住该行为。
- **后台解析调度**：文本变更只登记版本化快照，合并窗口（约 70ms）后在后台线程解析，过期结果按版本丢弃；新增脏区间 diff 与块首行哈希匹配（失败自动降级全量），长文档连续输入不再阻塞 UI 线程。
- **新增能力 API**：`ExportKind.Html`（自包含单文件 HTML，同时提供打印预览/剪贴板用的 `RenderHtml`）、`MarkdownViewer.SaveReadingPosition/RestoreReadingPosition`、`MarkdownViewer.RefreshRemoteImages`、`MarkdownDocumentModel.GetOutline`、`MarkdownTextStatistics.Calculate`、任务列表勾选回写（点击勾选框 → 按源码偏移改写 Markdown 并抛出变更区间）。宿主不必再自行反射滚动位置、给图片 URL 追参或正则改写源码。
- **Demo 主窗体对齐原型**：标题栏 48px、徽章与窗口按钮尺寸按 `design/prototype.html` 收敛。
- **破坏性变更**：主包不再内置高亮/数学/图片/导出，需按需引用对应能力包并一行注册（UseHighlighting / UseMath / UseImages / UseMermaid）。

## 12.1.2.13 (2026-10-01)

- 🐛[修复]-修复标题等容器内含行内代码（自定义字号、基线对齐、背景）的文本被选中后渲染移位的问题：Avalonia 12.1 `SelectableTextBlock` 构造选区前景色覆盖时会用控件级 FontSize 与默认 BaselineAlignment 重建 Run 属性，导致选中的行内代码按容器字号放大并下沉、背景块丢失。新增 `MarkdownSelectableTextBlock` 子类只替换选中部分前景色，完整保留 Run 原有属性，主控件与 Lite 控件同步修复。关联：dotnet9/Vex#3。
- 🧪[测试]-新增选区属性覆盖构建的回归测试，覆盖混合字号、部分选中和空选区场景。

## 12.1.2.11 (2026-09-23)

- 🐛[修复]-将 Avalonia 12.1.3 使用的 SkiaSharp、HarfBuzzSharp、Svg.Skia 和 Svg.Controls.Skia.Avalonia 依赖对齐到兼容版本，并替换 SkiaSharp 4 专用绘制 API，避免运行时混用不兼容的 Skia 原生库。

## 12.1.2.2 (2026-09-20)

- 🚀[新增]-NuGet 包统一支持 `net8.0;net10.0;net11.0`，并发布新版本。

## 12.1.1.3 (2026-08-13)

- 🔨[优化]-升级 `Svg.Skia` 至 5.2.1、`Svg.Controls.Skia.Avalonia` 至 12.0.0.15。
- 🔨[优化]-统一锁定 SkiaSharp 4.151.1 与 HarfBuzzSharp 14.2.1.2 及各平台原生资产，避免 SVG、Avalonia 与导出链路混用不同版本。

## 12.0.4.3 (2026-06-08)

- 🔨[优化]-补齐根目录 logo.svg、logo.png、logo.ico 三件套，子工程通过 MSBuild Link 引用根 logo，避免维护多份图标副本。
- 🔨[优化]-统一目标框架：NuGet 包项目支持 `net8.0;net10.0`，Demo、App、测试与内部应用项目升级到 `net11.0` / `net11.0-windows`。
- 🔨[优化]-保留运行时帮助、Markdown 示例、内置备忘录和业务设计文档，仅收敛仓库级重复文档入口。

## 12.0.4.2 (2026-06-08)

- 统一版本号维护入口，只在仓库根目录 `Directory.Build.props` 中定义 `<Version>`。
- 清理英文/双语文档入口，后续仅维护简体中文文档。
- 完善 NuGet 发布配置，补充 Source Link、符号包和标签格式规范。


## 12.0.3.17 - 2026-05-27

- 🔨[优化]-`MarkdownHtmlConverter` 粘贴转换会先识别内容是否为网页富文本，普通 diff、XML 和代码文本不再被当作 HTML 段落折叠空白。
- 🔨[优化]-当剪贴板把代码包成 `div`/`span` 等布局 HTML 时，会保留原始换行和缩进，避免 XML、git diff 等内容粘贴后丢失格式。
- 🧪[测试]-补充纯 diff、纯 XML、布局 HTML 包裹代码和布局 HTML 包裹 diff 的粘贴转换回归测试。

## 12.0.3.15 - 2026-05-27

- 🐛[修复]-修复 `MarkdownViewer` 同一行内混合加粗、斜体、删除线时强调样式没有稳定应用到对应文本片段的问题，避免整行样式只跟随首个字符或行内强调不生效。
- 🐛[修复]-默认正文字体族补充中文字体回退，修复中文普通文本后面的行内加粗在 Avalonia 字体回退下显示成普通字重的问题。
- 🐛[修复]-同步修复 `CodeWF.Markdown.Lite` 的强调样式下沉逻辑，保持主控件与 Lite 控件行内样式行为一致。
- 🧪[测试]-新增行内样式与默认中文字体回退测试，覆盖同一段落中普通文本与加粗、斜体、删除线混排场景。

## 12.0.3.14 - 2026-05-27

- 😄[新增]-新增 `MarkdownHtmlConverter` 和 `MarkdownHtmlClipboard.Html2Markdown(string htmlContent)`，宿主编辑器可在粘贴前把网页复制得到的 HTML 转为 Markdown。
- 🔨[优化]-内置 HTML 转 Markdown 覆盖标题、段落、链接、图片、列表、引用、代码块和表格等常见文章结构，不额外引入第三方包。
- 🧪[测试]-补充普通 HTML、表格/图片、CF_HTML 片段标记和 Windows 剪贴板 UTF-8 偏移的转换回归测试。

## 12.0.3.13 - 2026-05-25

- 🔨[优化]-PDF 导出改为写入可选择的 PDF 文本，不再把整页 Markdown 压平成位图切片写入 PDF。
- 🔨[优化]-PDF 导出会包含用于复制粘贴的 Unicode 文本映射，保留页眉页脚，并把 Markdown 图片作为 PDF 图片内容嵌入。
- 🧪[测试]-补充 PDF 回归测试，验证纯文本 PDF 包含字体和 ToUnicode 数据，并且不包含整页图片对象。
- 🧪[测试]-包版本提升到 12.0.3.13，并验证 Vex 可消费本地打包的 `CodeWF.Markdown` 与 `CodeWF.Markdown.Themes` 包。

## 12.0.3.12 - 2026-05-25

- 😄[新增]-新增 `CopyKind`、`MarkdownSocialCopyRenderer`、`MarkdownSocialCopyProfiles` 和 `MarkdownSocialCopyProfile`，微信公众号、知乎、稀土掘金以及后续发布目标可复用同一套 Markdown 到 inline HTML 的渲染链路。
- 😄[新增]-新增 `MarkdownHtmlClipboardExtensions`，Avalonia 应用可直接调用 `TrySetMarkdownHtmlAsync(markdown, themeName, targetName, typographySize)`，宿主侧只需要传当前 Markdown、排版主题和目标平台。
- 🔨[优化]-自媒体复制 HTML 现在会嵌入本地图片，内置主题名通过 `MarkdownExportStyle.Resolve` 解析，CF_HTML 继续复用公共剪贴板写入能力，目标尾注和工具名文案改为读取多语言资源。
- 🧪[测试]-包版本提升到 12.0.3.12，并在 Vex 使用本地 NuGet 包消费前验证 `CodeWF.Markdown.Tests` 41 项通过。

## 12.0.3.11 - 2026-05-25

- 😄[新增]-新增 `ExportKind` 以及更高层的 `MarkdownDocumentExporter.Export`、`ExportMarkdown`、`ExportFile` API，宿主应用可按导出类型一站式导出 Markdown 字符串或 Markdown 文件。
- 😄[新增]-新增 `MarkdownTypographyThemeRegistry`，支持应用注册自定义排版主题；内置主题继续保持字符串常量，方便扩展第三方主题 Key。
- 🔨[优化]-新增 `MarkdownExportStyle.FromResources` 与 `MarkdownThemes.CreateExportStyle`，导出和自定义自媒体复制样式可复用同一套排版资源字典，应用自定义主题也能参与样式解析。

## 12.0.3.10 - 2026-05-25

- 😄[新增]-新增 `MarkdownDocumentExporter`、`MarkdownExportDocument` 和 `MarkdownExportStyle`，为宿主应用提供可复用的 PNG、图像型 PDF、Word `.docx` 一站式导出 API。
- 🔨[优化]-将图像型 PDF/PNG 与 Word 导出实现下沉到 `CodeWF.Markdown`；Word 导出会把图片嵌入 `word/media`，并改用 SkiaSharp 读取图片尺寸，不再要求 Avalonia UI 平台初始化。
- 🧪[测试]-补充 Word 导出测试，验证 Markdown 中的 `data:image` 图片会嵌入生成的 `.docx` 包。

## 12.0.3.9 - 2026-05-25

- 😄[新增]-新增 `MarkdownHtmlClipboard` 富 HTML 剪贴板公共能力，统一生成 CF_HTML 字节偏移和 Windows 原生 `HTML Format` 字节数据，便于微信公众号、知乎、稀土掘金等编辑器按富文本粘贴。
- 🧪[测试]-补充片段标记规范化和 UTF-8 CF_HTML 偏移测试。

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
- 🔨[优化]-删除各工程目录下的 `UpdateLog.md`，后续统一维护根目录更新日志。

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
## 2026-06-08 仓库规范整理

- 统一文档维护入口：每个仓库只保留根目录 `README.md` 和根目录 `UpdateLog.md`，清理重复日志、英文文档和语言切换入口。
- 统一版本维护入口：包版本只在仓库根目录 `Directory.Build.props` 的 `<Version>` 节点维护，移除散落的程序集版本配置。
- 不再维护 `global.json`，SDK 选择交给本机或 CI 环境；NuGet 包和应用的目标框架在项目文件中明确声明。
- 统一 NuGet 包文档入口：包 README 统一引用仓库根 `README.md`，更新日志统一引用仓库根 `UpdateLog.md`。


