using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public enum JenisIsian { Teks, TeksPanjang, Angka, Bulat, Pilihan, PilihanBebas, Kode, Info, Centang }

/// <summary>Satu kolom tabel.</summary>
public class Kolom
{
    public string Judul { get; init; } = "";
    public string Path { get; init; } = "";
    /// <summary>Lebar tetap (piksel). 0 = mengisi sisa ruang.</summary>
    public double Lebar { get; init; }
    public double Bintang { get; init; } = 1;
    public double MinLebar { get; init; } = 120;
    public bool Angka { get; init; }
    public bool Tebal { get; init; }
    /// <summary>Teks boleh dua baris (untuk nama/uraian panjang).</summary>
    public bool DuaBaris { get; init; }
    public string? Format { get; init; }

    public Kolom() { }
    public Kolom(string judul, string path, double lebar = 0) { Judul = judul; Path = path; Lebar = lebar; }
}

public record PilihanKode(string Nilai, string Kode, string Nama);

/// <summary>Satu isian pada form di bawah tabel.</summary>
public class Isian
{
    public string Label { get; init; } = "";
    public string Path { get; init; } = "";
    public JenisIsian Jenis { get; init; } = JenisIsian.Teks;
    public bool Wajib { get; init; }
    /// <summary>Isian memakai satu baris penuh.</summary>
    public bool Penuh { get; init; }
    public string? Petunjuk { get; init; }
    public string? Format { get; init; }
    public Func<IEnumerable>? Pilihan { get; init; }
    public string? TampilPath { get; init; }
    public string? NilaiPath { get; init; }
    /// <summary>Untuk Jenis Kode: daftar pilihan berdasarkan objek yang sedang diisi.</summary>
    public Func<object, IEnumerable<PilihanKode>>? DaftarKode { get; init; }
    public Func<string, string>? NamaKode { get; init; }
    public Action<object, PilihanKode>? SetelahPilih { get; init; }
    public int MaksKarakter { get; init; }
}

/// <summary>
/// Halaman data bergaya Siskeudes: tabel (hanya-baca) di atas, form isian di bawah, tombol
/// Tambah · Ubah · Hapus · Batal · Simpan. Perubahan dilakukan pada salinan dan baru diterapkan saat Simpan.
/// </summary>
public class HalamanData : UserControl
{
    // ---------------- konfigurasi ----------------
    public string JudulData { get; set; } = "Data";
    public Func<string?>? Konteks { get; set; }
    /// <summary>Bila mengembalikan teks, halaman belum bisa dipakai (mis. induk belum dipilih).</summary>
    public Func<string?>? Syarat { get; set; }
    public List<Kolom> Kolom { get; } = new();
    public List<Isian> Isian { get; } = new();
    public Func<IList?> Daftar { get; set; } = () => null;
    public Func<IEnumerable?>? Tampilan { get; set; }
    public Func<object?>? BuatBaru { get; set; }
    public Func<object, bool, string?>? Validasi { get; set; }
    public Action<object>? Tambahkan { get; set; }
    public Action<object>? HapusItem { get; set; }
    public Func<object, string?>? PesanHapus { get; set; }
    public Func<object, string>? Kunci { get; set; }
    public Func<bool> BolehUbah { get; set; } = () => AppState.Instance.BisaUbah;
    public bool TanpaForm { get; set; }
    public Action? AksiTambah { get; set; }
    public Action? AksiUbah { get; set; }
    public Action? AksiHapus { get; set; }
    public Action<object>? SetelahSimpan { get; set; }
    public string LabelTambah { get; set; } = "Tambah";

    public event Action<object?>? PilihanBerubah;

    // ---------------- komponen ----------------
    readonly DataGrid _tabel = new() { IsReadOnly = true };
    readonly TextBlock _konteks = new() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
    readonly Border _barKonteks;
    readonly TextBlock _kosong = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12), Foreground = (Brush)Application.Current.FindResource("WarnaRedup"), VerticalAlignment = VerticalAlignment.Top };
    readonly Grid _form = new() { Margin = new Thickness(10, 8, 10, 2) };
    readonly Border _bingkaiForm;
    readonly Border _bingkaiBawah;
    readonly WrapPanel _tombol = new() { Margin = new Thickness(8, 6, 8, 2) };
    readonly TextBlock _jumlah = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 4), Foreground = (Brush)Application.Current.FindResource("WarnaRedup") };
    readonly TextBlock _mode = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 4), FontWeight = FontWeights.SemiBold };
    Button _bTambah = null!, _bUbah = null!, _bHapus = null!, _bBatal = null!, _bSimpan = null!;
    readonly List<(Button b, bool butuhUbah, bool butuhPilih)> _tombolTambahan = new();
    readonly List<(Isian isi, FrameworkElement label, FrameworkElement editor, List<Control> kontrol)> _editor = new();
    bool _dibangun;
    bool? _sempit;

    object? _draf;
    bool _baru;
    public bool SedangMengisi { get; private set; }

    public HalamanData()
    {
        var akar = new Grid();
        akar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        akar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 80 });
        akar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        akar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _barKonteks = new Border { Background = (Brush)Application.Current.FindResource("Primer"), Padding = new Thickness(10, 5, 10, 5), Child = _konteks };
        akar.Children.Add(_barKonteks);

        var tengah = new Grid();
        tengah.Children.Add(_tabel);
        tengah.Children.Add(_kosong);
        Grid.SetRow(tengah, 1);
        akar.Children.Add(tengah);

        _bingkaiForm = new Border
        {
            Background = Brushes.White, BorderBrush = (Brush)Application.Current.FindResource("Garis"),
            BorderThickness = new Thickness(1, 0, 1, 1),
            Child = new ScrollViewer { Content = _form, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Focusable = false }
        };
        Grid.SetRow(_bingkaiForm, 2);
        akar.Children.Add(_bingkaiForm);

        var bawah = new DockPanel { Background = (Brush)Application.Current.FindResource("PrimerMuda") };
        DockPanel.SetDock(_jumlah, Dock.Right);
        bawah.Children.Add(_jumlah);
        bawah.Children.Add(_tombol);
        _bingkaiBawah = new Border { BorderBrush = (Brush)Application.Current.FindResource("Garis"), BorderThickness = new Thickness(1, 0, 1, 1), Child = bawah };
        Grid.SetRow(_bingkaiBawah, 3);
        akar.Children.Add(_bingkaiBawah);

        Content = akar;

        _tabel.SelectionChanged += (_, _) => { if (!SedangMengisi) TampilkanTerpilih(); };
        _tabel.MouseDoubleClick += (_, e) =>
        {
            if (e.OriginalSource is DependencyObject d && UiBantu.Leluhur<DataGridRow>(d) != null) Ubah();
        };
        _tabel.PreviewKeyDown += SaatTombolTabel;
        PreviewKeyDown += SaatTombol;
        SizeChanged += (_, e) =>
        {
            AturForm(e.NewSize.Width < 760);
            AturTinggiForm();
        };
        LayoutUpdated += (_, _) => AturTinggiForm();
        Loaded += (_, _) =>
        {
            Bangun();
            AppState.Instance.PropertyChanged += SaatStatusBerubah;
            Segarkan();
        };
        Unloaded += (_, _) => AppState.Instance.PropertyChanged -= SaatStatusBerubah;
    }

    void SaatStatusBerubah(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppState.BisaUbah)) PerbaruiTombol();
    }

    public DataGrid Tabel => _tabel;
    public object? Terpilih => _tabel.SelectedItem;

    // ================================================================ bangun

    void Bangun()
    {
        if (_dibangun) return;
        _dibangun = true;

        foreach (var k in Kolom)
        {
            var b = new Binding(k.Path) { Mode = BindingMode.OneWay };
            if (k.Format != null) b.StringFormat = k.Format;
            else if (k.Angka) b.StringFormat = "#,##0.##";
            var gaya = new Style(typeof(TextBlock));
            gaya.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            gaya.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            if (k.Angka) gaya.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            if (k.Tebal) gaya.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            if (k.DuaBaris)
            {
                gaya.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                gaya.Setters.Add(new Setter(TextBlock.MaxHeightProperty, 36.0));
            }
            var tip = new Binding(k.Path) { Mode = BindingMode.OneWay };
            if (b.StringFormat != null) tip.StringFormat = b.StringFormat;
            gaya.Setters.Add(new Setter(ToolTipProperty, tip));
            var kol = new DataGridTextColumn
            {
                Header = k.Judul,
                Binding = b,
                IsReadOnly = true,
                ElementStyle = gaya,
                Width = k.Lebar > 0 ? new DataGridLength(k.Lebar) : new DataGridLength(k.Bintang, DataGridLengthUnitType.Star),
                MinWidth = k.Lebar > 0 ? k.Lebar : k.MinLebar
            };
            _tabel.Columns.Add(kol);
        }

        foreach (var isi in Isian) _editor.Add(BuatEditor(isi));
        _bingkaiForm.Visibility = TanpaForm || Isian.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        AturForm(ActualWidth > 0 && ActualWidth < 760, paksa: true);

        _bTambah = Tombol(LabelTambah, () => Tambah(), "Insert");
        _bUbah = Tombol("Ubah", Ubah, "F2 / klik dua kali");
        _bHapus = Tombol("Hapus", Hapus, "Delete");
        _bHapus.Style = (Style)Application.Current.FindResource("TombolBahaya");
        _bBatal = Tombol("Batal", Batal, "Esc");
        _bSimpan = Tombol("Simpan", () => Simpan(), "Ctrl+S");
        _bSimpan.Style = (Style)Application.Current.FindResource("TombolUtama");
        _tombol.Children.Add(_bTambah);
        _tombol.Children.Add(_bUbah);
        _tombol.Children.Add(_bHapus);
        if (!TanpaForm)
        {
            _tombol.Children.Add(_bBatal);
            _tombol.Children.Add(_bSimpan);
        }
        foreach (var (b, _, _) in _tombolTambahan) _tombol.Children.Add(b);
        _tombol.Children.Add(_mode);
        if (TanpaForm && AksiTambah == null && BuatBaru == null)
        {
            _bTambah.Visibility = _bUbah.Visibility = _bHapus.Visibility = Visibility.Collapsed;
        }
    }

    Button Tombol(string teks, Action aksi, string pintasan)
    {
        var b = new Button { Content = teks, ToolTip = $"{teks} ({pintasan})" };
        b.Click += (_, _) => Jalankan(aksi);
        return b;
    }

    /// <summary>Tambah tombol khusus di bilah tombol.</summary>
    public void TambahTombol(string teks, Action aksi, bool butuhUbah = true, bool butuhPilih = false, string? tip = null)
    {
        var b = new Button { Content = teks, ToolTip = tip };
        b.Click += (_, _) => Jalankan(aksi);
        _tombolTambahan.Add((b, butuhUbah, butuhPilih));
    }

    static void Jalankan(Action aksi)
    {
        try { aksi(); }
        catch (Exception ex) { Log.Error(ex, "Aksi halaman"); Pesan.Galat("Perintah gagal dijalankan.", ex); }
    }

    (Isian, FrameworkElement, FrameworkElement, List<Control>) BuatEditor(Isian isi)
    {
        var label = new TextBlock
        {
            Text = isi.Label + (isi.Wajib ? " *" : ""),
            Style = (Style)Application.Current.FindResource("Label"),
            MaxWidth = 170,
            ToolTip = isi.Petunjuk
        };
        var kontrol = new List<Control>();
        FrameworkElement editor;
        Binding B(string? format = null, UpdateSourceTrigger t = UpdateSourceTrigger.LostFocus)
        {
            var b = new Binding(isi.Path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = t, ValidatesOnExceptions = true };
            if (format != null) b.StringFormat = format;
            return b;
        }

        switch (isi.Jenis)
        {
            case JenisIsian.TeksPanjang:
            {
                var tb = new TextBox { TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, MinHeight = 44, MaxHeight = 96, VerticalContentAlignment = VerticalAlignment.Top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                tb.SetBinding(TextBox.TextProperty, B(t: UpdateSourceTrigger.Explicit));
                kontrol.Add(tb); editor = tb; break;
            }
            case JenisIsian.Angka:
            case JenisIsian.Bulat:
            {
                var tb = new TextBox { TextAlignment = TextAlignment.Right, MaxWidth = 220, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 120 };
                tb.SetBinding(TextBox.TextProperty, B(isi.Format ?? (isi.Jenis == JenisIsian.Bulat ? "0" : "#,##0.##"), UpdateSourceTrigger.Explicit));
                tb.GotKeyboardFocus += (_, _) => tb.SelectAll();
                // Perbarui salinan saat pindah isian agar nilai turunan (mis. Jumlah) ikut tampil
                tb.LostKeyboardFocus += (_, _) => { if (SedangMengisi) tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource(); };
                kontrol.Add(tb); editor = tb; break;
            }
            case JenisIsian.Pilihan:
            {
                var cb = new ComboBox { ItemsSource = isi.Pilihan?.Invoke(), MaxWidth = 420, HorizontalAlignment = HorizontalAlignment.Stretch };
                if (isi.TampilPath != null) cb.DisplayMemberPath = isi.TampilPath;
                if (isi.NilaiPath != null)
                {
                    cb.SelectedValuePath = isi.NilaiPath;
                    cb.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, B(t: UpdateSourceTrigger.Explicit));
                }
                else cb.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, B(t: UpdateSourceTrigger.Explicit));
                kontrol.Add(cb); editor = cb; break;
            }
            case JenisIsian.PilihanBebas:
            {
                var cb = new ComboBox { IsEditable = true, ItemsSource = isi.Pilihan?.Invoke(), MaxWidth = 420 };
                cb.SetBinding(ComboBox.TextProperty, B(t: UpdateSourceTrigger.Explicit));
                kontrol.Add(cb); editor = cb; break;
            }
            case JenisIsian.Kode:
            {
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var tb = new TextBox();
                tb.SetBinding(TextBox.TextProperty, B(isi.Format, UpdateSourceTrigger.Explicit));
                var tombol = new Button { Content = "…", MinWidth = 30, Padding = new Thickness(6, 0, 6, 0), Margin = new Thickness(4, 0, 0, 0), ToolTip = "Pilih dari daftar" };
                var nama = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Foreground = (Brush)Application.Current.FindResource("WarnaRedup") };
                if (isi.NamaKode != null) tb.TextChanged += (_, _) => nama.Text = isi.NamaKode(tb.Text);
                tombol.Click += (_, _) =>
                {
                    if (_draf == null || isi.DaftarKode == null) return;
                    var pilih = DialogPilih.Tampil(Window.GetWindow(this), "Pilih " + isi.Label, isi.DaftarKode(_draf));
                    if (pilih == null) return;
                    tb.Text = pilih.Nilai;
                    tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                    isi.SetelahPilih?.Invoke(_draf, pilih);
                    SegarkanEditor();
                };
                Grid.SetColumn(tombol, 1);
                Grid.SetColumn(nama, 2);
                g.Children.Add(tb); g.Children.Add(tombol); g.Children.Add(nama);
                kontrol.Add(tb); kontrol.Add(tombol);
                editor = g; break;
            }
            case JenisIsian.Info:
            {
                var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
                var b = new Binding(isi.Path) { Mode = BindingMode.OneWay };
                if (isi.Format != null) b.StringFormat = isi.Format;
                tb.SetBinding(TextBlock.TextProperty, b);
                editor = tb; break;
            }
            case JenisIsian.Centang:
            {
                var c = new CheckBox { Margin = new Thickness(0, 0, 0, 6) };
                c.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, B(t: UpdateSourceTrigger.Explicit));
                kontrol.Add(c); editor = c; break;
            }
            default:
            {
                var tb = new TextBox();
                if (isi.MaksKarakter > 0) tb.MaxLength = isi.MaksKarakter;
                tb.SetBinding(TextBox.TextProperty, B(t: UpdateSourceTrigger.Explicit));
                kontrol.Add(tb); editor = tb; break;
            }
        }
        if (isi.Jenis != JenisIsian.Info && isi.Jenis != JenisIsian.Centang && editor.Margin == default)
            editor.Margin = new Thickness(0, 0, 0, 6);
        if (isi.Petunjuk != null) editor.ToolTip = isi.Petunjuk;
        return (isi, label, editor, kontrol);
    }

    /// <summary>Dua kolom isian pada layar lebar, satu kolom pada layar sempit.</summary>
    void AturForm(bool sempit, bool paksa = false)
    {
        if (!_dibangun || (!paksa && _sempit == sempit)) return;
        _sempit = sempit;
        _form.Children.Clear();
        _form.RowDefinitions.Clear();
        _form.ColumnDefinitions.Clear();
        int kolomPasang = sempit ? 1 : 2;
        for (int i = 0; i < kolomPasang; i++)
        {
            _form.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 160 });
            if (i < kolomPasang - 1) _form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        }
        int baris = -1, pos = kolomPasang;
        foreach (var (isi, label, editor, _) in _editor)
        {
            if (isi.Penuh || pos >= kolomPasang) { baris++; pos = 0; _form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); }
            int kol = pos * 3;
            Grid.SetRow(label, baris); Grid.SetColumn(label, kol);
            Grid.SetRow(editor, baris); Grid.SetColumn(editor, kol + 1);
            Grid.SetColumnSpan(editor, isi.Penuh ? _form.ColumnDefinitions.Count - 1 : 1);
            _form.Children.Add(label);
            _form.Children.Add(editor);
            pos = isi.Penuh ? kolomPasang : pos + 1;
        }
    }

    /// <summary>Form tidak boleh mendorong tabel di bawah 80 px atau menutupi tombol; sisanya digulir.</summary>
    void AturTinggiForm()
    {
        var h = ActualHeight;
        if (h <= 0) return;
        var sisa = h - _barKonteks.ActualHeight - _bingkaiBawah.ActualHeight - 80 - 2;
        var maks = Math.Max(56, Math.Min(sisa, h * 0.5));
        if (Math.Abs(_bingkaiForm.MaxHeight - maks) > 0.5) _bingkaiForm.MaxHeight = maks;
    }

    // ================================================================ data

    public void Segarkan()
    {
        if (!_dibangun || SedangMengisi) return;
        try
        {
            var konteks = Konteks?.Invoke();
            _konteks.Text = string.IsNullOrWhiteSpace(konteks) ? JudulData : $"{JudulData}  ·  {konteks}";

            var syarat = Syarat?.Invoke();
            var kunciLama = _tabel.SelectedItem is { } lama ? (Kunci?.Invoke(lama) ?? null) : null;
            var objLama = _tabel.SelectedItem;
            if (syarat != null)
            {
                _tabel.ItemsSource = null;
                _tabel.Visibility = Visibility.Hidden;
                _kosong.Text = syarat;
                _kosong.Visibility = Visibility.Visible;
            }
            else
            {
                var items = (Tampilan ?? (() => Daftar()))();
                if (!ReferenceEquals(_tabel.ItemsSource, items)) _tabel.ItemsSource = items;
                _tabel.Visibility = Visibility.Visible;
                _kosong.Visibility = Visibility.Collapsed;
                object? pilih = null;
                if (items != null)
                {
                    foreach (var x in items)
                    {
                        if (ReferenceEquals(x, objLama) || (kunciLama != null && Kunci!(x) == kunciLama)) { pilih = x; break; }
                    }
                }
                if (pilih != null && !ReferenceEquals(_tabel.SelectedItem, pilih)) _tabel.SelectedItem = pilih;
                else if (pilih == null && _tabel.SelectedItem == null && items != null)
                {
                    foreach (var x in items) { _tabel.SelectedItem = x; break; }
                }
            }
            var n = _tabel.ItemsSource is ICollection c ? c.Count : _tabel.Items.Count;
            _jumlah.Text = syarat != null ? "" : $"{n} data";
            TampilkanTerpilih();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Segarkan " + JudulData);
        }
    }

    public void Pilih(object? item)
    {
        if (item == null) return;
        _tabel.SelectedItem = item;
        if (_tabel.SelectedItem != null) _tabel.ScrollIntoView(_tabel.SelectedItem);
    }

    public void PilihKunci(string kunci)
    {
        if (Kunci == null || _tabel.ItemsSource == null) return;
        foreach (var x in _tabel.ItemsSource)
            if (Kunci(x) == kunci) { Pilih(x); return; }
    }

    void TampilkanTerpilih()
    {
        if (SedangMengisi) return;
        _draf = _tabel.SelectedItem;
        _form.DataContext = _draf;
        AturHanyaBaca(true);
        SegarkanEditor();
        PerbaruiTombol();
        PilihanBerubah?.Invoke(_tabel.SelectedItem);
    }

    void SegarkanEditor()
    {
        foreach (var (_, _, _, kontrol) in _editor)
            foreach (var k in kontrol)
            {
                k.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                k.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
                k.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty)?.UpdateTarget();
                k.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty)?.UpdateTarget();
            }
    }

    void AturHanyaBaca(bool hanyaBaca)
    {
        foreach (var (_, _, _, kontrol) in _editor)
            foreach (var k in kontrol)
            {
                if (k is TextBox tb) tb.IsReadOnly = hanyaBaca;
                else if (k is ComboBox cb) cb.IsEnabled = !hanyaBaca;
                else k.IsEnabled = !hanyaBaca;
            }
    }

    void PerbaruiTombol()
    {
        if (!_dibangun) return;
        bool boleh = BolehUbah() && Syarat?.Invoke() == null;
        bool ada = _tabel.SelectedItem != null;
        _bTambah.IsEnabled = !SedangMengisi && boleh && (AksiTambah != null || BuatBaru != null);
        _bUbah.IsEnabled = !SedangMengisi && boleh && ada && (AksiUbah != null || !TanpaForm);
        _bHapus.IsEnabled = !SedangMengisi && boleh && ada;
        _bBatal.IsEnabled = SedangMengisi;
        _bSimpan.IsEnabled = SedangMengisi;
        foreach (var (b, butuhUbah, butuhPilih) in _tombolTambahan)
            b.IsEnabled = !SedangMengisi && (!butuhUbah || boleh) && (!butuhPilih || ada);
        _tabel.IsEnabled = !SedangMengisi;
        _mode.Text = SedangMengisi ? (_baru ? "● Menambah data baru" : "● Mengubah data") : "";
        _mode.Foreground = (Brush)Application.Current.FindResource("Primer");
        _bingkaiForm.Background = SedangMengisi ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFD, 0xF2)) : Brushes.White;
    }

    // ================================================================ aksi

    public void Tambah()
    {
        if (!_bTambah.IsEnabled) return;
        if (AksiTambah != null) { AksiTambah(); return; }
        var baru = BuatBaru?.Invoke();
        if (baru == null) return;
        MulaiMengisi(baru, true);
    }

    public void Ubah()
    {
        if (!_bUbah.IsEnabled || _tabel.SelectedItem is not { } item) return;
        if (AksiUbah != null) { AksiUbah(); return; }
        MulaiMengisi(AppState.SalinObjek(item), false);
    }

    void MulaiMengisi(object draf, bool baru)
    {
        _draf = draf;
        _baru = baru;
        SedangMengisi = true;
        _form.DataContext = draf;
        SegarkanEditor();
        AturHanyaBaca(false);
        PerbaruiTombol();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var pertama = _editor.SelectMany(e => e.kontrol).FirstOrDefault(k => k is TextBox { IsReadOnly: false } || k is ComboBox);
            pertama?.Focus();
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    public void Batal()
    {
        if (!SedangMengisi) return;
        SedangMengisi = false;
        TampilkanTerpilih();
    }

    public bool Simpan()
    {
        if (!SedangMengisi || _draf == null) return false;
        // Terapkan isian ke salinan
        bool galat = false;
        foreach (var (isi, _, _, kontrol) in _editor)
            foreach (var k in kontrol)
            {
                foreach (var dp in new DependencyProperty[] { TextBox.TextProperty, ComboBox.TextProperty, System.Windows.Controls.Primitives.Selector.SelectedValueProperty, System.Windows.Controls.Primitives.Selector.SelectedItemProperty, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty })
                {
                    if (dp == ComboBox.TextProperty && k is ComboBox { IsEditable: false }) continue;
                    var be = k.GetBindingExpression(dp);
                    if (be == null) continue;
                    be.UpdateSource();
                    if (be.HasError) galat = true;
                }
            }
        if (galat)
        {
            Pesan.Peringatan("Ada isian yang tidak valid (bertanda merah). Untuk angka, gunakan koma sebagai desimal, contoh 2,5.");
            return false;
        }
        foreach (var isi in Isian.Where(i => i.Wajib))
        {
            var v = BacaProperti(_draf, isi.Path);
            if (v == null || (v is string s && string.IsNullOrWhiteSpace(s)))
            {
                Pesan.Peringatan($"{isi.Label} wajib diisi.");
                return false;
            }
        }
        var pesan = Validasi?.Invoke(_draf, _baru);
        if (pesan != null) { Pesan.Peringatan(pesan); return false; }

        object hasil;
        if (_baru)
        {
            (Tambahkan ?? (x => Daftar()?.Add(x)))(_draf);
            hasil = _draf;
        }
        else
        {
            hasil = _tabel.SelectedItem!;
            SalinProperti(_draf, hasil);
        }
        SedangMengisi = false;
        Segarkan();
        Pilih(hasil);
        SetelahSimpan?.Invoke(hasil);
        return true;
    }

    public void Hapus()
    {
        if (!_bHapus.IsEnabled || _tabel.SelectedItem is not { } item) return;
        if (AksiHapus != null) { AksiHapus(); return; }
        var pesan = PesanHapus?.Invoke(item) ?? "Hapus data yang dipilih?";
        if (!Pesan.Tanya(pesan + "\n\n(Bisa dikembalikan dengan Batalkan / Ctrl+Z.)")) return;
        var idx = _tabel.SelectedIndex;
        (HapusItem ?? (x => Daftar()?.Remove(x)))(item);
        Segarkan();
        if (_tabel.Items.Count > 0) _tabel.SelectedIndex = Math.Clamp(idx, 0, _tabel.Items.Count - 1);
    }

    void SaatTombolTabel(object sender, KeyEventArgs e)
    {
        if (SedangMengisi) return;
        if (e.Key == Key.Insert) { Tambah(); e.Handled = true; }
        else if (e.Key is Key.F2 or Key.Enter) { Ubah(); e.Handled = true; }
        else if (e.Key == Key.Delete) { Hapus(); e.Handled = true; }
    }

    void SaatTombol(object sender, KeyEventArgs e)
    {
        if (!SedangMengisi) return;
        if (e.Key == Key.Escape) { Batal(); e.Handled = true; }
        else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0) { Simpan(); e.Handled = true; }
    }

    // ================================================================ refleksi

    static object? BacaProperti(object o, string path) => o.GetType().GetProperty(path)?.GetValue(o);

    /// <summary>Salin properti sederhana (teks, angka, tanggal, logika) dari salinan ke objek asli.</summary>
    public static void SalinProperti(object dari, object ke)
    {
        foreach (var p in ke.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite || p.Name == "Id" || p.GetIndexParameters().Length > 0) continue;
            if (p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null) continue;
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            if (!(t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime) || t.IsEnum)) continue;
            var v = p.GetValue(dari);
            if (!Equals(v, p.GetValue(ke))) p.SetValue(ke, v);
        }
    }
}
