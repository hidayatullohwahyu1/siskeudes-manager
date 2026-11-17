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

    public void GulirKePemeriksaan() => TabelSd.BringIntoView();

    /// <summary>Perintah navigasi dari panel Langkah Kerja (diisi oleh jendela utama).</summary>
    public static Action<string>? Perintah { get; set; }

    static bool KasLengkap(DataSkenario d) =>
        d.Kegiatan.Count > 0 && d.Kegiatan.All(k => k.Total == 0 ||
            (d.AnggaranKas.FirstOrDefault(x => x.Kunci == "K:" + k.Id)?.Bulan.Sum() ?? 0) == k.Total);

    void IsiLangkah()
    {
        PanelLangkah.Children.Clear();
        var d = S.Data;
        var h = S.Hasil;
        var info = S.SkenarioAktif;
        if (d == null || h == null || info == null) return;
        int kegKosong = d.Kegiatan.Count(k => k.Total == 0);
        var langkah = new (string judul, string menu, bool selesai, string status, string perintah)[]
        {
            ("Data Umum Desa", "Data Entri › Data Umum Desa",
                S.Desa.NamaDesa != "" && S.Desa.Perangkat.Count > 0,
                S.Desa.NamaDesa == "" ? "Nama desa belum diisi" : S.Desa.Perangkat.Count == 0 ? "Pelaksana kegiatan belum diisi" : $"{S.Desa.NamaDesa} · {S.Desa.Perangkat.Count} pelaksana", "desa"),
            ("Perencanaan: RPJMDes & RKPDes (opsional)", "Data Entri › Perencanaan › RPJMDes dan RKPDes",
                S.Perencanaan.KegiatanRkp.Any(k => k.Tahun == info.Tahun),
                S.Perencanaan.KegiatanRkp.Any(k => k.Tahun == info.Tahun)
                    ? $"RKPDes {info.Tahun}: {S.Perencanaan.KegiatanRkp.Count(k => k.Tahun == info.Tahun && k.Status == "Disepakati")} kegiatan disepakati"
                    : $"RPJMDes {S.Perencanaan.KegiatanRpjm.Count} kegiatan · RKPDes {info.Tahun} belum diisi", "perencanaan"),
            ("Kegiatan (Bidang › Sub Bidang › Kegiatan › Paket)", "Data Entri › Penganggaran › Isian Data Anggaran › 1. Kegiatan",
                d.Kegiatan.Count > 0, d.Kegiatan.Count == 0 ? "Belum ada kegiatan" : $"{d.Kegiatan.Count} kegiatan, {d.Kegiatan.Sum(k => k.Paket.Count)} paket", "kegiatan"),
            ("Pendapatan (RAP › RAP Rinci)", "Isian Data Anggaran › 2. Pendapatan",
                h.TotalPendapatan > 0, h.TotalPendapatan > 0 ? Kalkulasi.Rp(h.TotalPendapatan) : "Belum ada pendapatan", "pendapatan"),
            ("Belanja (RAB › RAB Rinci)", "Isian Data Anggaran › 3. Belanja",
                h.TotalBelanja > 0 && kegKosong == 0,
                h.TotalBelanja == 0 ? "Belum ada RAB" : kegKosong > 0 ? $"{Kalkulasi.Rp(h.TotalBelanja)} · {kegKosong} kegiatan belum ada RAB" : Kalkulasi.Rp(h.TotalBelanja), "belanja"),
            ("Pembiayaan 1 & 2 (bila ada SiLPA / penyertaan modal)", "Isian Data Anggaran › 4. Pembiayaan 1 / 5. Pembiayaan 2",
                true, h.PenerimaanPembiayaan + h.PengeluaranPembiayaan == 0 ? "Tidak ada (opsional)" : $"Penerimaan {Kalkulasi.Rp(h.PenerimaanPembiayaan)} · Pengeluaran {Kalkulasi.Rp(h.PengeluaranPembiayaan)}", "pembiayaan"),
            ("Anggaran Kas Desa (opsional)", "Data Entri › Penganggaran › Anggaran Kas Desa",
                KasLengkap(d), KasLengkap(d) ? "Semua kegiatan sudah dijadwalkan per bulan" : "Belum semua kegiatan dijadwalkan", "kas"),
            ("Periksa keseimbangan & aturan", "Beranda › Dana per Sumber Dana, Cek Aturan, Peringatan",
                h.JumlahKesalahan == 0 && h.SisaAnggaran >= 0,
                h.JumlahKesalahan > 0 ? $"{h.JumlahKesalahan} kesalahan perlu diperbaiki" : h.SisaAnggaran < 0 ? $"Defisit {Kalkulasi.Rp(-h.SisaAnggaran)}" : "Tidak ada kesalahan", "periksa"),
            ("Posting APBDes (kunci hasil kesepakatan)", "Data Entri › Penganggaran › Posting APBDes",
                info.IsFinal, info.IsFinal ? "Sudah diposting" : "Belum diposting (masih draft)", "posting"),
            ("Cetak & input ke Siskeudes", "Laporan › Pratinjau & Cetak › Daftar Siap Input Siskeudes",
                false, "Cetak laporan untuk rapat dan pedoman input", "cetak"),
        };
        for (int i = 0; i < langkah.Length; i++)
        {
            var (judul, menu, selesai, status, perintah) = langkah[i];
            var g = new Grid { Margin = new Thickness(0) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var bulat = new Border
            {
                Width = 26, Height = 26, CornerRadius = new CornerRadius(13), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
                Background = (Brush)FindResource(selesai ? "Primer" : "LatarPanel"),
                BorderBrush = (Brush)FindResource(selesai ? "Primer" : "GarisTegas"), BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = selesai ? "✓" : (i + 1).ToString(), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Foreground = selesai ? Brushes.White : (Brush)FindResource("TeksUtama")
                }
            };
            g.Children.Add(bulat);
            var kiri = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6) };
            kiri.Children.Add(new TextBlock { Text = $"{i + 1}. {judul}", FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            kiri.Children.Add(new TextBlock { Text = menu, FontSize = Huruf.Kecil, Foreground = (Brush)FindResource("WarnaRedup"), TextWrapping = TextWrapping.Wrap });
            Grid.SetColumn(kiri, 1);
            g.Children.Add(kiri);
            var st = new TextBlock
            {
                Text = status, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 8, 6),
                Foreground = (Brush)FindResource(selesai ? "Hijau" : perintah is "periksa" or "kegiatan" or "pendapatan" or "belanja" or "desa" ? "Merah" : "TeksUtama"),
                FontWeight = selesai ? FontWeights.Normal : FontWeights.SemiBold
            };
            if (perintah is "pembiayaan" or "cetak" or "posting" or "perencanaan" or "kas") st.Foreground = (Brush)FindResource(selesai ? "Hijau" : "TeksUtama");
            Grid.SetColumn(st, 2);
            g.Children.Add(st);
            var b = new Button { Content = perintah == "cetak" ? "Cetak…" : perintah == "posting" ? (info.IsFinal ? "Lihat" : "Posting…") : "Buka", Margin = new Thickness(0, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, MinWidth = 80 };
            var p = perintah;
            b.Click += (_, _) => { try { Perintah?.Invoke(p); } catch (Exception ex) { Log.Error(ex, "Langkah"); } };
            Grid.SetColumn(b, 3);
            g.Children.Add(b);
            PanelLangkah.Children.Add(new Border
            {
                Child = g, BorderBrush = (Brush)FindResource("Garis"), BorderThickness = new Thickness(0, 0, 0, i < langkah.Length - 1 ? 1 : 0),
                Background = i % 2 == 0 ? Brushes.White : (Brush)FindResource("LatarPanel")
            });
        }
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

        IsiLangkah();
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
