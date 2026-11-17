using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public static class UiBantu
{
    public static IEnumerable<T> Turunan<T>(DependencyObject akar) where T : DependencyObject
    {
        if (akar == null) yield break;
        int n = VisualTreeHelper.GetChildrenCount(akar);
        for (int i = 0; i < n; i++)
        {
            var c = VisualTreeHelper.GetChild(akar, i);
            if (c is T t) yield return t;
            foreach (var x in Turunan<T>(c)) yield return x;
        }
    }

    public static T? Leluhur<T>(DependencyObject? d) where T : DependencyObject
    {
        while (d != null)
        {
            if (d is T t) return t;
            d = d is Visual || d is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
        }
        return null;
    }

    /// <summary>Akhiri edit di tabel tanpa membuang data.</summary>
    public static void AkhiriEdit(DataGrid g)
    {
        try
        {
            g.CommitEdit(DataGridEditingUnit.Cell, true);
            g.CommitEdit(DataGridEditingUnit.Row, true);
        }
        catch
        {
            try { g.CancelEdit(DataGridEditingUnit.Row); } catch { /* abaikan */ }
        }
    }

    /// <summary>Agar roda mouse di atas tabel kecil tetap menggulir halaman.</summary>
    public static void TeruskanRoda(DataGrid g)
    {
        g.PreviewMouseWheel += (s, e) =>
        {
            if (e.Handled) return;
            e.Handled = true;
            var ev = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = UIElement.MouseWheelEvent, Source = s };
            (VisualTreeHelper.GetParent((DependencyObject)s) as UIElement)?.RaiseEvent(ev);
        };
    }

    public static string NamaFileAman(string nama)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) nama = nama.Replace(c, '-');
        nama = nama.Trim();
        return nama.Length > 120 ? nama[..120] : nama;
    }

    public static void BukaDenganWindows(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Log.Error(ex, "Buka " + path); Pesan.Galat("File tidak dapat dibuka otomatis. Buka secara manual:\n" + path, ex); }
    }

    public static void BukaFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { Log.Error(ex, "Buka folder"); Pesan.Galat("Folder tidak dapat dibuka:\n" + path, ex); }
    }

    /// <summary>Mulai edit sel pada kolom tertentu di baris terpilih.</summary>
    public static void EditSel(DataGrid g, object item, int kolom)
    {
        g.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                g.SelectedItem = item;
                g.ScrollIntoView(item);
                if (kolom < g.Columns.Count)
                {
                    g.CurrentCell = new DataGridCellInfo(item, g.Columns[kolom]);
                    g.Focus();
                    g.BeginEdit();
                }
            }
            catch (Exception ex) { Log.Error(ex, "EditSel"); }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }
}
