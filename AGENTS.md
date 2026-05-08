# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET/Avalonia Markdown viewer solution. Source projects live under `src/`:

- `CodeWF.Markdown` and `CodeWF.Markdown.Themes`: full viewer package, localization, templates, and typography themes.
- `CodeWF.Markdown.Sample`: runnable Avalonia sample app.
- `CodeWF.Markdown.Shared`: shared rendering model and diff/parser services linked by the viewer and tests.
- `tests/CodeWF.Markdown.Tests`: xUnit tests for rendering support code.

Assets are in each sample app's `Assets/` folder. Sample Markdown files are in `MarkdownSamples/`. Shared build and package settings are centralized in `Directory.Build.props`, `Directory.Build.targets`, and `Directory.Packages.props`.

## Build, Test, and Development Commands

- `dotnet restore CodeWF.Markdown.slnx`: restore all projects using the SDK pinned in `global.json`.
- `dotnet build CodeWF.Markdown.slnx --no-restore`: build the full solution.
- `dotnet test CodeWF.Markdown.slnx --no-restore`: run the test project.
- `dotnet run --project src/CodeWF.Markdown.Sample/CodeWF.Markdown.Sample.csproj`: run the full sample app.
- `.\pack.bat`: build Release and create NuGet packages in `artifacts/packages/`.
- `.\publish_Markdown.bat`: publish the sample app using the checked-in publish profiles.

Treat a clean solution build, passing tests, and manual verification in the sample app as the baseline check.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Follow the existing file style: tabs for indentation in C# and project files, file-scoped namespaces, PascalCase public types and members, camelCase locals, and `_camelCase` private fields. Keep Avalonia resource keys and control part names descriptive, for example `PART_DocumentHost`.

For XAML theme files, preserve the existing organization under `Themes/`.

## Testing Guidelines

Place automated tests under `tests/CodeWF.Markdown.Tests` unless a new test project is clearly warranted. Name test classes after the unit under test and use behavior-oriented test names. Also verify Markdown rendering, theme switching, image preview, code highlighting, localization, and incremental rendering scenarios through the sample app when UI behavior changes.

## Commit & Pull Request Guidelines

The current Git history is minimal and does not define a strict convention. Use short, imperative commit subjects, such as `Add sample theme resources` or `Fix incremental render update`. Pull requests should include a concise description, affected package or sample app, verification steps, linked issues when available, and screenshots or short recordings for visible UI/theme changes.

## Security & Configuration Tips

Do not commit generated packages, publish output, or local IDE state. Keep package versions centralized in `Directory.Packages.props`; avoid adding per-project versions unless the central package flow requires it.
