# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET/Avalonia Markdown viewer solution. Source projects live under `src/`:

- `CodeWF.Markdown` and `CodeWF.Markdown.Themes`: full viewer package, localization, templates, and typography themes.
- `CodeWF.Markdown.Lite` and `CodeWF.Markdown.Lite.Themes`: reduced-dependency viewer and matching themes.
- `CodeWF.Markdown.Sample` and `CodeWF.Markdown.Lite.Sample`: runnable Avalonia sample apps.

Assets are in each sample app's `Assets/` folder. Sample Markdown files are in `MarkdownSamples/`. Shared build and package settings are centralized in `Directory.Build.props`, `Directory.Build.targets`, and `Directory.Packages.props`.

## Build, Test, and Development Commands

- `dotnet restore CodeWF.Markdown.slnx`: restore all projects using the SDK pinned in `global.json`.
- `dotnet build CodeWF.Markdown.slnx --no-restore`: build the full solution.
- `dotnet run --project src/CodeWF.Markdown.Sample/CodeWF.Markdown.Sample.csproj`: run the full sample app.
- `dotnet run --project src/CodeWF.Markdown.Lite.Sample/CodeWF.Markdown.Lite.Sample.csproj`: run the Lite sample app.
- `.\pack.bat`: build Release and create NuGet packages in `artifacts/packages/`.
- `.\publish_Markdown.bat`: publish the sample apps using the checked-in publish profiles.

There is currently no dedicated test project. Treat a clean solution build plus manual verification in both sample apps as the baseline check.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow the existing file style: tabs for indentation in C# and project files, file-scoped namespaces, PascalCase public types and members, camelCase locals, and `_camelCase` private fields. Keep Avalonia resource keys and control part names descriptive, for example `PART_DocumentHost`.

For XAML theme files, preserve the existing organization under `Themes/` and keep full and Lite theme changes synchronized when behavior is shared.

## Testing Guidelines

When adding automated tests, place them in a new `tests/` project or a clearly named sibling such as `CodeWF.Markdown.Tests`. Name test classes after the unit under test and use behavior-oriented test names. Until tests exist, verify Markdown rendering, theme switching, image preview, code highlighting, localization, and incremental rendering scenarios through the sample apps.

## Commit & Pull Request Guidelines

The current Git history is minimal and does not define a strict convention. Use short, imperative commit subjects, such as `Add Lite sample theme resources` or `Fix incremental render update`. Pull requests should include a concise description, affected package or sample app, verification steps, linked issues when available, and screenshots or short recordings for visible UI/theme changes.

## Security & Configuration Tips

Do not commit generated packages, publish output, or local IDE state. Keep package versions centralized in `Directory.Packages.props`; avoid adding per-project versions unless the central package flow requires it.
