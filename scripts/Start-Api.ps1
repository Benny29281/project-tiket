$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskDotnet = Join-Path $taskRoot '.tools\dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:ASPNETCORE_ENVIRONMENT = 'Development'
& $taskDotnet run --project (Join-Path $taskRoot 'src\HelpDeskKu.Api') --urls http://localhost:5080
if ($LASTEXITCODE -ne 0) { throw 'API gagal dijalankan. Periksa konfigurasi sesuai README.md.' }
