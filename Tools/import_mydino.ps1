param(
    [string]$Source = "E:\Aseprite\MyDino'.aseprite",
    [string]$Aseprite = 'C:\Program Files\Aseprite\Aseprite.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourceFolder = Join-Path $projectRoot 'Tools\SourceArt\MyDino'
New-Item -ItemType Directory -Path $sourceFolder -Force | Out-Null
$sourceCopy = Join-Path $sourceFolder 'MyDino.aseprite'
if ((Resolve-Path -LiteralPath $Source).Path -ne $sourceCopy) {
    Copy-Item -LiteralPath $Source -Destination $sourceCopy -Force
}
$export = Join-Path $sourceFolder 'MyDino.png'
$metadata = Join-Path $sourceFolder 'MyDino.json'
$arguments = '--batch --list-layers --list-tags "{0}" --format json-array --data "{1}" --sheet-type horizontal --sheet "{2}"' -f $sourceCopy, $metadata, $export
$process = Start-Process -FilePath $Aseprite -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $sourceFolder 'export.log') -RedirectStandardError (Join-Path $sourceFolder 'export-errors.log')
if ($process.ExitCode -ne 0) { throw "Aseprite export failed: $($process.ExitCode)" }
$data = Get-Content -LiteralPath $metadata -Raw | ConvertFrom-Json
if ($data.frames.Count -ne 1 -or $data.frames[0].sourceSize.w -ne 32 -or $data.frames[0].sourceSize.h -ne 32) {
    throw 'Expected the authored single-pose 32x32 MyDino document. Animation sheets require their own mapping.'
}
Copy-Item -LiteralPath $export -Destination (Join-Path $projectRoot 'Assets\Game\Art\my_dino.png') -Force
Write-Output 'Exported the original single pose for source preservation. Run Tools/import_mydino_animation.ps1 to update the animated game frames.'
