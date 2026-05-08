# 更新日志

V12.0.2.3（2026-05-06）

- 😄[新增]-新增 `CodeWF.Markdown.Lite.Sample` 示例应用，引用 `CodeWF.Markdown.Lite.Themes`
- 🔤[优化]-示例界面固定为简体中文，移除 `Lang.Avalonia.Resx`、资源文件和语言切换下拉框
- 🔤[优化]-Win-x64 发布保持 `net11.0-windows` + AOT，并补充 `YY-Thunks`、`VC-LTL`、`WindowsSupportedOSPlatformVersion=6.1`、`TargetPlatformMinVersion=6.1` 支持 Win7 运行
- 🔤[优化]-应用清单补充 Windows 7 / 8 / 8.1 / 10+ 的 `supportedOS` 声明
- 🔤[优化]-示例接入 `publish_Markdown.bat`，可与原 Markdown 示例一起发布
