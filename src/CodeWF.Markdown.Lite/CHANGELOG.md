# 更新日志

V12.0.2.3（2026-05-06）

- 😄[新增]-新增 `CodeWF.Markdown.Lite` 轻量 Markdown 控件包，命名空间独立为 `CodeWF.Markdown.Lite`
- 🔤[优化]-Lite 核心仅保留 `Avalonia` 与 `Markdig` 依赖，移除 `Lang.Avalonia.Resx`、`Sylinko.CSharpMath.Avalonia`、`Svg.Controls.Skia.Avalonia`、`Svg.Skia`、`TextMateSharp`、`TextMateSharp.Grammars`
- 🔤[优化]-移除 SVG 专用渲染路径，普通位图仍支持加载、预览和另存为
- 🔤[优化]-数学公式改为源码文本回退显示，避免引入数学渲染依赖
- 🔤[优化]-代码块保留语言标识、行号、复制按钮和水平滚动，语法高亮改为轻量纯文本渲染
- 🔤[优化]-控件内文案固定为简体中文，移除多语言资源和运行时语言切换依赖
