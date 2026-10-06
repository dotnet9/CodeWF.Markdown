@echo off
setlocal enabledelayedexpansion

set "project_paths=src\CodeWF.Markdown.Sample src\CodeWF.Markdown.Export src\CodeWF.Markdown.Highlighting src\CodeWF.Markdown.Images src\CodeWF.Markdown.Math src\CodeWF.Markdown.Mermaid"
set "platforms=win-x64 linux-x64"

call "%~dp0publishbase.bat" "%project_paths%" "%platforms%"
