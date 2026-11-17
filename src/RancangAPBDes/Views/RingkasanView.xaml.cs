using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public partial class RingkasanView : UserControl
{
    const int MaksPeringatan = 60;
    readonly AppState S = AppState.Instance;

    public RingkasanView()
    {
        InitializeComponent();
        UiBantu.TeruskanRoda(TabelSd);
        UiBantu.TeruskanRoda(TabelBidang);
        UiBantu.TeruskanRoda(TabelAturan);
        Loaded += (_, _) =>
        {
            S.HasilBerubah += Segarkan;
            S.DataDiganti += Segarkan;
            Segarkan();
        };
        Unloaded += (_, _) =>
        {
            S.HasilBerubah -= Segarkan;
            S.DataDiganti -= Segarkan;
        };
    }

    void Segarkan()
    {
        var info = S.SkenarioAktif;
        var h = S.Hasil;
        TxtCatatan.DataContext = S.Data;
        TxtCatatan.IsEnabled = S.Data != null;

        TxtJudul.Text = info == null ? "Ringkasan APBDes" : $"Ringkasan APBDes {info.Tahun} – {info.Nama}";
        TxtSub.Text = string.IsNullOrWhiteSpace(S.Desa.NamaDesa)
            ? "Nama desa belum diisi (halaman Data Desa)."
            : $"{S.Desa.NamaDesa}" + (string.IsNullOrWhiteSpace(S.Desa.Kecamatan) ? "" : $", Kec. {S.Desa.Kecamatan}") +
              (info == null ? "" : $" · terakhir diubah {info.Diubah:dd MMMM yyyy HH:mm}");

        PanelKartu.Children.Clear();
        if (h == null)
        {
            TabelSd.ItemsSource = null;
            TabelBidang.ItemsSource = null;
            TabelAturan.ItemsSource = null;
            DaftarPeringatan.ItemsSource = null;
            return;
        }

        PanelKartu.Children.Add(Kartu("Pendapatan", h.TotalPendapatan, null));
        PanelKartu.Children.Add(Kartu("Belanja", h.TotalBelanja, $"{h.JumlahKegiatan} kegiatan"));
        PanelKartu.Children.Add(Kartu(h.Surplus >= 0 ? "Surplus" : "Defisit", Math.Abs(h.Surplus), "pendapatan − belanja", h.Surplus < 0 ? "Kuning" : null));
        PanelKartu.Children.Add(Kartu("Pembiayaan Netto", h.PembiayaanNetto, "penerimaan − pengeluaran"));
        var sisa = h.SisaAnggaran;
        PanelKartu.Children.Add(Kartu(sisa < 0 ? "KEKURANGAN DANA" : sisa == 0 ? "Anggaran Seimbang" : "Belum Dialokasikan",
            Math.Abs(sisa), sisa < 0 ? "belanja melebihi dana" : "sisa lebih (SiLPA berjalan)", sisa < 0 ? "Merah" : sisa == 0 ? "Hijau" : null));

        TabelSd.ItemsSource = h.PerSumberDana;
        TabelBidang.ItemsSource = h.PerBidang;
        TabelAturan.ItemsSource = h.Aturan;
        TabelAturan.Visibility = h.Aturan.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        TxtAturanKosong.Visibility = h.Aturan.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        DaftarPeringatan.ItemsSource = h.Peringatan.Take(MaksPeringatan).ToList();
        TxtAman.Visibility = h.Peringatan.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtLebih.Text = h.Peringatan.Count > MaksPeringatan ? $"… dan {h.Peringatan.Count - MaksPeringatan} catatan lainnya." : "";
    }

    Border Kartu(string judul, decimal nilai, string? ket, string? warna = null)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = judul, Style = (Style)FindResource("TeksRedup"), FontSize = Huruf.Kecil });
        var angka = new TextBlock { Text = Kalkulasi.Rp(nilai), FontSize = Huruf.Besar, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
        if (warna != null) angka.Foreground = (Brush)FindResource(warna);
        sp.Children.Add(angka);
        if (ket != null) sp.Children.Add(new TextBlock { Text = ket, Style = (Style)FindResource("TeksRedup"), FontSize = Huruf.Kecil });
        return new Border { Style = (Style)FindResource("Kartu"), Child = sp, MinWidth = 190 };
    }
}
