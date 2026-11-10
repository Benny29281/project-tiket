# HelpDeskKu

Aplikasi manajemen tiket dukungan IT berdasarkan `PRD (3).md`. Implementasi awal mencakup fitur MVP/P0, ditambah catatan internal, pengambilan tiket, penanda SLA, dan arsip tiket.

## Teknologi

- Backend: ASP.NET Core Web API, .NET 8, C#, ASP.NET Core Identity, JWT, EF Core, Serilog, Swagger.
- Frontend: Angular 22, TypeScript 6, Reactive Forms, Router, RxJS; antarmuka Bahasa Indonesia yang responsif.
- Database aplikasi: Microsoft SQL Server. Database in-memory dipakai khusus tes otomatis.
- Angular yang digunakan adalah Angular modern berbasis TypeScript. Kebutuhan versi Node/TypeScript mengikuti [matriks resmi Angular](https://angular.dev/reference/versions).

## Fitur tersedia

- Registrasi Requester, login, refresh token berotasi, logout, lockout setelah 5 kegagalan login, dan rate limit autentikasi.
- Role Requester/Agent/Admin; pemeriksaan akses dilakukan pada API dan route frontend.
- Pembuatan nomor tiket `HD-YYYYMM-0001`, daftar dengan pencarian/filter/pagination/sorting, detail, dan edit tiket.
- Filter status, prioritas, kategori, agen, tanggal, dan antrian belum ditugaskan.
- Transisi status sesuai PRD, penugasan agen, ambil tiket, komentar publik/catatan internal, dan timeline otomatis.
- Dashboard berdasarkan role; Requester dibatasi ke tiket sendiri, Agent/Admin dapat melihat seluruh tiket.
- Admin membuat pengguna, mengubah role/status aktif, dan membuat/mengedit/menonaktifkan kategori.
- Prioritas dan target SLA bawaan; due date, penanda overdue, dan arsip tiket oleh Admin.
- Error ProblemDetails, migrasi EF Core, skema SQL, dan tes workflow/otorisasi API.

Implementasi ini belum mencakup seluruh fase PRD: lampiran, reset password/email, profil, pengaturan SLA, notifikasi, laporan/tren admin, feedback, ekspor, SignalR, audit log administrasi, auto-close, Docker deployment, dan backup terjadwal masih menjadi backlog. Timeline perubahan tiket sudah tersedia. Belum dilakukan pengujian beban atau audit keamanan produksi.

## Persiapan Windows

1. Pasang .NET 8 SDK dan Node.js 22.22.3+ (versi 22), atau gunakan SDK/Node portable yang sudah disiapkan di `.tools` pada workspace ini. Folder `.tools` diabaikan Git, sehingga tidak ikut saat clone baru.
2. Pasang **SQL Server Express LocalDB** untuk pengembangan lokal. Unduh dari [Microsoft SQL Server Downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads), lalu pilih media/komponen LocalDB (`SqlLocalDB.msi`). Alternatifnya, gunakan komponen LocalDB di Visual Studio Installer. Ikuti [panduan resmi Microsoft LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb).
3. Setelah instalasi, buka ulang terminal. Periksa instance:

```powershell
SqlLocalDB info
SqlLocalDB start MSSQLLocalDB
```

Jika `SqlLocalDB` belum dikenali, gunakan executable dari direktori `Tools\Binn` instalasi SQL Server atau perbarui PATH melalui installer. LocalDB menggunakan akun Windows Anda, jadi password database tidak diperlukan. Laragon/MySQL bukan pengganti SQL Server untuk project ini.

## Menyiapkan database dan admin

Hentikan backend terlebih dahulu dengan **Ctrl+C** pada terminal API atau hentikan debug API di IDE. Setup melakukan build; backend yang masih berjalan dapat mengunci file `HelpDeskKu.Api.exe` dan menyebabkan error MSB3021/MSB3027. Frontend boleh tetap berjalan. Jalankan backend setelah setup menampilkan `Database siap`.

Dari root project, jalankan:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Initialize-Local.ps1
```

Script meminta email dan password admin secara lokal, membuat JWT signing key acak, menyimpan konfigurasi dengan .NET user-secrets, menerapkan migrasi, dan mengisi role/kategori/prioritas. Tidak ada password admin bawaan. Password admin minimal 10 karakter dengan huruf besar, huruf kecil, angka, dan simbol. Password hanya dipakai selama inisialisasi; hash disimpan oleh Identity.

Untuk SQL Server Express biasa dengan Windows Authentication:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Initialize-Local.ps1 -Server '.\SQLEXPRESS'
```

Gunakan database baru khusus HelpDeskKu. Menjalankan script lagi akan mengganti signing key sehingga sesi sebelumnya berakhir. Admin hanya dibuat jika email belum ada; script tidak mengubah password atau role akun yang sudah ada. User-secrets disimpan di profil Windows, di luar repository, dan tidak dienkripsi; gunakan hanya untuk pengembangan.

Migrasi berada di `src/HelpDeskKu.Api/Migrations`. `database/schema.sql` adalah skrip SQL idempotent untuk database yang sudah dipilih/dibuat. Untuk perubahan berikutnya gunakan migrations, bukan `EnsureCreated`. Opsi `Database:Initialize` merupakan opt-in bootstrap database kosong dan tidak digunakan oleh script setup standar.

## Menjalankan aplikasi

Buka dua terminal dari root project:

```powershell
# Terminal 1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Api.ps1

# Terminal 2
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Web.ps1
```

- Website: http://127.0.0.1:4200
- Swagger API: http://localhost:5080/swagger
- Health proses API: http://localhost:5080/health (bukan pemeriksaan koneksi database).

Login menggunakan admin yang dibuat saat setup. Admin dapat membuat akun Agent; registrasi publik selalu menghasilkan Requester. Buat Requester melalui halaman Daftar, lalu uji pembuatan tiket, penugasan oleh Agent, komentar, perubahan status, dan penutupan oleh pemilik.

Jika memasang tool secara global, perintah standar juga dapat digunakan:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/HelpDeskKu.Api --urls http://localhost:5080
# Terminal terpisah
cd frontend
npm ci
npm start
```

## Pengujian

```powershell
dotnet test HelpDeskKu.sln
cd frontend
npm ci
npm run build
```

Tes API menjalankan ASP.NET TestServer dan ASP.NET Identity/JWT asli dengan EF Core InMemory; tes mencakup 401/403, isolasi data pemilik, kerahasiaan catatan internal, penugasan/timeline, validasi status/form, refresh token replay, dan logout. Provider ini tidak menguji transaksi, rowversion, constraint, atau perilaku query SQL Server. Verifikasi dengan SQL Server asli dan uji konkurensi nomor tiket masih diperlukan sebelum produksi.

## Struktur

```text
HelpDeskKu.sln
src/
  HelpDeskKu.Domain/           Entitas dan aturan status
  HelpDeskKu.Application/      Kontrak, DTO, validasi input, abstraksi user
  HelpDeskKu.Infrastructure/   EF Core, Identity, token, seed
  HelpDeskKu.Api/              Controllers, middleware, migrasi, Swagger
frontend/src/app/
  core/                       HTTP client, sesi, guard, interceptor, model
  features/                   Halaman aplikasi
  layouts/                    Sidebar dan header
  shared/                     Ikon dan badge status
tests/                        Unit test dan integration test API
scripts/                      Setup dan menjalankan aplikasi lokal
database/                     SQL hasil migrasi
```

## Catatan deployment

Frontend produksi dihasilkan di `frontend/dist/helpdeskku/browser`; host SPA harus melakukan fallback route ke `index.html` dan proxy `/api` ke ASP.NET. Backend membutuhkan hosting ASP.NET dan SQL Server tersendiri; belum dipublikasikan. Konfigurasikan connection string, JWT key/issuer/audience, origin CORS, AllowedHosts, HTTPS/reverse proxy, dan secret store sesuai lingkungan produksi. JWT disimpan di sessionStorage browser untuk MVP; gunakan penilaian ancaman dan hardening CSP/cookie sebelum produksi. Logout mencabut refresh token; access token yang telah terbit tetap berlaku sampai maksimal 15 menit, kecuali akun dinonaktifkan/role berubah.

Waktu disimpan UTC dan UI ditampilkan WIB. SLA memakai jam kalender. Halaman dashboard menampilkan data nyata dari API; tidak ada data contoh yang disamarkan sebagai data tersimpan.
