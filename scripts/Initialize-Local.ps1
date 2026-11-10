param(
    [string]$Server = '(localdb)\MSSQLLocalDB',
    [string]$Database = 'HelpDeskKu'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskDotnet = Join-Path $taskRoot '.tools\dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path -Parent $taskDotnet
$env:PATH = "$env:DOTNET_ROOT;" + $env:PATH
$taskProject = Join-Path $taskRoot 'src\HelpDeskKu.Api'
$taskRunningApi = Get-Process -Name 'HelpDeskKu.Api' -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and $_.Path.StartsWith($taskProject + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}
if ($taskRunningApi) {
    throw 'Backend HelpDeskKu masih berjalan. Tekan Ctrl+C pada terminal Start-Api.ps1 (atau hentikan debug API di IDE), lalu jalankan Initialize-Local.ps1 kembali. Setup harus selesai sebelum backend dijalankan.'
}
Write-Host 'Memeriksa build backend...'
& $taskDotnet build $taskProject --nologo
if ($LASTEXITCODE -ne 0) {
    throw 'Build backend gagal. Lihat error dotnet build di atas. Jika file sedang digunakan (MSB3021/MSB3027), hentikan backend/debug API dengan Ctrl+C lalu ulangi setup. Database dan akun admin belum diinisialisasi.'
}
$taskEf = Join-Path $taskRoot '.tools\ef\dotnet-ef.exe'
if (!(Test-Path -LiteralPath $taskEf)) {
    & $taskDotnet tool install dotnet-ef --tool-path (Join-Path $taskRoot '.tools\ef') --version 8.0.22
    if ($LASTEXITCODE -ne 0) { throw 'Instalasi EF Core CLI gagal.' }
}
$taskEmail = Read-Host 'Email admin pertama'
if ($taskEmail -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$') { throw 'Format email tidak valid.' }
$taskPassword = Read-Host 'Password admin (min. 10 karakter, huruf besar/kecil, angka, simbol)' -AsSecureString
$taskPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($taskPassword)
try {
    $taskClearPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($taskPointer)
    if ($taskClearPassword.Length -lt 10 -or $taskClearPassword -cnotmatch '[A-Z]' -or $taskClearPassword -cnotmatch '[a-z]' -or $taskClearPassword -notmatch '[0-9]' -or $taskClearPassword -notmatch '[^a-zA-Z0-9]') { throw 'Password belum memenuhi persyaratan.' }
    $taskConnection = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $taskConnection['Data Source'] = $Server
    $taskConnection['Initial Catalog'] = $Database
    $taskConnection['Integrated Security'] = $true
    $taskConnection['TrustServerCertificate'] = $true
    $taskBytes = New-Object byte[] 64
    $taskRandom = [Security.Cryptography.RandomNumberGenerator]::Create()
    $taskRandom.GetBytes($taskBytes)
    $taskRandom.Dispose()
    # Secrets are sent via stdin; never print credentials or include them in command arguments.
    $taskSecrets = @{
        'ConnectionStrings:Default' = $taskConnection.ConnectionString
        'Jwt:Key' = [Convert]::ToBase64String($taskBytes)
    } | ConvertTo-Json
    $taskSecrets | & $taskDotnet user-secrets set --project $taskProject
    if ($LASTEXITCODE -ne 0) { throw 'Penyimpanan konfigurasi lokal gagal.' }
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Default = $taskConnection.ConnectionString
    $env:Seed__AdminEmail = $taskEmail
    $env:Seed__AdminPassword = $taskClearPassword
    & $taskEf database update --project $taskProject --startup-project $taskProject --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Penerapan migrasi database gagal. Build sudah berhasil; lihat error EF Core di atas. Periksa instalasi SQL Server, nama instance, dan akses Windows ke database.' }
    & $taskDotnet run --no-build --project $taskProject -- --initialize
    if ($LASTEXITCODE -ne 0) { throw 'Inisialisasi role, kategori, prioritas, atau admin gagal.' }
    Write-Host 'Database siap. Jalankan scripts/Start-Api.ps1 dan scripts/Start-Web.ps1 pada dua terminal.'
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($taskPointer)
    $taskClearPassword = $null
    $taskSecrets = $null
    $env:Seed__AdminEmail = $null
    $env:Seed__AdminPassword = $null
    $env:ConnectionStrings__Default = $null
}
