param([Parameter(Mandatory=$true)][string]$InputPath)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$recording=(Resolve-Path -LiteralPath $InputPath).Path
$sourceFolder=Join-Path $projectRoot 'Tools/SourceArt/DinoVoice'
$audioFolder=Join-Path $projectRoot 'Assets/Game/Audio/Player'
New-Item -ItemType Directory -Path $sourceFolder,$audioFolder -Force | Out-Null
$original=Join-Path $sourceFolder ('Dino-Original'+[IO.Path]::GetExtension($recording))
if($recording -ne $original) { Copy-Item -LiteralPath $recording -Destination $original }
$output=Join-Path $audioFolder 'Dino-Selection.wav'
# Broad arcade-announcer processing: preserve the user's performance and timing.
# This does not synthesize or clone another speaker.
$treatment='silenceremove=start_periods=1:start_threshold=-42dB:start_duration=0.02:stop_periods=1:stop_threshold=-42dB:stop_duration=0.5,aresample=48000,asetrate=42720,aresample=48000,atempo=1.1235955,highpass=f=100,lowpass=f=6200,acompressor=threshold=0.12:ratio=4:attack=3:release=85:makeup=2,asoftclip=type=tanh:threshold=0.8,aecho=0.7:0.35:55:0.18,loudnorm=I=-14:TP=-1.5:LRA=7'
& ffmpeg -hide_banner -loglevel error -y -i $original -af $treatment -ar 48000 -ac 1 -c:a pcm_s16le $output
if($LASTEXITCODE -ne 0) { throw 'Could not process the Dino voice recording.' }
Write-Output "Processed Dino selection voice: $output"
Write-Output 'Import with Dino Slug > Import Dino Selection Voice, then rebuild Windows.'
