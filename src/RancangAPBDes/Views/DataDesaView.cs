using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Data Entri › Data Umum Desa: identitas desa dan Pelaksana Kegiatan Anggaran (PKA).</summary>
public class DataDesaView : UserControl
{
    readonly AppState S = AppState.Instance;
    readonly TabControl _tab = new() { Padding = new Thickness(0), BorderThickness = new Thickness(0), Margin = new Thickness(0, 6, 0, 0) };
    readonly HalamanData _pka;

    public DataDesaView()
    {
        var akar = new DockPanel();
        var kepala = new Border { Background = (Brush)Application.Current.FindResource("LatarKepalaForm"), Padding = new Thickness(12, 6, 12, 6) };
        kepala.Child = new TextBlock { Text = "DATA UMUM DESA", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.SemiBold };
        DockPanel.SetDock(kepala, Dock.Top);
        akar.Children.Add(kepala);
        akar.Children.Add(_tab);
        Content = akar;

        _tab.Items.Add(new TabItem { Header = "Data Umum", Content = BuatForm() });

        _pka = new HalamanData
        {
            JudulData = "Pelaksana Kegiatan Anggaran (PKA)",
            Konteks = () => "nama-nama ini menjadi pilihan Pelaksana di Isian Data Anggaran",
            BolehUbah = () => true,
            Daftar = () => S.Desa.Perangkat,
            BuatBaru = () => new Perangkat(),
            Kunci = o => ((Perangkat)o).Nama + "|" + ((Perangkat)o).Jabatan,
            PesanHapus = o => $"Hapus {((Perangkat)o).Nama} dari daftar pelaksana?",
            SetelahSimpan = _ => S.PerbaruiPelaksana()
        };
        _pka.Kolom.Add(new Kolom { Judul = "Nama", Path = "Nama", MinLebar = 200 });
        _pka.Kolom.Add(new Kolom { Judul = "Jabatan", Path = "Jabatan", MinLebar = 200 });
        _pka.Isian.Add(new Isian { Label = "Nama", Path = "Nama", Wajib = true });
        _pka.Isian.Add(new Isian { Label = "Jabatan", Path = "Jabatan", Wajib = true });
        _tab.Items.Add(new TabItem { Header = "Pelaksana Kegiatan", Content = _pka });

        Unloaded += (_, _) =>
        {
            if (_pka.SedangMengisi) _pka.Batal();
            S.PerbaruiPelaksana();
        };
    }

    public void BukaTab(int i) => _tab.SelectedIndex = i;

    FrameworkElement BuatForm()
    {
        var g = new Grid { Margin = new Thickness(14, 12, 14, 12), MaxWidth = 760, HorizontalAlignment = HorizontalAlignment.Stretch };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 220 });
        var isian = new (string label, string path, string? tip)[]
        {
            ("Nama Desa", "NamaDesa", "Contoh: Merden"),
            ("Kode Desa", "KodeDesa", "Seperti di Siskeudes, contoh 04.2002 (tidak wajib)"),
            ("Kecamatan", "Kecamatan", null),
            ("Kabupaten", "Kabupaten", null),
            ("Nama Kepala Desa", "KepalaDesa", null),
            ("Nama Sekretaris Desa", "Sekdes", null),
            ("Nama Kaur Keuangan", "KaurKeuangan", null),
            ("Tempat Penandatanganan", "TempatTtd", "Muncul di laporan cetak"),
        };
        for (int i = 0; i < isian.Length; i++)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var (label, path, tip) = isian[i];
            var l = new TextBlock { Text = label, Style = (Style)Application.Current.FindResource("Label"), MaxWidth = 200 };
            var tb = new TextBox { ToolTip = tip, Margin = new Thickness(0, 0, 0, 6) };
            tb.SetBinding(TextBox.TextProperty, new Binding(path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            Grid.SetRow(l, i);
            Grid.SetRow(tb, i);
            Grid.SetColumn(tb, 1);
            g.Children.Add(l);
            g.Children.Add(tb);
        }
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var catatan = new TextBlock
        {
            Text = "Perubahan tersimpan otomatis. Data ini berlaku untuk semua skenario di file ini.",
            Style = (Style)Application.Current.FindResource("TeksRedup"), Margin = new Thickness(0, 6, 0, 0)
        };
        Grid.SetRow(catatan, isian.Length);
        Grid.SetColumnSpan(catatan, 2);
        g.Children.Add(catatan);
        Loaded += (_, _) => g.DataContext = S.Desa;
        return new Border
        {
            Background = Brushes.White, BorderBrush = (Brush)Application.Current.FindResource("Garis"), BorderThickness = new Thickness(1),
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = new Border { Child = g, HorizontalAlignment = HorizontalAlignment.Left, Width = 760, MaxWidth = 760 } }
        };
    }
}
