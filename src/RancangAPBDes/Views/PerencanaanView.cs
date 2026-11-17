using System.Windows;
using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>
/// Data Entri › Perencanaan › RPJMDes dan RKPDes.
/// RPJMDes: Periode & Visi › Misi › Kegiatan RPJMDes. RKPDes (per tahun): Pagu Indikatif › Kegiatan RKPDes › kirim ke APBDes.
/// </summary>
public class PerencanaanView : UserControl
{
    static readonly string[] DaftarPola = { "Swakelola", "Kerjasama Antar Desa", "Kerjasama dengan Pihak Ketiga", "Penyedia" };
    static readonly string[] DaftarStatus = { "Disepakati", "Cadangan" };

    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = Gaya.Nav();
    readonly ContentControl _isi = new() { Focusable = false };
    readonly List<TabControl> _bagian = new();
    readonly List<HalamanData> _semua = new();

    DataPerencanaan R => S.Perencanaan;
    int Tahun => S.SkenarioAktif?.Tahun ?? S.TahunAktif;

    public PerencanaanView()
    {
        _bagian.Add(BangunRpjm());
        _bagian.Add(BangunRkp());
        _nav.Items.Add(Gaya.ItemNav("1. RPJMDes"));
        _nav.Items.Add(Gaya.ItemNav("2. RKPDes"));
        _nav.SelectionChanged += (_, _) =>
        {
            if (_nav.SelectedIndex < 0) return;
            _isi.Content = _bagian[_nav.SelectedIndex];
            HalamanAktif?.Segarkan();
        };
        _nav.SelectedIndex = 0;
        Content = Gaya.DenganNav(_nav, _isi);
        Loaded += (_, _) => { S.DataDiganti += Segarkan; S.HasilBerubah += Segarkan; Segarkan(); };
        Unloaded += (_, _) =>
        {
            S.DataDiganti -= Segarkan;
            S.HasilBerubah -= Segarkan;
            foreach (var h in _semua) if (h.SedangMengisi) h.Batal();
        };
    }

    void Segarkan() => HalamanAktif?.Segarkan();

    public HalamanData? HalamanAktif =>
        _nav.SelectedIndex < 0 ? null : (_bagian[_nav.SelectedIndex].SelectedItem as TabItem)?.Content as HalamanData;

    public void Buka(int bagian, int tab = 0)
    {
        _nav.SelectedIndex = bagian;
        _bagian[bagian].SelectedIndex = tab;
    }

    TabControl Tab(params (string judul, HalamanData h)[] halaman)
    {
        var tab = Gaya.TabKiri();
        foreach (var (judul, h) in halaman)
        {
            h.BolehUbah = () => true;
            tab.Items.Add(Gaya.Tab(judul, h));
            _semua.Add(h);
        }
        tab.SelectionChanged += (_, e) =>
        {
            if (e.Source == tab) Dispatcher.BeginInvoke(new Action(() => HalamanAktif?.Segarkan()), System.Windows.Threading.DispatcherPriority.Background);
        };
        return tab;
    }

    static IEnumerable<PilihanKode> SemuaRefKegiatan() =>
        AppState.Instance.Referensi.Kegiatan.OrderBy(r => r.Kode, KodeComparer.Instance)
            .Select(r => new PilihanKode(r.Kode.Split('.').Last(), r.Kode + ".", r.Nama));

    static void TerapkanRef(object o, PilihanKode p)
    {
        var kode = p.Kode.TrimEnd('.');
        var sub = kode[..kode.LastIndexOf('.')];
        switch (o)
        {
            case KegiatanRpjm k: k.KodeSubBidang = sub; k.Nama = p.Nama; break;
            case KegiatanRkp k: k.KodeSubBidang = sub; k.Nama = p.Nama; break;
        }
    }

    static string? ValidasiKode(string sub, int no, IEnumerable<(Guid id, string kode)> lain, Guid id)
    {
        if (string.IsNullOrWhiteSpace(sub)) return "Pilih kegiatan dari referensi (tombol … di Kd Kegiatan) atau pilih Sub Bidang.";
        var kode = $"{sub}.{no:00}";
        return lain.Any(x => x.id != id && x.kode == kode) ? $"Kode kegiatan {kode} sudah ada." : null;
    }

    // ================================================================ RPJMDes

    TabControl BangunRpjm()
    {
        var hVisi = new HalamanData
        {
            JudulData = "Periode & Visi RPJMDes",
            TanpaTabel = true,
            Konteks = () => "tekan Ubah untuk mengisi, lalu Simpan",
            Tampilan = () => new List<DataPerencanaan> { R }
        };
        hVisi.Isian.AddRange(new[]
        {
            new Isian { Label = "Tahun Awal", Path = "TahunAwal", Jenis = JenisIsian.Bulat },
            new Isian { Label = "Tahun Akhir", Path = "TahunAkhir", Jenis = JenisIsian.Bulat },
            new Isian { Label = "Nomor Perdes RPJMDes", Path = "NomorPerdes", Penuh = true },
            new Isian { Label = "Visi", Path = "Visi", Jenis = JenisIsian.TeksPanjang, Penuh = true },
        });
        hVisi.Validasi = (o, _) => ((DataPerencanaan)o).TahunAkhir < ((DataPerencanaan)o).TahunAwal ? "Tahun akhir tidak boleh sebelum tahun awal." : null;

        var hMisi = new HalamanData
        {
            JudulData = "Misi",
            Konteks = () => $"RPJMDes {R.Periode}",
            Daftar = () => R.Misi,
            Tampilan = () => R.Misi.OrderBy(m => m.No).ToList(),
            BuatBaru = () => new Misi { No = R.Misi.Count == 0 ? 1 : R.Misi.Max(m => m.No) + 1 },
            Kunci = o => ((Misi)o).No.ToString(),
            PesanHapus = o => $"Hapus misi {((Misi)o).No}?"
        };
        hMisi.Kolom.Add(new Kolom { Judul = "No", Path = "No", Lebar = 50, Format = "00" });
        hMisi.Kolom.Add(new Kolom { Judul = "Uraian Misi", Path = "Uraian", MinLebar = 300 });
        hMisi.Isian.Add(new Isian { Label = "No", Path = "No", Jenis = JenisIsian.Bulat });
        hMisi.Isian.Add(new Isian { Label = "Uraian Misi", Path = "Uraian", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true });

        var hKeg = new HalamanData
        {
            JudulData = "Kegiatan RPJMDes",
            Konteks = () => $"RPJMDes {R.Periode} · {R.KegiatanRpjm.Count} kegiatan · perkiraan biaya {Kalkulasi.Rp(R.KegiatanRpjm.Sum(k => k.PerkiraanBiaya))}",
            Daftar = () => R.KegiatanRpjm,
            Tampilan = () => R.KegiatanRpjm.OrderBy(k => k.Kode, KodeComparer.Instance).ToList(),
            BuatBaru = () => new KegiatanRpjm { Lokasi = S.Desa.NamaDesa, TahunPelaksanaan = Tahun.ToString() },
            Kunci = o => ((KegiatanRpjm)o).Id.ToString(),
            Validasi = (o, _) =>
            {
                var k = (KegiatanRpjm)o;
                var tahunSalah = k.DaftarTahun().Where(t => t < R.TahunAwal || t > R.TahunAkhir).ToList();
                if (tahunSalah.Count > 0) return $"Tahun pelaksanaan {string.Join(", ", tahunSalah)} di luar periode RPJMDes {R.Periode}.";
                return ValidasiKode(k.KodeSubBidang, k.NoUrut, R.KegiatanRpjm.Select(x => (x.Id, x.Kode)), k.Id);
            },
            PesanHapus = o => $"Hapus kegiatan RPJMDes {((KegiatanRpjm)o).Kode} \"{((KegiatanRpjm)o).Nama}\"?"
        };
        hKeg.Kolom.Add(new Kolom("Kd Kegiatan", "Kode", 85));
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", MinLebar = 200, Bintang = 2 });
        hKeg.Kolom.Add(new Kolom { Judul = "Lokasi", Path = "Lokasi", MinLebar = 90 });
        hKeg.Kolom.Add(new Kolom { Judul = "Volume", Path = "Volume", Lebar = 70, Angka = true });
        hKeg.Kolom.Add(new Kolom("Satuan", "Satuan", 70));
        hKeg.Kolom.Add(new Kolom("Tahun", "TahunPelaksanaan", 100));
        hKeg.Kolom.Add(new Kolom { Judul = "Perkiraan Biaya (Rp)", Path = "PerkiraanBiaya", Lebar = 130, Angka = true, Format = "#,##0" });
        hKeg.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 80));
        hKeg.Isian.AddRange(IsianKegiatan(rkp: false));
        hKeg.TambahTombol($"Masukkan ke RKPDes ›", () => SalinKeRkp(hKeg.Terpilih as KegiatanRpjm), butuhUbah: false, butuhPilih: true,
            tip: "Masukkan kegiatan terpilih ke RKPDes tahun skenario aktif");

        return Tab(("1. Periode & Visi", hVisi), ("2. Misi", hMisi), ("3. Kegiatan RPJMDes", hKeg));
    }

    IEnumerable<Isian> IsianKegiatan(bool rkp)
    {
        var daftar = new List<Isian>
        {
            new()
            {
                Label = "Kd Kegiatan", Path = "NoUrut", Jenis = JenisIsian.Kode, Wajib = true, Format = "00",
                Petunjuk = "Tekan … untuk memilih dari referensi kegiatan (Sub Bidang ikut terisi).",
                DaftarKode = _ => SemuaRefKegiatan(), SetelahPilih = TerapkanRef
            },
            new() { Label = "Sub Bidang", Path = "KodeSubBidang", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.Referensi.SubBidang, TampilPath = "Tampil", NilaiPath = "Kode" },
            new() { Label = "Nama Kegiatan", Path = "Nama", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true },
            new() { Label = "Lokasi", Path = "Lokasi" },
            new() { Label = "Keluaran", Path = "Keluaran" },
            new() { Label = "Volume", Path = "Volume", Jenis = JenisIsian.Angka },
            new() { Label = "Satuan", Path = "Satuan", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
            new() { Label = "Sasaran / Penerima Manfaat", Path = "SasaranManfaat" },
        };
        if (rkp)
        {
            daftar.AddRange(new Isian[]
            {
                new() { Label = "Waktu Pelaksanaan", Path = "Waktu" },
                new() { Label = "Pagu Indikatif (Rp)", Path = "PaguIndikatif", Jenis = JenisIsian.Angka },
                new() { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
                new() { Label = "Pola Pelaksanaan", Path = "Pola", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarPola },
                new() { Label = "Nama Pelaksana", Path = "Pelaksana", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarPelaksana },
                new() { Label = "Prioritas", Path = "Prioritas", Jenis = JenisIsian.Bulat, Petunjuk = "1 = paling utama" },
                new() { Label = "Status", Path = "Status", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarStatus, Petunjuk = "Disepakati = masuk APBDes; Cadangan = menunggu dana" },
            });
        }
        else
        {
            daftar.AddRange(new Isian[]
            {
                new() { Label = "Tahun Pelaksanaan", Path = "TahunPelaksanaan", Petunjuk = "Pisahkan dengan koma, contoh 2027, 2029" },
                new() { Label = "Perkiraan Biaya (Rp)", Path = "PerkiraanBiaya", Jenis = JenisIsian.Angka },
                new() { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
                new() { Label = "Pola Pelaksanaan", Path = "Pola", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarPola },
                new() { Label = "Asal Usulan", Path = "AsalUsulan", Petunjuk = "Dusun / RT / lembaga pengusul", Penuh = true },
            });
        }
        return daftar;
    }

    // ================================================================ RKPDes

    TabControl BangunRkp()
    {
        var hPagu = new HalamanData
        {
            JudulData = "Pagu Indikatif",
            Konteks = () => $"Tahun {Tahun} · total {Kalkulasi.Rp(R.Pagu.Where(p => p.Tahun == Tahun).Sum(p => p.Nilai))} (tahun mengikuti skenario aktif)",
            Daftar = () => R.Pagu,
            Tampilan = () =>
            {
                var list = R.Pagu.Where(p => p.Tahun == Tahun).OrderBy(p => p.SumberDana).ToList();
                foreach (var p in list)
                    p.Terpakai = R.KegiatanRkp.Where(k => k.Tahun == Tahun && k.Status == "Disepakati" && string.Equals(k.SumberDana, p.SumberDana, StringComparison.OrdinalIgnoreCase)).Sum(k => k.PaguIndikatif);
                return list;
            },
            BuatBaru = () => new PaguIndikatif { Tahun = Tahun },
            Kunci = o => ((PaguIndikatif)o).SumberDana,
            Validasi = (o, baru) =>
            {
                var p = (PaguIndikatif)o;
                int sama = R.Pagu.Count(x => x.Tahun == p.Tahun && x.SumberDana == p.SumberDana);
                return (baru ? sama > 0 : sama > 1) ? $"Pagu {p.SumberDana} tahun {p.Tahun} sudah ada." : null;
            },
            PesanHapus = o => $"Hapus pagu indikatif {((PaguIndikatif)o).SumberDana}?"
        };
        hPagu.Kolom.Add(new Kolom("Kode", "SumberDana", 70));
        hPagu.Kolom.Add(new Kolom { Judul = "Sumber Dana", Path = "NamaSumberDana", MinLebar = 160 });
        hPagu.Kolom.Add(new Kolom { Judul = "Pagu Indikatif (Rp)", Path = "Nilai", Lebar = 140, Angka = true, Format = "#,##0", Tebal = true });
        hPagu.Kolom.Add(new Kolom { Judul = "Dipakai RKPDes (Rp)", Path = "Terpakai", Lebar = 140, Angka = true, Format = "#,##0" });
        hPagu.Kolom.Add(new Kolom { Judul = "Sisa (Rp)", Path = "Sisa", Lebar = 130, Angka = true, Format = "#,##0;(#,##0);0", Tebal = true });
        hPagu.Kolom.Add(new Kolom { Judul = "Keterangan", Path = "Keterangan", MinLebar = 120 });
        hPagu.Isian.AddRange(new[]
        {
            new Isian { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
            new Isian { Label = "Pagu Indikatif (Rp)", Path = "Nilai", Jenis = JenisIsian.Angka },
            new Isian { Label = "Keterangan", Path = "Keterangan", Penuh = true, Petunjuk = "Mis. nomor surat pagu indikatif dari kabupaten" },
        });
        hPagu.TambahTombol("Ambil dari Pendapatan Skenario", AmbilPaguDariPendapatan, butuhUbah: false,
            tip: "Isi pagu indikatif dari jumlah pendapatan per sumber dana pada skenario aktif");

        HalamanData hRkp = null!;
        hRkp = new HalamanData
        {
            JudulData = "Kegiatan RKPDes",
            Konteks = () =>
            {
                var dis = R.KegiatanRkp.Where(k => k.Tahun == Tahun && k.Status == "Disepakati").ToList();
                var pagu = R.Pagu.Where(p => p.Tahun == Tahun).Sum(p => p.Nilai);
                return $"Tahun {Tahun} · {dis.Count} disepakati senilai {Kalkulasi.Rp(dis.Sum(k => k.PaguIndikatif))}" + (pagu > 0 ? $" dari pagu {Kalkulasi.Rp(pagu)}" : "");
            },
            Daftar = () => R.KegiatanRkp,
            Tampilan = () => R.KegiatanRkp.Where(k => k.Tahun == Tahun).OrderBy(k => k.Status != "Disepakati").ThenBy(k => k.Kode, KodeComparer.Instance).ToList(),
            BuatBaru = () => new KegiatanRkp { Tahun = Tahun, Lokasi = S.Desa.NamaDesa },
            Kunci = o => ((KegiatanRkp)o).Id.ToString(),
            Validasi = (o, _) =>
            {
                var k = (KegiatanRkp)o;
                return ValidasiKode(k.KodeSubBidang, k.NoUrut, R.KegiatanRkp.Where(x => x.Tahun == k.Tahun).Select(x => (x.Id, x.Kode)), k.Id);
            },
            PesanHapus = o => $"Hapus kegiatan RKPDes {((KegiatanRkp)o).Kode} \"{((KegiatanRkp)o).Nama}\"?"
        };
        hRkp.Kolom.Add(new Kolom("Kd Kegiatan", "Kode", 85));
        hRkp.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", MinLebar = 200, Bintang = 2 });
        hRkp.Kolom.Add(new Kolom { Judul = "Lokasi", Path = "Lokasi", MinLebar = 90 });
        hRkp.Kolom.Add(new Kolom { Judul = "Volume", Path = "Volume", Lebar = 70, Angka = true });
        hRkp.Kolom.Add(new Kolom("Satuan", "Satuan", 70));
        hRkp.Kolom.Add(new Kolom { Judul = "Pagu Indikatif (Rp)", Path = "PaguIndikatif", Lebar = 130, Angka = true, Format = "#,##0", Tebal = true });
        hRkp.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 80));
        hRkp.Kolom.Add(new Kolom { Judul = "Prioritas", Path = "Prioritas", Lebar = 70, Angka = true, Format = "0" });
        hRkp.Kolom.Add(new Kolom("Status", "Status", 95));
        hRkp.Isian.AddRange(IsianKegiatan(rkp: true));
        hRkp.TambahTombol("Ambil dari RPJMDes", () => AmbilDariRpjm(), butuhUbah: false, tip: "Masukkan kegiatan RPJMDes yang direncanakan pada tahun ini");
        hRkp.TambahTombol("Kirim ke Isian Data Anggaran ›", () => KirimKeApbdes(true), butuhUbah: false,
            tip: "Buat kegiatan & paket di skenario aktif dari kegiatan RKPDes yang disepakati");

        return Tab(("1. Pagu Indikatif", hPagu), ("2. Kegiatan RKPDes", hRkp));
    }

    void AmbilPaguDariPendapatan()
    {
        var d = S.Data;
        if (d == null) return;
        var per = d.Pendapatan.Concat(d.PenerimaanPembiayaan).GroupBy(r => r.SumberDana).Where(g => g.Key != "");
        int n = 0;
        foreach (var g in per)
        {
            var p = R.Pagu.FirstOrDefault(x => x.Tahun == Tahun && x.SumberDana == g.Key);
            if (p == null) { p = new PaguIndikatif { Tahun = Tahun, SumberDana = g.Key }; R.Pagu.Add(p); }
            p.Nilai = g.Sum(r => r.Jumlah);
            n++;
        }
        HalamanAktif?.Segarkan();
        Pesan.Info($"{n} pagu indikatif tahun {Tahun} diisi dari pendapatan & penerimaan pembiayaan skenario \"{S.SkenarioAktif?.Nama}\".");
    }

    void SalinKeRkp(KegiatanRpjm? k)
    {
        if (k == null) return;
        if (R.KegiatanRkp.Any(x => x.Tahun == Tahun && (x.IdRpjm == k.Id || x.Kode == k.Kode)))
        {
            Pesan.Info($"Kegiatan {k.Kode} sudah ada di RKPDes {Tahun}.");
            return;
        }
        R.KegiatanRkp.Add(DariRpjm(k));
        Pesan.Info($"Kegiatan {k.Kode} \"{k.Nama}\" dimasukkan ke RKPDes {Tahun}.");
    }

    KegiatanRkp DariRpjm(KegiatanRpjm k) => new()
    {
        IdRpjm = k.Id, Tahun = Tahun, KodeSubBidang = k.KodeSubBidang, NoUrut = k.NoUrut, Nama = k.Nama, Lokasi = k.Lokasi,
        Keluaran = k.Keluaran, Volume = k.Volume, Satuan = k.Satuan, SasaranManfaat = k.SasaranManfaat,
        PaguIndikatif = k.PerkiraanBiaya, SumberDana = k.SumberDana, Pola = k.Pola
    };

    /// <summary>Kegiatan RPJMDes yang direncanakan pada tahun ini dan belum ada di RKPDes.</summary>
    public int AmbilDariRpjm()
    {
        int n = 0;
        foreach (var k in R.KegiatanRpjm.Where(k => k.DaftarTahun().Contains(Tahun)).OrderBy(k => k.Kode, KodeComparer.Instance))
        {
            if (R.KegiatanRkp.Any(x => x.Tahun == Tahun && (x.IdRpjm == k.Id || x.Kode == k.Kode))) continue;
            R.KegiatanRkp.Add(DariRpjm(k));
            n++;
        }
        HalamanAktif?.Segarkan();
        if (!AppState.ModeUji || n == 0)
            Pesan.Info(n == 0 ? $"Tidak ada kegiatan RPJMDes baru untuk tahun {Tahun} (isi Tahun Pelaksanaan di Kegiatan RPJMDes)." : $"{n} kegiatan RPJMDes dimasukkan ke RKPDes {Tahun}.");
        return n;
    }

    /// <summary>Buat kegiatan & paket di skenario aktif dari RKPDes yang disepakati (seperti alur RKPDes → APBDes di Siskeudes).</summary>
    public int KirimKeApbdes(bool tanya)
    {
        var d = S.Data;
        var info = S.SkenarioAktif;
        if (d == null || info == null) return 0;
        if (!S.BisaUbah) { Pesan.Peringatan($"Skenario \"{info.Nama}\" sudah diposting (terkunci). Pilih skenario draft tahun {Tahun}."); return 0; }
        var daftar = R.KegiatanRkp.Where(k => k.Tahun == info.Tahun && k.Status == "Disepakati").OrderBy(k => k.Kode, KodeComparer.Instance).ToList();
        var baru = daftar.Where(k => !d.Kegiatan.Any(x => x.Kode == k.Kode)).ToList();
        if (baru.Count == 0)
        {
            Pesan.Info(daftar.Count == 0 ? $"Belum ada kegiatan RKPDes {info.Tahun} berstatus Disepakati." : "Semua kegiatan RKPDes yang disepakati sudah ada di Isian Data Anggaran.");
            return 0;
        }
        if (tanya && !Pesan.Tanya($"Buat {baru.Count} kegiatan baru di skenario \"{info.Nama}\" dari RKPDes {info.Tahun}?\n\n" +
                                  "Setiap kegiatan diberi satu paket. Rincian RAB diisi kemudian di Isian Data Anggaran › Belanja.\n" +
                                  $"{daftar.Count - baru.Count} kegiatan dengan kode yang sama sudah ada dan dilewati."))
            return 0;
        foreach (var r in baru)
        {
            var k = new Kegiatan
            {
                KodeSubBidang = r.KodeSubBidang, NoUrut = r.NoUrut, Nama = r.Nama, Lokasi = r.Lokasi, Waktu = r.Waktu,
                Pelaksana = r.Pelaksana, Keluaran = r.Keluaran, VolumeKeluaran = r.Volume, SatuanKeluaran = r.Satuan,
                Catatan = $"Dari RKPDes {r.Tahun}: pagu indikatif {Kalkulasi.Rp(r.PaguIndikatif)}" + (r.SasaranManfaat != "" ? $", sasaran {r.SasaranManfaat}" : "")
            };
            var p = new Paket { Nama = r.Nama, Lokasi = r.Lokasi, Pola = r.Pola, TargetOutput = r.Volume, Satuan = r.Satuan };
            p.SumberDana = r.SumberDana;
            k.Paket.Add(p);
            int i = 0;
            while (i < d.Kegiatan.Count && KodeComparer.Instance.Compare(d.Kegiatan[i].Kode, k.Kode) <= 0) i++;
            d.Kegiatan.Insert(i, k);
        }
        if (tanya) Pesan.Info($"{baru.Count} kegiatan dibuat. Lanjutkan mengisi RAB di Isian Data Anggaran › 3. Belanja.");
        return baru.Count;
    }
}
