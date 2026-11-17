using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RancangAPBDes.Core;
using RancangAPBDes.Views;

namespace RancangAPBDes;

/// <summary>
/// Audit tampilan yang terukur: ukuran & jenis font, kontras warna (WCAG), teks terpotong (termasuk "…"),
/// isian yang isinya tidak muat, dan tombol tanpa aksi.
/// </summary>
public static class AuditTampilan
{
    public static readonly SortedDictionary<double, int> Ukuran = new();
    public static readonly SortedDictionary<string, int> Jenis = new();
    public static readonly List<string> KontrasRendah = new();
    public static readonly List<string> Terpotong = new();
    public static readonly List<string> TanpaAksi = new();
    public static readonly List<string> GalatKlik = new();
    static readonly HashSet<string> _sudah = new();

    static void Catat(List<string> daftar, string teks)
    {
        if (_sudah.Add(daftar.GetHashCode() + teks)) daftar.Add(teks);
    }

    public static void Periksa(Window w, string halaman)
    {
        foreach (var tb in UiBantu.Turunan<TextBlock>(w))
        {
            if (!tb.IsVisible || tb.ActualWidth < 1 || string.IsNullOrWhiteSpace(tb.Text)) continue;
            var uk = Math.Round(tb.FontSize * 2) / 2;
            Ukuran[uk] = Ukuran.GetValueOrDefault(uk) + 1;
            var jenis = tb.FontFamily.Source;
            Jenis[jenis] = Jenis.GetValueOrDefault(jenis) + 1;
            var cuplik = tb.Text.Length > 40 ? tb.Text[..40] + "…" : tb.Text;

            // Kontras
            if (!Redup(tb) && UiBantu.Leluhur<Button>(tb) is not { IsEnabled: false } && tb.Foreground is SolidColorBrush fg)
            {
                var bg = Latar(tb);
                var r = Rasio(Campur(fg.Color, bg), bg);
                bool besar = tb.FontSize >= 18.5 || (tb.FontSize >= 14 && tb.FontWeight.ToOpenTypeWeight() >= 600);
                // Standar AAA: 7:1 untuk teks biasa, 4,5:1 untuk teks besar/tebal
                if (r < (besar ? 4.5 : 7.0))
                    Catat(KontrasRendah, $"{halaman}: \"{cuplik}\" kontras {r:0.0}:1 (teks #{fg.Color.R:X2}{fg.Color.G:X2}{fg.Color.B:X2} di latar #{bg.R:X2}{bg.G:X2}{bg.B:X2})");
            }

            // Teks dipangkas dengan "…" atau dibatasi tinggi
            if (tb.TextTrimming != TextTrimming.None || !double.IsInfinity(tb.MaxHeight))
            {
                var lebar = tb.TextWrapping == TextWrapping.NoWrap ? double.PositiveInfinity : tb.ActualWidth;
                var uji = new TextBlock { Text = tb.Text, FontFamily = tb.FontFamily, FontSize = tb.FontSize, FontWeight = tb.FontWeight, TextWrapping = tb.TextWrapping };
                uji.Measure(new Size(lebar, double.PositiveInfinity));
                if (uji.DesiredSize.Width > tb.ActualWidth + 1 || uji.DesiredSize.Height > tb.ActualHeight + 1)
                    Catat(Terpotong, $"{halaman}: \"{cuplik}\" dipangkas (…)");
            }
        }

        // Isian yang isinya lebih panjang dari kotaknya
        foreach (var t in UiBantu.Turunan<TextBox>(w))
        {
            if (!t.IsVisible || t.ActualWidth < 1 || string.IsNullOrEmpty(t.Text)) continue;
            if (t.TextWrapping == TextWrapping.NoWrap && t.ExtentWidth > t.ViewportWidth + 2)
                Catat(Terpotong, $"{halaman}: isian \"{(t.Text.Length > 40 ? t.Text[..40] + "…" : t.Text)}\" tidak muat di kotaknya");
        }

        // Tombol tanpa aksi
        foreach (var b in UiBantu.Turunan<Button>(w))
        {
            if (!b.IsVisible || b.Command != null || PunyaKlik(b)) continue;
            if (UiBantu.Leluhur<ComboBox>(b) != null || UiBantu.Leluhur<ScrollBar>(b) != null) continue;
            Catat(TanpaAksi, $"{halaman}: tombol \"{b.Content}\" tidak punya aksi");
        }
        foreach (var menu in UiBantu.Turunan<Menu>(w))
            foreach (var m in SemuaMenu(menu.Items))
                if (m.Command == null && !m.HasItems && !PunyaKlik(m))
                    Catat(TanpaAksi, $"menu \"{m.Header}\" tidak punya aksi");
    }

    /// <summary>Klik setiap tombol yang aktif pada halaman data (dialog dijawab "batal" secara otomatis).</summary>
    /// <summary>Pencatat langkah (untuk menemukan titik macet).</summary>
    public static Action<string>? Jejak { get; set; }

    public static void KlikSemua(Window w, string halaman)
    {
        foreach (var h in UiBantu.Turunan<HalamanData>(w).Where(x => x.IsVisible).ToList())
        {
            foreach (var b in UiBantu.Turunan<Button>(h).Where(b => b.IsVisible && b.IsEnabled && UiBantu.Leluhur<DataGrid>(b) == null).ToList())
            {
                var nama = b.Content?.ToString() ?? "?";
                if (nama.StartsWith("Lihat") || nama.StartsWith("Impor") || nama == "Tutup" || nama == "Cetak") continue; // berpindah halaman / membuka jendela
                var sebelum = Pesan.CatatanUji.Count;
                Jejak?.Invoke($"klik {halaman} › {h.JudulData} › {nama}");
                try
                {
                    b.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Tunggu();
                    if (h.SedangMengisi) { if (nama == "Ubah") h.Simpan(); else h.Batal(); }
                    Tunggu();
                }
                catch (Exception ex)
                {
                    GalatKlik.Add($"{halaman} › {h.JudulData} › {nama}: {ex.Message}");
                }
                foreach (var p in Pesan.CatatanUji.Skip(sebelum).Where(p => p.StartsWith("Error")))
                    GalatKlik.Add($"{halaman} › {h.JudulData} › {nama}: {p}");
            }
        }
    }

    static IEnumerable<MenuItem> SemuaMenu(System.Collections.IEnumerable items)
    {
        foreach (var x in items)
            if (x is MenuItem m)
            {
                yield return m;
                foreach (var a in SemuaMenu(m.Items)) yield return a;
            }
    }

    static void Tunggu()
    {
        for (int i = 0; i < 2; i++)
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    public static string Ringkasan()
    {
        var sb = new StringBuilder();
        sb.AppendLine("== AUDIT TAMPILAN ==");
        sb.AppendLine("Ukuran font dipakai (px: jumlah teks): " + string.Join(", ", Ukuran.Select(k => $"{k.Key}: {k.Value}")));
        sb.AppendLine("Jenis font dipakai: " + string.Join(", ", Jenis.Select(k => $"{k.Key}: {k.Value}")));
        void Daftar(string judul, List<string> d)
        {
            sb.AppendLine($"{judul}: {d.Count}");
            foreach (var x in d.Take(40)) sb.AppendLine("    " + x);
            if (d.Count > 40) sb.AppendLine($"    … dan {d.Count - 40} lagi");
        }
        Daftar("Kontras di bawah standar WCAG AAA (7:1, teks besar 4,5:1)", KontrasRendah);
        Daftar("Teks/isian terpotong", Terpotong);
        Daftar("Tombol/menu tanpa aksi", TanpaAksi);
        Daftar("Galat saat tombol diklik", GalatKlik);
        return sb.ToString();
    }

    // ------------------------------------------------------------ bantu

    static bool Redup(DependencyObject d)
    {
        for (var p = d; p != null; p = VisualTreeHelper.GetParent(p))
            if (p is UIElement u && u.Opacity < 0.95) return true;
        return false;
    }

    static Color Latar(DependencyObject d)
    {
        for (var p = VisualTreeHelper.GetParent(d); p != null; p = VisualTreeHelper.GetParent(p))
        {
            Brush? b = p switch { Border x => x.Background, Panel x => x.Background, Control x => x.Background, _ => null };
            if (b is SolidColorBrush s && s.Color.A > 200) return s.Color;
            if (b is GradientBrush g && g.GradientStops.Count > 0) return g.GradientStops[^1].Color;
        }
        return Colors.White;
    }

    static Color Campur(Color fg, Color bg)
    {
        double a = fg.A / 255.0;
        return Color.FromRgb((byte)(fg.R * a + bg.R * (1 - a)), (byte)(fg.G * a + bg.G * (1 - a)), (byte)(fg.B * a + bg.B * (1 - a)));
    }

    static double Lum(Color c)
    {
        static double f(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * f(c.R) + 0.7152 * f(c.G) + 0.0722 * f(c.B);
    }

    public static double Rasio(Color a, Color b)
    {
        double l1 = Lum(a), l2 = Lum(b);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    static readonly PropertyInfo? Store = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.NonPublic | BindingFlags.Instance);

    static bool PunyaKlik(UIElement e)
    {
        var s = Store?.GetValue(e);
        if (s == null) return false;
        var m = s.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        var ev = e is MenuItem ? MenuItem.ClickEvent : ButtonBase.ClickEvent;
        return m?.Invoke(s, new object[] { ev }) is Array a && a.Length > 0;
    }
}
