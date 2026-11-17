using System.Windows;
using System.Windows.Controls;

namespace RancangAPBDes.Views;

/// <summary>Jendela kecil untuk meminta satu isian teks.</summary>
public class DialogInput : Window
{
    readonly TextBox _isian;

    DialogInput(string judul, string label, string nilai)
    {
        Title = judul;
        FontFamily = (System.Windows.Media.FontFamily)Application.Current.FindResource("FontUtama");
        FontSize = 15;
        SizeToContent = SizeToContent.WidthAndHeight;
        MinWidth = 440;
        MaxWidth = 700;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        UseLayoutRounding = true;

        var sp = new StackPanel { Margin = new Thickness(18) };
        sp.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) });
        _isian = new TextBox { Text = nilai, MinWidth = 400 };
        sp.Children.Add(_isian);
        var tombol = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var ok = new Button { Content = "Simpan", IsDefault = true, MinWidth = 100, Style = (Style)Application.Current.FindResource("TombolUtama") };
        ok.Click += (_, _) => { DialogResult = true; };
        var batal = new Button { Content = "Batal", IsCancel = true, MinWidth = 100, Margin = new Thickness(0, 0, 0, 6) };
        tombol.Children.Add(ok);
        tombol.Children.Add(batal);
        sp.Children.Add(tombol);
        Content = sp;
        Loaded += (_, _) => { _isian.Focus(); _isian.SelectAll(); };
    }

    public static string? Tanya(Window pemilik, string judul, string label, string nilai = "")
    {
        var d = new DialogInput(judul, label, nilai) { Owner = pemilik };
        return d.ShowDialog() == true ? d._isian.Text.Trim() : null;
    }
}
