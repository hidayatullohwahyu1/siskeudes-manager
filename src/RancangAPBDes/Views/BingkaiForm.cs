using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Ukuran huruf yang diizinkan (satu skala untuk seluruh aplikasi).</summary>
public static class Huruf
{
    public const double Kecil = 12, Isi = 13, Judul = 15, Besar = 18;
}

/// <summary>
/// Bingkai yang sama untuk setiap form (seperti jendela form Siskeudes):
/// kepala "PEMERINTAH DESA …", judul form, isi, dan tombol Tutup. Lebar seragam di semua halaman.
/// </summary>
public class BingkaiForm : UserControl
{
    public const double LebarMaks = 1200;

    readonly TextBlock _desa = new() { Foreground = Brushes.White, FontSize = Huruf.Judul, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock _info = new() { Foreground = Brushes.White, FontSize = Huruf.Kecil, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap };
    readonly AppState S = AppState.Instance;

    public FrameworkElement Isi { get; }
    public string JudulForm { get; }

    public BingkaiForm(string judul, FrameworkElement isi, Action? tutup, string? jalurMenu = null)
    {
        JudulForm = judul;
        Isi = isi;
        var bingkai = new Border
        {
            Background = Brushes.White,
            BorderBrush = (Brush)Application.Current.FindResource("GarisTegas"),
            BorderThickness = new Thickness(1),
            MaxWidth = LebarMaks,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SnapsToDevicePixels = true
        };
        var dock = new DockPanel();
        bingkai.Child = dock;

        var kepala = new Border { Background = (Brush)Application.Current.FindResource("LatarKepalaForm"), Padding = new Thickness(12, 6, 12, 6) };
        var sp = new StackPanel();
        sp.Children.Add(_desa);
        sp.Children.Add(_info);
        kepala.Child = sp;
        DockPanel.SetDock(kepala, Dock.Top);
        dock.Children.Add(kepala);

        var sub = new Border
        {
            Background = (Brush)Application.Current.FindResource("LatarPanel"),
            BorderBrush = (Brush)Application.Current.FindResource("Garis"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 4, 12, 4),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = judul.ToUpperInvariant(), FontSize = Huruf.Isi, FontWeight = FontWeights.Bold,
                        Foreground = (Brush)Application.Current.FindResource("JudulForm"),
                        TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = jalurMenu == null ? "" : "Menu: " + jalurMenu, FontSize = Huruf.Kecil,
                        Foreground = (Brush)Application.Current.FindResource("WarnaRedup"),
                        TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
                        Visibility = jalurMenu == null ? Visibility.Collapsed : Visibility.Visible
                    }
                }
            }
        };
        DockPanel.SetDock(sub, Dock.Top);
        dock.Children.Add(sub);

        if (tutup != null)
        {
            var kaki = new Border
            {
                Background = (Brush)Application.Current.FindResource("LatarPanel"),
                BorderBrush = (Brush)Application.Current.FindResource("Garis"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(8, 4, 8, 0)
            };
            var tb = new Button { Content = "Tutup", HorizontalAlignment = HorizontalAlignment.Right, ToolTip = "Tutup form dan kembali ke Beranda" };
            tb.Click += (_, _) => tutup();
            kaki.Child = tb;
            DockPanel.SetDock(kaki, Dock.Bottom);
            dock.Children.Add(kaki);
        }

        dock.Children.Add(isi);
        Content = bingkai;

        Loaded += (_, _) => { S.PropertyChanged += SaatBerubah; S.Desa.PropertyChanged += SaatBerubah; Perbarui(); };
        Unloaded += (_, _) => { S.PropertyChanged -= SaatBerubah; S.Desa.PropertyChanged -= SaatBerubah; };
    }

    void SaatBerubah(object? s, System.ComponentModel.PropertyChangedEventArgs e) => Perbarui();

    void Perbarui()
    {
        var nama = string.IsNullOrWhiteSpace(S.Desa.NamaDesa) ? "(NAMA DESA BELUM DIISI)" : S.Desa.NamaDesa.ToUpperInvariant();
        _desa.Text = nama.StartsWith("DESA ") || nama.StartsWith("PEMERINTAH") ? nama : "PEMERINTAH DESA " + nama;
        var a = S.SkenarioAktif;
        _info.Text = a == null ? "" : $"Tahun Anggaran {a.Tahun}  ·  Skenario: {a.Nama}  ·  {(a.IsFinal ? "POSTING" : "DRAFT")}";
    }
}

public static class Gaya
{
    /// <summary>Tab vertikal di sisi kiri seperti Siskeudes.</summary>
    public static TabControl TabKiri()
    {
        var tab = new TabControl
        {
            TabStripPlacement = Dock.Left,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(1, 0, 0, 0),
            BorderBrush = (Brush)Application.Current.FindResource("Garis"),
            Background = Brushes.White
        };
        var s = new Style(typeof(TabItem));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        s.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 118.0));
        s.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, 150.0));
        s.Setters.Add(new Setter(Control.FontSizeProperty, Huruf.Isi));
        s.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
        var t = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
        t.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        s.Triggers.Add(t);
        tab.ItemContainerStyle = s;
        return tab;
    }

    public static TabItem Tab(string judul, object isi) => new()
    {
        Header = new TextBlock { Text = judul, TextWrapping = TextWrapping.Wrap },
        Content = isi
    };

    /// <summary>Panel navigasi kiri (Kegiatan, Pendapatan, …).</summary>
    public static ListBox Nav()
    {
        var nav = new ListBox
        {
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 6, 0, 6),
            Background = (Brush)Application.Current.FindResource("PrimerMuda")
        };
        nav.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        var s = new Style(typeof(ListBoxItem));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 8, 12, 8)));
        s.Setters.Add(new Setter(FrameworkElement.CursorProperty, System.Windows.Input.Cursors.Hand));
        s.Setters.Add(new Setter(Control.FontSizeProperty, Huruf.Isi));
        s.Setters.Add(new Setter(Control.ForegroundProperty, Application.Current.FindResource("PrimerGelap")));
        var t = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        t.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
        s.Triggers.Add(t);
        nav.ItemContainerStyle = s;
        return nav;
    }

    public static ListBoxItem ItemNav(string teks) => new() { Content = new TextBlock { Text = teks, TextWrapping = TextWrapping.Wrap } };

    /// <summary>Tata letak: navigasi kiri (lebar tetap) + isi.</summary>
    public static Grid DenganNav(ListBox nav, FrameworkElement isi)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var bingkaiNav = new Border
        {
            Background = (Brush)Application.Current.FindResource("PrimerMuda"),
            BorderBrush = (Brush)Application.Current.FindResource("Garis"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = nav
        };
        g.Children.Add(bingkaiNav);
        Grid.SetColumn(isi, 1);
        g.Children.Add(isi);
        // Layar sempit: navigasi dan tab menyempit agar tabel tetap lega
        g.SizeChanged += (_, e) =>
        {
            var sempit = e.NewSize.Width < 1000;
            g.ColumnDefinitions[0].Width = new GridLength(sempit ? 118 : 160);
            foreach (var t in UiBantu.Turunan<TabControl>(isi))
                if (t.TabStripPlacement == Dock.Left)
                    foreach (var it in t.Items.OfType<TabItem>()) { it.MinWidth = sempit ? 92 : 118; it.Padding = sempit ? new Thickness(6, 7, 6, 7) : new Thickness(10, 7, 10, 7); }
        };
        return g;
    }
}
