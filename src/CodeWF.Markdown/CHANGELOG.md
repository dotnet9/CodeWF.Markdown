# 更新日志

V12.0.2.3（2026-05-05）

- 🔴[修复]-修复 `BindTheme` 绑定订阅泄漏：`IDisposable` 令牌存储在 `RenderedBlock` 中，块重建时逐一 Dispose，杜绝 observer 列表无限增长
- 🔴[修复]-增量渲染 `ReplaceRenderedBlocks` 和全量重建 `RenderDocumentFull` 新增 `Cleanup()` 入口，旧控件及其绑定在移除前被完整释放
- 🟡[修复]-`ContextMenu` 缓存 `MenuItem` 实例，语言切换时只更新 `Header` 不重复订阅 `Click`，消除菜单反复重建导致的事件泄漏
- 🟡[修复]-代码块复制按钮改用 `Button.Tag` 存储代码文本，lambda 不再闭包捕获局部变量，避免闭包对象泄漏
- 🟡[修复]-`CodeHighlighter` 静态缓存新增 `MaxCacheSize=32` 上限和 `CacheOrder` 淘汰队列，防止未知语言无限堆积
- 🟡[修复]-`MarkdownImage` 加载接入 `CancellationTokenSource`，每次 `QueueLoad` 自动取消前一次；加载新 Bitmap 前 Dispose 旧 Bitmap；清除旧字节数组和文件名引用
- 🟡[修复]-`CodeHighlighter` 复制菜单硬编码中文改为走 `I18nManager.Instance.GetResource` 国际化资源
- 🟡[优化]-`MarkdownImage.LoadBytesAsync` 传递 CancellationToken 给 `HttpClient.GetAsync`，快速滚动时旧 HTTP 请求可被及时取消

V12.0.2.2（2026-05-05）

- 😄[新增]-接入 `Lang.Avalonia.Resx` 插件，通过 `Resources.Designer.cs` 暴露 ResourceManager 供 `ResxLangPlugin` 反射发现，实现控件内文案（图片预览、复制菜单等）的多语言切换
- 😄[新增]-新增 `Language.tt` T4 模板及生成的 `Language.cs`，提供强类型资源键（`MarkdownL` 静态类），支持简体中文、繁体中文、日语、英语四种语言
- 😄[新增]-新增图片预览窗口 `CultureChanged` 事件监听，切换语言时即时刷新按钮文本和缩放状态栏
- 🔤[优化]-超链接走 `PointerPressed` 拦截 + `PointerReleased` 调用 `UrlHelper.Open`，阻止 `SelectableTextBlock` 文本选择捕获指针，支持左键点击默认浏览器打开链接
- 🔤[优化]-超链接 `TextBlock` 移除 `LineHeight` 和 `VerticalAlignment.Center` 绑定，避免 `InlineUIContainer` 布局偏移导致链接文本与段落文本基线不对齐
- 🔤[优化]-图片预览窗口标题优先展示图片描述，描述为空时回退到图片路径
- 🔤[优化]-注音符号（Ruby）区域改用 `HorizontalAlignment.Left` + `VerticalAlignment.Bottom`，配合 Grid 按字符逐列布局，修复多字符注音堆叠偏移

V12.0.2.1（2026-05-05）

- 🔤[优化]-移除 `MarkdownViewer.Value` 兼容入口，`MarkdownViewer` 只通过 `Markdown` 属性接收原文
- 🔤[优化]-补充 `MarkdownViewer`、代码块工具栏事件参数、样式 Key 和图片路径相关公开 API 注释
- 🔤[优化]-补充 Markdown 控件使用文档，覆盖样式注册、XAML 绑定、主题切换和图片路径处理

V12.0.2（2026-05-05）

- 😄[新增]-新增当前工程独立更新日志文件，后续 `CodeWF.Markdown` 的修改历史改为在工程目录内持续记录
- 😄[新增]-新增基于 Avalonia 12 的 Markdown 渲染控件，支持常规 Markdown 元素、代码块渲染、图片预览与增量刷新
- 😄[新增]-接入 Markdig、TextMateSharp 与内置样式 Key，作为 `CodeWF.Markdown.Themes` 和 Markdown 示例工程的基础渲染库
- 🔤[优化]-移除 `MarkdownViewer.BasePath`，`MarkdownViewer` 不再对外暴露文件路径配置，保持以 Markdown 文本输入为主的控件接口
- 🔤[优化]-新增 `DocumentBottomPadding` 和底部占位样式 Key，用于改善滚动到底时的文档尾部留白
