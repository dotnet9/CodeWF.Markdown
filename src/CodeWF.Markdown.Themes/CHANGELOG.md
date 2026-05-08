# 更新日志

V12.0.2.1（2026-05-05）

- 🔤[优化]-补充 `MarkdownThemes` 和 `MarkdownTypographyThemes` 公开 API 注释，明确样式入口、排版主题 Key 与运行时覆盖方式

V12.0.2（2026-05-05）

- 😄[新增]-新增当前工程独立更新日志文件，后续 `CodeWF.Markdown.Themes` 的修改历史改为在工程目录内持续记录
- 😄[新增]-新增 Markdown 排版主题资源库，提供多套适配 Avalonia 明暗主题的文档阅读样式
- 🔤[优化]-将主题资源与核心 Markdown 渲染库拆分，便于 NuGet 分发和示例工程按需引用
- 🔤[优化]-为 `MarkdownViewer` 主题新增 `DocumentBottomPadding` 和模板底部占位，改善文档尾部滚动留白
