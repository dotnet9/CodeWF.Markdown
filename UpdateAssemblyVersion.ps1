[CmdletBinding()]
param(
	[string]$AssemblyInfoFile,
	[string]$Configuration,
	[string]$Platform,
	[string]$Version
)

$versionFile = Join-Path $PSScriptRoot "Directory.Build.props"
if (-not (Test-Path -LiteralPath $versionFile)) {
	throw "Version file was not found: $versionFile"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
	$props = Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8
	$versionMatch = [regex]::Match($props, '<Version>(?<version>[^<]+)</Version>')
	if (-not $versionMatch.Success) {
		throw "The root Directory.Build.props does not contain a <Version> value."
	}

	$Version = $versionMatch.Groups['version'].Value.Trim()
}

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
	throw "Version '$Version' must contain exactly four numeric components."
}

Write-Output "Version: $Version"
if (-not [string]::IsNullOrWhiteSpace($Configuration)) {
	Write-Output "Configuration: $Configuration"
}
if (-not [string]::IsNullOrWhiteSpace($Platform)) {
	Write-Output "Platform: $Platform"
}

if ([string]::IsNullOrWhiteSpace($AssemblyInfoFile)) {
	Write-Output "No legacy AssemblyInfo file supplied; MSBuild version properties remain authoritative."
	exit 0
}

if (-not (Test-Path -LiteralPath $AssemblyInfoFile)) {
	throw "AssemblyInfo file was not found: $AssemblyInfoFile"
}

$content = Get-Content -LiteralPath $AssemblyInfoFile -Raw -Encoding UTF8
$updatedContent = $content
$assemblyVersionReplacement = '[assembly: AssemblyVersion("{0}")]' -f $Version
$assemblyFileVersionReplacement = '[assembly: AssemblyFileVersion("{0}")]' -f $Version
$updatedContent = $updatedContent -replace '(?m)^\[assembly: AssemblyVersion\("[^"]*"\)\]$', $assemblyVersionReplacement
$updatedContent = $updatedContent -replace '(?m)^\[assembly: AssemblyFileVersion\("[^"]*"\)\]$', $assemblyFileVersionReplacement

if ($updatedContent -eq $content) {
	Write-Output "No legacy AssemblyVersion or AssemblyFileVersion attributes found; no file changes were required."
	exit 0
}

[System.IO.File]::WriteAllText(
	$AssemblyInfoFile,
	$updatedContent,
	[System.Text.UTF8Encoding]::new($false))
Write-Output "Updated legacy assembly version attributes in $AssemblyInfoFile"
