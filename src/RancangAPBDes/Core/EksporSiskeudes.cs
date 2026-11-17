using System.IO;
using ClosedXML.Excel;

namespace RancangAPBDes.Core;

/// <summary>
/// Ekspor Excel dengan susunan seperti cetakan/ekspor Siskeudes: Ringkasan (rekap per kegiatan), satu lembar RAB per
/// kegiatan, Pendapatan, dan Pembiayaan. Bila skenario pembanding diberikan, kolom menjadi SEMULA / MENJADI / BERTAMBAH.
/// File ini bisa dibaca kembali oleh menu Impor dari Excel Siskeudes.
/// </summary>
public static class EksporSiskeudes
{
    const string FmtRp = "#,##0";

    public static void Simpan(string path, DataDesa desa, SkenarioInfo info, DataSkenario d, SkenarioInfo? infoSemula = null, DataSkenario? semula = null)
    {
        bool ubah = semula != null;
        using var wb = new XLWorkbook();
        wb.Style.Font.FontName = "Calibri";
        wb.Style.Font.FontSize = 11;
        var nama = (string.IsNullOrWhiteSpace(desa.NamaDesa) ? "DESA" : desa.NamaDesa).ToUpperInvariant();
        var awalan = ubah ? "PERUBAHAN " : "";

        // ---------------- Ringkasan
        var rk = wb.AddWorksheet("Ringkasan");
        rk.Cell(1, 1).Value = $"PEMERINTAH DESA {nama}" + (string.IsNullOrWhiteSpace(desa.Kecamatan) ? "" : $" - KECAMATAN {desa.Kecamatan.ToUpperInvariant()}");
        rk.Cell(2, 1).Value = $"REKAPITULASI {awalan}RENCANA ANGGARAN BIAYA (RAB) TAHUN ANGGARAN {info.Tahun}";
        rk.Cell(3, 1).Value = ubah ? $"Semula: {infoSemula?.Nama} · Menjadi: {info.Nama} (disusun dengan Rancang APBDes)" : $"Skenario: {info.Nama} (disusun dengan Rancang APBDes)";
        var judul = ubah
            ? new[] { "NO", "KODE", "BIDANG / SUB BIDANG / NAMA KEGIATAN", "WAKTU", "PKA", "ANGGARAN SEMULA (Rp)", "ANGGARAN MENJADI (Rp)", "BERTAMBAH / (BERKURANG) (Rp)" }
            : new[] { "NO", "KODE", "BIDANG / SUB BIDANG / NAMA KEGIATAN", "WAKTU", "PKA", "ANGGARAN (Rp)" };
        Kepala(rk, 5, judul);
        var kegSemua = d.Kegiatan.Select(k => k.Kode)
            .Union(semula?.Kegiatan.Select(k => k.Kode) ?? Enumerable.Empty<string>())
            .Distinct().OrderBy(k => k, KodeComparer.Instance).ToList();
        int r = 6, no = 1;
        foreach (var kode in kegSemua)
        {
            var kb = d.Kegiatan.FirstOrDefault(k => k.Kode == kode);
            var ka = semula?.Kegiatan.FirstOrDefault(k => k.Kode == kode);
            var k0 = kb ?? ka!;
            rk.Cell(r, 1).Value = no++;
            rk.Cell(r, 2).Value = kode + ".";
            rk.Cell(r, 3).Value = k0.Nama;
            rk.Cell(r, 4).Value = k0.Waktu;
            rk.Cell(r, 5).Value = k0.Pelaksana;
            if (ubah)
            {
                rk.Cell(r, 6).Value = ka?.Total ?? 0;
                rk.Cell(r, 7).Value = kb?.Total ?? 0;
                rk.Cell(r, 8).FormulaA1 = $"G{r}-F{r}";
            }
            else rk.Cell(r, 6).Value = kb?.Total ?? 0;
            r++;
        }
        rk.Cell(r, 3).Value = "TOTAL ANGGARAN APBDES";
        for (int c = 6; c <= (ubah ? 8 : 6); c++)
        {
            var huruf = XLHelper.GetColumnLetterFromNumber(c);
            rk.Cell(r, c).FormulaA1 = r > 6 ? $"SUM({huruf}6:{huruf}{r - 1})" : "0";
        }
        rk.Range(r, 1, r, judul.Length).Style.Font.Bold = true;
        rk.Range(6, 6, r, judul.Length).Style.NumberFormat.Format = FmtRp;
        Garis(rk, 5, r, judul.Length);
        Lebar(rk, 6, 11, 60, 12, 22, 18, 18, 18);
        rk.Column(3).Style.Alignment.WrapText = true;
        TandaTangan(rk, r + 2, desa, 3);

        // ---------------- RAB per kegiatan
        var namaLembar = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ringkasan", "Pendapatan", "Pembiayaan" };
        foreach (var kode in kegSemua)
        {
            var kb = d.Kegiatan.FirstOrDefault(k => k.Kode == kode);
            var ka = semula?.Kegiatan.FirstOrDefault(k => k.Kode == kode);
            var k0 = kb ?? ka!;
            var ws = wb.AddWorksheet(NamaLembar($"{kode} {k0.Nama}", namaLembar));
            var bid = k0.KodeSubBidang.Split('.')[0];
            ws.Cell(1, 1).Value = $"PEMERINTAH DESA {nama}";
            ws.Cell(2, 1).Value = $"{awalan}RENCANA ANGGARAN BIAYA (RAB) TAHUN ANGGARAN {info.Tahun}";
            void Info(int baris, string label, string isi) { ws.Cell(baris, 1).Value = label; ws.Cell(baris, 2).Value = ":"; ws.Cell(baris, 3).Value = isi; }
            Info(3, "Bidang", $"{bid}. {Lookup.NamaBidang(bid).ToUpperInvariant()}");
            Info(4, "Sub Bidang", $"{k0.KodeSubBidang}. {Lookup.NamaSubBidang(k0.KodeSubBidang)}");
            Info(5, "Kegiatan", $"{kode}. {k0.Nama}");
            Info(6, "Waktu Pelaksanaan", k0.Waktu);
            Info(7, "Output / Keluaran", k0.Keluaran);
            var kepala = ubah
                ? new[] { "KODE REKENING", "URAIAN KEGIATAN / AKUN / RINCIAN", "SUMBER DANA", "VOLUME SEMULA", "HARGA SATUAN SEMULA (Rp)", "JUMLAH SEMULA (Rp)", "VOLUME MENJADI", "HARGA SATUAN MENJADI (Rp)", "JUMLAH MENJADI (Rp)", "BERTAMBAH / (BERKURANG) (Rp)" }
                : new[] { "KODE REKENING", "URAIAN KEGIATAN / AKUN / RINCIAN", "SUMBER DANA", "VOLUME", "HARGA SATUAN (Rp)", "JUMLAH (Rp)" };
            Kepala(ws, 8, kepala);
            int b = 9;
            void Jumlah(int baris, decimal a, decimal m)
            {
                if (ubah) { ws.Cell(baris, 6).Value = a; ws.Cell(baris, 9).Value = m; ws.Cell(baris, 10).Value = m - a; }
                else ws.Cell(baris, 6).Value = m;
            }
            ws.Cell(b, 1).Value = "5."; ws.Cell(b, 2).Value = "BELANJA"; Jumlah(b, ka?.Total ?? 0, kb?.Total ?? 0);
            ws.Range(b, 1, b, kepala.Length).Style.Font.Bold = true;
            b++;
            int nPaket = Math.Max(ka?.Paket.Count ?? 0, kb?.Paket.Count ?? 0);
            for (int i = 0; i < nPaket; i++)
            {
                var pa = ka != null && i < ka.Paket.Count ? ka.Paket[i] : null;
                var pb = kb != null && i < kb.Paket.Count ? kb.Paket[i] : null;
                ws.Cell(b, 2).Value = $"{i + 1:00}. {(pb ?? pa)!.Nama}";
                Jumlah(b, pa?.Total ?? 0, pb?.Total ?? 0);
                ws.Range(b, 1, b, kepala.Length).Style.Font.Bold = true;
                b++;
                var ra = pa?.Rincian.ToList() ?? new();
                var rb = pb?.Rincian.ToList() ?? new();
                var objek = ra.Concat(rb).Select(x => string.Join('.', x.KodeRekening.Split('.').Take(3))).Distinct().OrderBy(x => x, KodeComparer.Instance);
                foreach (var obj in objek)
                {
                    ws.Cell(b, 1).Value = obj + "."; ws.Cell(b, 2).Value = Lookup.NamaRekening(obj);
                    Jumlah(b, ra.Where(x => Batas.Cocok(x.KodeRekening, obj)).Sum(x => x.Jumlah), rb.Where(x => Batas.Cocok(x.KodeRekening, obj)).Sum(x => x.Jumlah));
                    b++;
                    foreach (var rek in ra.Concat(rb).Select(x => x.KodeRekening).Where(x => Batas.Cocok(x, obj)).Distinct().OrderBy(x => x, KodeComparer.Instance))
                    {
                        ws.Cell(b, 1).Value = rek + "."; ws.Cell(b, 2).Value = Lookup.NamaRekening(rek);
                        var la = ra.Where(x => x.KodeRekening == rek).ToList();
                        var lb = rb.Where(x => x.KodeRekening == rek).ToList();
                        Jumlah(b, la.Sum(x => x.Jumlah), lb.Sum(x => x.Jumlah));
                        b++;
                        int n = 1;
                        foreach (var (x, y) in Pasangkan(la, lb))
                        {
                            var z = (y ?? x)!;
                            ws.Cell(b, 2).Value = $"{n++:00}. {z.Uraian}";
                            ws.Cell(b, 3).Value = string.IsNullOrWhiteSpace(z.SumberDana) ? (pb ?? pa)!.SumberDana : z.SumberDana;
                            if (ubah)
                            {
                                Rinci(ws, b, 4, x, z.Satuan);
                                Rinci(ws, b, 7, y, z.Satuan);
                                ws.Cell(b, 10).FormulaA1 = $"I{b}-F{b}";
                            }
                            else Rinci(ws, b, 4, y, z.Satuan);
                            b++;
                        }
                    }
                }
            }
            ws.Cell(b, 1).Value = "JUMLAH TOTAL (Rp)";
            Jumlah(b, ka?.Total ?? 0, kb?.Total ?? 0);
            ws.Range(b, 1, b, kepala.Length).Style.Font.Bold = true;
            ws.Range(9, 4, b, kepala.Length).Style.NumberFormat.Format = FmtRp;
            ws.Range(9, 4, b, 4).Style.NumberFormat.Format = "@";
            if (ubah) ws.Range(9, 7, b, 7).Style.NumberFormat.Format = "@";
            Garis(ws, 8, b, kepala.Length);
            Lebar(ws, 14, 52, 10, 13, 16, 16, 13, 16, 16, 16);
            ws.Column(2).Style.Alignment.WrapText = true;
            TandaTangan(ws, b + 2, desa, 2, k0.Pelaksana);
        }

        // ---------------- Pendapatan & Pembiayaan
        LembarRekening(wb.AddWorksheet("Pendapatan"), nama, $"{awalan}RENCANA ANGGARAN PENDAPATAN — TAHUN ANGGARAN {info.Tahun}", ubah,
            new (string, string, IEnumerable<RincianAnggaran>?, IEnumerable<RincianAnggaran>)[] { ("4", "PENDAPATAN", semula?.Pendapatan, d.Pendapatan) }, desa);
        LembarRekening(wb.AddWorksheet("Pembiayaan"), nama, $"{awalan}RENCANA ANGGARAN PEMBIAYAAN — TAHUN ANGGARAN {info.Tahun}", ubah,
            new (string, string, IEnumerable<RincianAnggaran>?, IEnumerable<RincianAnggaran>)[] { ("6.1", "Penerimaan Pembiayaan", semula?.PenerimaanPembiayaan, d.PenerimaanPembiayaan), ("6.2", "Pengeluaran Pembiayaan", semula?.PengeluaranPembiayaan, d.PengeluaranPembiayaan) }, desa);

        var tmp = path + ".tmp";
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None)) wb.SaveAs(fs);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>Pasangkan rincian semula & menjadi berdasarkan uraian (urutan dipertahankan).</summary>
    static IEnumerable<(RincianAnggaran? a, RincianAnggaran? b)> Pasangkan(List<RincianAnggaran> a, List<RincianAnggaran> b)
    {
        var sisaA = new List<RincianAnggaran>(a);
        foreach (var y in b)
        {
            var x = sisaA.FirstOrDefault(r => string.Equals(r.Uraian.Trim(), y.Uraian.Trim(), StringComparison.OrdinalIgnoreCase));
            if (x != null) sisaA.Remove(x);
            yield return (x, y);
        }
        foreach (var x in sisaA) yield return (x, null);
    }

    static void Rinci(IXLWorksheet ws, int baris, int kolom, RincianAnggaran? r, string satuan)
    {
        ws.Cell(baris, kolom).Value = $"{(r?.Volume ?? 0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} {(r?.Satuan ?? satuan)}".Trim();
        ws.Cell(baris, kolom + 1).Value = r?.HargaSatuan ?? 0;
        ws.Cell(baris, kolom + 2).Value = r?.Jumlah ?? 0;
    }

    static void LembarRekening(IXLWorksheet ws, string nama, string judul, bool ubah,
        (string awalan, string uraian, IEnumerable<RincianAnggaran>? semula, IEnumerable<RincianAnggaran> menjadi)[] bagian, DataDesa desa)
    {
        ws.Cell(1, 1).Value = $"PEMERINTAH DESA {nama}";
        ws.Cell(2, 1).Value = judul;
        int kolom = ubah ? 10 : 6;
        if (ubah)
        {
            ws.Cell(4, 1).Value = "KODE"; ws.Cell(4, 2).Value = "URAIAN"; ws.Cell(4, 3).Value = "SUMBER DANA";
            ws.Cell(4, 4).Value = "SEMULA"; ws.Cell(4, 7).Value = "MENJADI"; ws.Cell(4, 10).Value = "BERTAMBAH / (BERKURANG) (Rp)";
            string[] sub = { "VOLUME", "HARGA SATUAN (Rp)", "JUMLAH (Rp)" };
            for (int i = 0; i < 3; i++) { ws.Cell(5, 4 + i).Value = sub[i]; ws.Cell(5, 7 + i).Value = sub[i]; }
            ws.Range(4, 4, 4, 6).Merge(); ws.Range(4, 7, 4, 9).Merge();
            var j = ws.Range(4, 1, 5, kolom);
            j.Style.Font.Bold = true; j.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6DE"); j.Style.Alignment.WrapText = true;
            j.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        else Kepala(ws, 4, new[] { "KODE", "URAIAN", "SUMBER DANA", "VOLUME", "HARGA SATUAN (Rp)", "JUMLAH (Rp)" });
        int b = ubah ? 6 : 5, awal = b;
        void Jumlah(int baris, decimal a, decimal m)
        {
            if (ubah) { ws.Cell(baris, 6).Value = a; ws.Cell(baris, 9).Value = m; ws.Cell(baris, 10).Value = m - a; }
            else ws.Cell(baris, 6).Value = m;
        }
        foreach (var (awalan, uraian, semula, menjadi) in bagian)
        {
            var la = semula?.ToList() ?? new();
            var lb = menjadi.ToList();
            ws.Cell(b, 1).Value = awalan + "."; ws.Cell(b, 2).Value = uraian;
            Jumlah(b, la.Sum(x => x.Jumlah), lb.Sum(x => x.Jumlah));
            ws.Range(b, 1, b, kolom).Style.Font.Bold = true;
            b++;
            var semua = la.Concat(lb).Select(x => x.KodeRekening).Distinct().ToList();
            var tingkat = semua.SelectMany(k =>
            {
                var seg = k.Split('.');
                return Enumerable.Range(awalan.Split('.').Length + 1, Math.Max(0, seg.Length - awalan.Split('.').Length)).Select(n => string.Join('.', seg.Take(n)));
            }).Distinct().OrderBy(k => k, KodeComparer.Instance);
            foreach (var k in tingkat)
            {
                ws.Cell(b, 1).Value = k + "."; ws.Cell(b, 2).Value = Lookup.NamaRekening(k);
                Jumlah(b, la.Where(x => Batas.Cocok(x.KodeRekening, k)).Sum(x => x.Jumlah), lb.Where(x => Batas.Cocok(x.KodeRekening, k)).Sum(x => x.Jumlah));
                b++;
                if (k.Split('.').Length != 4) continue;
                int n = 1;
                foreach (var (x, y) in Pasangkan(la.Where(r => r.KodeRekening == k).ToList(), lb.Where(r => r.KodeRekening == k).ToList()))
                {
                    var z = (y ?? x)!;
                    ws.Cell(b, 2).Value = $"{n++:00}. {z.Uraian}";
                    ws.Cell(b, 3).Value = z.SumberDana;
                    if (ubah) { Rinci(ws, b, 4, x, z.Satuan); Rinci(ws, b, 7, y, z.Satuan); ws.Cell(b, 10).FormulaA1 = $"I{b}-F{b}"; }
                    else Rinci(ws, b, 4, y, z.Satuan);
                    b++;
                }
            }
        }
        ws.Cell(b, 1).Value = "JUMLAH";
        ws.Range(b, 1, b, kolom).Style.Font.Bold = true;
        ws.Range(awal, 5, b, kolom).Style.NumberFormat.Format = FmtRp;
        if (ubah) ws.Range(awal, 6, b, 6).Style.NumberFormat.Format = FmtRp;
        Garis(ws, ubah ? 4 : 4, b, kolom);
        Lebar(ws, 14, 46, 10, 13, 16, 16, 13, 16, 16, 16);
        ws.Column(2).Style.Alignment.WrapText = true;
        TandaTangan(ws, b + 2, desa, 2);
    }

    static string NamaLembar(string teks, HashSet<string> dipakai)
    {
        foreach (var c in new[] { ':', '\\', '/', '?', '*', '[', ']' }) teks = teks.Replace(c, ' ');
        teks = teks.Trim();
        if (teks.Length > 31) teks = teks[..31].Trim();
        var dasar = teks;
        int i = 2;
        while (!dipakai.Add(teks)) { var akhiran = $" ({i++})"; teks = (dasar.Length + akhiran.Length > 31 ? dasar[..(31 - akhiran.Length)] : dasar) + akhiran; }
        return teks;
    }

    static void Kepala(IXLWorksheet ws, int baris, string[] judul)
    {
        for (int i = 0; i < judul.Length; i++) ws.Cell(baris, i + 1).Value = judul[i];
        var r = ws.Range(baris, 1, baris, judul.Length);
        r.Style.Font.Bold = true;
        r.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6DE");
        r.Style.Alignment.WrapText = true;
        r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    static void Garis(IXLWorksheet ws, int dari, int sampai, int kolom)
    {
        var r = ws.Range(dari, 1, sampai, kolom);
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }

    static void Lebar(IXLWorksheet ws, params double[] lebar)
    {
        for (int i = 0; i < lebar.Length; i++) ws.Column(i + 1).Width = lebar[i];
    }

    static void TandaTangan(IXLWorksheet ws, int baris, DataDesa desa, int kolom, string? pelaksana = null)
    {
        var nama = (string.IsNullOrWhiteSpace(desa.NamaDesa) ? "" : desa.NamaDesa).ToUpperInvariant();
        ws.Cell(baris, 1).Value = "Mengesahkan,";
        ws.Cell(baris + 1, 1).Value = $"KEPALA DESA {nama}".Trim();
        ws.Cell(baris + 5, 1).Value = string.IsNullOrWhiteSpace(desa.KepalaDesa) ? "(............................)" : desa.KepalaDesa.ToUpperInvariant();
        ws.Cell(baris + 1, kolom + 1).Value = "SEKRETARIS DESA";
        ws.Cell(baris + 5, kolom + 1).Value = string.IsNullOrWhiteSpace(desa.Sekdes) ? "(............................)" : desa.Sekdes.ToUpperInvariant();
        if (pelaksana != null)
        {
            ws.Cell(baris + 1, kolom + 4).Value = "Pelaksana Kegiatan Anggaran (PKA),";
            ws.Cell(baris + 5, kolom + 4).Value = string.IsNullOrWhiteSpace(pelaksana) ? "(............................)" : pelaksana.ToUpperInvariant();
        }
    }
}
