$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskNodeDirectory = Join-Path $taskRoot '.tools\node-v22.23.4-win-x64'
if (Test-Path -LiteralPath $taskNodeDirectory) { $env:PATH = "$taskNodeDirectory;" + $env:PATH }
Push-Location (Join-Path $taskRoot 'frontend')
try {
    if (!(Test-Path -LiteralPath 'node_modules')) {
        & npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'Instalasi dependency frontend gagal.' }
    }
    & npm.cmd start
} finally { Pop-Location }
