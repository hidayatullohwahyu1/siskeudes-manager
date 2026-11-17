using System.IO;
using System.Text.Json;
using System.Windows;

namespace RancangAPBDes.Core;

public static class Folder
{
    public static string DataAplikasi
    {
        get
        {
            var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RancangAPBDes");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string Cadangan
    {
        get
        {
            var p = Path.Combine(DataAplikasi, "Cadangan");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string DokumenDefault
    {
        get
        {
            var doc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(doc)) doc = DataAplikasi;
            return Path.Combine(doc, "Rancang APBDes");
        }
    }
}

public static class Log
{
    static readonly object Kunci = new();

    public static string FilePath => Path.Combine(Folder.DataAplikasi, "log.txt");

    public static void Info(string pesan) => Tulis("INFO", pesan);

    public static void Error(Exception ex, string? konteks = null) =>
        Tulis("ERROR", (konteks != null ? konteks + ": " : "") + ex);

    static void Tulis(string level, string pesan)
    {
        try
        {
            lock (Kunci)
            {
                var f = FilePath;
                if (File.Exists(f) && new FileInfo(f).Length > 2_000_000)
                    File.Move(f, f + ".lama", overwrite: true);
                File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {pesan}{Environment.NewLine}");
            }
        }
        catch
        {
            // log tidak boleh membuat aplikasi berhenti
        }
    }
}

public class Pengaturan
{
    public string? FileTerakhir { get; set; }
    public double Zoom { get; set; } = 1.0;

    static string FilePath => Path.Combine(Folder.DataAplikasi, "pengaturan.json");

    public static Pengaturan Muat()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Pengaturan>(File.ReadAllText(FilePath)) ?? new Pengaturan();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Muat pengaturan");
        }
        return new Pengaturan();
    }

    public void Simpan()
    {
        if (AppState.ModeUji) return;
        try
        {
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan pengaturan");
        }
    }
}

public static class Pesan
{
    const string Judul = "Rancang APBDes";

    static Window? Pemilik =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current?.MainWindow;

    static MessageBoxResult Tampil(string teks, MessageBoxButton tombol, MessageBoxImage ikon)
    {
        var w = Pemilik;
        return w != null && w.IsLoaded
            ? MessageBox.Show(w, teks, Judul, tombol, ikon)
            : MessageBox.Show(teks, Judul, tombol, ikon);
    }

    public static void Info(string teks) => Tampil(teks, MessageBoxButton.OK, MessageBoxImage.Information);

    public static void Peringatan(string teks) => Tampil(teks, MessageBoxButton.OK, MessageBoxImage.Warning);

    public static void Galat(string teks, Exception? ex = null)
    {
        var detail = ex == null ? "" : "\n\nRincian: " + ex.Message;
        Tampil(teks + detail, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static bool Tanya(string teks) =>
        Tampil(teks, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}

public static class Cadangan
{
    /// <summary>Membuat salinan otomatis file data dan menyimpan maksimal 30 salinan per file.</summary>
    public static void Otomatis(Database db)
    {
        try
        {
            var nama = Path.GetFileNameWithoutExtension(db.FilePath);
            var tujuan = Path.Combine(Folder.Cadangan, $"{nama}_{DateTime.Now:yyyyMMdd_HHmmss}.apbdes");
            db.SalinKe(tujuan);

            var lama = new DirectoryInfo(Folder.Cadangan)
                .GetFiles(nama + "_*.apbdes")
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(30);
            foreach (var f in lama)
            {
                try { f.Delete(); } catch { /* abaikan */ }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cadangan otomatis");
        }
    }
}
