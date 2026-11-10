# Status verifikasi MVP

Pemeriksaan dilakukan pada 10 November 2026.

| Pemeriksaan | Hasil |
|---|---|
| Build ASP.NET Core (.NET 8) | Lulus, tanpa warning/error |
| Unit test workflow tiket | 14 lulus |
| Integration test HTTP API + Identity/JWT | 8 lulus dengan EF InMemory |
| Build produksi Angular 22 / TypeScript 6 | Lulus; initial bundle sekitar 312 kB sebelum kompresi |
| Frontend HTTP lokal | HTTP 200 |
| Sintaks script PowerShell | Lulus |
| Migrasi awal & SQL idempotent | Berhasil dihasilkan; belum diterapkan ke SQL Server |
| Pemeriksaan visual browser | Belum tersedia; browser access host tidak tersedia |
| End-to-end dengan SQL Server asli | Belum dilakukan; SQL Server belum terpasang |

## Uji manual setelah pemasangan SQL Server

1. Jalankan `scripts/Initialize-Local.ps1`, API, dan frontend sesuai README.
2. Login sebagai Admin. Buat satu Agent dan satu Requester; pastikan password/role tersimpan.
3. Login Requester di browser terpisah. Buat tiket, cari berdasarkan judul/nomor, dan cek detail.
4. Gunakan Requester lain untuk memastikan tiket pertama tidak terlihat, termasuk bila membuka URL detail langsung.
5. Login Agent. Ambil tiket dari antrian. Status berubah menjadi Diproses dan penugasan/status tercatat di timeline.
6. Tambahkan komentar publik dan catatan internal. Requester hanya melihat komentar publik.
7. Ubah status menjadi Menunggu, Diproses, lalu Selesai. Requester dapat menutup atau membuka kembali tiket.
8. Pastikan Requester tidak dapat mengakses halaman administrasi atau mengubah status ke Selesai melalui API.
9. Uji pencarian, seluruh filter, sorting, pagination, dan batas tanggal dalam WIB.
10. Uji penonaktifan kategori, akun, dan perubahan role. Akun nonaktif tidak dapat memakai token lama/refresh token.
11. Uji dua pembuatan tiket bersamaan. Nomor tiket harus unik; alokasi nomor memakai transaction-owned SQL application lock.
12. Uji refresh token bersamaan/replay dan update tiket bersamaan. Rowversion SQL Server harus mencegah update yang saling menimpa.
13. Periksa desktop/mobile, navigasi keyboard, fokus, teks panjang, empty state, dan pesan kegagalan koneksi.

Pengujian otomatis InMemory tidak membuktikan perilaku locking, indeks/constraint, rowversion, atau translasi query SQL Server. Pemeriksaan ini harus dilengkapi sebelum deployment produksi.
