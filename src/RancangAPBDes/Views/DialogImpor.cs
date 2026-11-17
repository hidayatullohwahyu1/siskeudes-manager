using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Ringkasan hasil baca Excel Siskeudes dan pilihan skenario yang akan dibuat.</summary>
public class DialogImpor : Window
{
    readonly ImporSiskeudes.Hasil _hasil;
    readonly ComboBox _tahun = new() { MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Left };
    readonly Dictionary<string, (CheckBox centang, TextBox nama)> _versi = new();
    readonly CheckBox _pelaksana = new() { Content = "Tambahkan nama pelaksana (PKA) ke daftar Pelaksana Kegiatan", IsChecked = true, Margin = new Thickness(0, 4, 0, 0) };
    readonly CheckBox _referensi = new() { Content = "Tambahkan rekening/kegiatan yang belum ada ke Parameter", IsChecked = true, Margin = new Thickness(0, 4, 0, 0) };

    public int Tahun => _tahun.SelectedItem is int t ? t : DateTime.Today.Year;
    public IEnumerable<(string versi, string nama)> Pilihan =>
        _versi.Where(v => v.Value.centang.IsChecked == true && !string.IsNullOrWhiteSpace(v.Value.nama.Text)).Select(v => (v.Key, v.Value.nama.Text.Trim()));
    public bool TambahPelaksana => _pelaksana.IsChecked == true;
    public bool TambahReferensi => _referensi.IsChecked == true;

    public DialogImpor(ImporSiskeudes.Hasil hasil, int tahunAwal)
    {
        _hasil = hasil;
        Title = "Impor APBDes dari Excel Siskeudes";
        FontFamily = (FontFamily)Application.Current.FindResource("FontUtama");
        FontSize = Huruf.Isi;
        Width = 760;
        Height = 620;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        UseLayoutRounding = true;
        Background = (Brush)Application.Current.FindResource("LatarApp");

        var sp = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };
        sp.Children.Add(new TextBlock { Text = "Hasil membaca file", FontSize = Huruf.Judul, FontWeight = FontWeights.SemiBold });
        sp.Children.Add(new TextBlock { Text = $"{hasil.LembarDibaca} lembar dibaca.", Style = (Style)Application.Current.FindResource("TeksRedup"), Margin = new Thickness(0, 0, 0, 8) });

        foreach (var (versi, d) in hasil.Versi.OrderBy(v => v.Key == "Menjadi" ? 1 : 0))
        {
            var h = Kalkulasi.Hitung(d, AppState.Instance.Referensi);
            var kotak = new Border { Style = (Style)Application.Current.FindResource("Kartu"), Margin = new Thickness(0, 0, 0, 8) };
            var g = new StackPanel();
            var c = new CheckBox { IsChecked = true, Content = new TextBlock { Text = $"Buat skenario dari anggaran {versi.ToUpperInvariant()}", FontWeight = FontWeights.SemiBold } };
            g.Children.Add(c);
            g.Children.Add(new TextBlock
            {
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(22, 4, 0, 4),
                Text = $"{d.Kegiatan.Count} kegiatan · {d.Kegiatan.Sum(k => k.Paket.Count)} paket · " +
                       $"Pendapatan {Kalkulasi.Rp(h.TotalPendapatan)} · Belanja {Kalkulasi.Rp(h.TotalBelanja)} · " +
                       $"Penerimaan pembiayaan {Kalkulasi.Rp(h.PenerimaanPembiayaan)} · Pengeluaran pembiayaan {Kalkulasi.Rp(h.PengeluaranPembiayaan)}"
            });
            var baris = new DockPanel { Margin = new Thickness(22, 0, 0, 0) };
            var lbl = new TextBlock { Text = "Nama skenario:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            DockPanel.SetDock(lbl, Dock.Left);
            var nama = new TextBox { Text = $"Impor Siskeudes – {versi}" };
            baris.Children.Add(lbl);
            baris.Children.Add(nama);
            g.Children.Add(baris);
            kotak.Child = g;
            sp.Children.Add(kotak);
            _versi[versi] = (c, nama);
        }

        var tahunBaris = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
        tahunBaris.Children.Add(new TextBlock { Text = "Tahun anggaran:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        for (int t = DateTime.Today.Year - 3; t <= DateTime.Today.Year + 3; t++) _tahun.Items.Add(t);
        _tahun.SelectedItem = tahunAwal;
        tahunBaris.Children.Add(_tahun);
        sp.Children.Add(tahunBaris);
        if (hasil.Pelaksana.Count > 0) sp.Children.Add(_pelaksana);
        if (hasil.RekeningBaru.Count + hasil.KegiatanBaru.Count > 0)
        {
            _referensi.Content = $"Tambahkan {hasil.RekeningBaru.Count} rekening dan {hasil.KegiatanBaru.Count} kegiatan yang belum ada ke Parameter";
            sp.Children.Add(_referensi);
        }

        if (hasil.Selisih.Count > 0)
        {
            sp.Children.Add(new TextBlock { Text = $"Perlu dicek: {hasil.Selisih.Count} baris di file tidak sesuai rumus Volume × Harga Satuan", FontSize = Huruf.Judul, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 2), Foreground = (Brush)Application.Current.FindResource("Merah"), TextWrapping = TextWrapping.Wrap });
            sp.Children.Add(new TextBlock { TextWrapping = TextWrapping.Wrap, Style = (Style)Application.Current.FindResource("TeksRedup"), Margin = new Thickness(0, 0, 0, 4),
                Text = "Seperti Siskeudes, aplikasi ini memakai Jumlah = Volume × Harga Satuan. Cocokkan baris berikut dengan Siskeudes; kemungkinan ada salah ketik di file Excel." });
            foreach (var s in hasil.Selisih)
                sp.Children.Add(new TextBlock { Text = "• " + s, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) });
        }
        foreach (var c in hasil.Catatan)
            sp.Children.Add(new TextBlock { Text = "ⓘ " + c, TextWrapping = TextWrapping.Wrap, Style = (Style)Application.Current.FindResource("TeksRedup") });

        var tombol = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 8, 16, 12) };
        var ok = new Button { Content = "Impor", IsDefault = true, Style = (Style)Application.Current.FindResource("TombolUtama"), MinWidth = 100 };
        ok.Click += (_, _) =>
        {
            if (!Pilihan.Any()) { Pesan.Peringatan("Pilih minimal satu skenario yang akan dibuat."); return; }
            DialogResult = true;
        };
        var batal = new Button { Content = "Batal", IsCancel = true, MinWidth = 100 };
        batal.Click += (_, _) => { if (IsLoaded) Close(); };
        tombol.Children.Add(ok);
        tombol.Children.Add(batal);

        var dock = new DockPanel();
        DockPanel.SetDock(tombol, Dock.Bottom);
        dock.Children.Add(tombol);
        dock.Children.Add(new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Content = dock;
    }
}
