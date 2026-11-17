using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace RancangAPBDes.Core;

public static class ImporExcel
{
    static readonly Regex PolaKode = new(@"^\d{1,4}(\.\d{1,4})+\.?$", RegexOptions.Compiled);

    /// <summary>Membaca pasangan (kode, nama) dari semua lembar: sel berisi kode lalu sel teks di kanannya.</summary>
    public static List<(string Kode, string Nama)> BacaKodeNama(string path)
    {
        var hasil = new List<(string, string)>();
        var sudah = new HashSet<string>();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var wb = new XLWorkbook(stream);
        foreach (var ws in wb.Worksheets)
        {
            foreach (var row in ws.RowsUsed())
            {
                var sel = row.CellsUsed().ToList();
                for (int i = 0; i < sel.Count; i++)
                {
                    var teks = sel[i].GetFormattedString().Trim();
                    if (!PolaKode.IsMatch(teks)) continue;
                    var nama = sel.Skip(i + 1)
                        .Select(c => c.GetFormattedString().Trim())
                        .FirstOrDefault(s => s.Length > 1 && !PolaKode.IsMatch(s) && !decimal.TryParse(s, out _));
                    if (nama == null) break;
                    var kode = Batas.Kode(teks);
                    if (sudah.Add(kode)) hasil.Add((kode, nama));
                    break;
                }
            }
        }
        return hasil;
    }
}
