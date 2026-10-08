# CodeWF.Markdown Demo 原型与实现维护

`prototype.html` 是 Demo 的交互原型，可直接在浏览器中打开。尺寸和颜色以当前 HTML/CSS 为准；Vex 使用自己的 `design/`，两个应用的规格分别维护。

## 布局规格

基准窗口为 1400×900，窄窗回归尺寸为 980×640。

| 区域 | 原型规格 | 实现位置 |
| --- | --- | --- |
| 标题栏 | 44px、1px 下边框、左留白 12px；logo 24px、品牌 13px、gap 10px | `src/CodeWF.Markdown.Sample/Views/MainWindow.axaml` |
| 文档名/窗控 | 文档名左 margin/padding 各 14px、左分隔线；窗控宽 44px、组右留白 10px | `MainWindow.axaml`、`Themes/ShellStyles.axaml` |
| 侧栏 | 216px、右边框 1px；标题横向留白 18px | `MainWindow.axaml`、`ShellStyles.axaml` |
| 文件卡片 | 列表 padding 8px、卡片 padding 8px 10px、圆角 8px、底 margin 2px | `ShellStyles.axaml` |
| 选中态 | 图标 30px、圆角 7px、accent 描边；左条 3px、上下各 9px；背景 bg-active | `MainWindow.axaml`、`ShellStyles.axaml` |
| 工具栏 | 44px、左右 padding 12px、gap 10px；视图按钮 28px、横向 padding 14px | `MainWindow.axaml`、`ShellStyles.axaml` |
| 面板头 | 32px；accent 圆点 6px、标题 11px | `Views/MarkdownEditorPreviewView.axaml`、`ShellStyles.axaml` |
| 源码 | 字号 13px、行高 22.1px、padding 14px 16px、不折行、允许横向滚动 | `MarkdownEditorPreviewView.axaml` 和代码后置 |
| 默认预览 | 最大宽度 820px、padding 28px 32px 48px；正文 14px、行高 25.9px | `MarkdownEditorPreviewView.axaml` |
| 默认标题 | H1/H2/H3 为 28/21/17px；上下 margin 为 8/20、30/14、22/10px | `MarkdownEditorPreviewView.axaml` |
| 默认引用 | accent 左线、accent-soft 背景、padding 10px 16px、上下 margin 14px、右圆角 8px | `MarkdownEditorPreviewView.axaml` |
| 状态栏 | 全宽 30px、1px 上边框、左右留白 16px；字数、解析耗时、编码和版本使用真实数据 | `MainWindow.axaml`、`ShellStyles.axaml` |

主题令牌集中在 `src/CodeWF.Markdown.Sample/Themes/DesignTokens.axaml`，对应原型的 `--bg-*`、`--text-*`、`--border-*`、`--accent-*`、`--code-bg`。修改颜色时同时核对 Light/Dark，使用语义资源键，避免通用控件样式覆盖选中态。

## 编辑与能力入口

- 工具栏保留「编辑 / 分栏 / 实时 / 预览」四模式；窄窗隐藏辅助标签，模式和下拉入口仍可用。
- 源码使用 `MarkdownEditorView`；`EditorLineHeight` 指定设备无关像素行高，默认 NaN 沿用 AvaloniaEdit 行距。控件根据实测字体高度设置 `Options.LineHeightFactor`，不放大字号。
- 实时模式使用 `MarkdownLiveEditorView`，渲染块复用完整 `MarkdownViewer`。点击进入块编辑，输入立即回写源码，Esc 返回渲染态；浅层多段引用共用容器，段落可分别编辑。
- 完整包注册代码高亮、数学公式、Mermaid、图片和导出能力；性能演示支持增量压力、中部插入、尾部追加、停止，「对比模式」保留双预览。
- `docs/MarkdownSamples/` 是实际样例；`Regression/微信公众号教程.md` 和 `images/codewf.png` 保留作回归内容及本地图片依赖。

## 验证方法

```powershell
dotnet build CodeWF.Markdown.slnx -c Release
dotnet test tests/CodeWF.Markdown.Tests/CodeWF.Markdown.Tests.csproj -c Release
```

跨仓库离屏验证器位于 Vex 的 [scripts/ui-verification](https://github.com/dotnet9/Vex/tree/main/scripts/ui-verification)，运行方法见该目录 README。它加载实际 App/XAML/ViewModel，通过 Avalonia.Headless + Skia 绘制并发送输入，覆盖明暗主题、1400/980 宽度和四模式，以及源码行高、点击定位、键盘移动、实时标题/共享引用编辑和源码同步。截图、日志与构建输出可随时重新生成，属于临时产物。

最近回归：182 项 Release 测试通过，Demo 四模式离屏验证通过。Vex 正式版直接从 nuget.org 恢复四包；修改库代码后的发布前联调需升开发版本并打包到本地源，覆盖同版本 nupkg 不会刷新 NuGet 缓存。

## 验证边界

原型浏览器截图未在本环境获得；CSS/结构、实际布局和离屏截图共同用于核对，不能据此宣称全页逐像素一致。原生滚动条与字体度量仍可能与网页不同；Windows 窗控、拖动/最大化、多 DPI、中文 IME 连续组字需实机验证。

嵌套/混合引用仍按编辑块显示；修改列表/引用成员会规范化该组 Markdown，未改动组严格保留原文。实时控件提供块编辑，尚非完整 Typora 行内编辑。复杂嵌套续行、远程图片失败重试和全部导出格式未穷尽实测。
