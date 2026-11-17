using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace RancangAPBDes.Core;

/// <summary>
/// Membaca APBDes dari file Excel hasil cetak/ekspor Siskeudes (RAB per kegiatan, Rencana Anggaran Pendapatan,
/// Rencana Anggaran Pembiayaan). Kolom dikenali dari judulnya (KODE, URAIAN, SUMBER DANA, VOLUME, HARGA, JUMLAH,
/// SEMULA/MENJADI), sehingga tidak bergantung pada posisi kolom.
/// </summary>
public static class ImporSiskeudes
{
    public class Hasil
    {
        /// <summary>Versi anggaran yang ditemukan: "Semula", "Menjadi", atau "Anggaran" (bila tidak ada perubahan).</summary>
        public Dictionary<string, DataSkenario> Versi { get; } = new();
        public Dictionary<string, string> Pelaksana { get; } = new();
        public Dictionary<string, string> RekeningBaru { get; } = new();
        public Dictionary<string, string> KegiatanBaru { get; } = new();
        public List<string> Catatan { get; } = new();
        public List<string> Selisih { get; } = new();
        public int LembarDibaca { get; set; }
    }

    static readonly Regex PolaRek = new(@"^([4-6](?:\.\d{1,2}){0,3})\.?$");
    static readonly Regex PolaKeg = new(@"^(\d)\.(\d)\.(\d{1,2})\.?\s*(.*)$");
    static readonly Regex PolaNomor = new(@"^(\d{1,3})\.\s*(.*)$");
    static readonly Regex PolaNoSaja = new(@"^\d{1,3}\.?$");

    record Kolom(int Vol, int Sat, int Harga, int Jumlah);

    public static Hasil Baca(IEnumerable<string> files)
    {
        using var _ = ChangeTracker.Suspend();
        var hasil = new Hasil();
        foreach (var f in files)
        {
            using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var wb = new XLWorkbook(stream);
            foreach (var ws in wb.Worksheets)
            {
                try { BacaLembar(ws, hasil, Path.GetFileName(f)); }
                catch (Exception ex)
                {
                    Log.Error(ex, "Impor lembar " + ws.Name);
                    hasil.Catatan.Add($"Lembar \"{ws.Name}\" dilewati: {ex.Message}");
                }
            }
        }
        foreach (var d in hasil.Versi.Values)
        {
            foreach (var k in d.Kegiatan)
            {
                if (hasil.Pelaksana.TryGetValue(k.Kode, out var pka)) k.Pelaksana = pka;
                foreach (var p in k.Paket)
                {
                    var sd = p.Rincian.GroupBy(r => r.SumberDana).OrderByDescending(g => g.Sum(r => r.Jumlah)).FirstOrDefault()?.Key ?? "";
                    using (ChangeTracker.Suspend()) p.SumberDana = sd;
                }
            }
            var urut = d.Kegiatan.OrderBy(k => k.Kode, KodeComparer.Instance).ToList();
            d.Kegiatan.Clear();
            foreach (var k in urut) d.Kegiatan.Add(k);
        }
        return hasil;
    }

    static string Teks(IXLCell c) => c.IsEmpty() ? "" : c.GetFormattedString().Trim();

    static void BacaLembar(IXLWorksheet ws, Hasil hasil, string namaFile)
    {
        var rng = ws.RangeUsed();
        if (rng == null) return;
        int barisAkhir = rng.LastRow().RowNumber(), kolomAkhir = Math.Min(rng.LastColumn().ColumnNumber(), 30);

        // ---- kepala lembar (Bidang/Sub Bidang/Kegiatan/Waktu/Output) dan baris judul kolom
        string? kodeKeg = null, namaKeg = null, waktu = null, keluaran = null;
        int barisJudul = -1, kKode = -1, kUraian = -1, kSd = -1, kPka = -1, kWaktu = -1, kSemula = -1, kMenjadi = -1;
        for (int r = 1; r <= Math.Min(barisAkhir, 40) && barisJudul < 0; r++)
        {
            var label = Teks(ws.Cell(r, 1)).ToUpperInvariant();
            var nilai = Enumerable.Range(2, Math.Min(4, kolomAkhir)).Select(c => Teks(ws.Cell(r, c))).FirstOrDefault(t => t != "" && t != ":") ?? "";
            if (label == "KEGIATAN" && PolaKeg.Match(nilai) is { Success: true } m)
            {
                kodeKeg = Batas.KodeKeg($"{m.Groups[1].Value}.{m.Groups[2].Value}.{m.Groups[3].Value}");
                namaKeg = m.Groups[4].Value.Replace("**)", "").Trim();
            }
            else if (label.StartsWith("WAKTU")) waktu = nilai;
            else if (label.StartsWith("OUTPUT") || label.StartsWith("KELUARAN")) keluaran = nilai;

            for (int c = 1; c <= kolomAkhir; c++)
            {
                var t = Teks(ws.Cell(r, c)).ToUpperInvariant();
                if (t.StartsWith("KODE")) kKode = c;
                else if (t.StartsWith("URAIAN") || t.StartsWith("BIDANG / SUB") || t.Contains("NAMA KEGIATAN")) kUraian = c;
                else if (t.StartsWith("SUMBER")) kSd = c;
                else if (t == "PKA" || t.StartsWith("PELAKSANA")) kPka = c;
                else if (t == "WAKTU") kWaktu = c;
            }
            if (kKode > 0 && kUraian > 0) barisJudul = r;
        }
        if (barisJudul < 0) return;
        hasil.LembarDibaca++;

        // ---- lembar rekapitulasi: ambil nama pelaksana (PKA) per kegiatan
        if (kPka > 0)
        {
            for (int r = barisJudul + 1; r <= barisAkhir; r++)
            {
                var kode = Teks(ws.Cell(r, kKode));
                var pka = Teks(ws.Cell(r, kPka));
                if (PolaKeg.Match(kode) is { Success: true } m && pka != "")
                    hasil.Pelaksana[Batas.KodeKeg($"{m.Groups[1].Value}.{m.Groups[2].Value}.{m.Groups[3].Value}")] = pka;
            }
            return;
        }

        // ---- kelompok kolom SEMULA / MENJADI (judul bisa 1–3 baris, sel judul induk bisa digabung)
        var judul = new string[kolomAkhir + 1];
        string kelompok = "";
        var kelompokKolom = new string[kolomAkhir + 1];
        int barisData = barisJudul + 1;
        for (int c = 1; c <= kolomAkhir; c++)
        {
            var gabung = string.Join(" ", Enumerable.Range(barisJudul, 3).Select(r => Teks(ws.Cell(r, c)))).ToUpperInvariant();
            judul[c] = gabung;
            var atas = Teks(ws.Cell(barisJudul, c)).ToUpperInvariant();
            if (atas.Contains("SEMULA")) kelompok = "Semula";
            else if (atas.Contains("MENJADI")) kelompok = "Menjadi";
            else if (atas.Contains("BERTAMBAH") || atas.Contains("SELISIH")) kelompok = "";
            kelompokKolom[c] = gabung.Contains("SEMULA") ? "Semula" : gabung.Contains("MENJADI") ? "Menjadi" : kelompok;
        }
        for (int r = barisJudul + 1; r <= Math.Min(barisJudul + 3, barisAkhir); r++)
        {
            var a = Teks(ws.Cell(r, kKode));
            if (a == "" || a == "1" || !(PolaRek.IsMatch(a) || PolaNoSaja.IsMatch(a))) barisData = r + 1; else break;
        }

        Kolom? Cari(string versi)
        {
            int vol = -1, sat = -1, harga = -1, jml = -1;
            for (int c = 1; c <= kolomAkhir; c++)
            {
                if (kelompokKolom[c] != versi || c == kKode || c == kUraian || c == kSd) continue;
                var j = judul[c];
                if (j.Contains("BERTAMBAH")) continue;
                if (j.Contains("HARGA")) harga = c;
                else if (j.Contains("JUMLAH") || j.Contains("ANGGARAN")) jml = c;
                else if (j.Contains("VOL")) vol = c;
                else if (Regex.IsMatch(j, @"\bSAT\b")) sat = c;
            }
            return jml > 0 ? new Kolom(vol, sat, harga, jml) : null;
        }
        var versiKolom = new Dictionary<string, Kolom>();
        foreach (var v in new[] { "Semula", "Menjadi", "" })
            if (Cari(v) is { } k) versiKolom[v == "" ? "Anggaran" : v] = k;
        if (versiKolom.Count > 1) versiKolom.Remove("Anggaran");
        if (versiKolom.Count == 0) { hasil.Catatan.Add($"Lembar \"{ws.Name}\": kolom jumlah tidak ditemukan."); return; }

        // ---- baris data
        foreach (var (versi, kol) in versiKolom)
        {
            if (!hasil.Versi.TryGetValue(versi, out var data)) hasil.Versi[versi] = data = new DataSkenario();
            Kegiatan? keg = null;
            Paket? paket = null;
            if (kodeKeg != null)
            {
                keg = data.Kegiatan.FirstOrDefault(k => k.Kode == kodeKeg);
                if (keg == null)
                {
                    var seg = kodeKeg.Split('.');
                    using (ChangeTracker.Suspend())
                        keg = new Kegiatan
                        {
                            KodeSubBidang = $"{seg[0]}.{seg[1]}", NoUrut = int.Parse(seg[2]), Nama = namaKeg ?? "",
                            Waktu = waktu ?? "12 Bulan", Keluaran = keluaran ?? "", Lokasi = ""
                        };
                    data.Kegiatan.Add(keg);
                    if (!Lookup.AdaKegiatan(kodeKeg)) hasil.KegiatanBaru[kodeKeg] = namaKeg ?? "";
                }
            }
            string? rek = null;
            decimal total1 = 0, total2 = 0, totalBaca = 0;

            for (int r = barisData; r <= barisAkhir; r++)
            {
                var kode = Teks(ws.Cell(r, kKode));
                var uraian = Teks(ws.Cell(r, kUraian));
                var sd = kSd > 0 ? Teks(ws.Cell(r, kSd)).ToUpperInvariant() : "";
                if (kode == "" && uraian == "") continue;
                var atasUraian = uraian.ToUpperInvariant();
                if (atasUraian.StartsWith("JUMLAH") || kode.ToUpperInvariant().StartsWith("JUMLAH") || atasUraian.StartsWith("PEMBIAYAAN NETTO")
                    || kode.ToUpperInvariant().StartsWith("MENGESAHKAN") || atasUraian.StartsWith("MENGESAHKAN")) break;

                if (PolaRek.Match(kode) is { Success: true } mr)
                {
                    var k = mr.Groups[1].Value;
                    var seg = k.Split('.').Length;
                    if (seg == 1 && total1 == 0) total1 = Angka(ws.Cell(r, kol.Jumlah));
                    if (seg == 2) total2 += Angka(ws.Cell(r, kol.Jumlah));
                    if (!Lookup.AdaRekening(k) && uraian != "") hasil.RekeningBaru[k] = uraian;
                    if (seg == 4) rek = k;
                    continue;
                }

                // Paket kegiatan: "05. Nama paket" tanpa sumber dana & volume (hanya di lembar RAB)
                bool nomorDiKode = PolaNoSaja.IsMatch(kode);
                var mUraian = PolaNomor.Match(uraian);
                var punyaRinci = sd != "" || Angka(ws.Cell(r, kol.Jumlah)) != 0 || (kol.Harga > 0 && Angka(ws.Cell(r, kol.Harga)) != 0)
                                 || (kol.Vol > 0 && Teks(ws.Cell(r, kol.Vol)) != "");
                if (keg != null && kode == "" && mUraian.Success && sd == "" && (kol.Vol <= 0 || Teks(ws.Cell(r, kol.Vol)) == ""))
                {
                    paket = new Paket { Nama = mUraian.Groups[2].Value.Trim(), Lokasi = "" };
                    keg.Paket.Add(paket);
                    rek = null;
                    continue;
                }

                if (rek == null || !(kode == "" || nomorDiKode) || !punyaRinci) continue;
                var (vol, sat) = VolumeSatuan(ws.Cell(r, kol.Vol > 0 ? kol.Vol : kol.Jumlah), kol.Vol > 0);
                if (kol.Sat > 0) sat = Teks(ws.Cell(r, kol.Sat));
                var harga = kol.Harga > 0 ? Angka(ws.Cell(r, kol.Harga)) : 0;
                var jumlah = Angka(ws.Cell(r, kol.Jumlah));
                if (kol.Vol <= 0) { vol = 1; harga = jumlah; }
                if (vol == 0 && jumlah == 0) continue; // tidak dianggarkan pada versi ini
                if (harga == 0 && vol != 0) harga = jumlah / vol;
                var nama = mUraian.Success && !nomorDiKode ? mUraian.Groups[2].Value.Trim() : uraian;
                RincianAnggaran rinci;
                using (ChangeTracker.Suspend())
                    rinci = new RincianAnggaran { KodeRekening = rek, Uraian = nama, Volume = vol, Satuan = sat, HargaSatuan = harga, SumberDana = sd };
                if (rinci.Jumlah != jumlah && jumlah != 0)
                    hasil.Selisih.Add($"{ws.Name} › {nama}: volume × harga = {rinci.Jumlah:N0}, di file {jumlah:N0}");
                totalBaca += rinci.Jumlah;

                if (keg != null)
                {
                    if (paket == null) { paket = new Paket { Nama = keg.Nama }; keg.Paket.Add(paket); }
                    paket.Rincian.Add(rinci);
                }
                else if (rek.StartsWith("4")) data.Pendapatan.Add(rinci);
                else if (rek.StartsWith("6.1")) data.PenerimaanPembiayaan.Add(rinci);
                else if (rek.StartsWith("6.2")) data.PengeluaranPembiayaan.Add(rinci);
            }

            var totalFile = total1 != 0 ? total1 : total2;
            if (totalFile != 0 && Math.Abs(totalFile - totalBaca) > 1)
                hasil.Selisih.Add($"{ws.Name} ({versi}): total di file {totalFile:N0}, hasil hitung {totalBaca:N0}");
            if (keg != null && keg.Paket.Count == 0) keg.Paket.Add(new Paket { Nama = keg.Nama });
        }
    }

    static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    static decimal Angka(IXLCell c)
    {
        if (c.IsEmpty()) return 0;
        if (c.DataType == XLDataType.Number) return (decimal)c.GetDouble();
        return AngkaTeks(Teks(c));
    }

    static decimal AngkaTeks(string s)
    {
        s = s.Trim();
        if (s == "" || s == "-") return 0;
        var neg = s.StartsWith("(") && s.EndsWith(")");
        s = s.Trim('(', ')').Replace("Rp", "").Trim();
        decimal v;
        // 3.050 = tiga ribu lima puluh; 0.5 / 2,5 = desimal
        if (Regex.IsMatch(s, @"^-?\d{1,3}(\.\d{3})+(,\d+)?$")) v = decimal.Parse(s, NumberStyles.Number, Id);
        else if (Regex.IsMatch(s, @"^-?\d+,\d+$")) v = decimal.Parse(s, NumberStyles.Number, Id);
        else if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var x)) v = x;
        else return 0;
        return neg ? -v : v;
    }

    /// <summary>"84 hok" → (84, "hok"); "3.050 liter" → (3050, "liter"); "0.5 kg" → (0,5, "kg").</summary>
    static (decimal vol, string sat) VolumeSatuan(IXLCell c, bool adaKolom)
    {
        if (!adaKolom) return (1, "");
        if (c.IsEmpty()) return (0, "");
        if (c.DataType == XLDataType.Number) return ((decimal)c.GetDouble(), "");
        var t = Teks(c);
        var m = Regex.Match(t, @"^\s*([\d.,]+)\s*(.*)$");
        return m.Success ? (AngkaTeks(m.Groups[1].Value), m.Groups[2].Value.Trim()) : (0, t);
    }
}
