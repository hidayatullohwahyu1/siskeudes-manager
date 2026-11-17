using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RancangAPBDes.Core;
using RancangAPBDes.Views;

namespace RancangAPBDes;

public partial class MainWindow : Window
{
    readonly AppState S = AppState.Instance;
    readonly Dictionary<string, FrameworkElement> _halaman = new();
    WindowState _stateSebelumPenuh;
    bool _layarPenuh;

    public static readonly string[] DaftarHalaman = { "beranda", "perencanaan", "isian", "kas", "banding", "desa", "parameter", "bantuan" };

    public MainWindow()
    {
        InitializeComponent();
        TerapkanZoom(S.Pengaturan.Zoom);
        IsianAnggaranView.BukaLaporan = j => BukaPratinjau(j);
        RingkasanView.Perintah = p =>
        {
            switch (p)
            {
                case "desa": ((DataDesaView)Buka("desa")).BukaTab(S.Desa.NamaDesa == "" ? 0 : 1); break;
                case "kegiatan": ((IsianAnggaranView)Buka("isian")).Buka(0, 0); break;
                case "pendapatan": ((IsianAnggaranView)Buka("isian")).Buka(1, 0); break;
                case "belanja": ((IsianAnggaranView)Buka("isian")).Buka(2, 0); break;
                case "pembiayaan": ((IsianAnggaranView)Buka("isian")).Buka(3, 0); break;
                case "perencanaan": ((PerencanaanView)Buka("perencanaan")).Buka(S.Perencanaan.KegiatanRkp.Any() ? 1 : 0, S.Perencanaan.KegiatanRkp.Any() ? 1 : 2); break;
                case "kas": Buka("kas"); break;
                case "periksa": (Buka("beranda") as RingkasanView)?.GulirKePemeriksaan(); break;
                case "posting": Posting_Click(this, new RoutedEventArgs()); break;
                case "cetak": BukaPratinjau(Laporan.SiapInput); break;
            }
        };
        S.HasilBerubah += PerbaruiStatus;
        S.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppState.SkenarioAktif) or nameof(AppState.SkenarioFinal) or nameof(AppState.BisaUbah)) PerbaruiStatus();
        };
        PreviewKeyDown += SaatTombol;
        PreviewMouseWheel += SaatRoda;
        SourceInitialized += (_, _) => PulihkanJendela();
        Loaded += (_, _) => PerbaruiStatus();
    }

    // ------------------------------------------------------------ file awal

    public void BukaFileAwal(string? dariArgumen)
    {
        var kandidat = new[] { dariArgumen, S.Pengaturan.FileTerakhir }
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Cast<string>();
        foreach (var f in kandidat)
            if (S.BukaFile(f)) { Buka("beranda"); return; }

        var def = Path.Combine(Folder.DokumenDefault, "Data APBDes.apbdes");
        if (File.Exists(def) && S.BukaFile(def)) { Buka("beranda"); return; }
        if (!File.Exists(def) && S.BukaFile(def)) { Buka("desa"); TampilkanSelamatDatang(def); return; }

        var cadangan = Path.Combine(Folder.DataAplikasi, $"Data APBDes {DateTime.Now:yyyyMMdd_HHmmss}.apbdes");
        if (S.BukaFile(cadangan)) Buka("desa");
    }

    void TampilkanSelamatDatang(string path)
    {
        Pesan.Info("Selamat datang di Rancang APBDes.\n\n" +
                   "File data baru sudah dibuat di:\n" + path + "\n\n" +
                   "Langkah awal:\n" +
                   "1. Data Entri › Data Umum Desa: isi nama desa dan pelaksana kegiatan.\n" +
                   "2. Data Entri › Penganggaran › Isian Data Anggaran: isi Pendapatan, Kegiatan, lalu Belanja (RAB).\n" +
                   "3. Laporan: lihat Ringkasan, cetak, atau ekspor ke Excel.\n\n" +
                   "Semua perubahan tersimpan otomatis.");
    }

    // ------------------------------------------------------------ navigasi

    public FrameworkElement Buka(string kunci)
    {
        AkhiriIsian();
        if (Isi.Content is ParameterView or DataDesaView) S.PerbaruiDaftarReferensi();
        if (!_halaman.TryGetValue(kunci, out var h))
        {
            Action tutup = () => Buka("beranda");
            h = kunci switch
            {
                "beranda" => new BingkaiForm("Beranda – Ringkasan APBDes", new RingkasanView(), null, "Laporan › Ringkasan APBDes"),
                "isian" => new BingkaiForm("Isian Data Anggaran", new IsianAnggaranView(), tutup, "Data Entri › Penganggaran › Isian Data Anggaran"),
                "perencanaan" => new BingkaiForm("RPJMDes dan RKPDes", new PerencanaanView(), tutup, "Data Entri › Perencanaan › RPJMDes dan RKPDes"),
                "kas" => new BingkaiForm("Anggaran Kas Desa", new AnggaranKasView(), tutup, "Data Entri › Penganggaran › Anggaran Kas Desa"),
                "banding" => new BingkaiForm("Perbandingan Anggaran Semula – Menjadi", new BandingView(), tutup, "Simulasi › Bandingkan Skenario"),
                "desa" => new BingkaiForm("Data Umum Desa", new DataDesaView(), tutup, "Data Entri › Data Umum Desa"),
                "parameter" => new BingkaiForm("Parameter", new ParameterView(), tutup, "Parameter"),
                _ => new BingkaiForm("Petunjuk Penggunaan", new BantuanView(), tutup, "Bantuan › Petunjuk Penggunaan"),
            };
            _halaman[kunci] = h;
        }
        Isi.Content = h;
        return ((BingkaiForm)h).Isi;
    }

    /// <summary>Batalkan isian yang belum disimpan sebelum pindah halaman/skenario.</summary>
    void AkhiriIsian()
    {
        if (Isi.Content is FrameworkElement fe)
            foreach (var h in UiBantu.Turunan<HalamanData>(fe))
                if (h.SedangMengisi)
                {
                    if (Pesan.Tanya($"Isian \"{h.JudulData}\" belum disimpan. Simpan sekarang?")) { if (!h.Simpan()) h.Batal(); }
                    else h.Batal();
                }
    }

    void Beranda_Click(object sender, RoutedEventArgs e) => Buka("beranda");
    void Isian_Click(object sender, RoutedEventArgs e) => Buka("isian");
    void Banding_Click(object sender, RoutedEventArgs e) => Buka("banding");
    void DataUmum_Click(object sender, RoutedEventArgs e) => ((DataDesaView)Buka("desa")).BukaTab(0);
    void Pelaksana_Click(object sender, RoutedEventArgs e) => ((DataDesaView)Buka("desa")).BukaTab(1);
    void Bantuan_Click(object sender, RoutedEventArgs e) => Buka("bantuan");
    void Perencanaan_Click(object sender, RoutedEventArgs e) => Buka("perencanaan");
    void Kas_Click(object sender, RoutedEventArgs e) => Buka("kas");

    void Parameter_Click(object sender, RoutedEventArgs e)
    {
        var tab = int.TryParse((sender as FrameworkElement)?.Tag as string, out var i) ? i : 0;
        ((ParameterView)Buka("parameter")).BukaTab(tab);
    }

    // ------------------------------------------------------------ skenario

    void SkenarioBaru_Click(object sender, RoutedEventArgs e) => BuatSkenario(false);
    void SkenarioSalin_Click(object sender, RoutedEventArgs e) => BuatSkenario(true);

    void BuatSkenario(bool salin)
    {
        if (salin && S.SkenarioAktif == null) return;
        AkhiriIsian();
        var d = new DialogSkenario(salin, S.SkenarioAktif) { Owner = this };
        if (d.ShowDialog() == true) S.BuatSkenario(d.NamaSkenario, d.Tahun, salin);
    }

    void SkenarioNama_Click(object sender, RoutedEventArgs e)
    {
        var a = S.SkenarioAktif;
        if (a == null) return;
        var nama = DialogInput.Tanya(this, "Ganti nama skenario", "Nama skenario:", a.Nama);
        if (!string.IsNullOrWhiteSpace(nama)) S.GantiNama(a, nama);
    }

    void SkenarioHapus_Click(object sender, RoutedEventArgs e)
    {
        var a = S.SkenarioAktif;
        if (a == null) return;
        if (!Pesan.Tanya($"Hapus skenario \"{a.Nama}\" (tahun {a.Tahun})?\n\nSkenario yang dihapus tidak bisa dikembalikan dengan Batalkan, " +
                         "tetapi masih ada di cadangan otomatis (File › Buka Folder Cadangan Otomatis).")) return;
        AkhiriIsian();
        S.HapusSkenario(a);
    }

    void Posting_Click(object sender, RoutedEventArgs e)
    {
        var a = S.SkenarioAktif;
        if (a == null) return;
        AkhiriIsian();
        if (a.IsFinal)
        {
            if (Pesan.Tanya($"Batalkan posting skenario \"{a.Nama}\" sehingga bisa diubah kembali?")) S.SetFinal(a, false);
            return;
        }
        S.Hitung();
        var salah = S.Hasil?.JumlahKesalahan ?? 0;
        var peringatan = salah > 0 ? $"\n\nPerhatian: masih ada {salah} kesalahan di Beranda (mis. belanja melebihi pagu)." : "";
        if (Pesan.Tanya($"Posting skenario \"{a.Nama}\" sebagai hasil kesepakatan?\n\nSkenario yang sudah diposting dikunci dan siap diinput ke Siskeudes.{peringatan}"))
            S.SetFinal(a, true);
    }

    void PerbaruiStatus()
    {
        var a = S.SkenarioAktif;
        MenuPosting.Header = a?.IsFinal == true ? "Batalkan Posting APBDes…" : "Posting APBDes (kunci skenario)…";
        if (a == null) { LencanaStatus.Visibility = Visibility.Collapsed; }
        else
        {
            LencanaStatus.Visibility = Visibility.Visible;
            TxtStatus.Text = a.IsFinal ? "POSTING" : "DRAFT";
            LencanaStatus.Background = (Brush)FindResource(a.IsFinal ? "HijauMuda" : "PrimerMuda");
            TxtStatus.Foreground = (Brush)FindResource(a.IsFinal ? "Hijau" : "PrimerGelap");
            LencanaStatus.ToolTip = a.IsFinal ? "Skenario sudah diposting (terkunci)" : "Skenario masih bisa diubah";
        }

        var h = S.Hasil;
        if (h == null) { TxtSisaAnggaran.Text = ""; return; }
        var v = h.SisaAnggaran;
        TxtSisaAnggaran.Text = v == 0 ? "· Anggaran seimbang" : v > 0 ? $"· Belum dialokasikan {Kalkulasi.Rp(v)}" : $"· DEFISIT {Kalkulasi.Rp(-v)}";
        TxtSisaAnggaran.Foreground = (Brush)FindResource(v < 0 ? "Merah" : "WarnaRedup");
        TxtSisaAnggaran.FontWeight = v < 0 ? FontWeights.SemiBold : FontWeights.Normal;
    }

    void Batalkan_Click(object sender, RoutedEventArgs e) { AkhiriIsian(); S.Batalkan(); }
    void Ulangi_Click(object sender, RoutedEventArgs e) { AkhiriIsian(); S.Ulangi(); }

    // ------------------------------------------------------------ laporan

    void Ekspor_Click(object sender, RoutedEventArgs e)
    {
        if (S.Data == null || S.SkenarioAktif == null) return;
        AkhiriIsian();
        S.SimpanSegera();
        S.Hitung();
        var dlg = new SaveFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            FileName = UiBantu.NamaFileAman($"APBDes {S.Desa.NamaDesa} {S.SkenarioAktif.Tahun} - {S.SkenarioAktif.Nama}") + ".xlsx",
            InitialDirectory = Folder.DokumenDefault,
            OverwritePrompt = true
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            EksporExcel.Simpan(dlg.FileName, S.Desa, S.SkenarioAktif, S.Data, S.Hasil!);
            Mouse.OverrideCursor = null;
            if (Pesan.Tanya("File Excel berhasil dibuat:\n" + dlg.FileName + "\n\nBuka sekarang?"))
                UiBantu.BukaDenganWindows(dlg.FileName);
        }
        catch (IOException ex)
        {
            Mouse.OverrideCursor = null;
            Log.Error(ex, "Ekspor");
            Pesan.Galat("File Excel tidak dapat disimpan. Jika file itu sedang dibuka di Excel, tutup dulu lalu coba lagi.", ex);
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor = null;
            Log.Error(ex, "Ekspor");
            Pesan.Galat("Ekspor Excel gagal.", ex);
        }
    }

    void Cetak_Click(object sender, RoutedEventArgs e) => BukaPratinjau(null);

    void BukaPratinjau(string? jenis)
    {
        if (S.Data == null || S.SkenarioAktif == null) return;
        AkhiriIsian();
        S.SimpanSegera();
        S.Hitung();
        if (AppState.ModeUji) return;
        new PratinjauWindow(jenis) { Owner = this }.ShowDialog();
    }

    // ------------------------------------------------------------ file

    void BukaFile_Click(object sender, RoutedEventArgs e)
    {
        AkhiriIsian();
        var dlg = new OpenFileDialog
        {
            Filter = "File data Rancang APBDes (*.apbdes)|*.apbdes|Semua file (*.*)|*.*",
            InitialDirectory = Path.GetDirectoryName(S.FilePath ?? Folder.DokumenDefault)
        };
        if (dlg.ShowDialog(this) != true) return;
        if (string.Equals(dlg.FileName, S.FilePath, StringComparison.OrdinalIgnoreCase)) return;
        var lama = S.FilePath;
        if (!S.BukaFile(dlg.FileName) && lama != null && S.FilePath == null) S.BukaFile(lama);
        ResetHalaman();
    }

    void FileBaru_Click(object sender, RoutedEventArgs e)
    {
        AkhiriIsian();
        var dlg = new SaveFileDialog
        {
            Filter = "File data Rancang APBDes (*.apbdes)|*.apbdes",
            FileName = "Data APBDes Baru.apbdes",
            InitialDirectory = Folder.DokumenDefault,
            OverwritePrompt = true
        };
        if (dlg.ShowDialog(this) != true) return;
        var lama = S.FilePath;
        if (!S.BuatFileBaru(dlg.FileName) && lama != null && S.FilePath == null) S.BukaFile(lama);
        ResetHalaman();
        Buka("desa");
    }

    void SimpanSalinan_Click(object sender, RoutedEventArgs e)
    {
        if (S.FilePath == null) return;
        AkhiriIsian();
        var dlg = new SaveFileDialog
        {
            Filter = "File data Rancang APBDes (*.apbdes)|*.apbdes",
            FileName = UiBantu.NamaFileAman($"Salinan {Path.GetFileNameWithoutExtension(S.FilePath)} {DateTime.Now:yyyy-MM-dd}") + ".apbdes",
            OverwritePrompt = true
        };
        if (dlg.ShowDialog(this) != true) return;
        if (string.Equals(dlg.FileName, S.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            Pesan.Peringatan("Pilih nama atau lokasi yang berbeda dari file yang sedang dibuka.");
            return;
        }
        try
        {
            S.SalinFileKe(dlg.FileName);
            Pesan.Info("Salinan berhasil disimpan:\n" + dlg.FileName + "\n\nFile yang sedang dibuka tetap file yang lama.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan salinan");
            Pesan.Galat("Salinan gagal disimpan.", ex);
        }
    }

    void Impor_Click(object sender, RoutedEventArgs e)
    {
        if (S.FilePath == null) return;
        AkhiriIsian();
        var dlg = new OpenFileDialog
        {
            Filter = "File Excel (*.xlsx)|*.xlsx",
            Multiselect = true,
            Title = "Pilih file Excel APBDes dari Siskeudes (boleh beberapa: RAB, Pendapatan, Pembiayaan)"
        };
        if (dlg.ShowDialog(this) != true) return;
        ImporSiskeudes.Hasil h;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            h = ImporSiskeudes.Baca(dlg.FileNames);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Impor Siskeudes");
            Pesan.Galat("File tidak dapat dibaca. Pastikan formatnya .xlsx dan file tidak sedang dibuka di Excel.", ex);
            return;
        }
        finally { Mouse.OverrideCursor = null; }

        bool kosong = h.Versi.Values.All(d => d.Kegiatan.Count == 0 && d.Pendapatan.Count == 0 && d.PenerimaanPembiayaan.Count == 0 && d.PengeluaranPembiayaan.Count == 0);
        if (kosong)
        {
            Pesan.Peringatan("Tidak ada data anggaran yang dikenali di file tersebut.\n\nFile yang didukung: RAB per kegiatan, Rencana Anggaran Pendapatan, dan Rencana Anggaran Pembiayaan " +
                             "dengan kolom KODE, URAIAN, SUMBER DANA, VOLUME, HARGA SATUAN, JUMLAH.");
            return;
        }
        var d = new DialogImpor(h, S.SkenarioAktif?.Tahun ?? DateTime.Today.Year) { Owner = this };
        if (d.ShowDialog() != true) return;

        if (d.TambahReferensi)
        {
            foreach (var (k, n) in h.RekeningBaru) if (!S.Referensi.Rekening.Any(r => r.Kode == k)) S.Referensi.Rekening.Add(new RefRekening { Kode = k, Nama = n });
            foreach (var (k, n) in h.KegiatanBaru) if (!S.Referensi.Kegiatan.Any(r => r.Kode == k)) S.Referensi.Kegiatan.Add(new RefKegiatan { Kode = k, Nama = n });
        }
        if (d.TambahPelaksana)
            foreach (var nama in h.Pelaksana.Values.Distinct(StringComparer.OrdinalIgnoreCase))
                if (!S.Desa.Perangkat.Any(p => string.Equals(p.Nama, nama, StringComparison.OrdinalIgnoreCase)))
                    S.Desa.Perangkat.Add(new Perangkat { Nama = nama, Jabatan = "Pelaksana Kegiatan" });
        S.PerbaruiDaftarReferensi();

        var pilihan = d.Pilihan.ToList();
        for (int i = 0; i < pilihan.Count; i++)
        {
            var data = h.Versi[pilihan[i].versi];
            foreach (var k in data.Kegiatan)
            {
                if (string.IsNullOrWhiteSpace(k.Lokasi)) k.Lokasi = S.Desa.NamaDesa;
                foreach (var p in k.Paket) if (string.IsNullOrWhiteSpace(p.Lokasi)) p.Lokasi = k.Lokasi;
            }
            S.BuatSkenarioDari(pilihan[i].nama, d.Tahun, data, pilih: i == pilihan.Count - 1);
        }
        Buka("beranda");
        Pesan.Info($"{pilihan.Count} skenario berhasil diimpor ke tahun {d.Tahun}." +
                   (pilihan.Count > 1 ? "\n\nGunakan Simulasi › Bandingkan Skenario atau Laporan › Perubahan APBDes untuk melihat Semula – Menjadi." : ""));
    }

    void FolderData_Click(object sender, RoutedEventArgs e) => UiBantu.BukaFolder(Path.GetDirectoryName(S.FilePath ?? Folder.DokumenDefault)!);
    void FolderCadangan_Click(object sender, RoutedEventArgs e) => UiBantu.BukaFolder(Folder.Cadangan);
    void Keluar_Click(object sender, RoutedEventArgs e) => Close();

    void ResetHalaman()
    {
        _halaman.Clear();
        Isi.Content = null;
        Buka("beranda");
    }

    // ------------------------------------------------------------ tampilan

    void TerapkanZoom(double z)
    {
        z = Math.Round(Math.Clamp(z, 0.8, 1.8), 2);
        Skala.ScaleX = z;
        Skala.ScaleY = z;
        TxtZoom.Text = $"{z * 100:0}%";
        S.Pengaturan.Zoom = z;
    }

    internal void AturZoomUji(double z) => TerapkanZoom(z);

    void Perbesar_Click(object sender, RoutedEventArgs e) => TerapkanZoom(Skala.ScaleX + 0.1);
    void Perkecil_Click(object sender, RoutedEventArgs e) => TerapkanZoom(Skala.ScaleX - 0.1);
    void Normal_Click(object sender, RoutedEventArgs e) => TerapkanZoom(1.0);
    void LayarPenuh_Click(object sender, RoutedEventArgs e) => AlihLayarPenuh();

    void AlihLayarPenuh()
    {
        if (!_layarPenuh)
        {
            _stateSebelumPenuh = WindowState;
            WindowStyle = WindowStyle.None;
            if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
            _layarPenuh = true;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = _stateSebelumPenuh;
            _layarPenuh = false;
        }
    }

    void SaatTombol(object sender, KeyEventArgs e)
    {
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (e.Key == Key.F11) { AlihLayarPenuh(); e.Handled = true; return; }
        if (e.Key == Key.Escape && _layarPenuh && Keyboard.FocusedElement is not TextBox) { AlihLayarPenuh(); e.Handled = true; return; }
        if (!ctrl && Keyboard.FocusedElement is not TextBox)
        {
            if (e.Key == Key.F1) { Buka("bantuan"); e.Handled = true; }
            else if (e.Key == Key.F3) { Buka("isian"); e.Handled = true; }
            return;
        }
        if (!ctrl) return;
        if (e.Key is Key.OemPlus or Key.Add) { TerapkanZoom(Skala.ScaleX + 0.1); e.Handled = true; }
        else if (e.Key is Key.OemMinus or Key.Subtract) { TerapkanZoom(Skala.ScaleX - 0.1); e.Handled = true; }
        else if (e.Key is Key.D0 or Key.NumPad0) { TerapkanZoom(1.0); e.Handled = true; }
        else if (e.Key == Key.O) { BukaFile_Click(this, e); e.Handled = true; }
        else if (e.Key == Key.P) { Cetak_Click(this, e); e.Handled = true; }
        else if (Keyboard.FocusedElement is not TextBox && Keyboard.FocusedElement is not ComboBox)
        {
            if (e.Key == Key.Z) { AkhiriIsian(); S.Batalkan(); e.Handled = true; }
            else if (e.Key == Key.Y) { AkhiriIsian(); S.Ulangi(); e.Handled = true; }
        }
    }

    void SaatRoda(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        TerapkanZoom(Skala.ScaleX + (e.Delta > 0 ? 0.1 : -0.1));
        e.Handled = true;
    }

    /// <summary>Pertama kali: maksimal (paling lega, seperti Siskeudes). Berikutnya: ukuran & posisi terakhir.</summary>
    void PulihkanJendela()
    {
        if (AppState.ModeUji) return;
        var p = S.Pengaturan;
        var layar = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var simpan = new Rect(p.Kiri, p.Atas, p.Lebar, p.Tinggi);
        if (p.JendelaMaksimal != null && p.Lebar >= MinWidth && p.Tinggi >= MinHeight && layar.IntersectsWith(simpan)
            && simpan.Left >= layar.Left - 50 && simpan.Top >= layar.Top - 20 && simpan.Right <= layar.Right + 50)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = p.Kiri; Top = p.Atas; Width = p.Lebar; Height = p.Tinggi;
            if (p.JendelaMaksimal == true) WindowState = WindowState.Maximized;
        }
        else
        {
            var kerja = SystemParameters.WorkArea;
            Width = Math.Min(1400, kerja.Width);
            Height = Math.Min(860, kerja.Height);
            WindowState = WindowState.Maximized;
        }
    }

    void SimpanUkuranJendela()
    {
        if (AppState.ModeUji || _layarPenuh) return;
        var p = S.Pengaturan;
        var b = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        p.JendelaMaksimal = WindowState == WindowState.Maximized;
        if (!b.IsEmpty) { p.Kiri = b.Left; p.Atas = b.Top; p.Lebar = b.Width; p.Tinggi = b.Height; }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SimpanUkuranJendela();
        AkhiriIsian();
        if (!S.SimpanSegera() &&
            !Pesan.Tanya("Sebagian perubahan GAGAL disimpan ke file (misalnya flashdisk terlepas).\n\n" +
                         "Pilih \"No\" untuk kembali lalu simpan salinan ke lokasi lain lewat File › Simpan Salinan Sebagai.\n" +
                         "Pilih \"Yes\" untuk tetap menutup (perubahan terakhir bisa hilang)."))
        {
            e.Cancel = true;
            return;
        }
        S.TutupFile();
        S.Pengaturan.Simpan();
        base.OnClosing(e);
    }
}
