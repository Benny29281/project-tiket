# PRD: HelpDeskKu – Sistem Manajemen Tiket Dukungan (IT Helpdesk Ticketing System)

| Item | Detail |
|---|---|
| Versi Dokumen | 1.0 |
| Status | Draft |
| Tech Stack | ASP.NET Core Web API (.NET 8), C#, Entity Framework Core, SQL Server, Angular (TypeScript) |
| Tipe Proyek | Web Application (SPA + REST API) |

---

## 1. Ringkasan Produk

**HelpDeskKu** adalah aplikasi web untuk mengelola permintaan bantuan (tiket) dari karyawan atau pelanggan kepada tim support/IT. Pengguna dapat membuat tiket, melampirkan file, memantau status, dan berkomunikasi dengan agen. Agen dan admin dapat menetapkan prioritas, menugaskan tiket, memantau SLA, serta melihat laporan kinerja.

### 1.1 Latar Belakang
Banyak organisasi kecil-menengah masih mengelola permintaan bantuan lewat chat, email, atau spreadsheet. Akibatnya:
- Permintaan sering terlewat atau tidak ada yang bertanggung jawab.
- Tidak ada riwayat dan pelacakan status yang jelas.
- Tidak ada data untuk mengevaluasi kinerja tim support.

### 1.2 Tujuan
1. Memusatkan semua permintaan bantuan di satu platform.
2. Mempercepat waktu respons dan penyelesaian tiket.
3. Menyediakan transparansi status bagi pengguna.
4. Menyediakan data dan laporan bagi manajemen.
5. Sebagai proyek portofolio yang menunjukkan penguasaan full-stack .NET + Angular.

### 1.3 Non-Goals (di luar cakupan v1.0)
- Integrasi telepon/WhatsApp.
- Aplikasi mobile native.
- Chatbot/AI otomatis.
- Multi-tenant (banyak perusahaan dalam satu instance).

---

## 2. Target Pengguna & Peran (Roles)

| Role | Deskripsi | Hak Akses Utama |
|---|---|---|
| **Requester (User)** | Karyawan/pelanggan yang membuat tiket | Buat tiket, lihat tiket sendiri, komentar, tutup/buka kembali tiket sendiri, beri rating |
| **Agent** | Staf support yang menangani tiket | Lihat antrian tiket, ambil/ditugaskan tiket, ubah status, komentar publik & catatan internal |
| **Admin** | Pengelola sistem | Semua hak Agent + kelola user, kategori, prioritas, SLA, lihat seluruh laporan |

### 2.1 Persona
- **Rina (Requester):** Staf HR yang laptopnya bermasalah; ingin melapor cepat dan tahu progresnya.
- **Budi (Agent):** Teknisi IT yang menangani 15–20 tiket per hari; butuh antrian jelas dan prioritas.
- **Pak Andi (Admin/Manajer IT):** Ingin tahu rata-rata waktu penyelesaian dan beban kerja tiap agen.

---

## 3. Ruang Lingkup Fitur (Functional Requirements)

Prioritas: **P0** = wajib MVP, **P1** = penting, **P2** = nice to have.

### 3.1 Autentikasi & Otorisasi
| ID | Requirement | Prioritas |
|---|---|---|
| FR-AUTH-01 | Registrasi akun dengan nama, email, password | P0 |
| FR-AUTH-02 | Login menggunakan email & password, menghasilkan JWT access token + refresh token | P0 |
| FR-AUTH-03 | Otorisasi berbasis role (Requester, Agent, Admin) di API dan route Angular | P0 |
| FR-AUTH-04 | Logout dan pencabutan refresh token | P0 |
| FR-AUTH-05 | Lupa/reset password via email | P1 |
| FR-AUTH-06 | Ubah password dan edit profil | P1 |
| FR-AUTH-07 | Akun dikunci sementara setelah 5 kali gagal login | P2 |

### 3.2 Manajemen Tiket
| ID | Requirement | Prioritas |
|---|---|---|
| FR-TKT-01 | Requester membuat tiket: judul, deskripsi, kategori, prioritas | P0 |
| FR-TKT-02 | Nomor tiket otomatis dengan format `HD-YYYYMM-0001` | P0 |
| FR-TKT-03 | Lampirkan file (maks. 5 file, 5 MB/file; jpg, png, pdf, docx, xlsx, txt) | P1 |
| FR-TKT-04 | Daftar tiket dengan pagination, sorting, pencarian, dan filter (status, prioritas, kategori, agen, rentang tanggal) | P0 |
| FR-TKT-05 | Halaman detail tiket: info, lampiran, timeline aktivitas, komentar | P0 |
| FR-TKT-06 | Alur status: `Open → In Progress → Pending (menunggu user) → Resolved → Closed`; tiket dapat `Reopened` | P0 |
| FR-TKT-07 | Admin/Agent menugaskan atau memindahkan tiket ke agen lain | P0 |
| FR-TKT-08 | Agent dapat mengambil tiket dari antrian (self-assign) | P1 |
| FR-TKT-09 | Requester hanya dapat melihat tiket miliknya; Agent melihat semua | P0 |
| FR-TKT-10 | Resolved otomatis menjadi Closed setelah 3 hari tanpa respons user | P2 |
| FR-TKT-11 | Soft delete tiket (hanya Admin) | P1 |

### 3.3 Komentar & Kolaborasi
| ID | Requirement | Prioritas |
|---|---|---|
| FR-CMT-01 | Requester dan Agent saling berkomentar pada tiket | P0 |
| FR-CMT-02 | Agent dapat menulis **catatan internal** yang tidak terlihat oleh Requester | P1 |
| FR-CMT-03 | Komentar dapat melampirkan file | P2 |
| FR-CMT-04 | Riwayat aktivitas otomatis (perubahan status, penugasan, prioritas) | P0 |

### 3.4 SLA & Prioritas
| ID | Requirement | Prioritas |
|---|---|---|
| FR-SLA-01 | Admin mengatur target waktu respons pertama dan waktu penyelesaian per prioritas | P1 |
| FR-SLA-02 | Sistem menghitung due date tiket otomatis berdasarkan prioritas | P1 |
| FR-SLA-03 | Penanda visual untuk tiket mendekati/melewati SLA (overdue) | P1 |

Contoh default SLA:

| Prioritas | Respons Pertama | Penyelesaian |
|---|---|---|
| Critical | 30 menit | 4 jam |
| High | 2 jam | 8 jam |
| Medium | 8 jam | 3 hari |
| Low | 24 jam | 7 hari |

### 3.5 Notifikasi
| ID | Requirement | Prioritas |
|---|---|---|
| FR-NTF-01 | Notifikasi in-app (ikon lonceng) untuk tiket baru, penugasan, komentar, perubahan status | P1 |
| FR-NTF-02 | Notifikasi email untuk kejadian penting | P1 |
| FR-NTF-03 | Notifikasi real-time dengan SignalR | P2 |

### 3.6 Dashboard & Laporan
| ID | Requirement | Prioritas |
|---|---|---|
| FR-DSH-01 | Dashboard Requester: jumlah tiket per status | P0 |
| FR-DSH-02 | Dashboard Agent: tiket saya, antrian belum ditugaskan, tiket overdue | P0 |
| FR-DSH-03 | Dashboard Admin: total tiket, tiket per kategori/prioritas, tren harian/bulanan, rata-rata waktu penyelesaian, beban kerja per agen | P1 |
| FR-DSH-04 | Ekspor laporan ke CSV/Excel | P2 |

### 3.7 Feedback
| ID | Requirement | Prioritas |
|---|---|---|
| FR-FBK-01 | Requester memberi rating (1–5) dan komentar setelah tiket Resolved/Closed | P2 |
| FR-FBK-02 | Admin melihat rata-rata rating per agen | P2 |

### 3.8 Administrasi
| ID | Requirement | Prioritas |
|---|---|---|
| FR-ADM-01 | CRUD pengguna (aktif/nonaktif, ubah role) | P0 |
| FR-ADM-02 | CRUD kategori tiket (mis. Hardware, Software, Jaringan, Akun) | P0 |
| FR-ADM-03 | Konfigurasi prioritas dan SLA | P1 |
| FR-ADM-04 | Audit log aktivitas penting | P2 |

---

## 4. User Stories & Acceptance Criteria

**US-01 – Membuat tiket**
> Sebagai Requester, saya ingin membuat tiket agar masalah saya ditangani.

- Given saya sudah login, When saya mengisi form tiket yang valid dan menekan *Kirim*, Then tiket tersimpan dengan status `Open` dan nomor tiket unik.
- Given judul atau deskripsi kosong, Then form menampilkan pesan validasi dan tiket tidak tersimpan.
- Given saya mengunggah file melebihi 5 MB atau tipe tidak diizinkan, Then sistem menolak file tersebut dengan pesan yang jelas.

**US-02 – Menangani tiket**
> Sebagai Agent, saya ingin mengambil tiket dan mengubah statusnya agar progres terlihat.

- Given tiket berstatus `Open` dan belum ditugaskan, When saya klik *Ambil Tiket*, Then tiket ditugaskan ke saya dan status menjadi `In Progress`.
- Given saya mengubah status, Then perubahan tercatat di timeline dan Requester menerima notifikasi.

**US-03 – Memantau tiket**
> Sebagai Requester, saya ingin melihat status dan riwayat tiket saya.

- Given saya membuka daftar tiket, Then hanya tiket milik saya yang tampil.
- Given Agent menulis catatan internal, Then catatan tersebut tidak tampil di tampilan saya.

**US-04 – Laporan**
> Sebagai Admin, saya ingin melihat statistik tiket untuk mengevaluasi tim.

- Given saya membuka dashboard, Then grafik tiket per status, kategori, dan rata-rata waktu penyelesaian tampil sesuai rentang tanggal yang dipilih.

**US-05 – Akses terbatas**
> Sebagai sistem, saya harus mencegah akses di luar hak peran.

- Given Requester mengakses endpoint admin, Then API mengembalikan `403 Forbidden`.
- Given token tidak valid/kedaluwarsa, Then API mengembalikan `401 Unauthorized` dan Angular mengarahkan ke halaman login.

---

## 5. Alur Status Tiket

```
            ┌───────────────────────────────┐
            │                               ▼
[Open] → [In Progress] ⇄ [Pending] → [Resolved] → [Closed]
   ▲                                      │            │
   └──────────────── [Reopened] ◄─────────┴────────────┘
```

Aturan transisi:
- Hanya Agent/Admin yang dapat memindahkan ke `In Progress`, `Pending`, dan `Resolved`.
- Requester dapat `Close` atau `Reopen` tiket miliknya.
- Setiap transisi tercatat di tabel riwayat aktivitas.

---

## 6. Non-Functional Requirements

| Kategori | Requirement |
|---|---|
| **Performa** | Waktu respons API < 500 ms untuk 95% request (hingga 100 pengguna bersamaan); halaman awal < 3 detik |
| **Keamanan** | HTTPS, password di-hash (ASP.NET Identity/PBKDF2 atau BCrypt), JWT dengan masa berlaku singkat, proteksi SQL Injection (EF Core parameterized), XSS, CSRF, CORS terbatas, rate limiting pada login, validasi tipe & ukuran file |
| **Skalabilitas** | Arsitektur stateless sehingga mudah di-scale horizontal |
| **Ketersediaan** | Target uptime 99% (lingkungan produksi) |
| **Usability** | Responsive (desktop, tablet, mobile); mengikuti prinsip aksesibilitas dasar (WCAG AA untuk kontras & navigasi keyboard) |
| **Maintainability** | Clean Architecture, unit test, konvensi penamaan konsisten, dokumentasi API (Swagger) |
| **Logging** | Structured logging (Serilog) dan penanganan error terpusat |
| **Backup** | Backup database terjadwal harian |
| **Lokalisasi** | Bahasa Indonesia (default), struktur siap i18n; zona waktu disimpan UTC dan ditampilkan lokal (WIB) |

---

## 7. Arsitektur & Tech Stack

### 7.1 Stack

| Layer | Teknologi |
|---|---|
| Frontend | Angular (versi terbaru stabil), TypeScript, Angular Router, Reactive Forms, RxJS, Angular Material / Bootstrap, Chart.js/ngx-charts |
| Backend | ASP.NET Core Web API (.NET 8), C# |
| ORM | Entity Framework Core (Code First + Migrations) |
| Database | Microsoft SQL Server |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Validasi | FluentValidation |
| Mapping | AutoMapper (atau mapping manual) |
| Logging | Serilog |
| Real-time (opsional) | SignalR |
| Email | SMTP / MailKit |
| Dokumentasi API | Swagger / OpenAPI |
| Testing | xUnit + Moq (backend), Jasmine/Karma atau Jest (frontend) |
| DevOps | Git, GitHub Actions, Docker |

> **Catatan:** Dokumen ini memakai **Angular modern (2+)** yang berbasis TypeScript, bukan AngularJS (1.x) yang sudah End-of-Life dan tidak dirancang untuk TypeScript.

### 7.2 Arsitektur Tingkat Tinggi

```
┌─────────────┐   HTTPS/JSON    ┌───────────────────────┐    EF Core    ┌─────────────┐
│  Angular    │ ──────────────► │  ASP.NET Core Web API │ ────────────► │ SQL Server  │
│  (SPA)      │ ◄────────────── │  (JWT, REST, SignalR) │ ◄──────────── │             │
└─────────────┘                 └───────────┬───────────┘               └─────────────┘
                                            │
                                  ┌─────────┴─────────┐
                                  │ File Storage /    │
                                  │ SMTP Email Server │
                                  └───────────────────┘
```

### 7.3 Struktur Solusi Backend (Clean Architecture)

```
HelpDeskKu.sln
├── src/
│   ├── HelpDeskKu.Domain          // Entities, Enums, Value Objects
│   ├── HelpDeskKu.Application     // DTOs, Interfaces, Services, Validators
│   ├── HelpDeskKu.Infrastructure  // DbContext, Repositories, Email, File Storage, Identity
│   └── HelpDeskKu.Api             // Controllers, Middleware, Program.cs
└── tests/
    ├── HelpDeskKu.UnitTests
    └── HelpDeskKu.IntegrationTests
```

### 7.4 Struktur Frontend Angular

```
src/app/
├── core/          // auth service, interceptors, guards, models
├── shared/        // komponen reusable, pipes, directives
├── features/
│   ├── auth/      // login, register, reset password
│   ├── tickets/   // list, create, detail, edit
│   ├── dashboard/
│   ├── admin/     // users, categories, SLA settings
│   └── notifications/
├── layouts/       // main layout, auth layout
└── app.routes.ts
```

---

## 8. Model Data (Database)

### 8.1 Entitas Utama

| Tabel | Kolom Penting |
|---|---|
| **Users** (Identity) | Id, FullName, Email, PasswordHash, IsActive, CreatedAt |
| **Roles / UserRoles** | Requester, Agent, Admin |
| **Tickets** | Id, TicketNumber, Title, Description, CategoryId, PriorityId, Status, RequesterId, AssigneeId (nullable), DueDate, FirstResponseAt, ResolvedAt, ClosedAt, CreatedAt, UpdatedAt, IsDeleted |
| **Categories** | Id, Name, Description, IsActive |
| **Priorities** | Id, Name, Level, ResponseTimeMinutes, ResolutionTimeMinutes |
| **TicketComments** | Id, TicketId, AuthorId, Content, IsInternal, CreatedAt |
| **TicketAttachments** | Id, TicketId, CommentId (nullable), FileName, StoredPath, ContentType, FileSize, UploadedById, UploadedAt |
| **TicketActivities** | Id, TicketId, ActorId, ActivityType, OldValue, NewValue, CreatedAt |
| **Notifications** | Id, UserId, Title, Message, TicketId, IsRead, CreatedAt |
| **TicketFeedbacks** | Id, TicketId, Rating, Comment, CreatedAt |
| **RefreshTokens** | Id, UserId, Token, ExpiresAt, RevokedAt |
| **AuditLogs** | Id, UserId, Action, Entity, EntityId, Timestamp |

### 8.2 Relasi (ERD Ringkas)

```
Users 1──* Tickets (Requester)
Users 1──* Tickets (Assignee)
Categories 1──* Tickets
Priorities 1──* Tickets
Tickets 1──* TicketComments
Tickets 1──* TicketAttachments
Tickets 1──* TicketActivities
Tickets 1──0..1 TicketFeedbacks
Users 1──* Notifications
```

### 8.3 Indeks yang Disarankan
- `Tickets(Status, PriorityId)`
- `Tickets(AssigneeId)` dan `Tickets(RequesterId)`
- `Tickets(TicketNumber)` unique
- `Notifications(UserId, IsRead)`

---

## 9. Desain API (REST)

Base URL: `/api/v1`

### 9.1 Auth
| Method | Endpoint | Deskripsi | Akses |
|---|---|---|---|
| POST | `/auth/register` | Registrasi | Publik |
| POST | `/auth/login` | Login, mengembalikan JWT | Publik |
| POST | `/auth/refresh` | Refresh token | Publik |
| POST | `/auth/logout` | Cabut refresh token | Login |
| POST | `/auth/forgot-password` | Kirim link reset | Publik |
| POST | `/auth/reset-password` | Reset password | Publik |
| GET/PUT | `/auth/me` | Lihat/ubah profil | Login |

### 9.2 Tickets
| Method | Endpoint | Deskripsi | Akses |
|---|---|---|---|
| GET | `/tickets` | Daftar tiket (filter, paging, sort) | Login |
| POST | `/tickets` | Buat tiket | Login |
| GET | `/tickets/{id}` | Detail tiket | Pemilik/Agent/Admin |
| PUT | `/tickets/{id}` | Ubah tiket | Agent/Admin (atau pemilik sebelum diproses) |
| PATCH | `/tickets/{id}/status` | Ubah status | Sesuai aturan transisi |
| PATCH | `/tickets/{id}/assign` | Tugaskan agen | Agent/Admin |
| DELETE | `/tickets/{id}` | Soft delete | Admin |
| GET | `/tickets/{id}/activities` | Timeline aktivitas | Pemilik/Agent/Admin |
| POST | `/tickets/{id}/comments` | Tambah komentar | Pemilik/Agent/Admin |
| POST | `/tickets/{id}/attachments` | Unggah lampiran | Pemilik/Agent/Admin |
| GET | `/attachments/{id}/download` | Unduh lampiran | Sesuai akses tiket |
| POST | `/tickets/{id}/feedback` | Beri rating | Pemilik |

### 9.3 Master & Admin
| Method | Endpoint | Akses |
|---|---|---|
| GET/POST/PUT/DELETE | `/categories` | GET: Login; lainnya: Admin |
| GET/PUT | `/priorities` | Admin |
| GET/POST/PUT | `/users` | Admin |
| GET | `/notifications` | Login |
| PATCH | `/notifications/{id}/read` | Login |
| GET | `/dashboard/summary` | Login (data sesuai role) |
| GET | `/reports/tickets` | Admin |

### 9.4 Konvensi
- Format respons error mengikuti **ProblemDetails (RFC 7807)**.
- Pagination: `?page=1&pageSize=20&sortBy=createdAt&sortDir=desc`.
- Kode status: `200/201/204`, `400`, `401`, `403`, `404`, `409`, `500`.
- Semua waktu dalam UTC (ISO 8601).

---

## 10. Desain UI / Halaman

| Halaman | Role | Deskripsi |
|---|---|---|
| Login / Register / Reset Password | Publik | Form autentikasi |
| Dashboard | Semua | Ringkasan sesuai role |
| Daftar Tiket | Semua | Tabel dengan filter, pencarian, paging |
| Buat Tiket | Requester | Form + unggah file |
| Detail Tiket | Semua | Info, timeline, komentar, aksi status/penugasan |
| Antrian Tiket | Agent/Admin | Tiket belum ditugaskan, diurutkan prioritas & SLA |
| Manajemen User | Admin | CRUD user & role |
| Master Kategori & SLA | Admin | Pengaturan |
| Laporan | Admin | Grafik dan ekspor |
| Notifikasi | Semua | Daftar notifikasi |
| Profil | Semua | Ubah profil & password |

Prinsip UI: konsisten, responsive, status ditandai dengan warna dan label (bukan warna saja), loading state dan empty state jelas, konfirmasi untuk aksi destruktif.

---

## 11. Metrik Keberhasilan (KPI)

| Metrik | Target |
|---|---|
| Waktu respons pertama rata-rata | Sesuai SLA ≥ 90% tiket |
| Waktu penyelesaian rata-rata | Turun 20% dalam 3 bulan pertama |
| Persentase tiket melewati SLA | < 10% |
| Skor kepuasan (CSAT) | ≥ 4.0 / 5 |
| Cakupan unit test (backend, layer Application) | ≥ 70% |
| Tiket tanpa penanggung jawab > 1 jam | < 5% |

---

## 12. Rencana Rilis & Milestone

| Fase | Durasi (estimasi) | Cakupan |
|---|---|---|
| **Fase 0 – Persiapan** | 1 minggu | Setup repo, solusi .NET, proyek Angular, database, CI dasar |
| **Fase 1 – MVP** | 4–5 minggu | Auth + role, CRUD tiket, komentar, status, penugasan, dashboard dasar, manajemen user & kategori (semua P0) |
| **Fase 2 – Peningkatan** | 3 minggu | Lampiran, SLA, notifikasi in-app & email, catatan internal, dashboard admin, reset password (P1) |
| **Fase 3 – Penyempurnaan** | 2 minggu | Rating, ekspor laporan, SignalR, audit log, auto-close, hardening keamanan (P2) |
| **Fase 4 – Deployment** | 1 minggu | Docker, deployment, dokumentasi, UAT |

---

## 13. Strategi Pengujian

| Jenis | Alat | Cakupan |
|---|---|---|
| Unit test backend | xUnit, Moq | Service, validator, aturan transisi status |
| Integration test | WebApplicationFactory + SQL test DB | Endpoint API & otorisasi |
| Unit test frontend | Jasmine/Karma atau Jest | Service, guard, komponen utama |
| E2E | Cypress / Playwright | Login, buat tiket, ubah status, hak akses |
| UAT | Manual | Skenario berdasarkan user story |
| Keamanan | OWASP ZAP / checklist OWASP Top 10 | Autentikasi, upload file, injection |

---

## 14. Risiko & Mitigasi

| Risiko | Dampak | Mitigasi |
|---|---|---|
| Scope creep (fitur terus bertambah) | Jadwal molor | Kunci MVP pada fitur P0; fitur baru masuk backlog |
| Kebocoran data tiket antar-user | Tinggi | Filter otorisasi di level query + integration test hak akses |
| Upload file berbahaya | Tinggi | Whitelist tipe, batasi ukuran, simpan di luar web root, ganti nama file |
| Performa daftar tiket melambat | Sedang | Indeks DB, pagination server-side, projection DTO |
| Kompleksitas SLA | Sedang | Mulai dari SLA sederhana berbasis jam kalender; jam kerja jadi pengembangan lanjutan |

---

## 15. Asumsi & Ketergantungan

- Pengguna memiliki browser modern (Chrome, Edge, Firefox, Safari versi terbaru).
- Tersedia SMTP server untuk pengiriman email.
- Lingkungan dev: .NET 8 SDK, Node.js LTS, SQL Server (Express/LocalDB/Docker).
- Seluruh pengguna terautentikasi; tidak ada akses anonim ke data tiket.

---

## 16. Pengembangan Lanjutan (Backlog v2.0)

- Integrasi email-to-ticket (tiket dibuat dari email masuk).
- Knowledge base / FAQ dengan pencarian.
- Jam kerja & hari libur pada perhitungan SLA.
- Template balasan (canned responses).
- Multi-bahasa dan multi-tenant.
- Single Sign-On (Azure AD / Google).
- Integrasi Slack/Teams/WhatsApp.
- Rekomendasi kategori/prioritas otomatis dengan AI.

---

## 17. Glosarium

| Istilah | Arti |
|---|---|
| **Tiket** | Satu permintaan bantuan yang dicatat dan dilacak |
| **SLA** | Service Level Agreement, target waktu respons/penyelesaian |
| **Requester** | Pembuat tiket |
| **Agent** | Staf yang menangani tiket |
| **JWT** | JSON Web Token untuk autentikasi stateless |
| **MVP** | Minimum Viable Product, versi minimal yang sudah berguna |
| **UAT** | User Acceptance Testing |
