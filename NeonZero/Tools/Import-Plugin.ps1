[CmdletBinding()]
param([Parameter(Mandatory)][string]$Package)
$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $Package).Path
if ([IO.Path]::GetExtension($source) -ne '.iplugin') { throw 'Select a native source plugin package.' }
$storyRoot = Split-Path -Parent $PSScriptRoot
Copy-Item -LiteralPath $source -Destination (Join-Path $storyRoot 'Plugins\InnoEngine.VisualNovel.iplugin') -Force
Write-Host 'Installed VisualNovel source package. Restart the Editor before using the new generation.'
