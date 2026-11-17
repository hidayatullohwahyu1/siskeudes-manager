using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    public MainWindow()
    {
        InitializeComponent();
        TerapkanZoom(S.Pengaturan.Zoom);
        S.HasilBerubah += PerbaruiSisaAnggaran;
        PreviewKeyDown += SaatTombol;
        PreviewMouseWheel += SaatRoda;
        Loaded += (_, _) =>
        {
            if (!AppState.ModeUji && WindowState != WindowState.Maximized && SystemParameters.WorkArea.Width < 1500)
                WindowState = WindowState.Maximized;
        };
    }

    // ------------------------------------------------------------ file awal

    public void BukaFileAwal(string? dariArgumen)
    {
        var kandidat = new[] { dariArgumen, S.Pengaturan.FileTerakhir }
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Cast<string>();
        foreach (var f in kandidat)
            if (S.BukaFile(f)) { Nav.SelectedIndex = 0; return; }

        var def = Path.Combine(Folder.DokumenDefault, "Data APBDes.apbdes");
        if (File.Exists(def) && S.BukaFile(def)) { Nav.SelectedIndex = 0; return; }

        if (!File.Exists(def) && S.BukaFile(def)) { Nav.SelectedIndex = 4; TampilkanSelamatDatang(def); return; }

        // Upaya terakhir: file baru di folder data aplikasi
        var cadangan = Path.Combine(Folder.DataAplikasi, $"Data APBDes {DateTime.Now:yyyyMMdd_HHmmss}.apbdes");
        if (S.BukaFile(cadangan)) Nav.SelectedIndex = 4;
    }

    void TampilkanSelamatDatang(string path)
    {
        Pesan.Info("Selamat datang di Rancang APBDes.\n\n" +
                   "File data baru sudah dibuat di:\n" + path + "\n\n" +
                   "Langkah awal:\n" +
                   "1. Isi Data Desa (nama, kode desa, perangkat).\n" +
                   "2. Cocokkan Referensi (kode rekening, sumber dana) dengan Siskeudes.\n" +
                   "3. Isi Pendapatan, lalu susun Belanja per kegiatan.\n\n" +
                   "Semua perubahan tersimpan otomatis.");
    }

    // ------------------------------------------------------------ navigasi

    void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Nav.SelectedItem is not ListBoxItem item || item.Tag is not string kunci) return;
        try
        {
            if (Isi.Content is ReferensiView || Isi.Content is DesaView)
                S.PerbaruiDaftarReferensi();
            AkhiriEdit();
            if (!_halaman.TryGetValue(kunci, out var h))
            {
                h = kunci switch
                {
                    "ringkasan" => new RingkasanView(),
                    "belanja" => new BelanjaView(),
                    "pendapatan" => new PendapatanView(),
                    "banding" => new BandingView(),
                    "desa" => new DesaView(),
                    "referensi" => new ReferensiView(),
                    _ => new BantuanView(),
                };
                _halaman[kunci] = h;
            }
            Isi.Content = h;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Navigasi");
            Pesan.Galat("Halaman tidak dapat dibuka.", ex);
        }
    }

    void AkhiriEdit()
    {
        if (Isi.Content is FrameworkElement fe)
            foreach (var g in UiBantu.Turunan<DataGrid>(fe))
            {
                try { g.CommitEdit(DataGridEditingUnit.Row, true); } catch { g.CancelEdit(); }
            }
    }

    // ------------------------------------------------------------ bilah alat

    void Skenario_Click(object sender, RoutedEventArgs e)
    {
        var m = new ContextMenu { PlacementTarget = BtnSkenario, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        var aktif = S.SkenarioAktif;
        m.Items.Add(Menu("Skenario baru (kosong)…", () => BuatSkenario(false)));
        m.Items.Add(Menu("Salin skenario ini…", () => BuatSkenario(true), aktif != null));
        m.Items.Add(Menu("Ganti nama…", GantiNama, aktif != null));
        m.Items.Add(new Separator());
        if (aktif?.IsFinal == true)
            m.Items.Add(Menu("Buka kunci (kembali ke Draft)", () => { AkhiriEdit(); S.SetFinal(aktif, false); }));
        else
            m.Items.Add(Menu("Tandai FINAL – siap diinput ke Siskeudes", () =>
            {
                AkhiriEdit();
                if (Pesan.Tanya($"Kunci skenario \"{aktif!.Nama}\" sebagai FINAL?\n\nSkenario final tidak bisa diubah sampai kuncinya dibuka kembali."))
                    S.SetFinal(aktif, true);
            }, aktif != null));
        m.Items.Add(new Separator());
        m.Items.Add(Menu("Hapus skenario ini…", HapusSkenario, aktif != null));
        m.IsOpen = true;
    }

    static MenuItem Menu(string teks, Action aksi, bool aktif = true)
    {
        var mi = new MenuItem { Header = teks, IsEnabled = aktif };
        mi.Click += (_, _) =>
        {
            try { aksi(); }
            catch (Exception ex) { Log.Error(ex, teks); Pesan.Galat("Perintah gagal dijalankan.", ex); }
        };
        return mi;
    }

    void BuatSkenario(bool salin)
    {
        AkhiriEdit();
        var d = new DialogSkenario(salin, S.SkenarioAktif) { Owner = this };
        if (d.ShowDialog() == true)
            S.BuatSkenario(d.NamaSkenario, d.Tahun, salin);
    }

    void GantiNama()
    {
        var a = S.SkenarioAktif;
        if (a == null) return;
        var nama = DialogInput.Tanya(this, "Ganti nama skenario", "Nama skenario:", a.Nama);
        if (!string.IsNullOrWhiteSpace(nama)) S.GantiNama(a, nama);
    }

    void HapusSkenario()
    {
        var a = S.SkenarioAktif;
        if (a == null) return;
        if (!Pesan.Tanya($"Hapus skenario \"{a.Nama}\" (tahun {a.Tahun})?\n\nSkenario yang dihapus tidak bisa dibatalkan dengan tombol Batalkan, " +
                         "tetapi masih ada di file cadangan otomatis (File ▸ Buka Folder Cadangan).")) return;
        AkhiriEdit();
        S.HapusSkenario(a);
    }

    void Batalkan_Click(object sender, RoutedEventArgs e) { AkhiriEdit(); S.Batalkan(); }
    void Ulangi_Click(object sender, RoutedEventArgs e) { AkhiriEdit(); S.Ulangi(); }

    void Ekspor_Click(object sender, RoutedEventArgs e)
    {
        if (S.Data == null || S.SkenarioAktif == null) return;
        AkhiriEdit();
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

    void Cetak_Click(object sender, RoutedEventArgs e)
    {
        if (S.Data == null || S.SkenarioAktif == null) return;
        AkhiriEdit();
        S.SimpanSegera();
        S.Hitung();
        new PratinjauWindow { Owner = this }.ShowDialog();
    }

    void File_Click(object sender, RoutedEventArgs e)
    {
        var m = new ContextMenu { PlacementTarget = BtnFile, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        m.Items.Add(Menu("Buka file data…", BukaFile));
        m.Items.Add(Menu("Buat file data baru…", FileBaru));
        m.Items.Add(Menu("Simpan salinan sebagai… (mis. ke flashdisk)", SimpanSalinan, S.FilePath != null));
        m.Items.Add(new Separator());
        m.Items.Add(Menu("Buka folder file data", () => UiBantu.BukaFolder(Path.GetDirectoryName(S.FilePath ?? Folder.DokumenDefault)!)));
        m.Items.Add(Menu("Buka folder cadangan otomatis", () => UiBantu.BukaFolder(Folder.Cadangan)));
        m.IsOpen = true;
    }

    void BukaFile()
    {
        AkhiriEdit();
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

    void FileBaru()
    {
        AkhiriEdit();
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
        Nav.SelectedIndex = 4;
    }

    void SimpanSalinan()
    {
        AkhiriEdit();
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

    void ResetHalaman()
    {
        var idx = Nav.SelectedIndex;
        _halaman.Clear();
        Isi.Content = null;
        Nav.SelectedIndex = -1;
        Nav.SelectedIndex = idx < 0 ? 0 : idx;
    }

    // ------------------------------------------------------------ zoom & layar penuh

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
        if (e.Key == Key.F11 || (e.Key == Key.Escape && _layarPenuh && Keyboard.FocusedElement is not TextBox))
        {
            if (e.Key == Key.F11 || _layarPenuh) { AlihLayarPenuh(); e.Handled = true; }
            return;
        }
        if (!ctrl) return;
        if (e.Key is Key.OemPlus or Key.Add) { TerapkanZoom(Skala.ScaleX + 0.1); e.Handled = true; }
        else if (e.Key is Key.OemMinus or Key.Subtract) { TerapkanZoom(Skala.ScaleX - 0.1); e.Handled = true; }
        else if (e.Key is Key.D0 or Key.NumPad0) { TerapkanZoom(1.0); e.Handled = true; }
        else if (Keyboard.FocusedElement is not TextBox && Keyboard.FocusedElement is not ComboBox)
        {
            if (e.Key == Key.Z) { AkhiriEdit(); S.Batalkan(); e.Handled = true; }
            else if (e.Key == Key.Y) { AkhiriEdit(); S.Ulangi(); e.Handled = true; }
        }
    }

    void SaatRoda(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        TerapkanZoom(Skala.ScaleX + (e.Delta > 0 ? 0.1 : -0.1));
        e.Handled = true;
    }

    // ------------------------------------------------------------ status

    void PerbaruiSisaAnggaran()
    {
        var h = S.Hasil;
        if (h == null) { TxtSisaAnggaran.Text = ""; return; }
        var v = h.SisaAnggaran;
        TxtSisaAnggaran.Text = v == 0
            ? "· Anggaran seimbang"
            : v > 0 ? $"· Belum dialokasikan {Kalkulasi.Rp(v)}" : $"· DEFISIT {Kalkulasi.Rp(-v)}";
        TxtSisaAnggaran.Foreground = v < 0 ? (System.Windows.Media.Brush)FindResource("Merah") : (System.Windows.Media.Brush)FindResource("WarnaRedup");
        TxtSisaAnggaran.FontWeight = v < 0 ? FontWeights.SemiBold : FontWeights.Normal;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        AkhiriEdit();
        if (!S.SimpanSegera() &&
            !Pesan.Tanya("Sebagian perubahan GAGAL disimpan ke file (misalnya flashdisk terlepas).\n\n" +
                         "Pilih \"No\" untuk kembali lalu simpan salinan ke lokasi lain lewat File ▸ Simpan salinan sebagai.\n" +
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
