namespace RancangAPBDes.Core;

public class BarisSumberDana
{
    public string Kode { get; init; } = "";
    public string Nama { get; init; } = "";
    public decimal Pagu { get; init; }
    public decimal Dipakai { get; init; }
    public decimal Sisa => Pagu - Dipakai;
    public bool Kurang => Sisa < 0;
    public decimal Persen => Pagu == 0 ? (Dipakai > 0 ? 100 : 0) : Math.Round(Dipakai / Pagu * 100, 1);
}

public class BarisBidang
{
    public string Kode { get; init; } = "";
    public string Nama { get; init; } = "";
    public decimal Jumlah { get; init; }
    public decimal Persen { get; init; }
    public int JumlahKegiatan { get; init; }
}

public class HasilAturan
{
    public string Nama { get; init; } = "";
    public string Batas { get; init; } = "";
    public decimal Nilai { get; init; }
    public decimal Dasar { get; init; }
    public string NamaDasar { get; init; } = "";
    public decimal Persen { get; init; }
    public bool Terpenuhi { get; init; }
    public string Status => Terpenuhi ? "Sesuai" : "TIDAK SESUAI";
}

public class Peringatan
{
    public string Level { get; init; } = "Info";   // Kesalahan / Perhatian / Info
    public string Teks { get; init; } = "";
    public int Urutan => Level switch { "Kesalahan" => 0, "Perhatian" => 1, _ => 2 };
    public string Ikon => Level switch { "Kesalahan" => "✖", "Perhatian" => "⚠", _ => "ℹ" };
}

public class HasilHitung
{
    public decimal TotalPendapatan { get; init; }
    public decimal TotalBelanja { get; init; }
    public decimal Surplus => TotalPendapatan - TotalBelanja;
    public decimal PenerimaanPembiayaan { get; init; }
    public decimal PengeluaranPembiayaan { get; init; }
    public decimal PembiayaanNetto => PenerimaanPembiayaan - PengeluaranPembiayaan;
    /// <summary>Sisa lebih/kurang perhitungan anggaran tahun berjalan (idealnya 0).</summary>
    public decimal SisaAnggaran => Surplus + PembiayaanNetto;
    public int JumlahKegiatan { get; init; }
    public List<BarisSumberDana> PerSumberDana { get; init; } = new();
    public List<BarisBidang> PerBidang { get; init; } = new();
    public List<HasilAturan> Aturan { get; init; } = new();
    public List<Peringatan> Peringatan { get; init; } = new();
    public int JumlahKesalahan => Peringatan.Count(p => p.Level == "Kesalahan");
}

public static class Kalkulasi
{
    public static string Rp(decimal v) => "Rp " + v.ToString("#,##0", Budaya.Id);

    static string SdRincian(RincianAnggaran r, Paket? p) =>
        !string.IsNullOrWhiteSpace(r.SumberDana) ? r.SumberDana : (p?.SumberDana ?? "");

    public static IEnumerable<(Kegiatan K, Paket P, RincianAnggaran R, string Sd)> RincianBelanja(DataSkenario d)
    {
        foreach (var k in d.Kegiatan)
            foreach (var p in k.Paket)
                foreach (var r in p.Rincian)
                    yield return (k, p, r, SdRincian(r, p));
    }

    public static HasilHitung Hitung(DataSkenario d, Referensi refr)
    {
        var peringatan = new List<Peringatan>();
        void Tambah(string level, string teks) => peringatan.Add(new Peringatan { Level = level, Teks = teks });

        decimal pend = d.Pendapatan.Sum(r => r.Jumlah);
        decimal terima = d.PenerimaanPembiayaan.Sum(r => r.Jumlah);
        decimal keluar = d.PengeluaranPembiayaan.Sum(r => r.Jumlah);
        var belanjaRows = RincianBelanja(d).ToList();
        decimal belanja = belanjaRows.Sum(x => x.R.Jumlah);

        // ---- per sumber dana ----
        var pagu = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var pakai = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        void Add(Dictionary<string, decimal> dict, string k, decimal v)
        {
            k = string.IsNullOrWhiteSpace(k) ? "" : k.Trim().ToUpperInvariant();
            dict[k] = dict.TryGetValue(k, out var x) ? x + v : v;
        }
        foreach (var r in d.Pendapatan) Add(pagu, r.SumberDana, r.Jumlah);
        foreach (var r in d.PenerimaanPembiayaan) Add(pagu, r.SumberDana, r.Jumlah);
        foreach (var r in d.PengeluaranPembiayaan) Add(pakai, r.SumberDana, r.Jumlah);
        foreach (var x in belanjaRows) Add(pakai, x.Sd, x.R.Jumlah);

        var kodeSd = pagu.Keys.Union(pakai.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var urutRef = refr.SumberDana.Select((s, i) => (s.Kode, i)).ToDictionary(x => x.Kode, x => x.i, StringComparer.OrdinalIgnoreCase);
        var perSd = kodeSd
            .Select(k => new BarisSumberDana
            {
                Kode = k == "" ? "(kosong)" : k,
                Nama = k == "" ? "Sumber dana belum dipilih" : Lookup.NamaSumberDana(k),
                Pagu = pagu.GetValueOrDefault(k),
                Dipakai = pakai.GetValueOrDefault(k)
            })
            .Where(b => b.Pagu != 0 || b.Dipakai != 0)
            .OrderBy(b => urutRef.TryGetValue(b.Kode, out var i) ? i : 999).ThenBy(b => b.Kode)
            .ToList();

        foreach (var b in perSd)
        {
            if (b.Kode == "(kosong)")
                Tambah("Kesalahan", $"Ada rincian senilai {Rp(b.Dipakai + b.Pagu)} yang belum dipilih sumber dananya.");
            else if (b.Kurang)
                Tambah("Kesalahan", $"Belanja dari {b.Kode} ({b.Nama}) melebihi pagu sebesar {Rp(-b.Sisa)}.");
            else if (b.Sisa > 0)
                Tambah("Info", $"Dana {b.Kode} ({b.Nama}) masih tersisa {Rp(b.Sisa)} yang belum dialokasikan.");
        }

        // ---- keseimbangan APBDes ----
        var sisaAnggaran = pend - belanja + terima - keluar;
        if (sisaAnggaran < 0)
            Tambah("Kesalahan", $"Anggaran defisit dan tidak tertutup pembiayaan: kurang {Rp(-sisaAnggaran)}.");
        else if (sisaAnggaran > 0)
            Tambah("Info", $"Masih ada {Rp(sisaAnggaran)} yang belum dialokasikan (SiLPA tahun berjalan).");

        // ---- per bidang ----
        var perBidang = refr.Bidang
            .Select(b =>
            {
                var keg = d.Kegiatan.Where(k => k.KodeSubBidang.Split('.')[0] == b.Kode).ToList();
                var jml = keg.Sum(k => k.Total);
                return new BarisBidang
                {
                    Kode = b.Kode, Nama = b.Nama, Jumlah = jml, JumlahKegiatan = keg.Count,
                    Persen = belanja == 0 ? 0 : Math.Round(jml / belanja * 100, 1)
                };
            })
            .ToList();
        var bidangTakDikenal = d.Kegiatan.Where(k => !refr.Bidang.Any(b => b.Kode == k.KodeSubBidang.Split('.')[0])).ToList();
        if (bidangTakDikenal.Count > 0)
        {
            var jml = bidangTakDikenal.Sum(k => k.Total);
            perBidang.Add(new BarisBidang
            {
                Kode = "?", Nama = "Bidang tidak ada di referensi", Jumlah = jml, JumlahKegiatan = bidangTakDikenal.Count,
                Persen = belanja == 0 ? 0 : Math.Round(jml / belanja * 100, 1)
            });
        }

        // ---- pemeriksaan isian ----
        foreach (var g in d.Kegiatan.GroupBy(k => k.Kode).Where(g => g.Count() > 1))
            Tambah("Kesalahan", $"Kode kegiatan {g.Key} dipakai {g.Count()} kali. Ubah nomor urut salah satunya.");

        foreach (var k in d.Kegiatan)
        {
            var nama = string.IsNullOrWhiteSpace(k.Nama) ? "(tanpa nama)" : k.Nama;
            if (string.IsNullOrWhiteSpace(k.Nama))
                Tambah("Perhatian", $"Kegiatan {k.Kode} belum diberi nama.");
            if (!refr.SubBidang.Any(s => s.Kode == k.KodeSubBidang))
                Tambah("Perhatian", $"Kegiatan {k.Kode} {nama}: sub bidang {k.KodeSubBidang} tidak ada di referensi.");
            if (k.Paket.Count == 0)
                Tambah("Perhatian", $"Kegiatan {k.Kode} {nama} belum memiliki paket/rincian.");
            foreach (var p in k.Paket)
            {
                var np = string.IsNullOrWhiteSpace(p.Nama) ? "(paket tanpa nama)" : p.Nama;
                if (p.Rincian.Count == 0)
                    Tambah("Perhatian", $"{k.Kode} {nama} › {np}: belum ada rincian RAB.");
                foreach (var r in p.Rincian)
                {
                    var tempat = $"{k.Kode} {nama} › {np}";
                    CekRincian(r, tempat, "5", Tambah);
                    if (!string.IsNullOrWhiteSpace(p.SumberDana) && !string.IsNullOrWhiteSpace(r.SumberDana) &&
                        !string.Equals(p.SumberDana, r.SumberDana, StringComparison.OrdinalIgnoreCase))
                        Tambah("Info", $"{tempat}: rincian \"{r.Uraian}\" memakai {r.SumberDana}, berbeda dengan sumber dana paket ({p.SumberDana}).");
                }
            }
        }
        foreach (var r in d.Pendapatan) CekRincian(r, "Pendapatan", "4", Tambah);
        foreach (var r in d.PenerimaanPembiayaan) CekRincian(r, "Penerimaan pembiayaan", "6.1", Tambah);
        foreach (var r in d.PengeluaranPembiayaan) CekRincian(r, "Pengeluaran pembiayaan", "6.2", Tambah);

        // ---- aturan batas ----
        var hasilAturan = new List<HasilAturan>();
        foreach (var a in refr.Aturan.Where(a => a.Aktif))
        {
            decimal nilai = belanjaRows
                .Where(x => Batas.Cocok(x.K.KodeSubBidang, a.AwalanSubBidang))
                .Where(x => Batas.Cocok(x.R.KodeRekening, a.AwalanRekening))
                .Where(x => a.SumberDana == "" || string.Equals(x.Sd, a.SumberDana, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.R.Jumlah);
            bool pakaiPagu = a.Dasar == "Pagu Sumber Dana" && a.SumberDana != "";
            decimal dasar = pakaiPagu ? pagu.GetValueOrDefault(a.SumberDana) : belanja;
            string namaDasar = pakaiPagu ? $"Pagu {a.SumberDana}" : "Total belanja";
            decimal persen = dasar == 0 ? 0 : Math.Round(nilai / dasar * 100, 2);
            bool ok = dasar == 0 || (a.Jenis == "Minimal" ? persen >= a.Persen : persen <= a.Persen);
            var batasTeks = $"{(a.Jenis == "Minimal" ? "min." : "maks.")} {a.Persen.ToString("0.##", Budaya.Id)}%";
            hasilAturan.Add(new HasilAturan
            {
                Nama = a.Nama, Batas = batasTeks, Nilai = nilai, Dasar = dasar, NamaDasar = namaDasar,
                Persen = persen, Terpenuhi = ok
            });
            if (!ok)
                Tambah("Kesalahan", $"Aturan \"{a.Nama}\": {persen.ToString("0.##", Budaya.Id)}% dari {namaDasar.ToLowerInvariant()} ({batasTeks}).");
        }

        return new HasilHitung
        {
            TotalPendapatan = pend,
            TotalBelanja = belanja,
            PenerimaanPembiayaan = terima,
            PengeluaranPembiayaan = keluar,
            JumlahKegiatan = d.Kegiatan.Count,
            PerSumberDana = perSd,
            PerBidang = perBidang,
            Aturan = hasilAturan,
            Peringatan = peringatan.OrderBy(p => p.Urutan).ToList()
        };
    }

    static void CekRincian(RincianAnggaran r, string tempat, string awalanRek, Action<string, string> tambah)
    {
        var u = string.IsNullOrWhiteSpace(r.Uraian) ? "(tanpa uraian)" : r.Uraian;
        if (string.IsNullOrWhiteSpace(r.KodeRekening))
            tambah("Perhatian", $"{tempat}: \"{u}\" belum dipilih kode rekeningnya.");
        else if (!Lookup.AdaRekening(r.KodeRekening))
            tambah("Perhatian", $"{tempat}: kode rekening {r.KodeRekening} untuk \"{u}\" tidak ada di referensi.");
        else if (r.KodeRekening.Split('.').Length != 4)
            tambah("Perhatian", $"{tempat}: kode {r.KodeRekening} bukan rekening rincian (harus 4 tingkat, mis. 5.2.1.01).");
        else if (!Batas.Cocok(r.KodeRekening, awalanRek))
            tambah("Perhatian", $"{tempat}: kode rekening {r.KodeRekening} untuk \"{u}\" bukan kelompok {awalanRek}.");
        if (r.Jumlah == 0)
            tambah("Perhatian", $"{tempat}: \"{u}\" bernilai Rp 0.");
        if (!string.IsNullOrWhiteSpace(r.SumberDana) && !Lookup.AdaSumberDana(r.SumberDana))
            tambah("Perhatian", $"{tempat}: sumber dana {r.SumberDana} tidak ada di referensi.");
    }
}

public static class Budaya
{
    public static readonly System.Globalization.CultureInfo Id = System.Globalization.CultureInfo.GetCultureInfo("id-ID");
}
