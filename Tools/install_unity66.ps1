$ErrorActionPreference = 'Stop'
$migrationInstaller = 'E:\Unity\Downloads\UnitySetup64-6000.6.7f1.exe'
$migrationEditor = 'E:\Unity\Editors\6000.6.7f1\Editor\Unity.exe'
$migrationRoot = Split-Path $PSScriptRoot -Parent
Start-Transcript -Path (Join-Path $migrationRoot 'Logs\unity66-install.log') -Force
try {
    if ((Get-Item -LiteralPath $migrationInstaller).Length -ne 3854250800) { throw 'Incomplete Unity installer.' }
    $migrationSignature = Get-AuthenticodeSignature -LiteralPath $migrationInstaller
    Write-Output "Installer signature: $($migrationSignature.Status)"
    Write-Output "Publisher: $($migrationSignature.SignerCertificate.Subject)"
    if ($migrationSignature.Status -ne 'Valid' -or $migrationSignature.SignerCertificate.Subject -notmatch 'Unity') {
        throw 'Unity publisher signature could not be verified; installation was not started.'
    }
    if (-not (Test-Path -LiteralPath $migrationEditor)) {
        $migrationInstall = Start-Process -FilePath $migrationInstaller -ArgumentList '/S -CPULightmapper=no /D=E:\Unity\Editors\6000.6.7f1' -WindowStyle Hidden -Wait -PassThru
        Write-Output "Installer exit code: $($migrationInstall.ExitCode)"
        if ($migrationInstall.ExitCode -ne 0) { throw "Unity installation failed: $($migrationInstall.ExitCode)" }
    }
    if (-not (Test-Path -LiteralPath $migrationEditor)) { throw 'Installed editor executable was not found.' }
    (Get-Item -LiteralPath $migrationEditor).VersionInfo | Select-Object FileVersion,ProductVersion
    Write-Output "Unity 6.6 installed at $migrationEditor"
} finally { Stop-Transcript }
