using System.Collections;
using System.Windows;
using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Tabel rincian anggaran yang dipakai untuk pendapatan, pembiayaan, dan RAB belanja.</summary>
public partial class RincianEditor : UserControl
{
    public static readonly DependencyProperty RekeningSourceProperty =
        DependencyProperty.Register(nameof(RekeningSource), typeof(IEnumerable), typeof(RincianEditor));

    public IEnumerable? RekeningSource
    {
        get => (IEnumerable?)GetValue(RekeningSourceProperty);
        set => SetValue(RekeningSourceProperty, value);
    }

    TrackedCollection<RincianAnggaran>? _items;

    /// <summary>Sumber dana awal untuk baris baru (mis. sumber dana paket).</summary>
    public Func<string>? SumberDanaAwal { get; set; }

    /// <summary>Kolom Keterangan (disembunyikan di RAB karena Siskeudes tidak memakainya).</summary>
    public bool TampilkanKeterangan
    {
        get => KolKeterangan.Visibility == Visibility.Visible;
        set => KolKeterangan.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Label total di bawah tabel.</summary>
    public string LabelTotal { get; set; } = "Total";

    public RincianEditor()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AppState.Instance.HasilBerubah += PerbaruiTotal;
            AppState.Instance.SebelumDataDiganti += AkhiriEdit;
            PerbaruiTotal();
        };
        Unloaded += (_, _) =>
        {
            AkhiriEdit();
            AppState.Instance.HasilBerubah -= PerbaruiTotal;
            AppState.Instance.SebelumDataDiganti -= AkhiriEdit;
        };
        Tabel.PreviewKeyDown += Tabel_PreviewKeyDown;
    }

    public TrackedCollection<RincianAnggaran>? Items
    {
        get => _items;
        set
        {
            AkhiriEdit();
            _items = value;
            Tabel.ItemsSource = value;
            PerbaruiTotal();
        }
    }

    public void AkhiriEdit() => UiBantu.AkhiriEdit(Tabel);

    void PerbaruiTotal()
    {
        var total = _items?.Sum(r => r.Jumlah) ?? 0;
        TxtTotal.Text = $"{LabelTotal}: {Kalkulasi.Rp(total)}";
    }

    bool BisaUbah => AppState.Instance.BisaUbah && _items != null;

    void Tambah_Click(object sender, RoutedEventArgs e)
    {
        if (!BisaUbah) return;
        AkhiriEdit();
        var terpilih = Tabel.SelectedItem as RincianAnggaran;
        var acuan = terpilih ?? _items!.LastOrDefault();
        var baru = new RincianAnggaran
        {
            KodeRekening = acuan?.KodeRekening ?? "",
            Volume = 1,
            Satuan = acuan?.Satuan ?? "",
            SumberDana = SumberDanaAwal?.Invoke() is { Length: > 0 } sd ? sd : acuan?.SumberDana ?? ""
        };
        var idx = terpilih != null ? _items!.IndexOf(terpilih) + 1 : _items!.Count;
        _items.Insert(Math.Clamp(idx, 0, _items.Count), baru);
        UiBantu.EditSel(Tabel, baru, 2);
    }

    void Salin_Click(object sender, RoutedEventArgs e)
    {
        if (!BisaUbah || Tabel.SelectedItem is not RincianAnggaran r) return;
        AkhiriEdit();
        var s = r.Salin();
        _items!.Insert(_items.IndexOf(r) + 1, s);
        Tabel.SelectedItem = s;
        Tabel.ScrollIntoView(s);
    }

    void Hapus_Click(object sender, RoutedEventArgs e)
    {
        if (!BisaUbah || Tabel.SelectedItem is not RincianAnggaran r) return;
        AkhiriEdit();
        var idx = _items!.IndexOf(r);
        _items.Remove(r);
        if (_items.Count > 0) Tabel.SelectedItem = _items[Math.Clamp(idx, 0, _items.Count - 1)];
    }

    void Naik_Click(object sender, RoutedEventArgs e) => Pindah(-1);
    void Turun_Click(object sender, RoutedEventArgs e) => Pindah(1);

    void Pindah(int arah)
    {
        if (!BisaUbah || Tabel.SelectedItem is not RincianAnggaran r) return;
        AkhiriEdit();
        var i = _items!.IndexOf(r);
        var j = i + arah;
        if (i < 0 || j < 0 || j >= _items.Count) return;
        _items.Move(i, j);
        Tabel.SelectedItem = r;
        Tabel.ScrollIntoView(r);
    }

    void Tabel_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Tombol Delete saat tidak sedang mengetik = hapus baris
        if (e.Key == System.Windows.Input.Key.Delete && e.OriginalSource is DataGridCell && BisaUbah)
        {
            Hapus_Click(sender, e);
            e.Handled = true;
        }
    }
}
