using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Satu baris tabel anggaran kas (kegiatan atau rekening).</summary>
public class BarisKasTampil : Bindable
{
    public string Kode { get; init; } = "";
    public string Nama { get; init; } = "";
    public decimal Pagu { get; init; }
    public BarisKas Kas { get; }

    public BarisKasTampil(BarisKas kas, Action? saatDiubah = null)
    {
        Kas = kas;
        Kas.Bulan.CollectionChanged += (_, _) => { saatDiubah?.Invoke(); OnPropertyChanged(nameof(Total)); OnPropertyChanged(nameof(Selisih)); OnPropertyChanged(nameof(Status)); };
    }

    public decimal Total => Kas.Bulan.Sum();
    public decimal Selisih => Pagu - Total;
    public string Status => Selisih == 0 ? "Sesuai" : Selisih > 0 ? "Kurang" : "Lebih";
}

/// <summary>Data Entri › Penganggaran › Anggaran Kas Desa: rencana penerimaan & pengeluaran kas per bulan.</summary>
public class AnggaranKasView : UserControl
{
    public static readonly string[] NamaBulan = { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" };

    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = Gaya.Nav();
    readonly DataGrid _tabel = new() { CanUserResizeColumns = true, FrozenColumnCount = 2, SelectionUnit = DataGridSelectionUnit.CellOrRowHeader };
    readonly TextBlock _konteks = new() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold };
    readonly TextBlock _ringkas = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 4) };
    readonly WrapPanel _tombol = new() { Margin = new Thickness(6, 6, 6, 2), HorizontalAlignment = HorizontalAlignment.Right };

    public AnggaranKasView()
    {
        foreach (var n in new[] { "1. Belanja (per Kegiatan)", "2. Pendapatan (per Rekening)", "3. Pembiayaan (per Rekening)" })
            _nav.Items.Add(Gaya.ItemNav(n));
        _nav.SelectionChanged += (_, _) => Isi();

        var kanan = new Grid();
        kanan.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        kanan.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        kanan.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        kanan.Children.Add(new Border
        {
            Background = (Brush)Application.Current.FindResource("PrimerMuda"), BorderBrush = (Brush)Application.Current.FindResource("Garis"),
            BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(10, 5, 10, 5), Child = _konteks
        });
        Grid.SetRow(_tabel, 1);
        kanan.Children.Add(_tabel);
        var bawah = new DockPanel { Background = (Brush)Application.Current.FindResource("LatarPanel") };
        DockPanel.SetDock(_tombol, Dock.Right);
        bawah.Children.Add(_tombol);
        bawah.Children.Add(_ringkas);
        var bb = new Border { BorderBrush = (Brush)Application.Current.FindResource("Garis"), BorderThickness = new Thickness(0, 1, 0, 0), Child = bawah };
        Grid.SetRow(bb, 2);
        kanan.Children.Add(bb);
        Content = Gaya.DenganNav(_nav, kanan);

        BangunKolom();
        Tombol("Bagi Rata 12 Bulan (baris terpilih)", () => BagiRata(semua: false));
        Tombol("Bagi Rata Semua Baris", () => BagiRata(semua: true));
        Tombol("Kosongkan Baris Terpilih", Kosongkan);
        _tombol.Children.Add(new Button
        {
            Content = "Cetak", ToolTip = "Pratinjau laporan Rencana Anggaran Kas (RAK)",
        });
        ((Button)_tombol.Children[^1]).Click += (_, _) => IsianAnggaranView.BukaLaporan?.Invoke(Laporan.Rak);

        _nav.SelectedIndex = 0;
        Loaded += (_, _) => { S.DataDiganti += Isi; S.PropertyChanged += SaatStatus; Isi(); };
        Unloaded += (_, _) => { UiBantu.AkhiriEdit(_tabel); S.DataDiganti -= Isi; S.PropertyChanged -= SaatStatus; };
    }

    void SaatStatus(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppState.BisaUbah)) AturBisaUbah();
    }

    void AturBisaUbah()
    {
        _tabel.IsReadOnly = !S.BisaUbah;
        foreach (var b in _tombol.Children.OfType<Button>().Take(3)) b.IsEnabled = S.BisaUbah;
    }

    void Tombol(string teks, Action aksi)
    {
        var b = new Button { Content = teks };
        b.Click += (_, _) => { try { UiBantu.AkhiriEdit(_tabel); aksi(); } catch (Exception ex) { Log.Error(ex, teks); Pesan.Galat("Perintah gagal.", ex); } };
        _tombol.Children.Add(b);
    }

    void BangunKolom()
    {
        Style Gaya(bool angka, bool tebal = false)
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));
            if (angka) s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            else s.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            if (tebal) s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            return s;
        }
        var edit = new Style(typeof(TextBox));
        edit.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right));
        edit.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2, 1, 2, 1)));

        _tabel.Columns.Add(new DataGridTextColumn { Header = "Kode", Binding = new Binding("Kode") { Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 80, MinWidth = 80, ElementStyle = Gaya(false) });
        _tabel.Columns.Add(new DataGridTextColumn { Header = "Uraian", Binding = new Binding("Nama") { Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 220, MinWidth = 180, ElementStyle = Gaya(false) });
        _tabel.Columns.Add(new DataGridTextColumn { Header = "Anggaran (Rp)", Binding = new Binding("Pagu") { Mode = BindingMode.OneWay, StringFormat = "#,##0" }, IsReadOnly = true, Width = 115, MinWidth = 115, ElementStyle = Gaya(true, true) });
        for (int i = 0; i < 12; i++)
        {
            _tabel.Columns.Add(new DataGridTextColumn
            {
                Header = NamaBulan[i],
                Binding = new Binding($"Kas.Bulan[{i}]") { StringFormat = "#,##0", ValidatesOnExceptions = true },
                Width = 95, MinWidth = 95, ElementStyle = Gaya(true), EditingElementStyle = edit
            });
        }
        _tabel.Columns.Add(new DataGridTextColumn { Header = "Jumlah Kas (Rp)", Binding = new Binding("Total") { Mode = BindingMode.OneWay, StringFormat = "#,##0" }, IsReadOnly = true, Width = 115, MinWidth = 115, ElementStyle = Gaya(true, true) });
        var gayaSelisih = Gaya(true, true);
        var t = new DataTrigger { Binding = new Binding("Status"), Value = "Sesuai" };
        t.Setters.Add(new Setter(TextBlock.ForegroundProperty, Application.Current.FindResource("Hijau")));
        gayaSelisih.Triggers.Add(t);
        gayaSelisih.Setters.Add(new Setter(TextBlock.ForegroundProperty, Application.Current.FindResource("Merah")));
        _tabel.Columns.Add(new DataGridTextColumn { Header = "Belum Dijadwalkan (Rp)", Binding = new Binding("Selisih") { Mode = BindingMode.OneWay, StringFormat = "#,##0;(#,##0);0" }, IsReadOnly = true, Width = 120, MinWidth = 120, ElementStyle = gayaSelisih });
        _tabel.CellEditEnding += (_, _) => Dispatcher.BeginInvoke(new Action(PerbaruiRingkas), System.Windows.Threading.DispatcherPriority.Background);
    }

    List<BarisKasTampil> _baris = new();

    /// <summary>Isi tabel sesuai bagian yang dipilih.</summary>
    public void Isi()
    {
        UiBantu.AkhiriEdit(_tabel);
        var d = S.Data;
        _baris = new();
        if (d != null)
        {
            // Baris kas baru hanya disimpan setelah benar-benar diisi (melihat saja tidak mengubah data)
            BarisKasTampil Baris(string kunci, string kode, string nama, decimal pagu)
            {
                var kas = d.AnggaranKas.FirstOrDefault(x => x.Kunci == kunci);
                if (kas != null && kas.Bulan.Count != 12) kas.Pastikan12();
                kas ??= new BarisKas { Kunci = kunci };
                var k2 = kas;
                return new BarisKasTampil(kas, () => { if (!d.AnggaranKas.Contains(k2)) d.AnggaranKas.Add(k2); }) { Kode = kode, Nama = nama, Pagu = pagu };
            }
            switch (_nav.SelectedIndex)
            {
                case 1:
                    foreach (var g in d.Pendapatan.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
                        _baris.Add(Baris("R:" + g.Key, g.Key + ".", Lookup.NamaRekening(g.Key), g.Sum(r => r.Jumlah)));
                    break;
                case 2:
                    foreach (var g in d.PenerimaanPembiayaan.Concat(d.PengeluaranPembiayaan).GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
                        _baris.Add(Baris("R:" + g.Key, g.Key + ".", Lookup.NamaRekening(g.Key), g.Sum(r => r.Jumlah)));
                    break;
                default:
                    foreach (var k in d.Kegiatan.OrderBy(k => k.Kode, KodeComparer.Instance))
                        _baris.Add(Baris("K:" + k.Id, k.Kode + ".", k.Nama, k.Total));
                    break;
            }
        }
        _tabel.ItemsSource = _baris;
        var judul = _nav.SelectedIndex switch { 1 => "Anggaran Kas Pendapatan", 2 => "Anggaran Kas Pembiayaan", _ => "Anggaran Kas Belanja" };
        _konteks.Text = $"{judul}  ·  isi rencana per bulan (klik dua kali sel bulan). Jumlah kas setiap baris sebaiknya sama dengan anggarannya.";
        AturBisaUbah();
        PerbaruiRingkas();
    }

    void PerbaruiRingkas()
    {
        var belum = _baris.Count(b => b.Selisih != 0);
        _ringkas.Text = $"{_baris.Count} baris · anggaran {Kalkulasi.Rp(_baris.Sum(b => b.Pagu))} · dijadwalkan {Kalkulasi.Rp(_baris.Sum(b => b.Total))}" +
                        (belum > 0 ? $" · {belum} baris belum sesuai" : " · semua sesuai");
        _ringkas.Foreground = (Brush)Application.Current.FindResource(belum > 0 ? "Merah" : "Hijau");
    }

    void BagiRata(bool semua)
    {
        if (!S.BisaUbah) return;
        var target = semua ? _baris : _tabel.SelectedCells.Select(c => c.Item).OfType<BarisKasTampil>().Distinct().ToList();
        if (target.Count == 0 && _tabel.CurrentItem is BarisKasTampil cur) target = new() { cur };
        if (target.Count == 0) { Pesan.Info("Pilih baris terlebih dahulu."); return; }
        if (semua && _baris.Any(b => b.Total != 0) && !Pesan.Tanya("Ganti rencana kas semua baris menjadi dibagi rata 12 bulan?")) return;
        foreach (var b in target) IsiRata(b.Kas, b.Pagu);
        PerbaruiRingkas();
    }

    /// <summary>Bagi rata dalam rupiah bulat; sisa pembulatan masuk bulan Desember.</summary>
    public static void IsiRata(BarisKas kas, decimal pagu)
    {
        kas.Pastikan12();
        var per = Math.Floor(pagu / 12m);
        for (int i = 0; i < 12; i++) kas.Bulan[i] = per;
        kas.Bulan[11] = pagu - per * 11;
    }

    void Kosongkan()
    {
        if (!S.BisaUbah) return;
        foreach (var b in _tabel.SelectedCells.Select(c => c.Item).OfType<BarisKasTampil>().Distinct())
            for (int i = 0; i < 12; i++) b.Kas.Bulan[i] = 0;
        PerbaruiRingkas();
    }

    public void BukaBagian(int i) => _nav.SelectedIndex = i;
}
