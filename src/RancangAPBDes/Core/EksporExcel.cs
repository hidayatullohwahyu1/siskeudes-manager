using System.IO;
using ClosedXML.Excel;

namespace RancangAPBDes.Core;

public static class EksporExcel
{
    const string FmtRp = "#,##0";
    const string FmtAngka = "#,##0.##";

    public static void Simpan(string path, DataDesa desa, SkenarioInfo info, DataSkenario d, HasilHitung h)
    {
        using var wb = new XLWorkbook();
        wb.Style.Font.FontName = "Calibri";
        wb.Style.Font.FontSize = 11;

        Ringkasan(wb.AddWorksheet("Ringkasan"), desa, info, d, h);
        Rincian(wb.AddWorksheet("Pendapatan"), desa, info, "PENDAPATAN", d.Pendapatan);
        Rincian(wb.AddWorksheet("Pembiayaan"), desa, info, "PENERIMAAN PEMBIAYAAN", d.PenerimaanPembiayaan, "PENGELUARAN PEMBIAYAAN", d.PengeluaranPembiayaan);
        Rab(wb.AddWorksheet("Belanja (RAB)"), desa, info, d);
        Kegiatan(wb.AddWorksheet("Daftar Kegiatan"), desa, info, d);
        Peringatan(wb.AddWorksheet("Peringatan"), desa, info, h);

        // Simpan ke file sementara dulu agar file lama tidak rusak bila gagal
        var tmp = path + ".tmp";
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            wb.SaveAs(fs);
        File.Move(tmp, path, overwrite: true);
    }

    static int Kop(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, string judul, int kolom)
    {
        ws.Cell(1, 1).Value = judul.ToUpperInvariant();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"{desa.NamaDesa} – Tahun Anggaran {info.Tahun} – Skenario: {info.Nama} ({(info.IsFinal ? "FINAL" : "DRAFT")})";
        ws.Cell(3, 1).Value = $"Dibuat {DateTime.Now:dd MMMM yyyy HH:mm} dengan Rancang APBDes. Bahan musyawarah, bukan dokumen resmi.";
        ws.Cell(3, 1).Style.Font.Italic = true;
        for (int r = 1; r <= 3; r++) ws.Range(r, 1, r, kolom).Merge();
        return 5;
    }

    static void Kepala(IXLWorksheet ws, int baris, params string[] judul)
    {
        for (int i = 0; i < judul.Length; i++) ws.Cell(baris, i + 1).Value = judul[i];
        var r = ws.Range(baris, 1, baris, judul.Length);
        r.Style.Font.Bold = true;
        r.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3E9F1");
        r.Style.Alignment.WrapText = true;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    static void Garis(IXLWorksheet ws, int dari, int sampai, int kolom)
    {
        if (sampai < dari) return;
        var r = ws.Range(dari, 1, sampai, kolom);
        r.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        r.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        r.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }

    static void Tebal(IXLWorksheet ws, int baris, int kolom, string? warna = "#F2F5F9")
    {
        var r = ws.Range(baris, 1, baris, kolom);
        r.Style.Font.Bold = true;
        if (warna != null) r.Style.Fill.BackgroundColor = XLColor.FromHtml(warna);
    }

    static void Lebar(IXLWorksheet ws, params double[] lebar)
    {
        for (int i = 0; i < lebar.Length; i++) ws.Column(i + 1).Width = lebar[i];
    }

    static string KodeRek(string k) => string.IsNullOrWhiteSpace(k) ? "" : k + ".";

    // ------------------------------------------------------------

    static void Ringkasan(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, DataSkenario d, HasilHitung h)
    {
        int r = Kop(ws, desa, info, "Ringkasan APBDes", 6);
        Kepala(ws, r, "Kode", "Uraian", "Jumlah (Rp)");
        int awal = ++r;
        void Baris(string kode, string uraian, decimal nilai, bool tebal = false)
        {
            ws.Cell(r, 1).Value = kode;
            ws.Cell(r, 2).Value = uraian;
            ws.Cell(r, 3).Value = nilai;
            ws.Cell(r, 3).Style.NumberFormat.Format = FmtRp;
            if (tebal) Tebal(ws, r, 3);
            r++;
        }
        Baris("4", "PENDAPATAN", 0, true); ws.Cell(r - 1, 3).Clear(XLClearOptions.Contents);
        foreach (var g in d.Pendapatan.GroupBy(x => x.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            Baris(KodeRek(g.Key), Lookup.NamaRekening(g.Key), g.Sum(x => x.Jumlah));
        Baris("", "Jumlah Pendapatan", h.TotalPendapatan, true);
        Baris("5", "BELANJA", 0, true); ws.Cell(r - 1, 3).Clear(XLClearOptions.Contents);
        foreach (var b in h.PerBidang.Where(b => b.Jumlah != 0 || b.JumlahKegiatan > 0))
            Baris(b.Kode, b.Nama, b.Jumlah);
        Baris("", "Jumlah Belanja", h.TotalBelanja, true);
        Baris("", h.Surplus >= 0 ? "SURPLUS" : "DEFISIT", Math.Abs(h.Surplus), true);
        Baris("6", "PEMBIAYAAN", 0, true); ws.Cell(r - 1, 3).Clear(XLClearOptions.Contents);
        Baris("6.1", "Penerimaan Pembiayaan", h.PenerimaanPembiayaan);
        Baris("6.2", "Pengeluaran Pembiayaan", h.PengeluaranPembiayaan);
        Baris("", "Pembiayaan Netto", h.PembiayaanNetto, true);
        Baris("", h.SisaAnggaran < 0 ? "KEKURANGAN DANA" : "Sisa Lebih Pembiayaan Anggaran Tahun Berjalan", Math.Abs(h.SisaAnggaran), true);
        if (h.SisaAnggaran < 0) ws.Range(r - 1, 1, r - 1, 3).Style.Font.FontColor = XLColor.FromHtml("#B42318");
        Garis(ws, awal, r - 1, 3);

        r += 2;
        ws.Cell(r, 1).Value = "DANA PER SUMBER DANA";
        ws.Cell(r, 1).Style.Font.Bold = true;
        r++;
        Kepala(ws, r, "Kode", "Sumber Dana", "Pagu (Rp)", "Dipakai (Rp)", "Sisa (Rp)", "% Terpakai");
        awal = ++r;
        foreach (var b in h.PerSumberDana)
        {
            ws.Cell(r, 1).Value = b.Kode;
            ws.Cell(r, 2).Value = b.Nama;
            ws.Cell(r, 3).Value = b.Pagu;
            ws.Cell(r, 4).Value = b.Dipakai;
            ws.Cell(r, 5).Value = b.Sisa;
            ws.Cell(r, 6).Value = b.Persen / 100m;
            ws.Range(r, 3, r, 5).Style.NumberFormat.Format = FmtRp;
            ws.Cell(r, 6).Style.NumberFormat.Format = "0.0%";
            if (b.Kurang) ws.Range(r, 1, r, 6).Style.Font.FontColor = XLColor.FromHtml("#B42318");
            r++;
        }
        Garis(ws, awal, r - 1, 6);

        if (h.Aturan.Count > 0)
        {
            r += 2;
            ws.Cell(r, 1).Value = "CEK ATURAN";
            ws.Cell(r, 1).Style.Font.Bold = true;
            r++;
            Kepala(ws, r, "Status", "Aturan", "Nilai (Rp)", "Dasar", "Hasil", "Batas");
            awal = ++r;
            foreach (var a in h.Aturan)
            {
                ws.Cell(r, 1).Value = a.Status;
                ws.Cell(r, 2).Value = a.Nama;
                ws.Cell(r, 3).Value = a.Nilai;
                ws.Cell(r, 3).Style.NumberFormat.Format = FmtRp;
                ws.Cell(r, 4).Value = a.NamaDasar;
                ws.Cell(r, 5).Value = a.Persen / 100m;
                ws.Cell(r, 5).Style.NumberFormat.Format = "0.00%";
                ws.Cell(r, 6).Value = a.Batas;
                if (!a.Terpenuhi) ws.Range(r, 1, r, 6).Style.Font.FontColor = XLColor.FromHtml("#B42318");
                r++;
            }
            Garis(ws, awal, r - 1, 6);
        }

        if (!string.IsNullOrWhiteSpace(d.Catatan))
        {
            r += 2;
            ws.Cell(r, 1).Value = "CATATAN MUSYAWARAH";
            ws.Cell(r, 1).Style.Font.Bold = true;
            r++;
            ws.Cell(r, 1).Value = d.Catatan;
            ws.Range(r, 1, r, 6).Merge();
            ws.Range(r, 1, r, 6).Style.Alignment.WrapText = true;
            ws.Range(r, 1, r, 6).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            ws.Row(r).Height = Math.Min(400, 15 * (1 + d.Catatan.Count(c => c == '\n') + d.Catatan.Length / 110));
        }

        Lebar(ws, 12, 60, 18, 18, 18, 14);
        ws.Column(2).Style.Alignment.WrapText = true;
        AturHalaman(ws, false);
    }

    static void Rincian(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, string judul1, IEnumerable<RincianAnggaran> items1,
        string? judul2 = null, IEnumerable<RincianAnggaran>? items2 = null)
    {
        int r = Kop(ws, desa, info, judul2 == null ? judul1 : "Pembiayaan", 9);
        void Bagian(string judul, IEnumerable<RincianAnggaran> items)
        {
            ws.Cell(r, 1).Value = judul;
            ws.Cell(r, 1).Style.Font.Bold = true;
            r++;
            Kepala(ws, r, "Kode Rekening", "Nama Rekening", "Uraian", "Volume", "Satuan", "Harga Satuan (Rp)", "Jumlah (Rp)", "Sumber Dana", "Keterangan");
            int awal = ++r;
            foreach (var g in items.GroupBy(x => x.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            {
                foreach (var x in g)
                {
                    ws.Cell(r, 1).Value = KodeRek(x.KodeRekening);
                    ws.Cell(r, 2).Value = x.NamaRekening;
                    ws.Cell(r, 3).Value = x.Uraian;
                    ws.Cell(r, 4).Value = x.Volume;
                    ws.Cell(r, 5).Value = x.Satuan;
                    ws.Cell(r, 6).Value = x.HargaSatuan;
                    ws.Cell(r, 7).FormulaA1 = $"D{r}*F{r}";
                    ws.Cell(r, 8).Value = x.SumberDana;
                    ws.Cell(r, 9).Value = x.Keterangan;
                    r++;
                }
            }
            ws.Cell(r, 3).Value = "JUMLAH";
            ws.Cell(r, 7).FormulaA1 = r > awal ? $"SUM(G{awal}:G{r - 1})" : "0";
            Tebal(ws, r, 9);
            ws.Range(awal, 4, r, 4).Style.NumberFormat.Format = FmtAngka;
            ws.Range(awal, 6, r, 7).Style.NumberFormat.Format = FmtRp;
            Garis(ws, awal, r, 9);
            r += 2;
        }
        Bagian(judul1, items1);
        if (judul2 != null && items2 != null) Bagian(judul2, items2);
        Lebar(ws, 14, 32, 40, 10, 14, 18, 18, 12, 26);
        ws.Columns(2, 3).Style.Alignment.WrapText = true;
        ws.Column(9).Style.Alignment.WrapText = true;
        AturHalaman(ws, true);
    }

    static void Rab(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, DataSkenario d)
    {
        int r = Kop(ws, desa, info, "Rencana Anggaran Biaya (RAB) per Kegiatan", 10);
        Kepala(ws, r, "Kode", "Kegiatan / Paket / Rekening / Uraian", "Volume", "Satuan", "Harga Satuan (Rp)", "Jumlah (Rp)", "Sumber Dana", "Lokasi", "Pelaksana", "Keterangan");
        ws.SheetView.FreezeRows(r);
        int awal = ++r;
        var totalBaris = new List<int>();
        foreach (var bid in d.Kegiatan.GroupBy(k => k.KodeSubBidang.Split('.')[0]).OrderBy(g => g.Key, KodeComparer.Instance))
        {
            ws.Cell(r, 1).Value = bid.Key;
            ws.Cell(r, 2).Value = Lookup.NamaBidang(bid.Key).ToUpperInvariant();
            ws.Cell(r, 6).Value = bid.Sum(k => k.Total);
            Tebal(ws, r, 10, "#DCE6F2");
            r++;
            foreach (var keg in bid.OrderBy(k => k.Kode, KodeComparer.Instance))
            {
                ws.Cell(r, 1).Value = Laporan.KodeLengkap(desa, keg.Kode);
                ws.Cell(r, 2).Value = keg.Nama;
                ws.Cell(r, 6).Value = keg.Total;
                ws.Cell(r, 8).Value = keg.Lokasi;
                ws.Cell(r, 9).Value = keg.Pelaksana;
                ws.Cell(r, 10).Value = keg.Catatan;
                Tebal(ws, r, 10, "#EEF2F7");
                totalBaris.Add(r);
                r++;
                foreach (var p in keg.Paket)
                {
                    ws.Cell(r, 2).Value = "Paket: " + p.Nama;
                    ws.Cell(r, 6).Value = p.Total;
                    ws.Cell(r, 7).Value = p.SumberDana;
                    ws.Cell(r, 10).Value = $"{p.Pola}, {p.Sifat}; target {p.TargetOutput.ToString(FmtAngka, Budaya.Id)} {p.Satuan}";
                    ws.Range(r, 1, r, 10).Style.Font.Italic = true;
                    r++;
                    foreach (var g in p.Rincian.GroupBy(x => x.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
                    {
                        ws.Cell(r, 1).Value = KodeRek(g.Key);
                        ws.Cell(r, 2).Value = Lookup.NamaRekening(g.Key);
                        ws.Cell(r, 6).Value = g.Sum(x => x.Jumlah);
                        ws.Range(r, 1, r, 10).Style.Font.Bold = true;
                        r++;
                        foreach (var x in g)
                        {
                            ws.Cell(r, 2).Value = "   " + x.Uraian;
                            ws.Cell(r, 3).Value = x.Volume;
                            ws.Cell(r, 4).Value = x.Satuan;
                            ws.Cell(r, 5).Value = x.HargaSatuan;
                            ws.Cell(r, 6).FormulaA1 = $"C{r}*E{r}";
                            ws.Cell(r, 7).Value = string.IsNullOrWhiteSpace(x.SumberDana) ? p.SumberDana : x.SumberDana;
                            ws.Cell(r, 10).Value = x.Keterangan;
                            r++;
                        }
                    }
                }
            }
        }
        ws.Cell(r, 2).Value = "JUMLAH BELANJA";
        ws.Cell(r, 6).FormulaA1 = totalBaris.Count == 0 ? "0" : "SUM(" + string.Join(",", totalBaris.Select(b => $"F{b}")) + ")";
        Tebal(ws, r, 10, "#DCE6F2");
        ws.Range(awal, 3, r, 3).Style.NumberFormat.Format = FmtAngka;
        ws.Range(awal, 5, r, 6).Style.NumberFormat.Format = FmtRp;
        Garis(ws, awal, r, 10);
        Lebar(ws, 20, 52, 10, 14, 18, 18, 10, 18, 22, 30);
        ws.Column(2).Style.Alignment.WrapText = true;
        ws.Columns(8, 10).Style.Alignment.WrapText = true;
        AturHalaman(ws, true);
    }

    static void Kegiatan(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, DataSkenario d)
    {
        int r = Kop(ws, desa, info, "Daftar Kegiatan", 9);
        Kepala(ws, r, "Kode Kegiatan", "Nama Kegiatan", "Sub Bidang", "Lokasi", "Waktu", "Pelaksana", "Keluaran", "Pagu (Rp)", "Catatan Rapat");
        ws.SheetView.FreezeRows(r);
        int awal = ++r;
        foreach (var k in d.Kegiatan.OrderBy(k => k.Kode, KodeComparer.Instance))
        {
            ws.Cell(r, 1).Value = Laporan.KodeLengkap(desa, k.Kode);
            ws.Cell(r, 2).Value = k.Nama;
            ws.Cell(r, 3).Value = $"{k.KodeSubBidang} {Lookup.NamaSubBidang(k.KodeSubBidang)}";
            ws.Cell(r, 4).Value = k.Lokasi;
            ws.Cell(r, 5).Value = k.Waktu;
            ws.Cell(r, 6).Value = k.Pelaksana;
            ws.Cell(r, 7).Value = $"{k.Keluaran} ({k.VolumeKeluaran.ToString(FmtAngka, Budaya.Id)} {k.SatuanKeluaran})";
            ws.Cell(r, 8).Value = k.Total;
            ws.Cell(r, 9).Value = k.Catatan;
            r++;
        }
        ws.Cell(r, 2).Value = "JUMLAH";
        ws.Cell(r, 8).FormulaA1 = r > awal ? $"SUM(H{awal}:H{r - 1})" : "0";
        Tebal(ws, r, 9);
        ws.Range(awal, 8, r, 8).Style.NumberFormat.Format = FmtRp;
        Garis(ws, awal, r, 9);
        Lebar(ws, 20, 45, 35, 18, 12, 22, 30, 18, 35);
        ws.Columns(2, 9).Style.Alignment.WrapText = true;
        AturHalaman(ws, true);
    }

    static void Peringatan(IXLWorksheet ws, DataDesa desa, SkenarioInfo info, HasilHitung h)
    {
        int r = Kop(ws, desa, info, "Peringatan & Catatan Pemeriksaan", 2);
        Kepala(ws, r, "Tingkat", "Keterangan");
        int awal = ++r;
        foreach (var p in h.Peringatan)
        {
            ws.Cell(r, 1).Value = p.Level;
            ws.Cell(r, 2).Value = p.Teks;
            if (p.Level == "Kesalahan") ws.Range(r, 1, r, 2).Style.Font.FontColor = XLColor.FromHtml("#B42318");
            r++;
        }
        if (h.Peringatan.Count == 0) { ws.Cell(r, 2).Value = "Tidak ada masalah yang ditemukan."; r++; }
        Garis(ws, awal, r - 1, 2);
        Lebar(ws, 14, 110);
        ws.Column(2).Style.Alignment.WrapText = true;
        AturHalaman(ws, false);
    }

    static void AturHalaman(IXLWorksheet ws, bool landscape)
    {
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = landscape ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = 0.5;
        ws.PageSetup.Margins.Right = 0.5;
    }
}
