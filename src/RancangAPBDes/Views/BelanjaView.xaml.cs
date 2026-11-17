using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Satu baris di daftar kiri: bidang (B), sub bidang (S), atau kegiatan (K).</summary>
public class NodePohon : Bindable
{
    public string Jenis { get; init; } = "K";
    public string KodeSub { get; init; } = "";
    public string KodeBidang { get; init; } = "";
    public Kegiatan? Kegiatan { get; init; }
    public int Level { get; init; }
    public List<NodePohon> Anak { get; } = new();
    public Func<decimal> HitungTotal { get; init; } = () => 0;

    string _kode = "", _nama = "";
    public string Kode { get => Kegiatan?.Kode ?? _kode; init => _kode = value; }
    public string Nama { get => Kegiatan != null ? (string.IsNullOrWhiteSpace(Kegiatan.Nama) ? "(tanpa nama)" : Kegiatan.Nama) : _nama; init => _nama = value; }

    bool _terbuka = true;
    public bool Terbuka
    {
        get => _terbuka;
        set { if (Set(ref _terbuka, value)) OnPropertyChanged(nameof(Panah)); }
    }

    public string Panah => Jenis == "K" ? "" : (Terbuka ? "▾" : "▸");
    public Thickness Indent => new(Level * 16, 0, 0, 0);
    public decimal Total => HitungTotal();
    public bool Kosong { get; set; }

    public string Kunci => Jenis == "K" ? "K:" + Kegiatan!.Id : Jenis + ":" + (Jenis == "B" ? KodeBidang : KodeSub);

    public void Segarkan()
    {
        OnPropertyChanged(nameof(Total));
        if (Kegiatan != null)
        {
            OnPropertyChanged(nameof(Kode));
            OnPropertyChanged(nameof(Nama));
        }
    }
}

public partial class BelanjaView : UserControl
{
    readonly AppState S = AppState.Instance;
    readonly ObservableCollection<NodePohon> _baris = new();
    List<NodePohon> _pohon = new();
    readonly HashSet<string> _tertutup = new();
    DataSkenario? _dataTerpasang;
    Kegiatan? _kegiatanTerpasang;
    bool _bangunDijadwalkan;

    public BelanjaView()
    {
        InitializeComponent();
        Daftar.ItemsSource = _baris;
        UiBantu.TeruskanRoda(TabelPaket);
        UiBantu.TeruskanRoda(EdRab.Tabel);
        EdRab.SumberDanaAwal = () => (TabelPaket.SelectedItem as Paket)?.SumberDana ?? "";
        SizeChanged += (_, e) => AturTataLetak(e.NewSize.Width < 900);

        Loaded += (_, _) =>
        {
            S.DataDiganti += SaatDataDiganti;
            S.HasilBerubah += SegarkanTotal;
            S.ReferensiBerubah += JadwalkanBangun;
            S.SebelumDataDiganti += AkhiriEdit;
            SaatDataDiganti();
        };
        Unloaded += (_, _) =>
        {
            AkhiriEdit();
            S.DataDiganti -= SaatDataDiganti;
            S.HasilBerubah -= SegarkanTotal;
            S.ReferensiBerubah -= JadwalkanBangun;
            S.SebelumDataDiganti -= AkhiriEdit;
        };
    }

    bool? _sempit;

    /// <summary>Layar lebar: daftar di kiri, detail di kanan. Layar sempit: daftar di atas, detail di bawah.</summary>
    void AturTataLetak(bool sempit)
    {
        if (_sempit == sempit) return;
        _sempit = sempit;
        if (sempit)
        {
            Kol0.Width = new GridLength(1, GridUnitType.Star); Kol0.MinWidth = 0;
            Kol1.Width = new GridLength(0); Kol2.Width = new GridLength(0); Kol2.MinWidth = 0;
            Bar0.Height = new GridLength(2, GridUnitType.Star); Bar0.MinHeight = 180;
            Bar1.Height = new GridLength(8);
            Bar2.Height = new GridLength(3, GridUnitType.Star); Bar2.MinHeight = 220;
            Grid.SetColumn(PanelKiri, 0); Grid.SetRow(PanelKiri, 0);
            Grid.SetColumn(Pemisah, 0); Grid.SetRow(Pemisah, 1);
            Grid.SetColumn(PanelKanan, 0); Grid.SetRow(PanelKanan, 2);
            Pemisah.ResizeDirection = GridResizeDirection.Rows;
            Pemisah.Cursor = Cursors.SizeNS;
        }
        else
        {
            Kol0.Width = new GridLength(3, GridUnitType.Star); Kol0.MinWidth = 250;
            Kol1.Width = new GridLength(8); Kol2.Width = new GridLength(7, GridUnitType.Star); Kol2.MinWidth = 380;
            Bar0.Height = new GridLength(1, GridUnitType.Star); Bar0.MinHeight = 0;
            Bar1.Height = new GridLength(0);
            Bar2.Height = new GridLength(0); Bar2.MinHeight = 0;
            Grid.SetColumn(PanelKiri, 0); Grid.SetRow(PanelKiri, 0);
            Grid.SetColumn(Pemisah, 1); Grid.SetRow(Pemisah, 0);
            Grid.SetColumn(PanelKanan, 2); Grid.SetRow(PanelKanan, 0);
            Pemisah.ResizeDirection = GridResizeDirection.Columns;
            Pemisah.Cursor = Cursors.SizeWE;
        }
    }

    void AkhiriEdit()
    {
        UiBantu.AkhiriEdit(TabelPaket);
        EdRab.AkhiriEdit();
    }

    // ------------------------------------------------------------ pohon

    void SaatDataDiganti()
    {
        if (_dataTerpasang != null) _dataTerpasang.Kegiatan.CollectionChanged -= KegiatanBerubah;
        _dataTerpasang = S.Data;
        if (_dataTerpasang != null) _dataTerpasang.Kegiatan.CollectionChanged += KegiatanBerubah;
        Bangun();
    }

    void KegiatanBerubah(object? sender, NotifyCollectionChangedEventArgs e) => JadwalkanBangun();

    void JadwalkanBangun()
    {
        if (_bangunDijadwalkan) return;
        _bangunDijadwalkan = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _bangunDijadwalkan = false;
            Bangun();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    void Bangun()
    {
        var kunciTerpilih = (Daftar.SelectedItem as NodePohon)?.Kunci;
        foreach (var n in SemuaNode(_pohon))
            if (n.Kegiatan != null) n.Kegiatan.PropertyChanged -= KegiatanPropBerubah;

        _pohon = new List<NodePohon>();
        var d = S.Data;
        if (d != null)
        {
            var refr = S.Referensi;
            var kodeBidang = refr.Bidang.Select(b => b.Kode)
                .Union(d.Kegiatan.Select(k => k.KodeSubBidang.Split('.')[0]))
                .Distinct().OrderBy(k => k, KodeComparer.Instance);
            foreach (var kb in kodeBidang)
            {
                var namaB = refr.Bidang.FirstOrDefault(b => b.Kode == kb)?.Nama ?? "(bidang tidak ada di referensi)";
                var nb = new NodePohon
                {
                    Jenis = "B", KodeBidang = kb, Kode = kb, Nama = namaB, Level = 0,
                    HitungTotal = () => d.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == kb).Sum(k => k.Total)
                };
                var kodeSub = refr.SubBidang.Where(s => s.KodeBidang == kb).Select(s => s.Kode)
                    .Union(d.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == kb).Select(k => k.KodeSubBidang))
                    .Distinct().OrderBy(k => k, KodeComparer.Instance);
                foreach (var ks in kodeSub)
                {
                    var namaS = refr.SubBidang.FirstOrDefault(s => s.Kode == ks)?.Nama ?? "(sub bidang tidak ada di referensi)";
                    var keg = d.Kegiatan.Where(k => k.KodeSubBidang == ks).OrderBy(k => k.NoUrut).ToList();
                    var ns = new NodePohon
                    {
                        Jenis = "S", KodeBidang = kb, KodeSub = ks, Kode = ks, Nama = namaS, Level = 1, Kosong = keg.Count == 0,
                        HitungTotal = () => d.Kegiatan.Where(k => k.KodeSubBidang == ks).Sum(k => k.Total)
                    };
                    foreach (var k in keg)
                    {
                        var kk = k;
                        k.PropertyChanged += KegiatanPropBerubah;
                        ns.Anak.Add(new NodePohon { Jenis = "K", KodeBidang = kb, KodeSub = ks, Kegiatan = kk, Level = 2, HitungTotal = () => kk.Total });
                    }
                    nb.Anak.Add(ns);
                }
                nb.Kosong = nb.Anak.All(a => a.Kosong);
                _pohon.Add(nb);
            }
        }

        foreach (var n in SemuaNode(_pohon))
            if (n.Jenis != "K") n.Terbuka = !_tertutup.Contains(n.Kunci);

        Ratakan(kunciTerpilih);
    }

    static IEnumerable<NodePohon> SemuaNode(IEnumerable<NodePohon> akar)
    {
        foreach (var n in akar)
        {
            yield return n;
            foreach (var a in SemuaNode(n.Anak)) yield return a;
        }
    }

    void Ratakan(string? kunciTerpilih)
    {
        var hanyaIsi = ChkHanyaIsi.IsChecked == true;
        var list = new List<NodePohon>();
        void Tambah(NodePohon n)
        {
            if (hanyaIsi && n.Jenis != "K" && n.Kosong) return;
            list.Add(n);
            if (n.Terbuka) foreach (var a in n.Anak) Tambah(a);
        }
        foreach (var n in _pohon) Tambah(n);

        _baris.Clear();
        foreach (var n in list) _baris.Add(n);

        var pilih = kunciTerpilih == null ? null : _baris.FirstOrDefault(n => n.Kunci == kunciTerpilih);
        if (pilih != null)
        {
            Daftar.SelectedItem = pilih;
            Daftar.ScrollIntoView(pilih);
        }
        else
        {
            TampilkanPanel(null);
        }
    }

    void KegiatanPropBerubah(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Kegiatan.KodeSubBidang) or nameof(Kegiatan.NoUrut))
        {
            JadwalkanBangun();
        }
        else if (e.PropertyName == nameof(Kegiatan.Nama))
        {
            foreach (var n in _baris.Where(n => n.Kegiatan == sender)) n.Segarkan();
        }
    }

    void SegarkanTotal()
    {
        foreach (var n in SemuaNode(_pohon)) n.Segarkan();
        if (Daftar.SelectedItem is NodePohon { Jenis: "S" } ns) IsiPanelSub(ns);
    }

    void Panah_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not NodePohon n || n.Jenis == "K") return;
        n.Terbuka = !n.Terbuka;
        if (n.Terbuka) _tertutup.Remove(n.Kunci); else _tertutup.Add(n.Kunci);
        Ratakan((Daftar.SelectedItem as NodePohon)?.Kunci ?? n.Kunci);
        e.Handled = true;
    }

    void Daftar_KeyDown(object sender, KeyEventArgs e)
    {
        if (Daftar.SelectedItem is not NodePohon n || n.Jenis == "K") return;
        if (e.Key == Key.Left && n.Terbuka || e.Key == Key.Right && !n.Terbuka)
        {
            n.Terbuka = !n.Terbuka;
            if (n.Terbuka) _tertutup.Remove(n.Kunci); else _tertutup.Add(n.Kunci);
            Ratakan(n.Kunci);
            e.Handled = true;
        }
    }

    void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) Ratakan((Daftar.SelectedItem as NodePohon)?.Kunci);
    }

    void BukaSemua_Click(object sender, RoutedEventArgs e)
    {
        _tertutup.Clear();
        foreach (var n in SemuaNode(_pohon)) n.Terbuka = true;
        Ratakan((Daftar.SelectedItem as NodePohon)?.Kunci);
    }

    void LipatSemua_Click(object sender, RoutedEventArgs e)
    {
        foreach (var n in SemuaNode(_pohon).Where(n => n.Jenis == "B"))
        {
            n.Terbuka = false;
            _tertutup.Add(n.Kunci);
        }
        Ratakan(null);
    }

    // ------------------------------------------------------------ panel kanan

    void Daftar_SelectionChanged(object sender, SelectionChangedEventArgs e) => TampilkanPanel(Daftar.SelectedItem as NodePohon);

    void TampilkanPanel(NodePohon? n)
    {
        AkhiriEdit();
        PanelKosong.Visibility = n == null || n.Jenis == "B" ? Visibility.Visible : Visibility.Collapsed;
        PanelSub.Visibility = n?.Jenis == "S" ? Visibility.Visible : Visibility.Collapsed;
        PanelKegiatan.Visibility = n?.Jenis == "K" ? Visibility.Visible : Visibility.Collapsed;

        if (n?.Jenis == "S") IsiPanelSub(n);
        PasangKegiatan(n?.Kegiatan);
    }

    void IsiPanelSub(NodePohon n)
    {
        TxtSubJudul.Text = $"{n.KodeSub}  {n.Nama}";
        var jml = n.Anak.Count;
        TxtSubTotal.Text = $"{jml} kegiatan · total {Kalkulasi.Rp(n.Total)}";
        var refs = S.Referensi.Kegiatan.Where(r => r.KodeSubBidang == n.KodeSub).OrderBy(r => r.Kode, KodeComparer.Instance).ToList();
        if (DaftarRef.ItemsSource is not List<RefKegiatan> lama || !lama.SequenceEqual(refs))
            DaftarRef.ItemsSource = refs;
        TxtRefKosong.Visibility = refs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void PasangKegiatan(Kegiatan? k)
    {
        if (ReferenceEquals(_kegiatanTerpasang, k)) return;
        _kegiatanTerpasang = k;
        IsiKegiatan.DataContext = k;
        if (k == null)
        {
            EdRab.Items = null;
            return;
        }
        TabelPaket.SelectedItem = k.Paket.FirstOrDefault();
        PasangPaket();
    }

    void TabelPaket_SelectionChanged(object sender, SelectionChangedEventArgs e) => PasangPaket();

    void PasangPaket()
    {
        var p = TabelPaket.SelectedItem as Paket;
        if (p == null && _kegiatanTerpasang?.Paket.Count > 0)
        {
            TabelPaket.SelectedItem = _kegiatanTerpasang.Paket[0];
            return;
        }
        EdRab.Items = p?.Rincian;
        EdRab.Visibility = p == null ? Visibility.Collapsed : Visibility.Visible;
        TxtTanpaPaket.Visibility = p == null ? Visibility.Visible : Visibility.Collapsed;
        TxtJudulRab.Text = p == null ? "Rincian RAB" : $"Rincian RAB – Paket: {(string.IsNullOrWhiteSpace(p.Nama) ? "(tanpa nama)" : p.Nama)}";
    }

    internal void GulirUntukUji(double offset) => PanelKegiatan.ScrollToVerticalOffset(offset);

    internal void PilihUntukUji(int indeksKegiatan)
    {
        Bangun();
        var n = _baris.Where(x => x.Jenis == "K").ElementAtOrDefault(indeksKegiatan);
        if (n != null) Daftar.SelectedItem = n;
    }

    // ------------------------------------------------------------ aksi kegiatan

    int NomorBerikut(string kodeSub)
    {
        var d = S.Data!;
        var dipakai = d.Kegiatan.Where(k => k.KodeSubBidang == kodeSub).Select(k => k.NoUrut).ToHashSet();
        int n = 1;
        while (dipakai.Contains(n)) n++;
        return n;
    }

    void SisipkanUrut(Kegiatan k)
    {
        var d = S.Data!;
        int idx = 0;
        while (idx < d.Kegiatan.Count && KodeComparer.Instance.Compare(d.Kegiatan[idx].Kode, k.Kode) <= 0) idx++;
        d.Kegiatan.Insert(idx, k);
    }

    Kegiatan BuatKegiatan(string kodeSub, string nama, int? no)
    {
        var d = S.Data!;
        var n = no ?? NomorBerikut(kodeSub);
        if (d.Kegiatan.Any(k => k.KodeSubBidang == kodeSub && k.NoUrut == n)) n = NomorBerikut(kodeSub);
        var k = new Kegiatan { KodeSubBidang = kodeSub, NoUrut = n, Nama = nama, Lokasi = S.Desa.NamaDesa };
        k.Paket.Add(new Paket { Nama = string.IsNullOrWhiteSpace(nama) ? "Paket 1" : nama, Lokasi = S.Desa.NamaDesa });
        SisipkanUrut(k);
        PilihKegiatan(k);
        return k;
    }

    void PilihKegiatan(Kegiatan k)
    {
        _tertutup.Remove("B:" + k.KodeSubBidang.Split('.')[0]);
        _tertutup.Remove("S:" + k.KodeSubBidang);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Bangun();
            var n = _baris.FirstOrDefault(x => x.Kegiatan == k);
            if (n != null)
            {
                Daftar.SelectedItem = n;
                Daftar.ScrollIntoView(n);
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    void TambahKegiatan_Click(object sender, RoutedEventArgs e)
    {
        if (!S.BisaUbah || S.Data == null) return;
        AkhiriEdit();
        if (Daftar.SelectedItem is not NodePohon n || n.Jenis == "B")
        {
            Pesan.Info("Pilih dulu SUB BIDANG di daftar sebelah kiri (misalnya 02.03 Pekerjaan Umum dan Penataan Ruang), lalu tekan \"+ Kegiatan\".");
            return;
        }
        BuatKegiatan(n.KodeSub, "", null);
    }

    void TambahDariRef_Click(object sender, RoutedEventArgs e)
    {
        if (!S.BisaUbah || S.Data == null || (sender as FrameworkElement)?.Tag is not RefKegiatan r) return;
        AkhiriEdit();
        int? no = int.TryParse(r.Kode.Split('.').Last(), out var x) ? x : null;
        var ada = S.Data.Kegiatan.FirstOrDefault(k => k.KodeSubBidang == r.KodeSubBidang && k.NoUrut == no);
        if (ada != null && !Pesan.Tanya($"Kode {r.Kode} sudah dipakai oleh kegiatan \"{ada.Nama}\".\n\nTetap tambahkan dengan nomor urut berikutnya?"))
            return;
        BuatKegiatan(r.KodeSubBidang, r.Nama, no);
    }

    void SalinKegiatan_Click(object sender, RoutedEventArgs e)
    {
        if (!S.BisaUbah || S.Data == null || _kegiatanTerpasang == null) return;
        AkhiriEdit();
        var k = _kegiatanTerpasang.Salin();
        k.NoUrut = NomorBerikut(k.KodeSubBidang);
        k.Nama += " (salinan)";
        SisipkanUrut(k);
        PilihKegiatan(k);
    }

    void HapusKegiatan_Click(object sender, RoutedEventArgs e)
    {
        if (!S.BisaUbah || S.Data == null || _kegiatanTerpasang == null) return;
        AkhiriEdit();
        var k = _kegiatanTerpasang;
        var rincian = k.Paket.Sum(p => p.Rincian.Count);
        if (!Pesan.Tanya($"Hapus kegiatan {k.Kode} \"{k.Nama}\"?\n\n{k.Paket.Count} paket, {rincian} rincian, nilai {Kalkulasi.Rp(k.Total)} ikut terhapus.\n" +
                         "(Bisa dikembalikan dengan tombol Batalkan.)"))
            return;
        PasangKegiatan(null);
        S.Data.Kegiatan.Remove(k);
    }

    void PilihRef_Click(object sender, RoutedEventArgs e)
    {
        var k = _kegiatanTerpasang;
        if (k == null || !S.BisaUbah) return;
        var refs = S.Referensi.Kegiatan.Where(r => r.KodeSubBidang == k.KodeSubBidang).OrderBy(r => r.Kode, KodeComparer.Instance).ToList();
        var m = new ContextMenu { PlacementTarget = BtnPilihRef, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom, MaxWidth = 640 };
        if (refs.Count == 0)
            m.Items.Add(new MenuItem { Header = new TextBlock { Text = "Belum ada referensi kegiatan untuk sub bidang ini.", TextWrapping = TextWrapping.Wrap }, IsEnabled = false });
        foreach (var r in refs)
        {
            var mi = new MenuItem { Header = new TextBlock { Text = $"{r.Kode}  {r.Nama}", TextWrapping = TextWrapping.Wrap, MaxWidth = 600 } };
            var rr = r;
            mi.Click += (_, _) =>
            {
                k.Nama = rr.Nama;
                if (int.TryParse(rr.Kode.Split('.').Last(), out var no) &&
                    !S.Data!.Kegiatan.Any(x => x != k && x.KodeSubBidang == k.KodeSubBidang && x.NoUrut == no))
                    k.NoUrut = no;
            };
            m.Items.Add(mi);
        }
        m.IsOpen = true;
    }

    // ------------------------------------------------------------ aksi paket

    void TambahPaket_Click(object sender, RoutedEventArgs e)
    {
        var k = _kegiatanTerpasang;
        if (k == null || !S.BisaUbah) return;
        AkhiriEdit();
        var acuan = TabelPaket.SelectedItem as Paket ?? k.Paket.LastOrDefault();
        var p = new Paket
        {
            Nama = $"Paket {k.Paket.Count + 1}",
            Lokasi = acuan?.Lokasi ?? k.Lokasi,
            Pola = acuan?.Pola ?? "Swakelola",
            Sifat = acuan?.Sifat ?? "Non Fisik",
        };
        p.SumberDana = acuan?.SumberDana ?? "";
        k.Paket.Add(p);
        TabelPaket.SelectedItem = p;
        UiBantu.EditSel(TabelPaket, p, 0);
    }

    void SalinPaket_Click(object sender, RoutedEventArgs e)
    {
        var k = _kegiatanTerpasang;
        if (k == null || !S.BisaUbah || TabelPaket.SelectedItem is not Paket p) return;
        AkhiriEdit();
        var s = p.Salin();
        s.Nama += " (salinan)";
        k.Paket.Insert(k.Paket.IndexOf(p) + 1, s);
        TabelPaket.SelectedItem = s;
    }

    void HapusPaket_Click(object sender, RoutedEventArgs e)
    {
        var k = _kegiatanTerpasang;
        if (k == null || !S.BisaUbah || TabelPaket.SelectedItem is not Paket p) return;
        AkhiriEdit();
        if (p.Rincian.Count > 0 &&
            !Pesan.Tanya($"Hapus paket \"{p.Nama}\" beserta {p.Rincian.Count} rincian senilai {Kalkulasi.Rp(p.Total)}?\n(Bisa dikembalikan dengan tombol Batalkan.)"))
            return;
        EdRab.Items = null;
        k.Paket.Remove(p);
        TabelPaket.SelectedItem = k.Paket.FirstOrDefault();
        PasangPaket();
    }
}
