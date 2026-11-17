using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RancangAPBDes.Views;

/// <summary>Jendela pencarian & pemilihan kode (rekening, kegiatan, dll.) seperti tombol "..." di Siskeudes.</summary>
public class DialogPilih : Window
{
    readonly List<PilihanKode> _semua;
    readonly TextBox _cari = new() { Margin = new Thickness(0, 0, 0, 8) };
    readonly ListBox _daftar = new();
    PilihanKode? _hasil;

    DialogPilih(string judul, IEnumerable<PilihanKode> pilihan)
    {
        _semua = pilihan.ToList();
        Title = judul;
        FontFamily = (FontFamily)Application.Current.FindResource("FontUtama");
        FontSize = 13;
        Width = 680;
        Height = 520;
        MinWidth = 420;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        UseLayoutRounding = true;
        Background = (Brush)Application.Current.FindResource("LatarApp");

        var akar = new DockPanel { Margin = new Thickness(12) };
        var atas = new StackPanel();
        atas.Children.Add(new TextBlock { Text = "Cari kode atau nama:", Margin = new Thickness(0, 0, 0, 4) });
        atas.Children.Add(_cari);
        DockPanel.SetDock(atas, Dock.Top);
        akar.Children.Add(atas);

        var bawah = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        var ok = new Button { Content = "Pilih", IsDefault = true, Style = (Style)Application.Current.FindResource("TombolUtama"), MinWidth = 90 };
        ok.Click += (_, _) => Selesai();
        var batal = new Button { Content = "Batal", IsCancel = true, MinWidth = 90 };
        bawah.Children.Add(ok);
        bawah.Children.Add(batal);
        DockPanel.SetDock(bawah, Dock.Bottom);
        akar.Children.Add(bawah);

        _daftar.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        _daftar.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _daftar.ItemTemplate = BuatTemplate();
        _daftar.MouseDoubleClick += (_, e) => { if (e.OriginalSource is DependencyObject d && UiBantu.Leluhur<ListBoxItem>(d) != null) Selesai(); };
        akar.Children.Add(_daftar);
        Content = akar;

        _cari.TextChanged += (_, _) => Saring();
        _cari.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Down && _daftar.Items.Count > 0)
            {
                _daftar.SelectedIndex = Math.Min(_daftar.SelectedIndex + 1, _daftar.Items.Count - 1);
                _daftar.ScrollIntoView(_daftar.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up && _daftar.SelectedIndex > 0)
            {
                _daftar.SelectedIndex--;
                _daftar.ScrollIntoView(_daftar.SelectedItem);
                e.Handled = true;
            }
        };
        Loaded += (_, _) => { Saring(); _cari.Focus(); };
    }

    static DataTemplate BuatTemplate()
    {
        var f = new FrameworkElementFactory(typeof(Grid));
        var c1 = new FrameworkElementFactory(typeof(ColumnDefinition));
        c1.SetValue(ColumnDefinition.WidthProperty, new GridLength(90));
        var c2 = new FrameworkElementFactory(typeof(ColumnDefinition));
        c2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
        f.AppendChild(c1);
        f.AppendChild(c2);
        var kode = new FrameworkElementFactory(typeof(TextBlock));
        kode.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(PilihanKode.Kode)));
        kode.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        kode.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        var nama = new FrameworkElementFactory(typeof(TextBlock));
        nama.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(PilihanKode.Nama)));
        nama.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        nama.SetValue(Grid.ColumnProperty, 1);
        f.AppendChild(kode);
        f.AppendChild(nama);
        f.SetValue(FrameworkElement.MarginProperty, new Thickness(2, 2, 2, 2));
        return new DataTemplate { VisualTree = f };
    }

    void Saring()
    {
        var q = _cari.Text.Trim();
        var hasil = string.IsNullOrEmpty(q)
            ? _semua
            : _semua.Where(p => p.Kode.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Nama.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        _daftar.ItemsSource = hasil;
        if (hasil.Count > 0) _daftar.SelectedIndex = 0;
    }

    void Selesai()
    {
        if (_daftar.SelectedItem is not PilihanKode p) return;
        _hasil = p;
        DialogResult = true;
    }

    public static PilihanKode? Tampil(Window? pemilik, string judul, IEnumerable<PilihanKode> pilihan)
    {
        if (RancangAPBDes.Core.AppState.ModeUji) return null;
        var d = new DialogPilih(judul, pilihan);
        if (pemilik != null && pemilik.IsLoaded) d.Owner = pemilik;
        return d.ShowDialog() == true ? d._hasil : null;
    }
}
