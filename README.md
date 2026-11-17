# Rancang APBDes

Aplikasi desktop Windows untuk **menyusun dan mensimulasikan perencanaan & penganggaran desa** saat musyawarah,
sebelum data diinput dan di-posting di Siskeudes resmi.

- Offline penuh, satu file data `.apbdes` (SQLite) yang mudah dibawa.
- Struktur dan istilah mengikuti Siskeudes (Bidang, Sub Bidang, Kegiatan, Paket, RAP/RAB, Sumberdana).
- Beberapa skenario per tahun anggaran, bisa dibandingkan dan dikunci (Posting).
- Laporan cetak/PDF dan ekspor Excel, termasuk daftar siap input ke Siskeudes.
- Tidak mencakup penatausahaan (SPP, pencairan, dll.) dan pembukuan — itu tetap di Siskeudes.

Aplikasi ini **bukan** produk resmi BPKP/Kemendagri dan tidak terhubung ke Siskeudes.

## Build

Butuh .NET SDK 10.

```bash
dotnet build src/RancangAPBDes
dotnet publish src/RancangAPBDes -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/RancangAPBDes
```

## Uji otomatis

```bash
RancangAPBDes.exe --uji "C:\folder-hasil-uji"
```

Mengisi data contoh, menguji simpan/batalkan/skenario/ekspor/laporan, lalu memotret setiap halaman di beberapa
ukuran layar dan memeriksa teks yang terpotong. Hasil di `hasil.txt`.

Rancangan dan catatan alur kerja: [ALUR_KERJA_SISKEUDES.md](ALUR_KERJA_SISKEUDES.md).
