using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Pratinjau laporan dan cetak (termasuk ke PDF lewat "Microsoft Print to PDF").</summary>
public class PratinjauWindow : Window
{
    readonly ComboBox _jenis;
    readonly FlowDocumentReader _pembaca;
    readonly AppState S = AppState.Instance;

    public PratinjauWindow()
    {
        Title = "Pratinjau & Cetak";
        FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("FontUtama");
        FontSize = 15;
        Width = 1100;
        Height = 800;
        MinWidth = 700;
        MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        Background = (System.Windows.Media.Brush)Application.Current.FindResource("LatarApp");

        var akar = new DockPanel();
        var bar = new WrapPanel { Margin = new Thickness(12, 10, 12, 4) };
        DockPanel.SetDock(bar, Dock.Top);
        bar.Children.Add(new TextBlock { Text = "Laporan", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 6) });
        _jenis = new ComboBox { MinWidth = 320, Margin = new Thickness(0, 0, 10, 6), ItemsSource = Laporan.Jenis, SelectedIndex = 0 };
        _jenis.SelectionChanged += (_, _) => Muat();
        bar.Children.Add(_jenis);
        var cetak = new Button { Content = "Cetak…", Style = (Style)Application.Current.FindResource("TombolUtama"), MinWidth = 110 };
        cetak.Click += (_, _) => Cetak();
        bar.Children.Add(cetak);
        var tutup = new Button { Content = "Tutup", MinWidth = 90, IsCancel = true };
        tutup.Click += (_, _) => Close();
        bar.Children.Add(tutup);
        bar.Children.Add(new TextBlock
        {
            Text = "Untuk menyimpan sebagai PDF, pilih printer \"Microsoft Print to PDF\". Pilih Landscape untuk tabel yang lebar.",
            TextWrapping = TextWrapping.Wrap, MaxWidth = 520, VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)Application.Current.FindResource("TeksRedup"), FontSize = 13, Margin = new Thickness(6, 0, 0, 6)
        });
        akar.Children.Add(bar);

        _pembaca = new FlowDocumentReader
        {
            ViewingMode = FlowDocumentReaderViewingMode.Scroll,
            IsFindEnabled = true,
            IsPrintEnabled = false,
            Zoom = 100,
            Margin = new Thickness(8, 0, 8, 8),
            Background = System.Windows.Media.Brushes.White
        };
        akar.Children.Add(_pembaca);
        Content = akar;
        Loaded += (_, _) => Muat();
    }

    FlowDocument? BuatDokumen()
    {
        if (S.Data == null || S.SkenarioAktif == null || S.Hasil == null) return null;
        return Laporan.Buat(_jenis.SelectedItem as string ?? Laporan.Ringkasan, S.Desa, S.SkenarioAktif, S.Data, S.Hasil);
    }

    void Muat()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var doc = BuatDokumen();
            if (doc != null)
            {
                doc.PageWidth = 820;
                doc.MinPageWidth = 600;
            }
            _pembaca.Document = doc;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Pratinjau");
            Pesan.Galat("Laporan gagal disusun.", ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    void Cetak()
    {
        try
        {
            var pd = new PrintDialog { UserPageRangeEnabled = false };
            if (pd.ShowDialog() != true) return;
            var doc = BuatDokumen();
            if (doc == null) return;
            doc.PageWidth = pd.PrintableAreaWidth;
            doc.PageHeight = pd.PrintableAreaHeight;
            doc.ColumnWidth = pd.PrintableAreaWidth;
            doc.PagePadding = new Thickness(40, 36, 40, 36);
            var judul = $"{_jenis.SelectedItem} - {S.SkenarioAktif?.Nama}";
            pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, judul);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cetak");
            Pesan.Galat("Pencetakan gagal. Periksa printer, atau pilih \"Microsoft Print to PDF\".", ex);
        }
    }
}
