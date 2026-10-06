[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("linux-x64", "linux-arm64")]
    [string] $RuntimeIdentifier,

    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $SourceDirectory = "",
    [string] $OutputDirectory = "",
    [switch] $Force
)

$ErrorActionPreference = "Stop"
$scriptRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $scriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($SourceDirectory)) {
    $SourceDirectory = Join-Path $repositoryRoot "artifacts/publish/$RuntimeIdentifier/CodeWF.Markdown.Sample"
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot "artifacts/release"
}

$sourcePath = (Resolve-Path -LiteralPath $SourceDirectory).Path
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$cleanVersion = $Version.Trim().TrimStart('v', 'V').Split('+')[0]
if ($cleanVersion -notmatch '^\d+(\.\d+){1,3}$') {
    throw "Version must be numeric (for example 13.0.0): $Version"
}

$architecture = if ($RuntimeIdentifier -eq "linux-arm64") { "arm64" } else { "amd64" }
$packageName = "CodeWFMarkdownDemo-v$cleanVersion-$RuntimeIdentifier.deb"
$packagePath = Join-Path $outputPath $packageName
$stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("CodeWFMarkdownDemo-deb-" + [Guid]::NewGuid().ToString("N"))
$appPath = Join-Path $stageRoot "usr/lib/codewf-markdown-demo"
$binPath = Join-Path $stageRoot "usr/bin"
$desktopPath = Join-Path $stageRoot "usr/share/applications"
$controlPath = Join-Path $stageRoot "DEBIAN"

if ((Test-Path -LiteralPath $packagePath) -and -not $Force) {
    throw "Artifact already exists: $packagePath (use -Force to overwrite)."
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
New-Item -ItemType Directory -Path $appPath, $binPath, $desktopPath, $controlPath -Force | Out-Null

try {
    Get-ChildItem -LiteralPath $sourcePath -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $appPath -Recurse -Force
    }

    $executable = Join-Path $appPath "CodeWF.Markdown.Sample"
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
        throw "Published executable was not produced: $executable"
    }
    & chmod +x $executable

    @(
        "#!/bin/sh",
        'exec /usr/lib/codewf-markdown-demo/CodeWF.Markdown.Sample "$@"'
    ) | Set-Content -LiteralPath (Join-Path $binPath "codewf-markdown-demo") -Encoding ascii
    & chmod +x (Join-Path $binPath "codewf-markdown-demo")

    @(
        "[Desktop Entry]",
        "Type=Application",
        "Name=CodeWF Markdown Demo",
        "Comment=CodeWF.Markdown rendering demo",
        "Exec=codewf-markdown-demo",
        "Terminal=false",
        "Categories=Office;TextEditor;"
    ) | Set-Content -LiteralPath (Join-Path $desktopPath "codewf-markdown-demo.desktop") -Encoding ascii

    @(
        "Package: codewf-markdown-demo",
        "Version: $cleanVersion",
        "Section: text",
        "Priority: optional",
        "Architecture: $architecture",
        "Maintainer: Dotnet9 <1012434131@qq.com>",
        "Depends: libc6, libx11-6, libxrandr2, libxrender1, libxi6, libfontconfig1, libfreetype6, libglib2.0-0",
        "Description: CodeWF.Markdown rendering demo",
        " A Markdown rendering demo desktop application built with Avalonia."
    ) | Set-Content -LiteralPath (Join-Path $controlPath "control") -Encoding ascii

    if (Test-Path -LiteralPath $packagePath) {
        Remove-Item -LiteralPath $packagePath -Force
    }
    & dpkg-deb --build --root-owner-group $stageRoot $packagePath
    if ($LASTEXITCODE -ne 0) {
        throw "dpkg-deb failed with exit code $LASTEXITCODE."
    }

    $sha = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$sha  $packageName" | Set-Content -LiteralPath "$packagePath.sha256" -Encoding ascii
    Write-Host "Package: $packagePath"
}
finally {
    if (Test-Path -LiteralPath $stageRoot) {
        Remove-Item -LiteralPath $stageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
