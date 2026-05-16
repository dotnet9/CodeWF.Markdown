# Changelog

## 12.0.3.1 - 2026-05-16

- Added an internal `MarkdownMathView` for formula rendering so math foreground color follows the active Markdown theme.
- Updated the sample carousel Markdown in both full and Lite sample apps to use CodeWF screenshots.
- Added sample trimming roots for the Markdown, theme, SVG, and related assemblies used by trimmed publishing.
- Updated `publishbase.bat` to publish into a deterministic runtime/project output folder and fail when the expected executable is missing.
- Bumped the shared package version to `12.0.3.1` and centralized Markdown package versioning through the root build props.
- Updated SVG and runtime helper package baselines, including `Svg.Controls.Skia.Avalonia`, `Svg.Skia`, and `YY-Thunks`.

## 12.0.2.7 - 2026-05-13

- Added `CodeWF.Markdown.Lite`, a basic Markdown viewer package with only direct `Avalonia` and `Markdig` package references.
- Added Lite rendering support for common headings, paragraphs, lists, task lists, quotes, tables, bitmap images, plain-text code blocks, and copy buttons.
- Added `CodeWF.Markdown.Lite.Themes` with the same template and typography resources as `CodeWF.Markdown.Themes`, except for referencing the Lite viewer assembly.
- Added `CodeWF.Markdown.Lite.Sample`, mirroring the editor preview and multi-viewer theme demos in Simplified Chinese without localization switching or AvaloniaEdit.
- Fixed Lite inline text inheritance so heading typography resources are applied correctly.
- Fixed full-viewer incremental rendering so per-block theme bindings are released when Markdown files are switched repeatedly.
- Improved full and Lite image cleanup by cancelling in-flight image loads and disposing replaced or detached bitmaps.
- Updated full image preview windows to own their preview bitmap so Markdown switches can release the viewer image safely.
- Verified the full and Lite sample apps with repeated Markdown file switching and scrolling stress runs; no out-of-memory failure or CPU spike was observed after cleanup.
- Updated solution, packing, and publishing scripts to include the Lite package line.
- Removed project-level `CHANGELOG.md` files; root changelogs are now the single release-note source for all projects.

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
