namespace RancangAPBDes.Core;

/// <summary>
/// Referensi bawaan. Format kode mengikuti cetakan Siskeudes: Bidang "1", Sub Bidang "1.1", Kegiatan "1.1.01",
/// Rekening "5.3.5.01". Kode rekening dan kegiatan diambil dari APBDes yang sudah berjalan; lengkapi atau
/// perbaiki lewat menu Parameter (bisa impor dari Excel).
/// </summary>
public static class DefaultReferensi
{
    public static Referensi Buat()
    {
        var r = new Referensi();
        foreach (var (k, n) in Bidang) r.Bidang.Add(new RefBidang { Kode = k, Nama = n });
        foreach (var (k, n) in SubBidang) r.SubBidang.Add(new RefSubBidang { Kode = k, Nama = n });
        foreach (var (k, n) in SumberDana) r.SumberDana.Add(new RefSumberDana { Kode = k, Nama = n });
        foreach (var (k, n) in Rekening) r.Rekening.Add(new RefRekening { Kode = k, Nama = n });
        foreach (var (k, n) in Kegiatan) r.Kegiatan.Add(new RefKegiatan { Kode = k, Nama = n });
        foreach (var s in Satuan) r.Satuan.Add(new RefSatuan { Nama = s });
        IsiAturan(r);
        return r;
    }

    public static readonly (string Kode, string Nama)[] Bidang =
    {
        ("1", "Bidang Penyelenggaraan Pemerintahan Desa"),
        ("2", "Bidang Pelaksanaan Pembangunan Desa"),
        ("3", "Bidang Pembinaan Kemasyarakatan Desa"),
        ("4", "Bidang Pemberdayaan Masyarakat Desa"),
        ("5", "Bidang Penanggulangan Bencana, Keadaan Darurat dan Mendesak Desa"),
    };

    public static readonly (string Kode, string Nama)[] SubBidang =
    {
        ("1.1", "Penyelenggaraan Belanja Siltap, Tunjangan dan Operasional Pemerintahan Desa"),
        ("1.2", "Sarana dan Prasarana Pemerintahan Desa"),
        ("1.3", "Administrasi Kependudukan, Pencatatan Sipil, Statistik dan Kearsipan"),
        ("1.4", "Tata Praja Pemerintahan, Perencanaan, Keuangan dan Pelaporan"),
        ("1.5", "Pertanahan"),
        ("2.1", "Pendidikan"),
        ("2.2", "Kesehatan"),
        ("2.3", "Pekerjaan Umum dan Penataan Ruang"),
        ("2.4", "Kawasan Permukiman"),
        ("2.5", "Kehutanan dan Lingkungan Hidup"),
        ("2.6", "Perhubungan, Komunikasi dan Informatika"),
        ("2.7", "Energi dan Sumber Daya Mineral"),
        ("2.8", "Pariwisata"),
        ("3.1", "Ketenteraman, Ketertiban Umum dan Pelindungan Masyarakat"),
        ("3.2", "Kebudayaan dan Keagamaan"),
        ("3.3", "Kepemudaan dan Olah Raga"),
        ("3.4", "Kelembagaan Masyarakat"),
        ("4.1", "Kelautan dan Perikanan"),
        ("4.2", "Pertanian dan Peternakan"),
        ("4.3", "Peningkatan Kapasitas Aparatur Desa"),
        ("4.4", "Pemberdayaan Perempuan, Perlindungan Anak dan Keluarga"),
        ("4.5", "Koperasi, Usaha Mikro Kecil dan Menengah (UMKM)"),
        ("4.6", "Dukungan Penanaman Modal"),
        ("4.7", "Perdagangan dan Perindustrian"),
        ("5.1", "Penanggulangan Bencana"),
        ("5.2", "Keadaan Darurat"),
        ("5.3", "Keadaan Mendesak"),
    };

    public static readonly (string Kode, string Nama)[] SumberDana =
    {
        ("PAD", "Pendapatan Asli Desa"),
        ("DDS", "Dana Desa (APBN)"),
        ("ADD", "Alokasi Dana Desa"),
        ("PBH", "Bagi Hasil Pajak dan Retribusi Daerah"),
        ("PBP", "Bantuan Keuangan Provinsi"),
        ("PBK", "Bantuan Keuangan Kabupaten/Kota"),
        ("SWD", "Swadaya Masyarakat"),
        ("DLL", "Pendapatan Lain-lain"),
    };

    public static readonly string[] Satuan =
    {
        "orang", "bulan", "tahun", "hari", "kali", "kegiatan", "paket", "Ls", "unit", "buah", "set", "lembar",
        "rim", "dos", "kotak", "sak", "batang", "rol", "kg", "liter", "m", "m2", "m3", "titik", "lokasi", "KK",
        "OB", "OH", "OK", "hok"
    };
    public static readonly (string Kode, string Nama)[] Rekening =
    {
        ("4", "PENDAPATAN"),
        ("4.1", "Pendapatan Asli Desa"),
        ("4.1.2", "Hasil Aset Desa"),
        ("4.1.2.01", "Pengelolaan Tanah Kas Desa"),
        ("4.1.2.08", "Pemanfaatan Sarana/Prasarana Olahraga"),
        ("4.1.4", "Lain-Lain Pendapatan Asli Desa"),
        ("4.1.4.90", "Lain-Lain Pendapatan Asli Desa"),
        ("4.2", "Pendapatan Transfer"),
        ("4.2.1", "Dana Desa"),
        ("4.2.1.01", "Dana Desa"),
        ("4.2.2", "Bagi Hasil Pajak dan Retribusi"),
        ("4.2.2.01", "Bagi Hasil Pajak dan Retribusi Daerah Kabupaten/Kota"),
        ("4.2.3", "Alokasi Dana Desa"),
        ("4.2.3.01", "Alokasi Dana Desa"),
        ("4.2.4", "Bantuan Keuangan Provinsi"),
        ("4.2.4.01", "Bantuan Keuangan dari APBD Provinsi"),
        ("4.2.5", "Bantuan Keuangan Kabupaten/Kota"),
        ("4.2.5.01", "Bantuan Keuangan dari APBD Kabupaten/Kota"),
        ("4.3", "Pendapatan Lain-lain"),
        ("4.3.6", "Bunga Bank"),
        ("4.3.6.01", "Bunga Bank"),
        ("5", "BELANJA"),
        ("5.1", "Belanja Pegawai"),
        ("5.1.1", "Penghasilan Tetap dan Tunjangan Kepala Desa"),
        ("5.1.1.01", "Penghasilan Tetap Kepala Desa"),
        ("5.1.1.02", "Tunjangan Kepala Desa"),
        ("5.1.1.90", "Tambahan Tunjangan Kepala Desa"),
        ("5.1.2", "Penghasilan Tetap dan Tunjangan Perangkat Desa"),
        ("5.1.2.01", "Penghasilan Tetap Perangkat Desa"),
        ("5.1.2.02", "Tunjangan Perangkat Desa"),
        ("5.1.2.90", "Tambahan Tunjangan Perangkat Desa"),
        ("5.1.3", "Jaminan Sosial Kepala Desa dan Perangkat Desa"),
        ("5.1.3.01", "Jaminan Kesehatan Kepala Desa"),
        ("5.1.3.02", "Jaminan Kesehatan Perangkat Desa"),
        ("5.1.3.04", "Jaminan Ketenagakerjaan Perangkat Desa"),
        ("5.1.4", "Tunjangan BPD"),
        ("5.1.4.01", "Tunjangan Kedudukan BPD"),
        ("5.1.4.02", "Tunjangan Kinerja BPD"),
        ("5.2", "Belanja Barang dan Jasa"),
        ("5.2.1", "Belanja Barang Perlengkapan"),
        ("5.2.1.01", "Belanja Alat Tulis Kantor dan Benda Pos"),
        ("5.2.1.03", "Belanja Perlengkapan Alat Rumah Tangga dan Bahan Kebersihan"),
        ("5.2.1.05", "Belanja Barang Cetak dan Penggandaan"),
        ("5.2.1.06", "Belanja Barang Konsumsi (Makan/Minum)"),
        ("5.2.1.08", "Belanja Bendera/Umbul-umbul/Spanduk"),
        ("5.2.1.99", "Belanja Barang Perlengkapan Lainnya"),
        ("5.2.2", "Belanja Jasa Honorarium"),
        ("5.2.2.02", "Belanja Jasa Honorarium Pembantu Tugas Umum Desa/Operator"),
        ("5.2.2.04", "Belanja Jasa Honorarium Tenaga Ahli/Profesi/Konsultan/Narasumber"),
        ("5.2.2.05", "Belanja Jasa Honorarium Petugas"),
        ("5.2.2.06", "Belanja Jasa Honorarium PKPKD dan PPKD"),
        ("5.2.2.08", "Belanja Jasa Uang Saku Pelatihan/Seminar/Bimbingan Teknis"),
        ("5.2.2.99", "Belanja Jasa Honorarium Lainnya"),
        ("5.2.3", "Belanja Perjalanan Dinas"),
        ("5.2.3.01", "Belanja Perjalanan Dinas Dalam Kabupaten/Kota"),
        ("5.2.3.02", "Belanja Perjalanan Dinas Luar Kabupaten/Kota"),
        ("5.2.4", "Belanja Jasa Sewa"),
        ("5.2.4.02", "Belanja Jasa Sewa Peralatan/Perlengkapan"),
        ("5.2.5", "Belanja Operasional Perkantoran"),
        ("5.2.5.01", "Belanja Jasa Langganan Listrik"),
        ("5.2.5.03", "Belanja Jasa Langganan Majalah/Surat Kabar"),
        ("5.2.5.05", "Belanja Jasa Langganan Internet"),
        ("5.2.5.07", "Belanja Jasa Perpanjangan Ijin/Pajak"),
        ("5.2.5.08", "Belanja Insentif/Operasional RT/RW"),
        ("5.2.5.90", "Belanja Jaminan Sosial Ketenagakerjaan Kelembagaan Desa"),
        ("5.2.5.99", "Belanja Operasional Perkantoran lainnya"),
        ("5.2.6", "Belanja Pemeliharaan"),
        ("5.2.6.03", "Belanja Pemeliharaan Peralatan"),
        ("5.2.7", "Belanja Barang dan Jasa yang Diserahkan kepada Masyarakat"),
        ("5.2.7.05", "Belanja Bantuan Bibit Tanaman/Hewan/Ikan"),
        ("5.2.7.99", "Belanja Barang untuk Diserahkan kepada Masyarakat Lainnya"),
        ("5.3", "Belanja Modal"),
        ("5.3.2", "Belanja Modal Pengadaan Peralatan, Mesin dan Alat Berat"),
        ("5.3.2.02", "Belanja Modal Peralatan Elektronik dan Alat Studio"),
        ("5.3.2.03", "Belanja Modal Peralatan Komputer"),
        ("5.3.2.04", "Belanja Modal Peralatan Mebelair dan Aksesoris Ruangan"),
        ("5.3.3", "Belanja Modal Kendaraan"),
        ("5.3.3.02", "Belanja Modal Kendaraan Darat Bermotor"),
        ("5.3.4", "Belanja Modal Gedung, Bangunan dan Taman"),
        ("5.3.4.01", "Belanja Modal Gedung, Bangunan, Taman Honor Pelaksana Kegiatan"),
        ("5.3.4.02", "Belanja Modal Gedung, Bangunan, Taman Upah Tenaga Kerja"),
        ("5.3.4.03", "Belanja Modal Gedung, Bangunan, Taman Bahan Baku/Material"),
        ("5.3.4.05", "Belanja Modal Gedung, Bangunan, Taman Administrasi Kegiatan"),
        ("5.3.5", "Belanja Modal Jalan/Prasarana Jalan"),
        ("5.3.5.01", "Belanja Modal Jalan Honor Tim Pelaksana Kegiatan"),
        ("5.3.5.02", "Belanja Modal Jalan Upah Tenaga Kerja"),
        ("5.3.5.03", "Belanja Modal Jalan Bahan Baku/Material"),
        ("5.3.5.04", "Belanja Modal Jalan Sewa Peralatan"),
        ("5.3.5.05", "Belanja Modal Jalan Administrasi Kegiatan"),
        ("5.3.9", "Belanja Modal Lainnya"),
        ("5.3.9.02", "Belanja Khusus Olahraga"),
        ("5.3.9.99", "Belanja Modal Lainnya"),
        ("5.4", "Belanja Tidak Terduga"),
        ("5.4.1", "Belanja Tidak Terduga"),
        ("5.4.1.01", "Belanja Tidak Terduga"),
        ("6", "PEMBIAYAAN"),
        ("6.1", "Penerimaan Pembiayaan"),
        ("6.1.1", "SILPA Tahun Sebelumnya"),
        ("6.1.1.01", "SILPA Tahun Sebelumnya"),
        ("6.2", "Pengeluaran Pembiayaan"),
        ("6.2.2", "Penyertaan Modal Desa"),
        ("6.2.2.01", "Penyertaan Modal Desa"),
    };

    public static readonly (string Kode, string Nama)[] Kegiatan =
    {
        ("1.1.01", "Penyediaan Penghasilan Tetap dan Tunjangan Kepala Desa"),
        ("1.1.02", "Penyediaan Penghasilan Tetap dan Tunjangan Perangkat Desa"),
        ("1.1.03", "Penyediaan Jaminan Sosial bagi Kepala Desa dan Perangkat Desa"),
        ("1.1.04", "Penyediaan Operasional Pemerintah Desa (ATK, Honor PKPKD dan PPKD dll)"),
        ("1.1.05", "Penyediaan Tunjangan BPD"),
        ("1.1.06", "Penyediaan Operasional BPD (rapat, ATK, Makan Minum, Pakaian Seragam, Listrik dll)"),
        ("1.1.07", "Penyediaan Insentif/Operasional RT/RW"),
        ("1.1.08", "Penyediaan Operasional Pemerintah Desa yang bersumber dari Dana Desa"),
        ("1.1.92", "Penerimaan Lain Kepala Desa dan Perangkat Desa"),
        ("1.1.93", "Intensifikasi dan/atau Eksentifikasi Pajak Daerah"),
        ("1.2.01", "Penyediaan Sarana (Aset Tetap) Perkantoran/Pemerintahan"),
        ("1.2.03", "Pembangunan/Rehabilitasi/Peningkatan Gedung/Prasarana Kantor Desa"),
        ("1.3.02", "Penyusunan, Pendataan, dan Pemutakhiran Profil Desa"),
        ("1.4.01", "Penyelenggaraan Musyawarah Perencanaan Desa/Pembahasan APBDes (Reguler)"),
        ("1.4.03", "Penyusunan Dokumen Perencanaan Desa (RPJMDesa/RKPDesa dll)"),
        ("1.4.07", "Penyusunan Laporan Kepala Desa, LPPDesa dan Informasi Kepada Masyarakat"),
        ("1.4.97", "Sosialisasi Berbagai Peraturan Perundang-undangan di Tingkat Desa"),
        ("1.5.06", "Adminstrasi Pajak Bumi dan Bangunan (PBB)"),
        ("1.5.94", "Pengelolaan Tanah Kas Desa"),
        ("2.2.02", "Penyelenggaraan Posyandu (Makanan Tambahan, Kelas Bumil, Lansia, Insentif)"),
        ("2.3.10", "Pembangunan/Rehabilitas/Peningkatan/Pengerasan Jalan Desa"),
        ("2.3.11", "Pembangunan/Rehabilitasi/Peningkatan/Pengerasan Jalan Lingkungan Permukiman"),
        ("2.3.12", "Pembangunan/Rehabilitasi/Peningkatan/Pengerasan Jalan Usaha Tani"),
        ("2.3.14", "Pembangunan/Rehabilitasi/Peningkatan Prasarana Jalan Desa (Gorong, selokan dll)"),
        ("2.3.93", "Pembentukan/Pengembangan Sarana dan Prasarana BUM Desa/Penguatan Permodalan BUM Desa"),
        ("2.4.14", "Pembangunan/Rehabilitasi/Peningkatan Fasilitas Jamban Umum/MCK umum, dll"),
        ("2.4.15", "Pembangunan/Rehabilitasi/Peningkatan Fasilitas Pengelolaan Sampah"),
        ("2.5.90", "Penghijauan/Pelestarian Lingkungan Hidup"),
        ("2.6.02", "Penyelenggaraan Informasi Publik Desa (Poster, Baliho Dll)"),
        ("2.6.92", "Pengadaan/pembangunan/pengembangan/pemeliharaan jaringan internet untuk warga Desa/website Desa/per"),
        ("3.1.90", "Pembinaan keamanan dan ketertiban"),
        ("3.2.03", "Penyelenggaraan Festival Kesenian, Adat/Kebudayaan, dan Keagamaan (HUT RI, Raya Keagamaan dll)"),
        ("3.3.05", "Pembangunan/Rehabilitasi/Peningkatan Sarana dan Prasarana Kepemudaan & Olahraga Milik Desa"),
        ("3.3.06", "Pembinaan Karang Taruna/Klub Kepemudaan/Olahraga Tingkat Desa"),
        ("3.4.02", "Pembinaan LKMD/LPM/LPMD"),
        ("3.4.03", "Pembinaan PKK"),
        ("3.4.95", "Jaminan Sosial Bagi Masyarakat dan Lembaga Kemasyarakatan Desa"),
        ("4.2.90", "Pengelolaan jaringan irigasi/Prduksi Prtanian/prduksi usha prtanian untuk ketahanan pangan dan usha"),
        ("4.3.91", "Peningkatan Kapasitas Lembaga Kemasyarakat Desa/ kader pemberdayaan masyarakat Desa/kader kesehatan"),
        ("4.4.93", "Pemantauan pertumbuhan dan penyediaan makanan sehat untuk peningkatan gizi bagi balita dan anak sekolah"),
        ("4.4.96", "Bantuan insentif untuk kader kesehatan masyarakat/ bantuan insentif guru Pendidikan Anak Usia Dini/"),
        ("4.5.02", "Pengembangan Sarana Prasarana Usaha Mikro, Kecil, Menengah dan Koperasi"),
        ("5.1.00", "Kegiatan Penanggulangan Bencana"),
        ("5.2.00", "Penanganan Keadaan Darurat"),
        ("5.3.00", "Penanganan Keadaan Mendesak"),
    };

    static void IsiAturan(Referensi r)
    {
        r.Aturan.Add(new AturanBatas
        {
            Aktif = true,
            Nama = "Siltap, tunjangan & operasional pemerintah desa (contoh: maks. 30% belanja)",
            Jenis = "Maksimal", Persen = 30, AwalanSubBidang = "1.1", Dasar = "Total Belanja"
        });
        r.Aturan.Add(new AturanBatas
        {
            Aktif = false,
            Nama = "BLT Dana Desa (contoh – sesuaikan persen dengan aturan tahun berjalan)",
            Jenis = "Minimal", Persen = 15, AwalanSubBidang = "5.3", SumberDana = "DDS", Dasar = "Pagu Sumber Dana"
        });
        r.Aturan.Add(new AturanBatas
        {
            Aktif = false,
            Nama = "Ketahanan pangan dari Dana Desa (contoh – sesuaikan persen dengan aturan tahun berjalan)",
            Jenis = "Minimal", Persen = 20, AwalanSubBidang = "4.2", SumberDana = "DDS", Dasar = "Pagu Sumber Dana"
        });
    }

    /// <summary>Menambahkan isi bawaan yang belum ada (berdasarkan kode), tanpa menimpa yang sudah diubah.</summary>
    public static int Lengkapi(Referensi r)
    {
        int n = 0;
        foreach (var (k, nm) in Bidang)
            if (!r.Bidang.Any(x => x.Kode == k)) { r.Bidang.Add(new RefBidang { Kode = k, Nama = nm }); n++; }
        foreach (var (k, nm) in SubBidang)
            if (!r.SubBidang.Any(x => x.Kode == k)) { r.SubBidang.Add(new RefSubBidang { Kode = k, Nama = nm }); n++; }
        foreach (var (k, nm) in SumberDana)
            if (!r.SumberDana.Any(x => x.Kode == k)) { r.SumberDana.Add(new RefSumberDana { Kode = k, Nama = nm }); n++; }
        foreach (var (k, nm) in Rekening)
            if (!r.Rekening.Any(x => x.Kode == k)) { r.Rekening.Add(new RefRekening { Kode = k, Nama = nm }); n++; }
        foreach (var (k, nm) in Kegiatan)
            if (!r.Kegiatan.Any(x => x.Kode == k)) { r.Kegiatan.Add(new RefKegiatan { Kode = k, Nama = nm }); n++; }
        foreach (var s in Satuan)
            if (!r.Satuan.Any(x => string.Equals(x.Nama, s, StringComparison.OrdinalIgnoreCase))) { r.Satuan.Add(new RefSatuan { Nama = s }); n++; }
        return n;
    }
}