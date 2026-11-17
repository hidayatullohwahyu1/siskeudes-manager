namespace RancangAPBDes.Core;

/// <summary>
/// Referensi bawaan. Bidang & sub bidang mengikuti Permendagri 20/2018.
/// Kode rekening dan referensi kegiatan adalah CONTOH awal: cocokkan dengan Siskeudes
/// (Laporan → Parameter) lalu perbaiki atau impor dari Excel.
/// </summary>
public static class DefaultReferensi
{
    public static Referensi Buat()
    {
        var r = new Referensi();
        IsiBidang(r);
        IsiSumberDana(r);
        IsiRekening(r);
        IsiKegiatan(r);
        IsiSatuan(r);
        IsiAturan(r);
        return r;
    }

    public static readonly (string Kode, string Nama)[] Bidang =
    {
        ("01", "Bidang Penyelenggaraan Pemerintahan Desa"),
        ("02", "Bidang Pelaksanaan Pembangunan Desa"),
        ("03", "Bidang Pembinaan Kemasyarakatan Desa"),
        ("04", "Bidang Pemberdayaan Masyarakat Desa"),
        ("05", "Bidang Penanggulangan Bencana, Keadaan Darurat dan Mendesak Desa"),
    };

    public static readonly (string Kode, string Nama)[] SubBidang =
    {
        ("01.01", "Penyelenggaraan Belanja Siltap, Tunjangan dan Operasional Pemerintahan Desa"),
        ("01.02", "Penyediaan Sarana Prasarana Pemerintahan Desa"),
        ("01.03", "Pengelolaan Administrasi Kependudukan, Pencatatan Sipil, Statistik dan Kearsipan"),
        ("01.04", "Penyelenggaraan Tata Praja Pemerintahan, Perencanaan, Keuangan dan Pelaporan"),
        ("01.05", "Sub Bidang Pertanahan"),
        ("02.01", "Sub Bidang Pendidikan"),
        ("02.02", "Sub Bidang Kesehatan"),
        ("02.03", "Sub Bidang Pekerjaan Umum dan Penataan Ruang"),
        ("02.04", "Sub Bidang Kawasan Permukiman"),
        ("02.05", "Sub Bidang Kehutanan dan Lingkungan Hidup"),
        ("02.06", "Sub Bidang Perhubungan, Komunikasi dan Informatika"),
        ("02.07", "Sub Bidang Energi dan Sumber Daya Mineral"),
        ("02.08", "Sub Bidang Pariwisata"),
        ("03.01", "Sub Bidang Ketenteraman, Ketertiban Umum dan Pelindungan Masyarakat"),
        ("03.02", "Sub Bidang Kebudayaan dan Keagamaan"),
        ("03.03", "Sub Bidang Kepemudaan dan Olah Raga"),
        ("03.04", "Sub Bidang Kelembagaan Masyarakat"),
        ("04.01", "Sub Bidang Kelautan dan Perikanan"),
        ("04.02", "Sub Bidang Pertanian dan Peternakan"),
        ("04.03", "Sub Bidang Peningkatan Kapasitas Aparatur Desa"),
        ("04.04", "Sub Bidang Pemberdayaan Perempuan, Perlindungan Anak dan Keluarga"),
        ("04.05", "Sub Bidang Koperasi, Usaha Mikro Kecil dan Menengah (UMKM)"),
        ("04.06", "Sub Bidang Dukungan Penanaman Modal"),
        ("04.07", "Sub Bidang Perdagangan dan Perindustrian"),
        ("05.01", "Sub Bidang Penanggulangan Bencana"),
        ("05.02", "Sub Bidang Keadaan Darurat"),
        ("05.03", "Sub Bidang Keadaan Mendesak"),
    };

    public static readonly (string Kode, string Nama)[] SumberDana =
    {
        ("PAD", "Pendapatan Asli Desa"),
        ("DDS", "Dana Desa"),
        ("ADD", "Alokasi Dana Desa"),
        ("PBH", "Bagi Hasil Pajak dan Retribusi Daerah"),
        ("PBP", "Bantuan Keuangan Provinsi"),
        ("PBK", "Bantuan Keuangan Kabupaten/Kota"),
        ("DLL", "Lain-lain Pendapatan Desa yang Sah"),
    };

    public static readonly (string Kode, string Nama)[] Rekening =
    {
        // Pendapatan
        ("4.1.1.01", "Hasil Usaha BUM Desa"),
        ("4.1.2.01", "Pengelolaan Tanah Kas Desa"),
        ("4.1.2.08", "Pemanfaatan Sarana/Prasarana Olahraga"),
        ("4.1.3.01", "Swadaya, Partisipasi dan Gotong Royong"),
        ("4.1.4.90", "Lain-Lain Pendapatan Asli Desa"),
        ("4.2.1.01", "Dana Desa"),
        ("4.2.2.01", "Bagi Hasil Pajak dan Retribusi Daerah Kabupaten/Kota"),
        ("4.2.3.01", "Alokasi Dana Desa"),
        ("4.2.4.01", "Bantuan Keuangan dari APBD Provinsi"),
        ("4.2.5.01", "Bantuan Keuangan dari APBD Kabupaten/Kota"),
        ("4.3.9.01", "Lain-lain Pendapatan Desa yang Sah"),
        // Belanja Pegawai
        ("5.1.1.01", "Penghasilan Tetap Kepala Desa"),
        ("5.1.1.02", "Penghasilan Tetap Perangkat Desa"),
        ("5.1.2.01", "Tunjangan Kepala Desa"),
        ("5.1.2.02", "Tunjangan Perangkat Desa"),
        ("5.1.3.01", "Jaminan Sosial Kepala Desa dan Perangkat Desa"),
        ("5.1.4.01", "Tunjangan BPD"),
        // Belanja Barang dan Jasa
        ("5.2.1.01", "Belanja Perlengkapan Alat Tulis Kantor dan Benda Pos"),
        ("5.2.1.02", "Belanja Perlengkapan Alat-alat Listrik"),
        ("5.2.1.03", "Belanja Perlengkapan Alat-alat Rumah Tangga/Peralatan dan Bahan Kebersihan"),
        ("5.2.1.04", "Belanja Bahan Bakar Minyak/Gas"),
        ("5.2.1.05", "Belanja Perlengkapan Cetak/Penggandaan"),
        ("5.2.1.06", "Belanja Makanan dan Minuman Rapat"),
        ("5.2.1.07", "Belanja Bahan/Material"),
        ("5.2.1.99", "Belanja Barang Perlengkapan Lainnya"),
        ("5.2.2.01", "Belanja Jasa Honorarium Tim yang Melaksanakan Kegiatan"),
        ("5.2.2.02", "Belanja Jasa Honorarium Narasumber/Pembicara"),
        ("5.2.2.03", "Belanja Insentif RT/RW"),
        ("5.2.2.04", "Belanja Upah Tenaga Kerja"),
        ("5.2.3.01", "Belanja Perjalanan Dinas Dalam Kabupaten/Kota"),
        ("5.2.3.02", "Belanja Perjalanan Dinas Luar Kabupaten/Kota"),
        ("5.2.4.01", "Belanja Jasa Sewa Bangunan/Gedung/Ruang"),
        ("5.2.4.02", "Belanja Jasa Sewa Peralatan/Perlengkapan"),
        ("5.2.5.01", "Belanja Jasa Langganan Listrik"),
        ("5.2.5.02", "Belanja Jasa Langganan Telepon dan Internet"),
        ("5.2.5.03", "Belanja Jasa Langganan Majalah/Surat Kabar"),
        ("5.2.6.01", "Belanja Pemeliharaan"),
        ("5.2.7.01", "Belanja Barang dan Jasa yang Diserahkan kepada Masyarakat"),
        // Belanja Modal
        ("5.3.1.01", "Belanja Modal Pengadaan Tanah"),
        ("5.3.2.01", "Belanja Modal Peralatan, Mesin dan Alat Berat"),
        ("5.3.3.01", "Belanja Modal Kendaraan"),
        ("5.3.4.01", "Belanja Modal Gedung, Bangunan dan Taman"),
        ("5.3.5.01", "Belanja Modal Jalan, Prasarana Jalan dan Jembatan"),
        ("5.3.6.01", "Belanja Modal Irigasi/Embung/Air Sungai/Drainase/Air Limbah/Persampahan"),
        ("5.3.7.01", "Belanja Modal Jaringan/Instalasi"),
        ("5.3.9.01", "Belanja Modal Pengadaan Aset Lainnya"),
        // Belanja Tak Terduga
        ("5.4.1.01", "Belanja Tak Terduga"),
        // Pembiayaan
        ("6.1.1.01", "SILPA Tahun Sebelumnya"),
        ("6.1.2.01", "Pencairan Dana Cadangan"),
        ("6.1.3.01", "Hasil Penjualan Kekayaan Desa yang Dipisahkan"),
        ("6.2.1.01", "Pembentukan Dana Cadangan"),
        ("6.2.2.01", "Penyertaan Modal Desa"),
    };

    public static readonly (string Kode, string Nama)[] Kegiatan =
    {
        ("01.01.01", "Penyediaan Penghasilan Tetap dan Tunjangan Kepala Desa"),
        ("01.01.02", "Penyediaan Penghasilan Tetap dan Tunjangan Perangkat Desa"),
        ("01.01.03", "Penyediaan Jaminan Sosial bagi Kepala Desa dan Perangkat Desa"),
        ("01.01.04", "Penyediaan Operasional Pemerintah Desa (ATK, Honor PKPKD dan PPKD dll)"),
        ("01.01.05", "Penyediaan Tunjangan BPD"),
        ("01.01.06", "Penyediaan Operasional BPD (rapat, ATK, Makan Minum, Pakaian Seragam, Listrik dll)"),
        ("01.01.07", "Penyediaan Insentif/Operasional RT/RW"),
    };

    public static readonly string[] Satuan =
    {
        "OB (Orang/Bulan)", "Orang", "OH (Orang/Hari)", "OK (Orang/Kegiatan)", "Bulan", "Tahun", "Hari",
        "Kali", "Kegiatan", "Paket", "Unit", "Buah", "Set", "Lembar", "Rim", "Dos", "Kotak", "Kg", "Liter",
        "Meter", "M2", "M3", "Titik", "Lokasi", "KK", "Ls (Lumpsum)"
    };

    static void IsiBidang(Referensi r)
    {
        foreach (var (k, n) in Bidang) r.Bidang.Add(new RefBidang { Kode = k, Nama = n });
        foreach (var (k, n) in SubBidang) r.SubBidang.Add(new RefSubBidang { Kode = k, Nama = n });
    }

    static void IsiSumberDana(Referensi r)
    {
        foreach (var (k, n) in SumberDana) r.SumberDana.Add(new RefSumberDana { Kode = k, Nama = n });
    }

    static void IsiRekening(Referensi r)
    {
        foreach (var (k, n) in Rekening) r.Rekening.Add(new RefRekening { Kode = k, Nama = n });
    }

    static void IsiKegiatan(Referensi r)
    {
        foreach (var (k, n) in Kegiatan) r.Kegiatan.Add(new RefKegiatan { Kode = k, Nama = n });
    }

    static void IsiSatuan(Referensi r)
    {
        foreach (var s in Satuan) r.Satuan.Add(new RefSatuan { Nama = s });
    }

    static void IsiAturan(Referensi r)
    {
        r.Aturan.Add(new AturanBatas
        {
            Aktif = true,
            Nama = "Siltap, tunjangan & operasional pemerintah desa (contoh: maks. 30% belanja)",
            Jenis = "Maksimal", Persen = 30, AwalanSubBidang = "01.01", Dasar = "Total Belanja"
        });
        r.Aturan.Add(new AturanBatas
        {
            Aktif = false,
            Nama = "BLT Dana Desa (contoh – sesuaikan persen dengan aturan tahun berjalan)",
            Jenis = "Minimal", Persen = 15, AwalanSubBidang = "05.03", SumberDana = "DDS", Dasar = "Pagu Sumber Dana"
        });
        r.Aturan.Add(new AturanBatas
        {
            Aktif = false,
            Nama = "Ketahanan pangan dari Dana Desa (contoh – sesuaikan persen dengan aturan tahun berjalan)",
            Jenis = "Minimal", Persen = 20, AwalanSubBidang = "04.02", SumberDana = "DDS", Dasar = "Pagu Sumber Dana"
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
