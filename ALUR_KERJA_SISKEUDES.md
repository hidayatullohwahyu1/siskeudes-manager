# Alur Kerja Siskeudes → Rancangan Simulator Perencanaan & APBDes

Sumber: penelusuran **hanya-baca** atas SisKeuDes V2.0.9 (BPKP–Kemendagri) lewat Siskeudes Cloud
Kab. Banjarnegara, 17 November 2026, akun desa operator. Tidak ada data yang diubah.
Dokumen ini mencatat **alur dan struktur data**, bukan tampilan/merek. Data pribadi (nama, NPWP,
nomor rekening) sengaja tidak dicatat.

Aplikasi yang akan dibangun: simulator perencanaan & penganggaran desktop Windows **dengan nama & logo sendiri** (lihat bagian 5),
mengikuti Permendagri 20/2018 tentang Pengelolaan Keuangan Desa.

---

## 1. Arsitektur aplikasi asli (pengamatan)

- Siskeudes Cloud = desktop Windows jarak jauh (HTML5 RDP) yang menjalankan aplikasi desktop
  SisKeuDes 2019–2026, Siswaskeudes, dan Teman Desa.
- SisKeuDes: aplikasi desktop klasik (form + grid), database **SQL Server** (`siskeudes2026`),
  satu database per tahun anggaran.
- Login: User ID + Password + **Tahun Anggaran**. Status bar menampilkan user, tahun, server DB.
- Pola form seragam: panel navigasi kiri → tab vertikal (master → detail → sub-detail) → grid di atas,
  isian di bawah → tombol **Tambah / Ubah / Hapus / Batal / Simpan / Cetak / Tutup**.

## 2. Struktur menu

| Menu | Isi |
|---|---|
| **File** | Login, Logout, Ganti ID Password, Pengaturan UserID*, Pengaturan Menu*, Keluar |
| **Parameter** | Data Umum Pemda*, Kecamatan dan Desa*, Referensi Kegiatan*, Referensi Sumberdana*, Rekening APB Desa*, Standar Satuan Harga*, Output Kegiatan DD*, Referensi Peraturan*, Belanja Operasional & Daftar Nominatif*, Mapping Korolari Aset*, **Rekening Bank dan Perangkat Desa** |
| **Data Entri** | Data Umum Desa · Perencanaan · Penganggaran · Penatausahaan · Pembukuan |
| **Laporan** | Parameter · Perencanaan · Penganggaran · Penatausahaan · Pertanggungjawaban · Kompilasi Pemda* · Monitoring Pemda* |
| **Tools** | Setting Konfigurasi, Update Database, Update SML Pemda, Ekspor/Impor Parameter, Penggabungan Data, Ekspor Data Untuk…, Kosongkan Data, Backup Database, Kunci Transaksi Desa, Teks Pesan, Monitoring, Cek Database (semua khusus admin) |

\* = hanya aktif untuk level Pemda/Admin. Jadi ada **dua level pengguna**: Admin Kabupaten (mengelola referensi) dan Operator Desa (entri transaksi).

### Data Entri – submenu
- **Perencanaan**: RPJMDes dan RKPDes · Ekspor/Impor Data RPJMDes
- **Penganggaran**: Isian Data Anggaran · Anggaran Kas Desa · Peraturan APBDes · Posting APBDesa* · Anggaran Lanjutan · Ekspor/Impor Data Anggaran
- **Penatausahaan**: Penerimaan Desa · SPP Kegiatan · Pencairan SPP · SPJ Kegiatan · Pengembalian · Penyetoran Pajak · Mutasi Kas · Output Dana Desa · Ekspor/Impor Data
- **Pembukuan**: Saldo Awal · Penyesuaian · Ekspor/Impor Data

## 3. Alur kerja utama (siklus satu tahun anggaran)

```
Parameter (Admin) ──► Data Umum Desa ──► Perencanaan (RPJMDes/RKPDes)
        │
        ▼
Penganggaran: Bidang → Sub Bidang → Kegiatan → Paket
              Pendapatan (RAP → RAP Rinci)
              Belanja   (Kegiatan → Paket → RAB → RAB Rinci)
              Pembiayaan 1 / 2
        │  Anggaran Kas → Perdes APBDes → POSTING (Awal / Perubahan = PAK)
        ▼
Penatausahaan:
  Pendapatan:  Penerimaan Tunai / Bank (TBP) ─► Penyetoran ke rekening kas desa
  Belanja:     SPP (Panjar / Definitif / Pembiayaan)
                 └ Rincian SPP (rekening belanja + sumber dana)
                 └ Bukti Pengeluaran (kuitansi, penerima, rekening bank)
                     └ Potongan (pajak: kode pot, billing)
               ─► Pencairan SPP (Tunai/Bank, potongan, netto)
               ─► SPJ Kegiatan (untuk SPP Panjar)  ─► Pengembalian sisa panjar
  Pajak:       Penyetoran Pajak (potongan yang dipungut → disetor)
  Kas:         Mutasi Kas (tunai ↔ bank)
        ▼
Pembukuan: Saldo Awal, Penyesuaian
        ▼
Laporan: Perdes APBDes, RAB, RAK, DPA, BKU, Buku Bank, Buku Pajak,
         Buku Pembantu, LRA/Realisasi, SPJ, LPJ
```

## 4. Detail form yang diamati

### 4.1 Data Umum Desa
Tab **Data Umum**: Tahun · Nama Desa · Nama/Pendidikan/Jabatan Kepala Desa · No & Tgl RKPDes ·
No & Tgl Perdes Pertanggungjawaban · Status APBDes (AWAL/PAK) · Nama/Jabatan/Pendidikan Sekdes ·
Nama/Jabatan/Pendidikan Kaur Keuangan · NPWP Desa · Ibukota Desa · Tipologi Desa · Status IDM.
Tab **Pelaksana Kegiatan** (PPKD): No ID · Nama · Jabatan.

### 4.2 Penganggaran – Kegiatan (hierarki kode)
| Level | Contoh kode | Field |
|---|---|---|
| Bidang | `04.2002.01` | Kd_Bid, Nama_Bidang (5 bidang standar) |
| Sub Bidang | `04.2002.01.01.` | Kd_Sub, Nama_SubBidang |
| Kegiatan | `04.2002.01.01.01.` | Kd_Keg, Nama, Lokasi, Waktu, Pelaksana (PPKD), Pagu setelah perubahan, Keluaran, Volume+Satuan keluaran |
| Paket | ID `01` | Nama Paket, Nilai, Sumberdana, Pola (Swakelola/…), Sifat (Fisik/Non fisik), Target Output, Satuan, Lokasi, PKTD/Non PKTD, Uraian Output, Tagging |

Kode desa = `<kode kecamatan>.<kode desa>` (contoh `04.2002`) jadi awalan semua kode.

### 4.3 Penganggaran – Pendapatan
- **RAP**: Kd_Rincian (rekening, mis. `4.1.2.01.`), Nama Rincian, Anggaran, AnggaranPAK, total.
- **RAP Rinci**: No urut, Uraian (maks 50 karakter), Jumlah Satuan + Satuan, Harga Satuan,
  Sumberdana → Anggaran / Perubahan / Jumlah.

### 4.4 Penganggaran – Belanja
Tab: Bidang → Kegiatan → Paket → **RAB** (rekening belanja, mis. `5.2.5.01.`: Anggaran, AnggaranPAK)
→ **RAB Rinci** (sama seperti RAP Rinci + acuan **SBU/Standar Satuan Harga**).

### 4.5 SPP Kegiatan
Jenis: **Panjar Kegiatan**, **SPP Definitif**, **SPP Pembiayaan**, plus Daftar Nominatif.
- **SPP**: No SPP (`0001/SPP/04.2002/2026`), Tgl SPP, Uraian, Jumlah, Status (Draft/Final), Daftar Nominatif (Ya/Tidak).
- **Rincian SPP**: Kd_Rincian (rekening belanja), NoID paket, Sumber dana (mis. PBH, DDS, ADD), Nilai.
- **Bukti Pengeluaran**: No Bukti (`00001/KWT/04.2002/2026`), Tgl, Uraian, Nilai, Link bukti pendukung,
  Penerima (Nama, Alamat, Kode Bank, No Rek, Nama Bank, NPWP).
- **Potongan** per bukti: Kode Pot, Nama Potongan, Nilai, Billing Pajak.

### 4.6 Pencairan SPP
Daftar **SPP Belum Cair** / **SPP Sudah Cair** (hanya SPP berstatus Final).
Bukti Pencairan: No Bukti (`0001/BANK/04.2002/2026`), Tgl Bayar, Uraian, Pembayaran (Tunai/Bank),
Nama Bank, CMS ID → Jumlah − Potongan = **Dibayarkan (netto)**.

### 4.7 Penerimaan Desa
Bagian: **SILPA Tahun Lalu**, **Penerimaan Tunai**, **Penerimaan Bank**, **Penyetoran**.
TBP: No Bukti (`0001/TBP/04.2002/2026`), Tgl, Uraian, Jumlah, Penyetor (Nama, Alamat, Ttd),
Bank Penerima (Rekening, Nama Bank). Tab **Rincian TBP**: rekening pendapatan + nilai.

### 4.8 Laporan Penganggaran (contoh daftar)
Lampiran 1a/1b Perdes APBDes · Lampiran 1c Penjabaran · RAB 1 Pendapatan · RAB 2 Belanja ·
RAB 3 Pembiayaan · RAK (Rencana Anggaran Kas) · RKA · DPA · DPA Kegiatan Lanjutan ·
Proporsi Belanja Operasional · Ringkasan APBDes per Sumberdana · RAB 2 per Sumberdana ·
Tagging · Rekap Tagging · Penganggaran PKTD.
Filter: Kecamatan, Desa, Draft (Awal/Perubahan), Posting, Footer (tempat & tanggal).

## 5. Rancangan aplikasi (REVISI): "Simulator Perencanaan & APBDes"

### Tujuan
Aplikasi **latihan/simulasi** untuk dipakai saat **musyawarah dengan perangkat desa** (Musdes/rapat
RKPDes & APBDes), **sebelum** data dimasukkan dan di-*posting* di Siskeudes asli.
- Siskeudes asli tidak untuk coba-coba dan tidak selalu bisa dibuka → aplikasi ini **offline penuh**.
- **Tidak ada** penatausahaan (SPP, pencairan, penerimaan, pajak) dan **tidak ada** pembukuan.
- Hasil akhirnya: angka & rincian yang sudah disepakati, siap diketik ulang ke Siskeudes.

### Prinsip
1. **Bebas coba**: bisa ubah, hapus, salin, dan kembalikan kapan saja (undo, riwayat versi).
2. **Skenario**: satu tahun anggaran bisa punya beberapa skenario (mis. "Usulan Kades", "Hasil Musdes",
   "Alternatif pagu DD turun 10%") untuk dibandingkan berdampingan.
3. **Struktur sama dengan Siskeudes**: kode bidang/sub bidang/kegiatan/rekening, sumber dana, dan istilah
   yang sama, supaya pemindahan ke Siskeudes tinggal salin.
4. **Cocok untuk rapat**: tampilan besar & jelas (mode presentasi ke proyektor), total dan sisa pagu
   selalu terlihat, peringatan langsung kalau melebihi pagu.

### Modul
| # | Modul | Isi |
|---|---|---|
| 1 | **Referensi** | Bidang & Sub Bidang standar, daftar kegiatan referensi, kode rekening pendapatan/belanja/pembiayaan, sumber dana (DDS, ADD, PBH, PAD, BKK, SILPA, dll.), satuan, standar harga (SSH/SBU) yang bisa diisi sendiri |
| 2 | **Data Umum Desa** | Nama desa, kode desa, kepala desa, sekdes, kaur keuangan, daftar PPKD/pelaksana kegiatan |
| 3 | **Perencanaan** | Daftar usulan (dari dusun/RT/lembaga), prioritas, tahun pelaksanaan (gaya RPJMDes 6 tahun) → dipilih masuk RKPDes tahun berjalan |
| 4 | **Pagu Pendapatan** | Pendapatan per rekening & sumber dana (RAP + rinci), SILPA/pembiayaan → menjadi **pagu** per sumber dana |
| 5 | **Belanja** | Bidang → Sub Bidang → Kegiatan → Paket → RAB → RAB Rinci (volume × satuan × harga, sumber dana), pelaksana, lokasi, keluaran |
| 6 | **Cek & Simulasi** | Sisa pagu per sumber dana · Surplus/defisit · Cek aturan (lihat bawah) · Bandingkan 2 skenario · "Bagaimana jika" (ubah pagu X%) |
| 7 | **Cetak & Ekspor** | Ringkasan APBDes, RAB per kegiatan, rekap per bidang & per sumber dana, **daftar isian siap-ketik ke Siskeudes**, ekspor Excel & PDF untuk dibagikan saat rapat; berita acara/notulen sederhana hasil musyawarah |
| 8 | **Arsip** | Simpan/buka file per tahun, salin skenario, backup, kunci skenario yang sudah disepakati ("Final – siap input") |

### Cek otomatis (peringatan, bukan pemblokir)
- Belanja per sumber dana ≤ pagu sumber dana; total belanja vs pendapatan + pembiayaan.
- Batas porsi belanja (mis. maks. 30% belanja untuk penghasilan tetap, tunjangan & operasional pemerintah desa
  — nilai batas **bisa diatur** sesuai Perbup/aturan tahun berjalan, bukan ditanam mati).
- Earmark Dana Desa (mis. BLT, ketahanan pangan, stunting) dengan persentase yang bisa diatur.
- Kegiatan tanpa rincian, rincian bernilai 0, kode rekening belum dipilih.

### Teknologi
C# .NET 8 **WPF** (MVVM), **SQLite** satu file per desa/tahun (mudah dibawa di flashdisk),
laporan PDF (QuestPDF) & Excel (ClosedXML), installer Windows (Inno Setup). Tanpa internet, tanpa server.

### Tabel inti (draf)
`Desa, Perangkat, Referensi*(Bidang, SubBidang, KegiatanRef, Rekening, SumberDana, Satuan, StandarHarga),
TahunAnggaran, Skenario(Nama, Status: Draft/Disepakati, DibuatDari), Usulan(Perencanaan),
Kegiatan, Paket, AnggaranRinci(Jenis: Pendapatan/Belanja/Pembiayaan, KodeRek, PaketId?, Uraian, Vol, Satuan, Harga, SumberDana),
AturanBatas(Nama, Persen, Dasar), RiwayatPerubahan`.

### Urutan pengerjaan
1. Kerangka aplikasi, database, referensi standar (bidang/sub bidang/rekening/sumber dana).
2. Data Umum Desa + Pagu Pendapatan.
3. Belanja (kegiatan → paket → RAB rinci) dengan total & sisa pagu langsung terlihat.
4. Skenario (salin, bandingkan) + cek otomatis.
5. Cetak/ekspor + daftar siap-ketik ke Siskeudes + mode presentasi.
6. Perencanaan (usulan → RKPDes).

### Perlu dipastikan
- Daftar **referensi** (kode rekening, kegiatan referensi, sumber dana) persis seperti Siskeudes Banjarnegara 2026:
  bisa diambil dari menu Laporan → Parameter di Siskeudes asli (cetak/ekspor), atau diisi bertahap.
- Apakah Siskeudes menerima **impor data anggaran** dari file (menu "Impor Data Anggaran" ada tetapi nonaktif
  untuk operator) — jika formatnya diketahui, ekspor langsung bisa dibuat; jika tidak, cukup daftar siap-ketik.
