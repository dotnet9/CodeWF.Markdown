# 更新日志

V12.0.2.2（2026-05-05）

- 😄[新增]-接入 `Lang.Avalonia.Resx` 多语言框架，在 `App.axaml.cs` 中注册 `ResxLangPlugin`，默认文化 `zh-CN`
- 😄[新增]-新增 `Resources.resx` 及 `Resources.zh-CN.resx` / `Resources.zh-Hant.resx` / `Resources.ja-JP.resx` 四语言资源文件，覆盖工具栏标签、增量演示按钮等界面文案
- 😄[新增]-新增 `Language.tt` T4 模板及生成的 `Language.cs`，提供强类型资源键（`SampleL` 静态类）
- 😄[新增]-新增 `SampleLanguage.cs` 语言选择模型，包含 `DisplayName` / `DisplayTag` 属性
- 😄[新增]-工具栏新增语言切换下拉框，支持简体中文、繁体中文、日语、英语四种语言即时切换
- 🔤[优化]-增量演示开关按钮文本绑定为 `I18nManager.Instance.GetResource`，切换语言时自动跟随

V12.0.2.1（2026-05-05）

- 😄[新增]-新增根目录 `publish_Markdown.bat`，便于单独发布 Markdown 示例应用
- 😄[新增]-新增 `VC-LTL` 引用，补齐 Windows 发布场景所需运行时依赖
- 🔤[优化]-示例文档改为展示当前 `Markdown` 绑定入口，不再保留旧主题属性写法对照

V12.0.2（2026-05-05）

- 😄[新增]-新增当前工程独立更新日志文件，后续 `CodeWF.Markdown.Sample` 的修改历史改为在工程目录内持续记录
- 😄[新增]-新增 Markdown 示例应用，用于展示基础元素、排版主题、代码块与表格等渲染效果
- 😄[新增]-内置示例文档、示例主题和基础预览界面，方便验证 `CodeWF.Markdown` 与 `CodeWF.Markdown.Themes` 的集成效果
- 😄[新增]-新增 Markdown 示例发布配置、裁剪根和 `net11.0-windows` 目标，并接入根目录 `publish_all.bat` 一键发布脚本
- 🔤[优化]-增量演示改为轮流模拟中文段落替换、正文中部插入和文档尾部追加，避免简单字母串造成不真实的中文 Markdown 测试
- 🔤[优化]-预览区移除对 `MarkdownViewer.BasePath` 的绑定，由 Markdown 文本内容直接驱动渲染
