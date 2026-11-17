using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RancangAPBDes.Core;
using RancangAPBDes.Views;

namespace RancangAPBDes;

/// <summary>Uji mandiri: jalankan "RancangAPBDes.exe --uji &lt;folder&gt;". Tidak menyentuh data pengguna.</summary>
public static class UjiOtomatis
{
    static readonly StringBuilder Hasil = new();
    static int _gagal;

    static void Cek(bool ok, string pesan)
    {
        Hasil.AppendLine((ok ? "OK    " : "GAGAL ") + pesan);
        if (!ok) _gagal++;
    }

    static void Tunggu()
    {
        for (int i = 0; i < 3; i++)
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    public static int Jalankan(string folder)
    {
        Directory.CreateDirectory(folder);
        var S = AppState.Instance;
        AppState.ModeUji = true;
        var file = Path.Combine(folder, "uji.apbdes");
        if (File.Exists(file)) File.Delete(file);

        try
        {
            Cek(S.BukaFile(file), "Buka file baru");
            IsiContoh(S);
            Cek(S.SimpanSegera(), "Simpan otomatis");
            S.Hitung();
            var h = S.Hasil!;
            Cek(h.TotalPendapatan == 2_412_000_000m, $"Total pendapatan = {h.TotalPendapatan:N0}");
            Cek(h.TotalBelanja > 0, $"Total belanja = {h.TotalBelanja:N0}");
            Cek(h.PerSumberDana.Any(b => b.Kode == "DDS"), "Ada baris sumber dana DDS");

            // Batalkan / ulangi
            var r = S.Data!.Kegiatan[0].Paket[0].Rincian[0];
            var lama = r.HargaSatuan;
            r.HargaSatuan = 123;
            S.SimpanSegera();
            S.Batalkan();
            var r2 = S.Data!.Kegiatan[0].Paket[0].Rincian[0];
            Cek(r2.HargaSatuan == lama, "Batalkan mengembalikan harga satuan");
            S.Ulangi();
            Cek(S.Data!.Kegiatan[0].Paket[0].Rincian[0].HargaSatuan == 123, "Ulangi menerapkan lagi");
            S.Batalkan();

            // Skenario salinan + final
            var asli = S.SkenarioAktif!;
            S.BuatSkenario("Alternatif DD turun", asli.Tahun, true);
            Cek(S.SkenarioAktif != asli && S.Data!.Kegiatan.Count == 4, "Salin skenario");
            S.Data!.Pendapatan.First(x => x.SumberDana == "DDS").HargaSatuan = 900_000_000m;
            S.SimpanSegera();
            S.SetFinal(S.SkenarioAktif!, true);
            Cek(!S.BisaUbah, "Skenario final terkunci");

            // Buka ulang file: data harus tetap
            var path = S.FilePath!;
            S.TutupFile();
            Cek(S.BukaFile(path), "Buka ulang file");
            Cek(S.SemuaSkenario.Count == 2, $"Jumlah skenario setelah dibuka ulang = {S.SemuaSkenario.Count}");
            S.SkenarioAktif = S.SkenarioTahunIni.First(s => s.Nama == "Hasil Musdes");
            Cek(S.Data!.Kegiatan.Count == 4, "Data kegiatan tersimpan");

            // Ekspor & laporan
            var xlsx = Path.Combine(folder, "uji.xlsx");
            EksporExcel.Simpan(xlsx, S.Desa, S.SkenarioAktif!, S.Data!, S.Hasil!);
            Cek(File.Exists(xlsx) && new FileInfo(xlsx).Length > 5000, "Ekspor Excel");
            foreach (var j in Laporan.Jenis)
            {
                var doc = Laporan.Buat(j, S.Desa, S.SkenarioAktif!, S.Data!, S.Hasil!);
                Cek(doc.Blocks.Count > 2, "Laporan: " + j);
            }

            // File rusak / bukan data
            var palsu = Path.Combine(folder, "palsu.apbdes");
            File.WriteAllText(palsu, "ini bukan database");
            try { Database.Open(palsu).Dispose(); Cek(false, "File palsu harus ditolak"); }
            catch { Cek(true, "File palsu ditolak dengan aman"); }

            // Tampilan
            AmbilGambar(folder, S);
        }
        catch (Exception ex)
        {
            Cek(false, "Pengecualian: " + ex);
        }
        finally
        {
            try { S.TutupFile(); } catch { /* abaikan */ }
            Hasil.AppendLine(_gagal == 0 ? "SEMUA UJI LULUS" : $"{_gagal} UJI GAGAL");
            File.WriteAllText(Path.Combine(folder, "hasil.txt"), Hasil.ToString());
        }
        return _gagal;
    }

    static void IsiContoh(AppState S)
    {
        var d = S.Desa;
        d.NamaDesa = "Desa Contoh Makmur";
        d.KodeDesa = "04.2002";
        d.Kecamatan = "Kecamatan Contoh";
        d.Kabupaten = "Kabupaten Contoh";
        d.KepalaDesa = "Kepala Desa Contoh";
        d.KaurKeuangan = "Bendahara Contoh";
        d.Perangkat.Add(new Perangkat { Nama = "Perangkat Satu", Jabatan = "Kaur Perencanaan" });
        d.Perangkat.Add(new Perangkat { Nama = "Perangkat Dua", Jabatan = "Kasi Kesejahteraan" });
        S.PerbaruiDaftarReferensi();

        S.GantiNama(S.SkenarioAktif!, "Hasil Musdes");
        var data = S.Data!;
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.1.01", Uraian = "Dana Desa tahap I-III", Volume = 1, Satuan = "Tahun", HargaSatuan = 1_200_000_000, SumberDana = "DDS" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.3.01", Uraian = "Alokasi Dana Desa", Volume = 1, Satuan = "Tahun", HargaSatuan = 800_000_000, SumberDana = "ADD" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.2.01", Uraian = "Bagi hasil pajak dan retribusi daerah kabupaten", Volume = 1, Satuan = "Tahun", HargaSatuan = 112_000_000, SumberDana = "PBH" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.1.2.01", Uraian = "Hasil lelang tanah kas desa", Volume = 1, Satuan = "Tahun", HargaSatuan = 300_000_000, SumberDana = "PAD" });
        data.PenerimaanPembiayaan.Add(new RincianAnggaran { KodeRekening = "6.1.1.01", Uraian = "SiLPA Dana Desa tahun lalu", Volume = 1, Satuan = "Paket", HargaSatuan = 25_000_000, SumberDana = "DDS" });

        Kegiatan K(string sub, int no, string nama, string sd, params (string rek, string uraian, decimal vol, string sat, decimal harga)[] rinci)
        {
            var k = new Kegiatan { KodeSubBidang = sub, NoUrut = no, Nama = nama, Lokasi = d.NamaDesa, Pelaksana = "Perangkat Satu (Kaur Perencanaan)", Keluaran = nama, SatuanKeluaran = "Kegiatan" };
            var p = new Paket { Nama = nama, Pola = "Swakelola", Sifat = "Non Fisik", Satuan = "Paket" };
            p.SumberDana = sd;
            foreach (var x in rinci)
                p.Rincian.Add(new RincianAnggaran { KodeRekening = x.rek, Uraian = x.uraian, Volume = x.vol, Satuan = x.sat, HargaSatuan = x.harga, SumberDana = sd });
            k.Paket.Add(p);
            data.Kegiatan.Add(k);
            return k;
        }
        K("01.01", 1, "Penyediaan Penghasilan Tetap dan Tunjangan Kepala Desa", "ADD",
            ("5.1.1.01", "Penghasilan tetap kepala desa", 12, "OB (Orang/Bulan)", 3_714_500),
            ("5.1.2.01", "Tunjangan kepala desa", 12, "OB (Orang/Bulan)", 1_000_000));
        K("01.01", 4, "Penyediaan Operasional Pemerintah Desa (ATK, Honor PKPKD dan PPKD dll)", "ADD",
            ("5.2.1.01", "Alat tulis kantor untuk pelayanan administrasi pemerintahan desa selama satu tahun penuh", 1, "Paket", 15_000_000),
            ("5.2.5.01", "Langganan listrik kantor desa", 12, "Bulan", 450_000));
        K("02.03", 1, "Pembangunan/Rehabilitasi/Peningkatan Jalan Desa Dusun Satu sampai Dusun Tiga beserta Drainase dan Talud", "DDS",
            ("5.3.5.01", "Pengerasan jalan rabat beton panjang 250 m lebar 3 m tebal 15 cm", 112.5m, "M3", 1_450_000),
            ("5.2.2.04", "Upah tukang dan pekerja", 180, "OH (Orang/Hari)", 120_000));
        K("05.03", 1, "Bantuan Langsung Tunai Dana Desa", "DDS",
            ("5.4.1.01", "BLT Dana Desa untuk keluarga penerima manfaat", 120, "KK", 3_600_000));
        data.Catatan = "Disepakati prioritas jalan Dusun Satu. Usulan lampu jalan ditunda ke tahun berikutnya.";
    }

    static void AmbilGambar(string folder, AppState S)
    {
        var ukuran = new (int w, int h, double zoom, string nama)[]
        {
            (1366, 728, 1.0, "1366x768"),
            (900, 580, 1.0, "minimum"),
            (1920, 1040, 1.3, "fullhd-zoom130"),
        };
        var w = new MainWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0, ShowActivated = false };
        w.Show();
        foreach (var (lebar, tinggi, zoom, nama) in ukuran)
        {
            w.WindowState = WindowState.Normal;
            w.Width = lebar;
            w.Height = tinggi;
            w.AturZoomUji(zoom);
            for (int i = 0; i < w.Nav.Items.Count; i++)
            {
                w.Nav.SelectedIndex = i;
                Tunggu();
                var tag = ((ListBoxItem)w.Nav.Items[i]).Tag as string ?? i.ToString();
                if (tag == "belanja" && w.Isi.Content is BelanjaView bv)
                {
                    bv.PilihUntukUji(2);
                    Tunggu();
                }
                Render(w, Path.Combine(folder, $"{nama}_{i}_{tag}.png"));
                CekTerpotong(w, $"{nama}/{tag}");
                if (tag == "ringkasan" && w.Isi.Content is RingkasanView rv)
                {
                    rv.Gulir.ScrollToVerticalOffset(380);
                    Tunggu();
                    Render(w, Path.Combine(folder, $"{nama}_{i}_{tag}_bawah.png"));
                    rv.Gulir.ScrollToTop();
                }
                if (tag == "belanja" && w.Isi.Content is BelanjaView bv2)
                {
                    bv2.GulirUntukUji(560);
                    Tunggu();
                    Render(w, Path.Combine(folder, $"{nama}_{i}_{tag}_rab.png"));
                    CekTerpotong(w, $"{nama}/{tag}-rab");
                }
            }
        }
        var pv = new PratinjauWindow { Width = 1100, Height = 800, Left = 0, Top = 0, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        pv.Show();
        Tunggu();
        Render(pv, Path.Combine(folder, "pratinjau.png"));
        pv.Close();
        w.Close();
    }

    static void Render(Window w, string path)
    {
        var el = (FrameworkElement)w.Content;
        el.UpdateLayout();
        var rtb = new RenderTargetBitmap((int)Math.Ceiling(el.ActualWidth), (int)Math.Ceiling(el.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, el.ActualWidth, el.ActualHeight));
            dc.DrawRectangle(new VisualBrush(el), null, new Rect(0, 0, el.ActualWidth, el.ActualHeight));
        }
        rtb.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    /// <summary>Mencari TextBlock yang terlihat tetapi tidak mendapat ruang sebesar yang dibutuhkan.</summary>
    static void CekTerpotong(Window w, string halaman)
    {
        int n = 0;
        var contoh = new List<string>();
        foreach (var tb in UiBantu.Turunan<TextBlock>(w))
        {
            if (!tb.IsVisible || string.IsNullOrWhiteSpace(tb.Text) || tb.ActualWidth < 1) continue;
            var m = tb.Margin;
            tb.Measure(new Size(tb.ActualWidth + m.Left + m.Right, double.PositiveInfinity));
            var perlu = new Size(Math.Max(0, tb.DesiredSize.Width - m.Left - m.Right), Math.Max(0, tb.DesiredSize.Height - m.Top - m.Bottom));
            if (perlu.Height > tb.ActualHeight + 1.5 || (tb.TextWrapping == TextWrapping.NoWrap && perlu.Width > tb.ActualWidth + 1.5))
            {
                n++;
                if (contoh.Count < 8) contoh.Add($"\"{(tb.Text.Length > 50 ? tb.Text[..50] + "…" : tb.Text)}\" perlu {perlu.Width:0}x{perlu.Height:0}, dapat {tb.ActualWidth:0}x{tb.ActualHeight:0}");
                continue;
            }
            // Kata yang dipotong di tengah karena kolom terlalu sempit (mis. "Volum|e")
            if (tb.TextWrapping != TextWrapping.NoWrap)
            {
                var tf = new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
                var lebar = tb.ActualWidth - tb.Padding.Left - tb.Padding.Right + 0.5;
                var dpi = VisualTreeHelper.GetDpi(tb).PixelsPerDip;
                foreach (var kata in tb.Text.Split(new[] { ' ', '/', '\\', '-', '\n', '(', ')' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var ft = new FormattedText(kata, Budaya.Id, FlowDirection.LeftToRight, tf, tb.FontSize, Brushes.Black, dpi);
                    if (ft.Width > lebar + 1.5)
                    {
                        n++;
                        if (contoh.Count < 8) contoh.Add($"kata \"{kata}\" terpotong (perlu {ft.Width:0}, dapat {lebar:0})");
                        break;
                    }
                }
            }
        }
        Cek(n == 0, $"Teks terpotong di {halaman}: {n}" + (contoh.Count > 0 ? "\n        " + string.Join("\n        ", contoh) : ""));
    }
}
