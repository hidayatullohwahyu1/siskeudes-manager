using System.Windows;
using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Dialog membuat skenario baru (kosong atau salinan).</summary>
public class DialogSkenario : Window
{
    readonly TextBox _nama;
    readonly ComboBox _tahun;

    public string NamaSkenario => _nama.Text.Trim();
    public int Tahun { get; private set; }

    public DialogSkenario(bool salin, SkenarioInfo? sumber)
    {
        Title = salin ? "Salin skenario" : "Skenario baru";
        FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("FontUtama");
        FontSize = Huruf.Isi;
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 460;
        MaxWidth = 720;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        UseLayoutRounding = true;

        var sp = new StackPanel { Margin = new Thickness(18) };
        var ket = salin && sumber != null
            ? $"Semua isi skenario \"{sumber.Nama}\" akan disalin. Skenario asli tidak berubah, jadi Anda bebas mencoba-coba di salinan."
            : "Skenario baru dimulai kosong (tanpa pendapatan dan kegiatan).";
        sp.Children.Add(new TextBlock { Text = ket, TextWrapping = TextWrapping.Wrap, MaxWidth = 520, Margin = new Thickness(0, 0, 0, 12) });

        sp.Children.Add(new TextBlock { Text = "Nama skenario (mis. \"Hasil Musdes 12 Juni\" atau \"Alternatif DD turun\"):", TextWrapping = TextWrapping.Wrap, MaxWidth = 520, Margin = new Thickness(0, 0, 0, 6) });
        _nama = new TextBox { Text = salin && sumber != null ? sumber.Nama + " (salinan)" : "Skenario baru", MinWidth = 420 };
        sp.Children.Add(_nama);

        sp.Children.Add(new TextBlock { Text = "Tahun anggaran:", Margin = new Thickness(0, 12, 0, 6) });
        var tahunSekarang = DateTime.Today.Year;
        var awal = sumber?.Tahun ?? tahunSekarang;
        _tahun = new ComboBox { MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Left };
        for (int t = Math.Min(awal, tahunSekarang) - 2; t <= Math.Max(awal, tahunSekarang) + 6; t++) _tahun.Items.Add(t);
        _tahun.SelectedItem = awal;
        sp.Children.Add(_tahun);

        var tombol = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var ok = new Button { Content = salin ? "Salin" : "Buat", IsDefault = true, MinWidth = 100, Style = (Style)Application.Current.FindResource("TombolUtama") };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nama.Text)) { Pesan.Peringatan("Nama skenario belum diisi."); _nama.Focus(); return; }
            Tahun = _tahun.SelectedItem is int t ? t : awal;
            DialogResult = true;
        };
        var batal = new Button { Content = "Batal", IsCancel = true, MinWidth = 100 };
        tombol.Children.Add(ok);
        tombol.Children.Add(batal);
        sp.Children.Add(tombol);
        Content = sp;
        Loaded += (_, _) => { _nama.Focus(); _nama.SelectAll(); };
    }
}
