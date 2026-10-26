param(
    [string]$Source = 'E:\Aseprite\MyDino-Running.aseprite',
    [string]$Aseprite = 'C:\Program Files\Aseprite\Aseprite.exe'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$output = Join-Path $projectRoot 'Tools\SourceArt\MyDinoAnimation'
$manifest = Join-Path $output 'animation.json'
$before = if (Test-Path -LiteralPath $manifest) { (Get-Item -LiteralPath $manifest).LastWriteTimeUtc } else { [datetime]::MinValue }
$arguments = '--batch --script-param "root={0}" --script-param "source={1}" --script "{2}"' -f $projectRoot, $sourcePath, (Join-Path $PSScriptRoot 'export_mydino_animation.lua')
$process = Start-Process -FilePath $Aseprite -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $output 'reexport.log') -RedirectStandardError (Join-Path $output 'reexport-errors.log')
if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $manifest) -or (Get-Item -LiteralPath $manifest).LastWriteTimeUtc -le $before) {
    throw 'Aseprite export failed; inspect Tools/SourceArt/MyDinoAnimation/reexport.log.'
}
$data = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
Write-Output "Exported $($data.frames.Count) frames. In Unity choose Dino Slug > Use MyDino Artwork."
