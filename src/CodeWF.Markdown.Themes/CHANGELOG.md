# 更新日志

V12.0.2.6（2026-05-12）

- 😄[新增]-`MarkdownThemes` 新增 `TypographySize`，默认 `Normal`，可设置为 `Small` 启用紧凑型排版
- 😄[新增]-新增 `CompactTypographyResources`，对任意排版主题叠加小字号、低行高和更紧凑的块间距
- 🔨[优化]-排版资源改为统一包装 `Shared/Common`、主题资源和尺寸覆盖资源，避免在多个作用域复用同一个资源字典实例
- 🔴[修复]-修复单个 `MarkdownViewer` 只覆盖尺寸并跟随外层主题时，`ResourceDictionary` 已有父级导致的运行期异常
- 🔨[优化]-切换全局排版资源时同步刷新逻辑树与可视树中的 `MarkdownViewer`，覆盖未选中 Tab 中的预览控件

V12.0.2.1（2026-05-05）

- 🔨[优化]-补充 `MarkdownThemes` 和 `MarkdownTypographyThemes` 公开 API 注释，明确样式入口、排版主题 Key 与运行时覆盖方式

V12.0.2（2026-05-05）

- 😄[新增]-新增当前工程独立更新日志文件，后续 `CodeWF.Markdown.Themes` 的修改历史改为在工程目录内持续记录
- 😄[新增]-新增 Markdown 排版主题资源库，提供多套适配 Avalonia 明暗主题的文档阅读样式
- 🔨[优化]-将主题资源与核心 Markdown 渲染库拆分，便于 NuGet 分发和示例工程按需引用
- 🔨[优化]-为 `MarkdownViewer` 主题新增 `DocumentBottomPadding` 和模板底部占位，改善文档尾部滚动留白
