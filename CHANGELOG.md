# Changelog

## 12.0.2.6 - 2026-05-12

- 😄[Added]-Added `MarkdownViewer.TypographyTheme` and `MarkdownViewer.TypographySize` for per-viewer typography overrides.
- 😄[Added]-Added `MarkdownThemes.TypographySize` and compact typography resources for smaller font sizes, line heights, and spacing.
- 🔨[优化]-Refined the sample app into a tabbed editor preview and multi-viewer typography demo with global and per-viewer settings.
- 🔴[修复]-Fixed inherited typography resource application to avoid reusing a `ResourceDictionary` that already has a parent.
- 🔨[优化]-Updated Markdown package version and dependency baselines for the new typography release.

## 12.0.2.5 - 2026-05-09

- 🔨[优化]-Replaced `Lang.Avalonia.Resx` localization with `Lang.Avalonia.Json` resources for the Markdown package and sample app.
- 🔨[优化]-Copied JSON language resources to the `I18n` output folder and package content files so AOT builds can switch languages normally.
- 🔨[优化]-Updated generated localization key templates and sample startup registration to use JSON resources.

## 12.0.2.4 - 2026-05-08

- 🔨[优化]-Split CodeWF.Markdown packages and samples into an independent repository.
- 🔨[优化]-Kept the Markdown, theme, sample, and test projects from the original CodeWF.AvaloniaControls repository.
- 🔨[优化]-Removed the obsolete reduced-dependency package, matching themes, and sample app.
- 🔨[优化]-Updated the solution, packaging scripts, publish scripts, README files, and repository guidelines to reference only the full Markdown package line.
- 😄[新增]-Added a Simplified Chinese changelog for repository-level release notes.
