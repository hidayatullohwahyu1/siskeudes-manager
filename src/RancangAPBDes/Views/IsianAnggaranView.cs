using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
/// Isian Data Anggaran – susunan mengikuti Siskeudes:
/// Kegiatan (Bidang › Sub Bidang › Kegiatan › Paket), Pendapatan (RAP › RAP Rinci),
/// Belanja (Kegiatan › Paket › RAB › RAB Rinci), Pembiayaan 1 dan Pembiayaan 2.
/// </summary>
public class IsianAnggaranView : UserControl
{
    static readonly string[] DaftarPola = { "Swakelola", "Kerjasama Antar Desa", "Kerjasama dengan Pihak Ketiga", "Penyedia" };
    static readonly string[] DaftarSifat = { "Fisik", "Non Fisik" };

    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = new();
    readonly ContentControl _isi = new() { Focusable = false };
    readonly TextBlock _subJudul = new() { Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    readonly List<(string nama, TabControl tab)> _bagian = new();
    readonly List<HalamanData> _semua = new();

    // Pilihan bersama
    string? _bidang, _sub;
    Kegiatan? _keg;
    Paket? _paket;

    public IsianAnggaranView()
    {
        var akar = new Grid();
        akar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        akar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var kepala = new Border { Background = (Brush)Application.Current.FindResource("LatarKepalaForm"), Padding = new Thickness(12, 6, 12, 6) };
        var kg = new Grid();
        kg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        kg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        kg.Children.Add(new TextBlock { Text = "ISIAN DATA ANGGARAN", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
        Grid.SetColumn(_subJudul, 1);
        _subJudul.TextAlignment = TextAlignment.Right;
        kg.Children.Add(_subJudul);
        kepala.Child = kg;
        akar.Children.Add(kepala);

        var badan = new Grid();
        badan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(178) });
        badan.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(badan, 1);
        akar.Children.Add(badan);

        var navBingkai = new Border { Background = Brushes.White, BorderBrush = (Brush)Application.Current.FindResource("Garis"), BorderThickness = new Thickness(1, 0, 1, 1), Child = _nav };
        _nav.BorderThickness = new Thickness(0);
        _nav.Padding = new Thickness(0, 6, 0, 6);
        _nav.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        _nav.ItemContainerStyle = GayaNav();
        badan.Children.Add(navBingkai);

        _isi.Margin = new Thickness(8, 8, 0, 0);
        Grid.SetColumn(_isi, 1);
        badan.Children.Add(_isi);
        Content = akar;

        BangunKegiatan();
        BangunRekening("Pendapatan", "RAP", "RAP Rinci", "4", () => S.Data?.Pendapatan, SumberDanaPendapatan, "tahun");
        BangunBelanja();
        BangunRekening("Pembiayaan 1 – Penerimaan", "Penerimaan Pembiayaan", "Rincian", "6.1", () => S.Data?.PenerimaanPembiayaan, _ => "", "Ls");
        BangunRekening("Pembiayaan 2 – Pengeluaran", "Pengeluaran Pembiayaan", "Rincian", "6.2", () => S.Data?.PengeluaranPembiayaan, _ => "", "tahun");

        foreach (var (nama, _) in _bagian)
            _nav.Items.Add(new ListBoxItem { Content = new TextBlock { Text = nama, TextWrapping = TextWrapping.Wrap } });
        _nav.SelectionChanged += (_, _) => TampilkanBagian();
        _nav.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            S.DataDiganti += SaatDataDiganti;
            S.HasilBerubah += SegarkanTampil;
            S.ReferensiBerubah += SegarkanTampil;
            S.SebelumDataDiganti += BatalkanIsian;
            PerbaruiJudul();
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

    static Style GayaNav()
    {
        var s = new Style(typeof(ListBoxItem));
        s.Setters.Add(new Setter(PaddingProperty, new Thickness(12, 8, 12, 8)));
        s.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
        var t = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        t.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
        s.Triggers.Add(t);
        return s;
    }

    /// <summary>Pilih bagian dan tab (dipakai uji dan navigasi).</summary>
    public void Buka(int bagian, int tab = 0)
    {
        _nav.SelectedIndex = bagian;
        if (bagian < _bagian.Count) _bagian[bagian].tab.SelectedIndex = tab;
    }

    public HalamanData? HalamanAktif =>
        _nav.SelectedIndex >= 0 && _nav.SelectedIndex < _bagian.Count
            ? (_bagian[_nav.SelectedIndex].tab.SelectedItem as TabItem)?.Content as HalamanData
            : null;

    void TampilkanBagian()
    {
        if (_nav.SelectedIndex < 0) return;
        _isi.Content = _bagian[_nav.SelectedIndex].tab;
        SegarkanTampil();
    }

    void PerbaruiJudul()
    {
        var info = S.SkenarioAktif;
        _subJudul.Text = info == null ? "" :
            $"{(string.IsNullOrWhiteSpace(S.Desa.NamaDesa) ? "Desa (belum diisi)" : S.Desa.NamaDesa)}  ·  Tahun {info.Tahun}  ·  Skenario: {info.Nama}{(info.IsFinal ? " (POSTING)" : "")}";
    }

    void SegarkanTampil()
    {
        PerbaruiJudul();
        HalamanAktif?.Segarkan();
    }

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
        var tab = new TabControl { Padding = new Thickness(0), BorderThickness = new Thickness(0) };
        foreach (var (judul, h) in halaman)
        {
            tab.Items.Add(new TabItem { Header = judul, Content = h });
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

    void PindahTab(TabControl tab, int idx) => tab.SelectedIndex = idx;

    // ================================================================ Kegiatan

    List<BarisKelompok> BarisBidang()
    {
        var d = S.Data;
        if (d == null) return new();
        return S.Referensi.Bidang.OrderBy(b => b.Kode, KodeComparer.Instance).Select(b =>
        {
            var keg = d.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == b.Kode).ToList();
            return new BarisKelompok { Kode = b.Kode, Nama = b.Nama, Jumlah = keg.Count, Nilai = keg.Sum(k => k.Total) };
        }).ToList();
    }

    List<BarisKelompok> BarisSub()
    {
        var d = S.Data;
        if (d == null || _bidang == null) return new();
        var kode = S.Referensi.SubBidang.Where(s => s.KodeBidang == _bidang).Select(s => s.Kode)
            .Union(d.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == _bidang).Select(k => k.KodeSubBidang))
            .Distinct().OrderBy(k => k, KodeComparer.Instance);
        return kode.Select(k =>
        {
            var keg = d.Kegiatan.Where(x => x.KodeSubBidang == k).ToList();
            var nama = Lookup.NamaSubBidang(k);
            return new BarisKelompok { Kode = k, Nama = nama == "" ? "(tidak ada di referensi)" : nama, Jumlah = keg.Count, Nilai = keg.Sum(x => x.Total) };
        }).ToList();
    }

    void BangunKegiatan()
    {
        HalamanData hBidang = null!, hSub = null!, hKeg = null!, hPaket = null!;
        TabControl tab = null!;

        hBidang = new HalamanData
        {
            JudulData = "Data Bidang",
            TanpaForm = true,
            BolehUbah = () => false,
            Kunci = o => ((BarisKelompok)o).Kode,
            Tampilan = BarisBidang,
            Konteks = () => "pilih bidang, lalu buka tab Sub Bidang"
        };
        hBidang.Kolom.AddRange(KolomKelompok("Kd Bidang", "Nama Bidang"));
        hBidang.PilihanBerubah += o => { var k = (o as BarisKelompok)?.Kode; if (k != _bidang) { _bidang = k; _sub = null; } };
        hBidang.TambahTombol("Lihat Sub Bidang ›", () => PindahTab(tab, 1), butuhUbah: false, butuhPilih: true);

        hSub = new HalamanData
        {
            JudulData = "Data Sub Bidang",
            TanpaForm = true,
            BolehUbah = () => false,
            Kunci = o => ((BarisKelompok)o).Kode,
            Syarat = () => _bidang == null ? "Pilih bidang terlebih dahulu di tab Bidang." : null,
            Konteks = () => $"Bidang {_bidang} {Lookup.NamaBidang(_bidang ?? "")}",
            Tampilan = BarisSub
        };
        hSub.Kolom.AddRange(KolomKelompok("Kd Sub Bidang", "Nama Sub Bidang"));
        hSub.PilihanBerubah += o => { var k = (o as BarisKelompok)?.Kode; if (k != _sub) _sub = k; };
        hSub.TambahTombol("Lihat Kegiatan ›", () => PindahTab(tab, 2), butuhUbah: false, butuhPilih: true);

        hKeg = new HalamanData
        {
            JudulData = "Data Kegiatan",
            Kunci = o => ((Kegiatan)o).Id.ToString(),
            Syarat = () => S.Data == null ? "Belum ada skenario." : _sub == null ? "Pilih sub bidang terlebih dahulu di tab Sub Bidang." : null,
            Konteks = () => $"Sub Bidang {_sub} {Lookup.NamaSubBidang(_sub ?? "")}",
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
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", DuaBaris = true, MinLebar = 220 });
        hKeg.Kolom.Add(new Kolom("Pelaksana (PKA)", "Pelaksana", 160));
        hKeg.Kolom.Add(new Kolom("Waktu", "Waktu", 85));
        hKeg.Kolom.Add(new Kolom { Judul = "Pagu (Rp)", Path = "Total", Lebar = 130, Angka = true, Tebal = true, Format = "#,##0" });
        hKeg.Isian.AddRange(new[]
        {
            new Isian
            {
                Label = "No. Kegiatan", Path = "NoUrut", Jenis = JenisIsian.Kode, Wajib = true, Penuh = true, Format = "00",
                Petunjuk = "Nomor kegiatan di dalam sub bidang. Tekan … untuk memilih dari referensi kegiatan.",
                DaftarKode = o => S.Referensi.Kegiatan.Where(r => r.KodeSubBidang == ((Kegiatan)o).KodeSubBidang)
                    .OrderBy(r => r.Kode, KodeComparer.Instance)
                    .Select(r => new PilihanKode(r.Kode.Split('.').Last(), r.Kode, r.Nama)),
                SetelahPilih = (o, p) => ((Kegiatan)o).Nama = p.Nama
            },
            new Isian { Label = "Nama Kegiatan", Path = "Nama", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true },
            new Isian { Label = "Lokasi", Path = "Lokasi" },
            new Isian { Label = "Waktu Pelaksanaan", Path = "Waktu" },
            new Isian { Label = "Pelaksana Kegiatan (PKA)", Path = "Pelaksana", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarPelaksana },
            new Isian { Label = "Keluaran / Output", Path = "Keluaran" },
            new Isian { Label = "Volume Keluaran", Path = "VolumeKeluaran", Jenis = JenisIsian.Angka },
            new Isian { Label = "Satuan Keluaran", Path = "SatuanKeluaran", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
            new Isian { Label = "Catatan Musyawarah", Path = "Catatan", Jenis = JenisIsian.TeksPanjang, Penuh = true, Petunjuk = "Catatan hasil rapat (tidak diinput ke Siskeudes)" },
        });
        hKeg.PilihanBerubah += o => { var k = o as Kegiatan; if (!ReferenceEquals(k, _keg)) { _keg = k; _paket = null; } };
        hKeg.TambahTombol("Lihat Paket ›", () => PindahTab(tab, 3), butuhUbah: false, butuhPilih: true);

        hPaket = BuatHalamanPaket(bisaUbah: true);
        tab = BuatTab("Kegiatan", ("Bidang", hBidang), ("Sub Bidang", hSub), ("Kegiatan", hKeg), ("Paket Kegiatan", hPaket));
    }

    HalamanData BuatHalamanPaket(bool bisaUbah)
    {
        var h = new HalamanData
        {
            JudulData = "Paket Kegiatan",
            TanpaForm = !bisaUbah,
            BolehUbah = bisaUbah ? () => S.BisaUbah : () => false,
            Kunci = o => ((Paket)o).Id.ToString(),
            Syarat = () => _keg == null ? "Pilih kegiatan terlebih dahulu di tab Kegiatan." : null,
            Konteks = () => _keg == null ? null : $"Kegiatan {_keg.Kode} {_keg.Nama}",
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
        h.Kolom.Add(new Kolom { Judul = "No", Path = "No", Lebar = 45, Format = "00" });
        h.Kolom.Add(new Kolom { Judul = "Nama Paket", Path = "Nama", DuaBaris = true, MinLebar = 200 });
        h.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 85));
        h.Kolom.Add(new Kolom("Pola", "Pola", 110));
        h.Kolom.Add(new Kolom("Sifat", "Sifat", 85));
        h.Kolom.Add(new Kolom { Judul = "Target", Path = "TargetOutput", Lebar = 70, Angka = true });
        h.Kolom.Add(new Kolom("Satuan", "Satuan", 75));
        h.Kolom.Add(new Kolom { Judul = "Nilai (Rp)", Path = "Total", Lebar = 130, Angka = true, Tebal = true, Format = "#,##0" });
        if (bisaUbah)
        {
            h.Isian.AddRange(new[]
            {
                new Isian { Label = "Nama Paket", Path = "Nama", Jenis = JenisIsian.TeksPanjang, Wajib = true, Penuh = true },
                new Isian { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
                new Isian { Label = "Pola Kegiatan", Path = "Pola", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarPola },
                new Isian { Label = "Sifat Kegiatan", Path = "Sifat", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarSifat },
                new Isian { Label = "Target Output", Path = "TargetOutput", Jenis = JenisIsian.Angka },
                new Isian { Label = "Satuan", Path = "Satuan", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
                new Isian { Label = "Lokasi Kegiatan", Path = "Lokasi" },
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

    static IEnumerable<Kolom> KolomKelompok(string judulKode, string judulNama) => new[]
    {
        new Kolom(judulKode, "Kode", 100),
        new Kolom { Judul = judulNama, Path = "Nama", DuaBaris = true, MinLebar = 220 },
        new Kolom { Judul = "Jml Kegiatan", Path = "Jumlah", Lebar = 95, Angka = true, Format = "0" },
        new Kolom { Judul = "Pagu (Rp)", Path = "Nilai", Lebar = 140, Angka = true, Tebal = true, Format = "#,##0" },
    };

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
        HalamanData hKeg = null!, hPaket = null!;
        TabControl tab = null!;
        hKeg = new HalamanData
        {
            JudulData = "Kegiatan",
            TanpaForm = true,
            BolehUbah = () => false,
            Kunci = o => ((Kegiatan)o).Id.ToString(),
            Syarat = () => S.Data == null ? "Belum ada skenario." : S.Data.Kegiatan.Count == 0 ? "Belum ada kegiatan. Tambahkan dulu di bagian Kegiatan." : null,
            Konteks = () => "pilih kegiatan, lalu buka tab Paket",
            Tampilan = () => S.Data?.Kegiatan.OrderBy(k => k.Kode, KodeComparer.Instance).ToList()
        };
        hKeg.Kolom.Add(new Kolom("Kd Kegiatan", "Kode", 90));
        hKeg.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", DuaBaris = true, MinLebar = 220 });
        hKeg.Kolom.Add(new Kolom("Pelaksana (PKA)", "Pelaksana", 160));
        hKeg.Kolom.Add(new Kolom { Judul = "Pagu (Rp)", Path = "Total", Lebar = 130, Angka = true, Tebal = true, Format = "#,##0" });
        hKeg.PilihanBerubah += o => { var k = o as Kegiatan; if (!ReferenceEquals(k, _keg)) { _keg = k; _paket = null; } };
        hKeg.TambahTombol("Lihat Paket ›", () => PindahTab(tab, 1), butuhUbah: false, butuhPilih: true);

        hPaket = BuatHalamanPaket(bisaUbah: false);
        hPaket.TambahTombol("Lihat RAB ›", () => PindahTab(tab, 2), butuhUbah: false, butuhPilih: true);

        var (hRab, hRinci) = BuatPasanganRekening("RAB", "RAB Rinci", "5", () => _paket?.Rincian,
            _ => _paket?.SumberDana ?? "", "Ls",
            syarat: () => _paket == null ? "Pilih paket terlebih dahulu di tab Paket." : null,
            konteks: () => _paket == null ? null : $"{_keg?.Kode} › Paket {_paket.No:00} {_paket.Nama}",
            pindahKeRinci: () => PindahTab(tab, 3));
        tab = BuatTab("Belanja", ("Kegiatan", hKeg), ("Paket", hPaket), ("RAB", hRab), ("RAB Rinci", hRinci));
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
        Func<TrackedCollection<RincianAnggaran>?> daftar, Func<string, string> sdAwal, string satuanAwal)
    {
        TabControl tab = null!;
        var (hRek, hRinci) = BuatPasanganRekening(judulRek, judulRinci, awalan, daftar, sdAwal, satuanAwal,
            syarat: () => S.Data == null ? "Belum ada skenario." : null,
            konteks: () => null,
            pindahKeRinci: () => PindahTab(tab, 1));
        tab = BuatTab(nama, (judulRek, hRek), (judulRinci, hRinci));
    }

    /// <summary>Tabel rekening (RAP/RAB) dan rinciannya.</summary>
    (HalamanData rek, HalamanData rinci) BuatPasanganRekening(string judulRek, string judulRinci, string awalan,
        Func<TrackedCollection<RincianAnggaran>?> daftar, Func<string, string> sdAwal, string satuanAwal,
        Func<string?> syarat, Func<string?> konteks, Action pindahKeRinci)
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
                Kode = k, Nama = Lookup.NamaRekening(k),
                Jumlah = list.Count(r => r.KodeRekening == k),
                Nilai = list.Where(r => r.KodeRekening == k).Sum(r => r.Jumlah)
            }).ToList();
        }

        IEnumerable<PilihanKode> PilihanRekening(string? kecuali) =>
            S.Referensi.Rekening
                .Where(r => r.Kode.Split('.').Length == 4 && Batas.Cocok(r.Kode, awalan) && r.Kode != kecuali)
                .OrderBy(r => r.Kode, KodeComparer.Instance)
                .Select(r => new PilihanKode(r.Kode, r.Kode + ".", r.Nama));

        hRek = new HalamanData
        {
            JudulData = judulRek,
            TanpaForm = true,
            Kunci = o => ((BarisKelompok)o).Kode,
            Syarat = syarat,
            Konteks = konteks,
            Tampilan = Baris,
            LabelTambah = "Tambah",
            AksiTambah = () =>
            {
                var p = DialogPilih.Tampil(Window.GetWindow(this), $"Pilih rekening {judulRek}", PilihanRekening(null));
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
                var p = DialogPilih.Tampil(Window.GetWindow(this), $"Ganti rekening {b.Kode}. menjadi…", PilihanRekening(b.Kode));
                if (p == null) return;
                var list = daftar();
                if (list == null) return;
                foreach (var r in list.Where(r => r.KodeRekening == b.Kode).ToList()) r.KodeRekening = p.Nilai;
                if (tertunda == b.Kode) tertunda = p.Nilai;
                rek = p.Nilai;
                hRek.Segarkan();
                hRek.PilihKunci(p.Nilai);
            },
            AksiHapus = () =>
            {
                if (hRek.Terpilih is not BarisKelompok b) return;
                if (!Pesan.Tanya($"Hapus rekening {b.Kode}. {b.Nama} beserta {b.Jumlah} rincian (Rp {b.Nilai:#,##0})?\n\n(Bisa dikembalikan dengan Batalkan / Ctrl+Z.)")) return;
                var list = daftar();
                if (list != null) foreach (var r in list.Where(r => r.KodeRekening == b.Kode).ToList()) list.Remove(r);
                if (tertunda == b.Kode) tertunda = null;
                hRek.Segarkan();
            }
        };
        hRek.Kolom.Add(new Kolom("Kode Rekening", "Kode", 105));
        hRek.Kolom.Add(new Kolom { Judul = "Nama Rekening", Path = "Nama", DuaBaris = true, MinLebar = 220 });
        hRek.Kolom.Add(new Kolom { Judul = "Jml Rinci", Path = "Jumlah", Lebar = 80, Angka = true, Format = "0" });
        hRek.Kolom.Add(new Kolom { Judul = "Anggaran (Rp)", Path = "Nilai", Lebar = 140, Angka = true, Tebal = true, Format = "#,##0" });
        hRek.PilihanBerubah += o => rek = (o as BarisKelompok)?.Kode;
        hRek.TambahTombol($"Lihat {judulRinci} ›", pindahKeRinci, butuhUbah: false, butuhPilih: true);

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
        hRinci.Kolom.Add(new Kolom { Judul = "Uraian", Path = "Uraian", DuaBaris = true, MinLebar = 200 });
        hRinci.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 85));
        hRinci.Kolom.Add(new Kolom { Judul = "Volume", Path = "Volume", Lebar = 75, Angka = true });
        hRinci.Kolom.Add(new Kolom("Satuan", "Satuan", 80));
        hRinci.Kolom.Add(new Kolom { Judul = "Harga Satuan (Rp)", Path = "HargaSatuan", Lebar = 130, Angka = true });
        hRinci.Kolom.Add(new Kolom { Judul = "Jumlah (Rp)", Path = "Jumlah", Lebar = 135, Angka = true, Tebal = true });
        hRinci.Isian.AddRange(new[]
        {
            new Isian { Label = "Uraian", Path = "Uraian", Wajib = true, Penuh = true, MaksKarakter = 200 },
            new Isian { Label = "Volume", Path = "Volume", Jenis = JenisIsian.Angka, Petunjuk = "Desimal memakai koma, contoh 2,5" },
            new Isian { Label = "Satuan", Path = "Satuan", Jenis = JenisIsian.PilihanBebas, Pilihan = () => S.DaftarSatuan },
            new Isian { Label = "Harga Satuan (Rp)", Path = "HargaSatuan", Jenis = JenisIsian.Angka },
            new Isian { Label = "Sumber Dana", Path = "SumberDana", Jenis = JenisIsian.Pilihan, Wajib = true, Pilihan = () => S.DaftarSumberDana, TampilPath = "Tampil", NilaiPath = "Kode" },
            new Isian { Label = "Jumlah", Path = "Jumlah", Jenis = JenisIsian.Info, Format = "Rp #,##0.##" },
            new Isian { Label = "Keterangan", Path = "Keterangan" },
        });
        return (hRek, hRinci);
    }
}
