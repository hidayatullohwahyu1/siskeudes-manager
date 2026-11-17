using System.Windows;
using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Baris ringkas untuk tabel bidang, sub bidang, dan rekening (RAP/RAB).</summary>
public class BarisKelompok
{
    public string Kode { get; init; } = "";
    public string Nama { get; init; } = "";
    public int Jumlah { get; init; }
    public decimal Nilai { get; init; }
}

/// <summary>
/// Data Entri › Penganggaran › Isian Data Anggaran, disusun seperti Siskeudes:
/// Kegiatan (Bidang › Sub Bidang › Kegiatan › Paket Kegiatan), Pendapatan (RAP › RAP Rinci),
/// Belanja (Bidang › Kegiatan › Paket › RAB › RAB Rinci), Pembiayaan 1 dan Pembiayaan 2.
/// </summary>
public class IsianAnggaranView : UserControl
{
    static readonly string[] DaftarPola = { "Swakelola", "Kerjasama Antar Desa", "Kerjasama dengan Pihak Ketiga", "Penyedia" };
    static readonly string[] DaftarSifat = { "Fisik", "Non Fisik" };
    static readonly string[] DaftarPktd = { "Non PKTD", "PKTD" };

    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = Gaya.Nav();
    readonly ContentControl _isi = new() { Focusable = false };
    readonly List<(string nama, TabControl tab)> _bagian = new();
    readonly List<HalamanData> _semua = new();

    // Pilihan bersama antar tab
    string? _bidang, _sub, _bidangBelanja;
    Kegiatan? _keg;
    Paket? _paket;

    /// <summary>Dipanggil tombol Cetak di tab RAP/RAB (membuka pratinjau laporan).</summary>
    public static Action<string>? BukaLaporan { get; set; }

    public IsianAnggaranView()
    {
        BangunKegiatan();
        BangunRekening("2. Pendapatan", "1. RAP", "2. RAP Rinci", "4", () => S.Data?.Pendapatan, SumberDanaPendapatan, "tahun", Laporan.Pendapatan);
        BangunBelanja();
        BangunRekening("4. Pembiayaan 1", "1. Penerimaan", "2. Rincian Penerimaan", "6.1", () => S.Data?.PenerimaanPembiayaan, _ => "", "Ls", Laporan.Pembiayaan);
        BangunRekening("5. Pembiayaan 2", "1. Pengeluaran", "2. Rincian Pengeluaran", "6.2", () => S.Data?.PengeluaranPembiayaan, _ => "", "tahun", Laporan.Pembiayaan);

        foreach (var (nama, _) in _bagian) _nav.Items.Add(Gaya.ItemNav(nama));
        _nav.SelectionChanged += (_, _) => TampilkanBagian();
        _nav.SelectedIndex = 0;
        Content = Gaya.DenganNav(_nav, _isi);

        Loaded += (_, _) =>
        {
            S.DataDiganti += SaatDataDiganti;
            S.HasilBerubah += SegarkanTampil;
            S.ReferensiBerubah += SegarkanTampil;
            S.SebelumDataDiganti += BatalkanIsian;
            SegarkanTampil();
        };
        Unloaded += (_, _) =>
        {
            S.DataDiganti -= SaatDataDiganti;
            S.HasilBerubah -= SegarkanTampil;
            S.ReferensiBerubah -= SegarkanTampil;
            S.SebelumDataDiganti -= BatalkanIsian;
        };
    }

    /// <summary>Pilih bagian dan tab (navigasi dan uji).</summary>
    public void Buka(int bagian, int tab = 0)
    {
        _nav.SelectedIndex = bagian;
        if (bagian < _bagian.Count) _bagian[bagian].tab.SelectedIndex = tab;
    }

    public HalamanData? HalamanAktif =>
        _nav.SelectedIndex >= 0 && _nav.SelectedIndex < _bagian.Count
            ? (_bagian[_nav.SelectedIndex].tab.SelectedItem as TabItem)?.Content as HalamanData
            : null;

    public IEnumerable<HalamanData> SemuaHalaman => _semua;

    void TampilkanBagian()
    {
        if (_nav.SelectedIndex < 0) return;
        _isi.Content = _bagian[_nav.SelectedIndex].tab;
        SegarkanTampil();
    }

    void SegarkanTampil() => HalamanAktif?.Segarkan();

    void BatalkanIsian()
    {
        foreach (var h in _semua) if (h.SedangMengisi) h.Batal();
    }

    void SaatDataDiganti()
    {
        BatalkanIsian();
        var d = S.Data;
        _keg = d == null || _keg == null ? null : d.Kegiatan.FirstOrDefault(k => k.Id == _keg.Id);
        _paket = _keg == null || _paket == null ? null : _keg.Paket.FirstOrDefault(p => p.Id == _paket.Id);
        SegarkanTampil();
    }

    TabControl BuatTab(string nama, params (string judul, HalamanData h)[] halaman)
    {
        var tab = Gaya.TabKiri();
        foreach (var (judul, h) in halaman)
        {
            tab.Items.Add(Gaya.Tab(judul, h));
            _semua.Add(h);
        }
        tab.SelectionChanged += (_, e) =>
        {
            if (e.Source != tab) return;
            Dispatcher.BeginInvoke(new Action(() => HalamanAktif?.Segarkan()), System.Windows.Threading.DispatcherPriority.Background);
        };
        _bagian.Add((nama, tab));
        return tab;
    }

    static void PindahTab(TabControl tab, int idx) => tab.SelectedIndex = idx;

    static IEnumerable<Kolom> KolomKelompok(string judulKode, string judulNama) => new[]
    {
        new Kolom(judulKode, "Kode", 90),
        new Kolom { Judul = judulNama, Path = "Nama", MinLebar = 220 },
        new Kolom { Judul = "Jml Kegiatan", Path = "Jumlah", Lebar = 90, Angka = true, Format = "0" },
        new Kolom { Judul = "Pagu (Rp)", Path = "Nilai", Lebar = 140, Angka = true, Tebal = true, Format = "#,##0" },
    };

    BarisKelompok BarisBidang(string kode)
    {
        var keg = S.Data!.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == kode).ToList();
        var nama = Lookup.NamaBidang(kode);
        return new BarisKelompok { Kode = kode, Nama = nama == "" ? "(tidak ada di referensi)" : nama, Jumlah = keg.Count, Nilai = keg.Sum(k => k.Total) };
    }

    BarisKelompok BarisSub(string kode)
    {
        var keg = S.Data!.Kegiatan.Where(k => k.KodeSubBidang == kode).ToList();
        var nama = Lookup.NamaSubBidang(kode);
        return new BarisKelompok { Kode = kode, Nama = nama == "" ? "(tidak ada di referensi)" : nama, Jumlah = keg.Count, Nilai = keg.Sum(k => k.Total) };
    }

    // ================================================================ Kegiatan

    void BangunKegiatan()
    {
        TabControl tab = null!;
        HalamanData hBidang = null!, hSub = null!;

        hBidang = new HalamanData
        {
            JudulData = "Data Bidang",
            TanpaForm = true,
            Kunci = o => ((BarisKelompok)o).Kode,
            Syarat = () => S.Data == null ? "Belum ada skenario." : null,
            Konteks = () => "Tambah bidang yang akan dianggarkan, lalu buka tab Sub Bidang",
            Tampilan = () => S.Data?.DaftarBidang().Select(BarisBidang).ToList(),
            AksiTambah = () =>
            {
                var ada = S.Data!.DaftarBidang();
                var p = DialogPilih.Tampil(Window.GetWindow(this), "Pilih Bidang",
                    S.Referensi.Bidang.Where(b => !ada.Contains(b.Kode)).OrderBy(b => b.Kode, KodeComparer.Instance)
                        .Select(b => new PilihanKode(b.Kode, b.Kode + ".", b.Nama)));
                if (p == null) return;
                S.Data.BidangDipakai.Add(p.Nilai);
                _bidang = p.Nilai;
                hBidang.Segarkan();
                hBidang.PilihKunci(p.Nilai);
            },
            AksiHapus = () =>
            {
                if (hBidang.Terpilih is not BarisKelompok b) return;
                if (b.Jumlah > 0) { Pesan.Peringatan($"Bidang {b.Kode} masih memiliki {b.Jumlah} kegiatan. Hapus kegiatannya terlebih dahulu."); return; }
                if (!Pesan.Tanya($"Hapus bidang {b.Kode}. {b.Nama} dari daftar?")) return;
                S.Data!.BidangDipakai.Remove(b.Kode);
                foreach (var s in S.Data.SubBidangDipakai.Where(s => s.Split('.')[0] == b.Kode).ToList()) S.Data.SubBidangDipakai.Remove(s);
                hBidang.Segarkan();
            }
        };
        hBidang.Kolom.AddRange(KolomKelompok("Kd Bidang", "Nama Bidang"));
        hBidang.PilihanBerubah += o => { var k = (o as BarisKelompok)?.Kode; if (k != _bidang) { _bidang = k; _sub = null; } };
        hBidang.TambahTombol("Sub Bidang ›", () => PindahTab(tab, 1), butuhUbah: false, butuhPilih: true, tip: "Buka tab Sub Bidang");

        hSub = new HalamanData
        {
            JudulData = "Data Sub Bidang",
            TanpaForm = true,
            Kunci = o => ((BarisKelompok)o).Kode,
            Syarat = () => S.Data == null ? "Belum ada skenario." : _bidang == null ? "Pilih bidang terlebih dahulu di tab Bidang." : null,
            Konteks = () => $"Bidang {_bidang}. {Lookup.NamaBidang(_bidang ?? "")}",
            Tampilan = () => _bidang == null ? null : S.Data?.DaftarSubBidang(_bidang).Select(BarisSub).ToList(),
            AksiTambah = () =>
            {
                if (_bidang == null) return;
                var ada = S.Data!.DaftarSubBidang(_bidang);
                var p = DialogPilih.Tampil(Window.GetWindow(this), "Pilih Sub Bidang",
                    S.Referensi.SubBidang.Where(s => s.KodeBidang == _bidang && !ada.Contains(s.Kode)).OrderBy(s => s.Kode, KodeComparer.Instance)
                        .Select(s => new PilihanKode(s.Kode, s.Kode + ".", s.Nama)));
                if (p == null) return;
                S.Data.SubBidangDipakai.Add(p.Nilai);
                if (!S.Data.BidangDipakai.Contains(_bidang)) S.Data.BidangDipakai.Add(_bidang);
                _sub = p.Nilai;
                hSub.Segarkan();
                hSub.PilihKunci(p.Nilai);
            },
            AksiHapus = () =>
            {
                if (hSub.Terpilih is not BarisKelompok b) return;
                if (b.Jumlah > 0) { Pesan.Peringatan($"Sub bidang {b.Kode} masih memiliki {b.Jumlah} kegiatan. Hapus kegiatannya terlebih dahulu."); return; }
                if (!Pesan.Tanya($"Hapus sub bidang {b.Kode}. {b.Nama} dari daftar?")) return;
                S.Data!.SubBidangDipakai.Remove(b.Kode);
                hSub.Segarkan();
            }
        };
        hSub.Kolom.AddRange(KolomKelompok("Kd Sub Bidang", "Nama Sub Bidang"));
        hSub.PilihanBerubah += o => { var k = (o as BarisKelompok)?.Kode; if (k != _sub) _sub = k; };
        hSub.TambahTombol("Kegiatan ›", () => PindahTab(tab, 2), butuhUbah: false, butuhPilih: true, tip: "Buka tab Kegiatan");

        var hKeg = new HalamanData
        {
            JudulData = "Data Kegiatan",
            Kunci = o => ((Kegiatan)o).Id.ToString(),
            Syarat = () => S.Data == null ? "Belum ada skenario." : _sub == null ? "Pilih sub bidang terlebih dahulu di tab Sub Bidang." : null,
            Konteks = () => $"Sub Bidang {_sub}. {Lookup.NamaSubBidang(_sub ?? "")}",
            Daftar = () => S.Data?.Kegiatan,
            Tampilan = () => S.Data?.Kegiatan.Where(k => k.KodeSubBidang == _sub).OrderBy(k => k.NoUrut).ToList(),
            BuatBaru = () => _sub == null ? null : new Kegiatan
            {
                KodeSubBidang = _sub, NoUrut = NomorKegiatanBerikut(_sub), Lokasi = S.Desa.NamaDesa, Waktu = "12 Bulan"
            },
            Tambahkan = o =>
            {
                var k = (Kegiatan)o;
                if (k.Paket.Count == 0) k.Paket.Add(new Paket { Nama = k.Nama, Lokasi = k.Lokasi });
                SisipkanUrut(k);
            },
            Validasi = (o, baru) =>
            {
                var k = (Kegiatan)o;
                var sama = S.Data!.Kegiatan.FirstOrDefault(x => x.Id != k.Id && x.KodeSubBidang == k.KodeSubBidang && x.NoUrut == k.NoUrut);
                return sama != null ? $"Kode kegiatan {k.Kode} sudah dipakai oleh \"{sama.Nama}\". Ganti nomor kegiatan." : null;
            },
            PesanHapus = o =>
            {
                var k = (Kegiatan)o;
                return $"Hapus kegiatan {k.Kode} \"{k.Nama}\" beserta {k.Paket.Count} paket dan seluruh RAB-nya (Rp {k.Total:#,##0})?";
            }
        };
        hKeg.Kolom.Add(new Kolom("Kd Kegiatan", "Kode", 90));
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", MinLebar = 220, Bintang = 2 });
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Pelaksana", Path = "Pelaksana", MinLebar = 130 });
        hKeg.Kolom.Add(new Kolom("Waktu", "Waktu", 85));
        hKeg.Kolom.Add(new Kolom { Judul = "Pagu (Rp)", Path = "Total", Lebar = 130, Angka = true, Tebal = true, Format = "#,##0" });
        hKeg.Isian.AddRange(new[]
        {
            new Isian
            {
                Label = "Kd Kegiatan", Path = "NoUrut", Jenis = JenisIsian.Kode, Wajib = true, Penuh = true, Format = "00",
                Petunjuk = "Nomor kegiatan di dalam sub bidang. Tekan … untuk memilih dari referensi kegiatan.",
                DaftarKode = o => S.Referensi.Kegiatan.Where(r => r.KodeSubBidang == ((Kegiatan)o).KodeSubBidang)
                    .OrderBy(r => r.Kode, KodeComparer.Instance)
                    .Select(r => new PilihanKode(r.Kode.Split('.').Last(), r.Kode + ".", r.Nama)),
                SetelahPilih = (o, p) => ((Kegiatan)o).Nama = p.Nama
            },
            new Isian { Label = "Nama Kegiatan", Path = "Nama", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true },
            new Isian { Label = "Lokasi", Path = "Lokasi" },
            new Isian { Label = "Waktu", Path = "Waktu" },
            new Isian { Label = "Nama Pelaksana", Path = "Pelaksana", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarPelaksana, Petunjuk = "Pelaksana Kegiatan Anggaran (PKA)" },
            new Isian { Label = "Pagu Kegiatan", Path = "Total", Jenis = JenisIsian.Info, Format = "Rp #,##0" },
            new Isian { Label = "Keluaran", Path = "Keluaran", Penuh = true },
            new Isian { Label = "Volume Keluaran", Path = "VolumeKeluaran", Jenis = JenisIsian.Angka },
            new Isian { Label = "Satuan Keluaran", Path = "SatuanKeluaran", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
            new Isian { Label = "Catatan Musyawarah", Path = "Catatan", Jenis = JenisIsian.TeksPanjang, Penuh = true, Petunjuk = "Catatan hasil rapat (tidak diinput ke Siskeudes)" },
        });
        hKeg.PilihanBerubah += o => { var k = o as Kegiatan; if (!ReferenceEquals(k, _keg)) { _keg = k; _paket = null; } };
        hKeg.TambahTombol("Paket Kegiatan ›", () => PindahTab(tab, 3), butuhUbah: false, butuhPilih: true, tip: "Buka tab Paket Kegiatan");

        var hPaket = BuatHalamanPaket(bisaUbah: true);
        tab = BuatTab("1. Kegiatan", ("1. Bidang", hBidang), ("2. Sub Bidang", hSub), ("3. Kegiatan", hKeg), ("4. Paket Kegiatan", hPaket));
    }

    HalamanData BuatHalamanPaket(bool bisaUbah)
    {
        var h = new HalamanData
        {
            JudulData = "Paket Kegiatan",
            TanpaForm = !bisaUbah,
            HanyaBaca = !bisaUbah,
            Kunci = o => ((Paket)o).Id.ToString(),
            Syarat = () => _keg == null ? "Pilih kegiatan terlebih dahulu di tab Kegiatan." : null,
            Konteks = () => _keg == null ? null : $"Kegiatan {_keg.Kode}. {_keg.Nama}",
            Daftar = () => _keg?.Paket,
            Tampilan = () =>
            {
                if (_keg == null) return null;
                for (int i = 0; i < _keg.Paket.Count; i++) _keg.Paket[i].No = i + 1;
                return _keg.Paket;
            },
            BuatBaru = () => _keg == null ? null : BuatPaketBaru(_keg),
            PesanHapus = o =>
            {
                var p = (Paket)o;
                return $"Hapus paket \"{p.Nama}\" beserta {p.Rincian.Count} rincian RAB (Rp {p.Total:#,##0})?";
            }
        };
        h.Kolom.Add(new Kolom { Judul = "No.ID", Path = "No", Lebar = 55, Format = "00" });
        h.Kolom.Add(new Kolom { Judul = "Nama Paket", Path = "Nama", MinLebar = 200, Bintang = 2 });
        h.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 85));
        h.Kolom.Add(new Kolom { Judul = "Pola", Path = "Pola", MinLebar = 95 });
        h.Kolom.Add(new Kolom("Sifat", "Sifat", 80));
        h.Kolom.Add(new Kolom { Judul = "Target", Path = "TargetOutput", Lebar = 65, Angka = true });
        h.Kolom.Add(new Kolom("Satuan", "Satuan", 75));
        h.Kolom.Add(new Kolom { Judul = "Nilai (Rp)", Path = "Total", Lebar = 125, Angka = true, Tebal = true, Format = "#,##0" });
        if (bisaUbah)
        {
            h.Isian.AddRange(new[]
            {
                new Isian { Label = "Nama Paket", Path = "Nama", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true },
                new Isian { Label = "Nilai (Rp)", Path = "Total", Jenis = JenisIsian.Info, Format = "#,##0" },
                new Isian { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
                new Isian { Label = "Pola Kegiatan", Path = "Pola", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarPola },
                new Isian { Label = "Sifat Kegiatan", Path = "Sifat", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarSifat },
                new Isian { Label = "Target Output", Path = "TargetOutput", Jenis = JenisIsian.Angka },
                new Isian { Label = "Satuan", Path = "Satuan", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
                new Isian { Label = "Lokasi Kegiatan", Path = "Lokasi" },
                new Isian { Label = "PKTD / Non PKTD", Path = "Pktd", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarPktd },
                new Isian { Label = "Uraian Output", Path = "UraianOutput", Jenis = JenisIsian.TeksPanjang, Penuh = true },
            });
        }
        h.PilihanBerubah += o => _paket = o as Paket;
        return h;
    }

    static Paket BuatPaketBaru(Kegiatan k)
    {
        var acuan = k.Paket.LastOrDefault();
        var p = new Paket { Lokasi = acuan?.Lokasi ?? k.Lokasi, Pola = acuan?.Pola ?? "Swakelola", Sifat = acuan?.Sifat ?? "Non Fisik", Satuan = acuan?.Satuan ?? "" };
        p.SumberDana = acuan?.SumberDana ?? "";
        return p;
    }

    int NomorKegiatanBerikut(string kodeSub)
    {
        var dipakai = S.Data!.Kegiatan.Where(k => k.KodeSubBidang == kodeSub).Select(k => k.NoUrut).ToHashSet();
        var refNo = S.Referensi.Kegiatan.Where(r => r.KodeSubBidang == kodeSub)
            .Select(r => int.TryParse(r.Kode.Split('.').Last(), out var n) ? n : -1).Where(n => n >= 0 && !dipakai.Contains(n)).OrderBy(n => n);
        if (refNo.Any()) return refNo.First();
        int x = 1;
        while (dipakai.Contains(x)) x++;
        return x;
    }

    void SisipkanUrut(Kegiatan k)
    {
        var list = S.Data!.Kegiatan;
        int i = 0;
        while (i < list.Count && KodeComparer.Instance.Compare(list[i].Kode, k.Kode) <= 0) i++;
        list.Insert(i, k);
    }

    // ================================================================ Belanja

    void BangunBelanja()
    {
        TabControl tab = null!;
        var hBidang = new HalamanData
        {
            JudulData = "Bidang",
            TanpaForm = true,
            HanyaBaca = true,
            Kunci = o => ((BarisKelompok)o).Kode,
            Syarat = () => S.Data == null ? "Belum ada skenario." : S.Data.Kegiatan.Count == 0 ? "Belum ada kegiatan. Tambahkan dulu di bagian Kegiatan." : null,
            Konteks = () => "pilih bidang, lalu buka tab Kegiatan",
            Tampilan = () => S.Data?.DaftarBidang().Select(BarisBidang).ToList()
        };
        hBidang.Kolom.AddRange(KolomKelompok("Kd Bidang", "Nama Bidang"));
        hBidang.PilihanBerubah += o => _bidangBelanja = (o as BarisKelompok)?.Kode;
        hBidang.TambahTombol("Kegiatan ›", () => PindahTab(tab, 1), butuhUbah: false, butuhPilih: true, tip: "Buka tab Kegiatan");

        var hKeg = new HalamanData
        {
            JudulData = "Kegiatan",
            TanpaForm = true,
            HanyaBaca = true,
            Kunci = o => ((Kegiatan)o).Id.ToString(),
            Syarat = () => S.Data == null ? "Belum ada skenario." : _bidangBelanja == null ? "Pilih bidang terlebih dahulu di tab Bidang." : null,
            Konteks = () => $"Bidang {_bidangBelanja}. {Lookup.NamaBidang(_bidangBelanja ?? "")}",
            Tampilan = () => S.Data?.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == _bidangBelanja).OrderBy(k => k.Kode, KodeComparer.Instance).ToList()
        };
        hKeg.Kolom.Add(new Kolom("Kd Kegiatan", "Kode", 90));
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", MinLebar = 220, Bintang = 2 });
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Pelaksana", Path = "Pelaksana", MinLebar = 130 });
        hKeg.Kolom.Add(new Kolom { Judul = "Pagu (Rp)", Path = "Total", Lebar = 130, Angka = true, Tebal = true, Format = "#,##0" });
        hKeg.PilihanBerubah += o => { var k = o as Kegiatan; if (!ReferenceEquals(k, _keg)) { _keg = k; _paket = null; } };
        hKeg.TambahTombol("Paket ›", () => PindahTab(tab, 2), butuhUbah: false, butuhPilih: true, tip: "Buka tab Paket");

        var hPaket = BuatHalamanPaket(bisaUbah: false);
        hPaket.TambahTombol("RAB ›", () => PindahTab(tab, 3), butuhUbah: false, butuhPilih: true, tip: "Buka tab RAB");

        var (hRab, hRinci) = BuatPasanganRekening("RAB", "RAB Rinci", "5", () => _paket?.Rincian,
            _ => _paket?.SumberDana ?? "", "Ls",
            syarat: () => _paket == null ? "Pilih paket terlebih dahulu di tab Paket." : null,
            konteks: () => _paket == null ? null : $"Kegiatan {_keg?.Kode}. › Paket {_paket.No:00} {_paket.Nama}",
            pindahKeRinci: () => PindahTab(tab, 4),
            laporan: Laporan.Rab);
        tab = BuatTab("3. Belanja", ("1. Bidang", hBidang), ("2. Kegiatan", hKeg), ("3. Paket", hPaket), ("4. RAB", hRab), ("5. RAB Rinci", hRinci));
    }

    // ================================================================ Pendapatan & Pembiayaan

    static string SumberDanaPendapatan(string rek)
    {
        if (Batas.Cocok(rek, "4.1")) return "PAD";
        if (Batas.Cocok(rek, "4.2.1")) return "DDS";
        if (Batas.Cocok(rek, "4.2.2")) return "PBH";
        if (Batas.Cocok(rek, "4.2.3")) return "ADD";
        if (Batas.Cocok(rek, "4.2.4")) return "PBP";
        if (Batas.Cocok(rek, "4.2.5")) return "PBK";
        if (Batas.Cocok(rek, "4.3")) return "DLL";
        return "";
    }

    void BangunRekening(string nama, string judulRek, string judulRinci, string awalan,
        Func<TrackedCollection<RincianAnggaran>?> daftar, Func<string, string> sdAwal, string satuanAwal, string laporan)
    {
        TabControl tab = null!;
        var (hRek, hRinci) = BuatPasanganRekening(judulRek, judulRinci, awalan, daftar, sdAwal, satuanAwal,
            syarat: () => S.Data == null ? "Belum ada skenario." : null,
            konteks: () => null,
            pindahKeRinci: () => PindahTab(tab, 1),
            laporan: laporan);
        tab = BuatTab(nama, (judulRek, hRek), (judulRinci, hRinci));
    }

    /// <summary>Tabel rekening (RAP/RAB) dan rinciannya.</summary>
    (HalamanData rek, HalamanData rinci) BuatPasanganRekening(string judulRek, string judulRinci, string awalan,
        Func<TrackedCollection<RincianAnggaran>?> daftar, Func<string, string> sdAwal, string satuanAwal,
        Func<string?> syarat, Func<string?> konteks, Action pindahKeRinci, string laporan)
    {
        string? rek = null, tertunda = null;
        HalamanData hRek = null!, hRinci = null!;

        List<BarisKelompok> Baris()
        {
            var list = daftar();
            if (list == null) return new();
            var kode = list.Select(r => r.KodeRekening).Distinct().ToList();
            if (tertunda != null && !kode.Contains(tertunda)) kode.Add(tertunda);
            return kode.OrderBy(k => k, KodeComparer.Instance).Select(k => new BarisKelompok
            {
                Kode = k + ".", Nama = Lookup.NamaRekening(k),
                Jumlah = list.Count(r => r.KodeRekening == k),
                Nilai = list.Where(r => r.KodeRekening == k).Sum(r => r.Jumlah)
            }).ToList();
        }

        static string Kode(object o) => ((BarisKelompok)o).Kode.TrimEnd('.');

        IEnumerable<PilihanKode> PilihanRekening(string? kecuali) =>
            S.Referensi.Rekening
                .Where(r => r.Kode.Split('.').Length == 4 && Batas.Cocok(r.Kode, awalan) && r.Kode != kecuali)
                .OrderBy(r => r.Kode, KodeComparer.Instance)
                .Select(r => new PilihanKode(r.Kode, r.Kode + ".", r.Nama));

        hRek = new HalamanData
        {
            JudulData = judulRek,
            TanpaForm = true,
            Kunci = Kode,
            Syarat = syarat,
            Konteks = konteks,
            Tampilan = Baris,
            AksiCetak = () => BukaLaporan?.Invoke(laporan),
            AksiTambah = () =>
            {
                var p = DialogPilih.Tampil(Window.GetWindow(this), $"Pilih Rekening {judulRek}", PilihanRekening(null));
                if (p == null) return;
                tertunda = p.Nilai;
                rek = p.Nilai;
                hRek.Segarkan();
                hRek.PilihKunci(p.Nilai);
                pindahKeRinci();
                Dispatcher.BeginInvoke(new Action(() => hRinci.Tambah()), System.Windows.Threading.DispatcherPriority.Background);
            },
            AksiUbah = () =>
            {
                if (hRek.Terpilih is not BarisKelompok b) return;
                var lama = Kode(b);
                var p = DialogPilih.Tampil(Window.GetWindow(this), $"Ganti rekening {lama}. menjadi…", PilihanRekening(lama));
                if (p == null) return;
                var list = daftar();
                if (list == null) return;
                foreach (var r in list.Where(r => r.KodeRekening == lama).ToList()) r.KodeRekening = p.Nilai;
                if (tertunda == lama) tertunda = p.Nilai;
                rek = p.Nilai;
                hRek.Segarkan();
                hRek.PilihKunci(p.Nilai);
            },
            AksiHapus = () =>
            {
                if (hRek.Terpilih is not BarisKelompok b) return;
                var kode = Kode(b);
                if (!Pesan.Tanya($"Hapus rekening {b.Kode} {b.Nama} beserta {b.Jumlah} rincian (Rp {b.Nilai:#,##0})?\n\n(Bisa dikembalikan dengan Batalkan / Ctrl+Z.)")) return;
                var list = daftar();
                if (list != null) foreach (var r in list.Where(r => r.KodeRekening == kode).ToList()) list.Remove(r);
                if (tertunda == kode) tertunda = null;
                hRek.Segarkan();
            }
        };
        hRek.Kolom.Add(new Kolom("Kd Rincian", "Kode", 100));
        hRek.Kolom.Add(new Kolom { Judul = "Nama Rincian", Path = "Nama", MinLebar = 220 });
        hRek.Kolom.Add(new Kolom { Judul = "Jml Rinci", Path = "Jumlah", Lebar = 75, Angka = true, Format = "0" });
        hRek.Kolom.Add(new Kolom { Judul = "Anggaran (Rp)", Path = "Nilai", Lebar = 140, Angka = true, Tebal = true, Format = "#,##0" });
        hRek.PilihanBerubah += o => rek = o == null ? null : Kode(o);
        hRek.TambahTombol($"{judulRinci} ›", pindahKeRinci, butuhUbah: false, butuhPilih: true, tip: $"Buka tab {judulRinci}");

        hRinci = new HalamanData
        {
            JudulData = judulRinci,
            Kunci = o => ((RincianAnggaran)o).Id.ToString(),
            Syarat = () => syarat() ?? (rek == null ? $"Pilih rekening terlebih dahulu di tab {judulRek}." : null),
            Konteks = () =>
            {
                var k = konteks();
                var r = rek == null ? "" : $"Rekening {rek}. {Lookup.NamaRekening(rek)}";
                return string.IsNullOrEmpty(k) ? r : $"{k} › {r}";
            },
            Daftar = () => daftar(),
            Tampilan = () =>
            {
                var list = daftar();
                if (list == null || rek == null) return null;
                var hasil = list.Where(r => r.KodeRekening == rek).ToList();
                for (int i = 0; i < hasil.Count; i++) hasil[i].No = i + 1;
                return hasil;
            },
            BuatBaru = () => rek == null ? null : new RincianAnggaran
            {
                KodeRekening = rek, Volume = 1, Satuan = satuanAwal, SumberDana = sdAwal(rek)
            },
            Tambahkan = o =>
            {
                var list = daftar();
                if (list == null) return;
                var r = (RincianAnggaran)o;
                var akhir = -1;
                for (int i = 0; i < list.Count; i++) if (list[i].KodeRekening == r.KodeRekening) akhir = i;
                if (akhir < 0) list.Add(r); else list.Insert(akhir + 1, r);
                if (tertunda == r.KodeRekening) tertunda = null;
            },
            PesanHapus = o => $"Hapus rincian \"{((RincianAnggaran)o).Uraian}\"?"
        };
        hRinci.Kolom.Add(new Kolom { Judul = "No", Path = "No", Lebar = 45, Format = "00" });
        hRinci.Kolom.Add(new Kolom { Judul = "Uraian", Path = "Uraian", MinLebar = 200 });
        hRinci.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 85));
        hRinci.Kolom.Add(new Kolom { Judul = "Volume", Path = "Volume", Lebar = 75, Angka = true });
        hRinci.Kolom.Add(new Kolom("Satuan", "Satuan", 80));
        hRinci.Kolom.Add(new Kolom { Judul = "Harga Satuan (Rp)", Path = "HargaSatuan", Lebar = 130, Angka = true });
        hRinci.Kolom.Add(new Kolom { Judul = "Anggaran (Rp)", Path = "Jumlah", Lebar = 135, Angka = true, Tebal = true });
        hRinci.Isian.AddRange(new[]
        {
            new Isian { Label = "Uraian", Path = "Uraian", Wajib = true, Penuh = true, MaksKarakter = 50,
                Petunjuk = "Siskeudes membatasi uraian rincian 50 karakter" },
            new Isian { Label = "Jumlah Satuan", Path = "Volume", Jenis = JenisIsian.Angka, Petunjuk = "Desimal memakai koma, contoh 2,5" },
            new Isian { Label = "Satuan", Path = "Satuan", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
            new Isian { Label = "Harga Satuan (Rp)", Path = "HargaSatuan", Jenis = JenisIsian.Angka },
            new Isian { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
            new Isian { Label = "Anggaran", Path = "Jumlah", Jenis = JenisIsian.Info, Format = "Rp #,##0.##" },
            new Isian { Label = "Keterangan", Path = "Keterangan", Petunjuk = "Catatan musyawarah (tidak diinput ke Siskeudes)" },
        });
        hRinci.AksiCetak = () => BukaLaporan?.Invoke(laporan);
        return (hRek, hRinci);
    }
}
