[CmdletBinding()]
param(
    [string]$EngineEditorDirectory = '',
    [string]$DotNet = '',
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$storyRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($DotNet)) {
    $userDotNet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    $DotNet = if (Test-Path -LiteralPath $userDotNet) { $userDotNet } else { (Get-Command dotnet).Source }
}
if ([string]::IsNullOrWhiteSpace($EngineEditorDirectory)) {
    $EngineEditorDirectory = Join-Path $storyRoot '..\..\InnoEngine.VisualNovel\artifacts\EngineEditorSnapshot'
}
$EngineEditorDirectory = (Resolve-Path -LiteralPath $EngineEditorDirectory).Path
$outputRoot = Join-Path $storyRoot 'artifacts\EditorHost'
& $DotNet build (Join-Path $PSScriptRoot 'EditorHost\Story.EditorHost.csproj') --artifacts-path $outputRoot "-p:EngineEditorDirectory=$EngineEditorDirectory"
if ($LASTEXITCODE -ne 0) { throw 'The selected Editor host could not be composed.' }
$runtime = Join-Path $outputRoot 'bin\Story.EditorHost\debug'
Get-ChildItem -LiteralPath $EngineEditorDirectory -File | Where-Object {
    $_.Extension -eq '.dll' -or $_.Name -like '*.deps.json'
} | ForEach-Object {
    $destination = Join-Path $runtime $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
}
foreach ($folder in @('native','runtimes','BuiltIn','bgcs')) {
    $source = Join-Path $EngineEditorDirectory $folder
    if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination $runtime -Recurse -Force }
}
if ($PrepareOnly) { Write-Output $runtime; return }
$env:DOTNET_ROOT = Split-Path -Parent $DotNet
$env:PATH = (Split-Path -Parent $DotNet) + [IO.Path]::PathSeparator + $env:PATH
& $DotNet (Join-Path $runtime 'Story.EditorHost.dll') $storyRoot
if ($LASTEXITCODE -ne 0) { throw ('Editor exited with code '+$LASTEXITCODE) }
