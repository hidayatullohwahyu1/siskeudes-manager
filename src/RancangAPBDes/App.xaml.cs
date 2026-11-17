using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using RancangAPBDes.Core;

namespace RancangAPBDes;

public partial class App : Application
{
    Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        var iUji = Array.IndexOf(e.Args, "--uji");
        if (iUji >= 0)
        {
            AturBudaya();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);
            var folder = iUji + 1 < e.Args.Length ? e.Args[iUji + 1] : Path.Combine(Path.GetTempPath(), "RancangAPBDes-uji");
            var gagal = UjiOtomatis.Jalankan(folder);
            Shutdown(gagal == 0 ? 0 : 1);
            return;
        }

        // Satu aplikasi saja agar file data tidak ditulis dua program sekaligus.
        _mutex = new Mutex(true, @"Local\RancangAPBDes_SatuInstans", out bool baru);
        if (!baru)
        {
            MessageBox.Show("Rancang APBDes sudah terbuka. Silakan gunakan jendela yang sudah ada.",
                "Rancang APBDes", MessageBoxButton.OK, MessageBoxImage.Information);
            _mutex = null;
            Shutdown();
            return;
        }

        AturBudaya();

        DispatcherUnhandledException += SaatGalatUi;
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
        {
            if (a.ExceptionObject is Exception ex) Log.Error(ex, "Galat fatal");
            try { AppState.Instance.SimpanSegera(); } catch { /* abaikan */ }
        };
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Log.Error(a.Exception, "Task");
            a.SetObserved();
        };

        base.OnStartup(e);
        Log.Info("Aplikasi dimulai, versi " + typeof(App).Assembly.GetName().Version);

        var w = new MainWindow();
        MainWindow = w;
        w.Show();
        w.BukaFileAwal(e.Args.FirstOrDefault(a => a.EndsWith(".apbdes", StringComparison.OrdinalIgnoreCase) && File.Exists(a)));
    }

    /// <summary>Format angka & tanggal Indonesia (1.234.567,50).</summary>
    static void AturBudaya()
    {
        var id = new CultureInfo("id-ID");
        CultureInfo.DefaultThreadCurrentCulture = id;
        CultureInfo.DefaultThreadCurrentUICulture = id;
        Thread.CurrentThread.CurrentCulture = id;
        Thread.CurrentThread.CurrentUICulture = id;
        var bahasa = XmlLanguage.GetLanguage(id.IetfLanguageTag);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(bahasa));
        FrameworkContentElement.LanguageProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(bahasa));
    }

    int _galatBeruntun;
    DateTime _galatTerakhir;

    void SaatGalatUi(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Galat tampilan");
        e.Handled = true;

        // Hindari jendela pesan beruntun bila galat terjadi berulang-ulang.
        var now = DateTime.Now;
        _galatBeruntun = (now - _galatTerakhir).TotalSeconds < 3 ? _galatBeruntun + 1 : 1;
        _galatTerakhir = now;
        if (_galatBeruntun > 2) return;

        try { AppState.Instance.SimpanSegera(); } catch { /* abaikan */ }
        Pesan.Galat("Terjadi kesalahan, tetapi aplikasi tetap berjalan dan data Anda sudah disimpan.\n" +
                    "Jika berulang, kirimkan file log berikut kepada pembuat aplikasi:\n" + Log.FilePath, e.Exception);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { AppState.Instance.Pengaturan.Simpan(); } catch { /* abaikan */ }
        Log.Info("Aplikasi ditutup");
        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* abaikan */ }
            _mutex.Dispose();
        }
        base.OnExit(e);
    }
}
