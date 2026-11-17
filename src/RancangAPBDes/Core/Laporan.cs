using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RancangAPBDes.Core;

/// <summary>Menyusun laporan cetak (FlowDocument).</summary>
public static class Laporan
{
    public const string Ringkasan = "Ringkasan APBDes (bahan musyawarah)";
    public const string Rab = "RAB per Kegiatan";
    public const string SiapInput = "Daftar Siap Input Siskeudes";
    public const string Pendapatan = "Rencana Anggaran Pendapatan";
    public const string Pembiayaan = "Rencana Anggaran Pembiayaan";
    public const string Perubahan = "Perubahan APBDes (Semula – Menjadi)";
    public const string Rpjm = "RPJMDes";
    public const string Rkp = "RKPDes";
    public const string Rak = "Rencana Anggaran Kas (RAK)";
    public static readonly string[] Jenis = { Ringkasan, Rpjm, Rkp, Pendapatan, Rab, Pembiayaan, Rak, Perubahan, SiapInput };

    static readonly Brush Garis = new SolidColorBrush(Color.FromRgb(0x88, 0x92, 0xA0));
    static readonly Brush LatarKepala = new SolidColorBrush(Color.FromRgb(0xE3, 0xE9, 0xF1));
    static readonly Brush LatarGrup = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xF9));
    static readonly Brush Merah = new SolidColorBrush(Color.FromRgb(0xB4, 0x23, 0x18));

    static string N(decimal v) => v.ToString("#,##0.##", Budaya.Id);
    static string N0(decimal v) => v.ToString("#,##0", Budaya.Id);
    static string KodeRek(string k) => string.IsNullOrWhiteSpace(k) ? "" : k + ".";

    public static string KodeLengkap(DataDesa desa, string kode) =>
        kode + ".";

    public static FlowDocument Buat(string jenis, DataDesa desa, SkenarioInfo info, DataSkenario data, HasilHitung hasil)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12.5,
            PagePadding = new Thickness(56, 48, 56, 48),
            ColumnWidth = double.PositiveInfinity,
            TextAlignment = TextAlignment.Left,
            Background = Brushes.White,
            Foreground = Brushes.Black,
            IsOptimalParagraphEnabled = true,
            IsHyphenationEnabled = false,
        };
        Kop(doc, jenis, desa, info);
        switch (jenis)
        {
            case Rab: IsiRab(doc, desa, data); break;
            case SiapInput: IsiSiapInput(doc, desa, data); break;
            case Pendapatan: IsiRekening(doc, "PENDAPATAN", data.Pendapatan, null, null); TandaTangan(doc, desa); break;
            case Pembiayaan: IsiRekening(doc, "PENERIMAAN PEMBIAYAAN", data.PenerimaanPembiayaan, "PENGELUARAN PEMBIAYAAN", data.PengeluaranPembiayaan); TandaTangan(doc, desa); break;
            case Perubahan: doc.Blocks.Add(Teks("Pilih skenario Semula di bagian atas jendela pratinjau.")); break;
            case Rpjm: IsiRpjm(doc, AppState.Instance.Perencanaan); TandaTangan(doc, desa); break;
            case Rkp: IsiRkp(doc, AppState.Instance.Perencanaan, info.Tahun); TandaTangan(doc, desa); break;
            case Rak: IsiRak(doc, data); TandaTangan(doc, desa); break;
            default: IsiRingkasan(doc, desa, data, hasil); break;
        }
        return doc;
    }

    // ------------------------------------------------------------ bantu

    static void Kop(FlowDocument doc, string jenis, DataDesa desa, SkenarioInfo info)
    {
        var p = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4) };
        p.Inlines.Add(new Run(jenis.ToUpperInvariant()) { FontSize = 16, FontWeight = FontWeights.Bold });
        p.Inlines.Add(new LineBreak());
        var namaDesa = string.IsNullOrWhiteSpace(desa.NamaDesa) ? "DESA (belum diisi)" : desa.NamaDesa.ToUpperInvariant();
        p.Inlines.Add(new Run($"{namaDesa} – TAHUN ANGGARAN {info.Tahun}") { FontSize = 14, FontWeight = FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(desa.Kecamatan) || !string.IsNullOrWhiteSpace(desa.Kabupaten))
        {
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run(string.Join(", ", new[] {
                string.IsNullOrWhiteSpace(desa.Kecamatan) ? null : "Kecamatan " + desa.Kecamatan,
                string.IsNullOrWhiteSpace(desa.Kabupaten) ? null : "Kabupaten " + desa.Kabupaten }.Where(x => x != null))));
        }
        doc.Blocks.Add(p);

        var s = new Paragraph { TextAlignment = TextAlignment.Center, FontSize = 11.5, Margin = new Thickness(0, 0, 0, 14) };
        s.Inlines.Add(new Run($"Skenario: {info.Nama} ({(info.IsFinal ? "FINAL" : "DRAFT")}) · dicetak {DateTime.Now:dd MMMM yyyy HH:mm}"));
        s.Inlines.Add(new LineBreak());
        s.Inlines.Add(new Run("Bahan musyawarah – bukan dokumen resmi APBDes. Data resmi tetap di Siskeudes.") { FontStyle = FontStyles.Italic });
        doc.Blocks.Add(s);
    }

    static Paragraph Judul(string teks) => new(new Run(teks))
    {
        FontSize = 14.5, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 14, 0, 6), KeepWithNext = true
    };

    static Paragraph Teks(string teks, double ukuran = 12.5) => new(new Run(teks)) { FontSize = ukuran, Margin = new Thickness(0, 0, 0, 6) };

    sealed class Tabel
    {
        public readonly Table T = new() { CellSpacing = 0, BorderBrush = Garis, BorderThickness = new Thickness(0.5, 0.5, 0, 0), Margin = new Thickness(0, 0, 0, 10) };
        readonly TableRowGroup _g = new();
        readonly bool[] _kanan;

        public Tabel(double[] lebar, string[] kepala, bool[] kanan)
        {
            _kanan = kanan;
            foreach (var l in lebar) T.Columns.Add(new TableColumn { Width = new GridLength(l, GridUnitType.Star) });
            T.RowGroups.Add(_g);
            var r = Baris(kepala, true, LatarKepala);
            _g.Rows.Add(r);
        }

        TableRow Baris(string[] isi, bool tebal, Brush? latar, Brush? warna = null)
        {
            var r = new TableRow();
            if (latar != null) r.Background = latar;
            for (int i = 0; i < isi.Length; i++)
            {
                var p = new Paragraph(new Run(isi[i] ?? ""))
                {
                    Margin = new Thickness(0),
                    TextAlignment = i < _kanan.Length && _kanan[i] ? TextAlignment.Right : TextAlignment.Left,
                    FontWeight = tebal ? FontWeights.SemiBold : FontWeights.Normal,
                };
                if (warna != null) p.Foreground = warna;
                r.Cells.Add(new TableCell(p)
                {
                    BorderBrush = Garis,
                    BorderThickness = new Thickness(0, 0, 0.5, 0.5),
                    Padding = new Thickness(4, 2, 4, 2),
                    ColumnSpan = 1
                });
            }
            return r;
        }

        public void Tambah(params string[] isi) => _g.Rows.Add(Baris(isi, false, null));
        public void Tebal(params string[] isi) => _g.Rows.Add(Baris(isi, true, LatarGrup));
        public void Merah(params string[] isi) => _g.Rows.Add(Baris(isi, true, null, Laporan.Merah));

        /// <summary>Baris grup yang menyatu di semua kolom.</summary>
        public void Grup(string teks)
        {
            var r = new TableRow { Background = LatarGrup };
            r.Cells.Add(new TableCell(new Paragraph(new Run(teks)) { Margin = new Thickness(0), FontWeight = FontWeights.SemiBold })
            {
                ColumnSpan = T.Columns.Count, BorderBrush = Garis, BorderThickness = new Thickness(0, 0, 0.5, 0.5), Padding = new Thickness(4, 3, 4, 3)
            });
            _g.Rows.Add(r);
        }
    }

    // ------------------------------------------------------------ ringkasan

    static void IsiRingkasan(FlowDocument doc, DataDesa desa, DataSkenario d, HasilHitung h)
    {
        doc.Blocks.Add(Judul("A. Struktur APBDes"));
        var t = new Tabel(new double[] { 1.3, 5, 2 }, new[] { "Kode", "Uraian", "Jumlah (Rp)" }, new[] { false, false, true });

        t.Grup("PENDAPATAN");
        foreach (var g in d.Pendapatan.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            t.Tambah(KodeRek(g.Key), Lookup.NamaRekening(g.Key), N0(g.Sum(r => r.Jumlah)));
        t.Tebal("", "Jumlah Pendapatan", N0(h.TotalPendapatan));

        t.Grup("BELANJA");
        foreach (var b in h.PerBidang.Where(b => b.Jumlah != 0 || b.JumlahKegiatan > 0))
            t.Tambah(b.Kode, b.Nama, N0(b.Jumlah));
        t.Tebal("", "Jumlah Belanja", N0(h.TotalBelanja));
        t.Tebal("", h.Surplus >= 0 ? "SURPLUS" : "DEFISIT", N0(Math.Abs(h.Surplus)));

        t.Grup("PEMBIAYAAN");
        foreach (var g in d.PenerimaanPembiayaan.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            t.Tambah(KodeRek(g.Key), Lookup.NamaRekening(g.Key), N0(g.Sum(r => r.Jumlah)));
        t.Tebal("", "Jumlah Penerimaan Pembiayaan", N0(h.PenerimaanPembiayaan));
        foreach (var g in d.PengeluaranPembiayaan.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            t.Tambah(KodeRek(g.Key), Lookup.NamaRekening(g.Key), N0(g.Sum(r => r.Jumlah)));
        t.Tebal("", "Jumlah Pengeluaran Pembiayaan", N0(h.PengeluaranPembiayaan));
        t.Tebal("", "Pembiayaan Netto", N0(h.PembiayaanNetto));
        if (h.SisaAnggaran < 0) t.Merah("", "KEKURANGAN DANA (belanja melebihi dana)", N0(-h.SisaAnggaran));
        else t.Tebal("", "Sisa Lebih Pembiayaan Anggaran Tahun Berjalan", N0(h.SisaAnggaran));
        doc.Blocks.Add(t.T);

        doc.Blocks.Add(Judul("B. Dana per Sumber Dana"));
        var s = new Tabel(new double[] { 1, 3.5, 2, 2, 2, 1.2 }, new[] { "Kode", "Sumber Dana", "Pagu (Rp)", "Dipakai (Rp)", "Sisa (Rp)", "%" },
            new[] { false, false, true, true, true, true });
        foreach (var b in h.PerSumberDana)
        {
            var isi = new[] { b.Kode, b.Nama, N0(b.Pagu), N0(b.Dipakai), N0(b.Sisa), b.Persen.ToString("0.#", Budaya.Id) + "%" };
            if (b.Kurang) s.Merah(isi); else s.Tambah(isi);
        }
        doc.Blocks.Add(s.T);

        doc.Blocks.Add(Judul("C. Daftar Kegiatan"));
        var k = new Tabel(new double[] { 1.6, 5, 1.4, 2 }, new[] { "Kode", "Kegiatan", "Sumber Dana", "Pagu (Rp)" }, new[] { false, false, false, true });
        foreach (var bid in d.Kegiatan.GroupBy(x => x.KodeSubBidang.Split('.')[0]).OrderBy(g => g.Key, KodeComparer.Instance))
        {
            k.Grup($"{bid.Key}  {Lookup.NamaBidang(bid.Key)}");
            foreach (var keg in bid.OrderBy(x => x.Kode, KodeComparer.Instance))
            {
                var sumber = keg.Paket.SelectMany(p => p.Rincian.Select(r => string.IsNullOrWhiteSpace(r.SumberDana) ? p.SumberDana : r.SumberDana))
                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
                k.Tambah(keg.Kode, keg.Nama, string.Join(", ", sumber), N0(keg.Total));
            }
        }
        k.Tebal("", "Jumlah", "", N0(h.TotalBelanja));
        doc.Blocks.Add(k.T);

        if (h.Aturan.Count > 0)
        {
            doc.Blocks.Add(Judul("D. Cek Aturan"));
            var a = new Tabel(new double[] { 5, 1.4, 2, 1.3, 1.6 }, new[] { "Aturan", "Batas", "Nilai (Rp)", "Hasil", "Status" },
                new[] { false, false, true, true, false });
            foreach (var x in h.Aturan)
            {
                var isi = new[] { x.Nama, x.Batas, N0(x.Nilai), x.Persen.ToString("0.##", Budaya.Id) + "%", x.Status };
                if (x.Terpenuhi) a.Tambah(isi); else a.Merah(isi);
            }
            doc.Blocks.Add(a.T);
        }

        var penting = h.Peringatan.Where(p => p.Level != "Info").Take(50).ToList();
        if (penting.Count > 0)
        {
            doc.Blocks.Add(Judul("E. Hal yang Perlu Diperbaiki"));
            var list = new List { MarkerStyle = TextMarkerStyle.Disc, Margin = new Thickness(0, 0, 0, 8) };
            foreach (var p in penting)
                list.ListItems.Add(new ListItem(new Paragraph(new Run($"[{p.Level}] {p.Teks}")) { Margin = new Thickness(0, 0, 0, 2) }));
            doc.Blocks.Add(list);
        }

        if (!string.IsNullOrWhiteSpace(d.Catatan))
        {
            doc.Blocks.Add(Judul("F. Catatan Musyawarah"));
            doc.Blocks.Add(Teks(d.Catatan));
        }

        TandaTangan(doc, desa);
    }

    static void TandaTangan(FlowDocument doc, DataDesa desa)
    {
        var t = new Table { CellSpacing = 0, Margin = new Thickness(0, 24, 0, 0) };
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var g = new TableRowGroup();
        t.RowGroups.Add(g);
        var r = new TableRow();
        var tempat = string.IsNullOrWhiteSpace(desa.TempatTtd) ? desa.NamaDesa : desa.TempatTtd;
        r.Cells.Add(Ttd("Mengetahui,\nKepala Desa", desa.KepalaDesa));
        r.Cells.Add(Ttd($"{tempat}, {DateTime.Now:dd MMMM yyyy}\nKaur Keuangan", desa.KaurKeuangan));
        g.Rows.Add(r);
        doc.Blocks.Add(t);
    }

    static TableCell Ttd(string atas, string nama)
    {
        var p = new Paragraph { TextAlignment = TextAlignment.Center, KeepTogether = true };
        foreach (var baris in atas.Split('\n'))
        {
            p.Inlines.Add(new Run(baris));
            p.Inlines.Add(new LineBreak());
        }
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new LineBreak());
        p.Inlines.Add(new Run(string.IsNullOrWhiteSpace(nama) ? "( ................................ )" : nama) { FontWeight = FontWeights.SemiBold, TextDecorations = TextDecorations.Underline });
        return new TableCell(p);
    }

    // ------------------------------------------------------------ Perencanaan & Anggaran Kas

    static void IsiRpjm(FlowDocument doc, DataPerencanaan r)
    {
        doc.Blocks.Add(Teks($"Periode RPJMDes: {r.Periode}" + (r.NomorPerdes != "" ? $"  ·  Perdes: {r.NomorPerdes}" : "")));
        doc.Blocks.Add(Judul("A. Visi"));
        doc.Blocks.Add(Teks(r.Visi == "" ? "(belum diisi)" : r.Visi));
        doc.Blocks.Add(Judul("B. Misi"));
        if (r.Misi.Count == 0) doc.Blocks.Add(Teks("(belum diisi)"));
        foreach (var m in r.Misi.OrderBy(m => m.No)) doc.Blocks.Add(Teks($"{m.No}. {m.Uraian}"));
        doc.Blocks.Add(Judul("C. Rencana Kegiatan"));
        var t = new Tabel(new double[] { 1.1, 3.6, 1.5, 1.3, 1.4, 1.3, 1.8, 0.8 },
            new[] { "Kode", "Kegiatan", "Lokasi", "Volume", "Sasaran", "Tahun", "Perkiraan Biaya (Rp)", "SD" },
            new[] { false, false, false, true, false, false, true, false });
        foreach (var bid in r.KegiatanRpjm.GroupBy(k => k.KodeSubBidang.Split('.')[0]).OrderBy(g => g.Key, KodeComparer.Instance))
        {
            t.Grup($"{bid.Key}. {Lookup.NamaBidang(bid.Key).ToUpperInvariant()}");
            foreach (var k in bid.OrderBy(k => k.Kode, KodeComparer.Instance))
                t.Tambah(k.Kode + ".", k.Nama, k.Lokasi, $"{N(k.Volume)} {k.Satuan}", k.SasaranManfaat, k.TahunPelaksanaan, N0(k.PerkiraanBiaya), k.SumberDana);
        }
        t.Tebal("", "JUMLAH", "", "", "", "", N0(r.KegiatanRpjm.Sum(k => k.PerkiraanBiaya)), "");
        doc.Blocks.Add(t.T);
    }

    static void IsiRkp(FlowDocument doc, DataPerencanaan r, int tahun)
    {
        var pagu = r.Pagu.Where(p => p.Tahun == tahun).ToList();
        var keg = r.KegiatanRkp.Where(k => k.Tahun == tahun).ToList();
        doc.Blocks.Add(Judul($"A. Pagu Indikatif Tahun {tahun}"));
        var tp = new Tabel(new double[] { 1, 3.4, 2, 2, 2 }, new[] { "Kode", "Sumber Dana", "Pagu (Rp)", "Kegiatan Disepakati (Rp)", "Sisa (Rp)" }, new[] { false, false, true, true, true });
        foreach (var p in pagu.OrderBy(p => p.SumberDana))
        {
            var pakai = keg.Where(k => k.Status == "Disepakati" && string.Equals(k.SumberDana, p.SumberDana, StringComparison.OrdinalIgnoreCase)).Sum(k => k.PaguIndikatif);
            tp.Tambah(p.SumberDana, Lookup.NamaSumberDana(p.SumberDana), N0(p.Nilai), N0(pakai), N0(p.Nilai - pakai));
        }
        tp.Tebal("", "JUMLAH", N0(pagu.Sum(p => p.Nilai)), N0(keg.Where(k => k.Status == "Disepakati").Sum(k => k.PaguIndikatif)), "");
        doc.Blocks.Add(tp.T);

        foreach (var (judul, status) in new[] { ("B. Kegiatan yang Disepakati", "Disepakati"), ("C. Kegiatan Cadangan", "Cadangan") })
        {
            var daftar = keg.Where(k => k.Status == status).ToList();
            doc.Blocks.Add(Judul(judul));
            if (daftar.Count == 0) { doc.Blocks.Add(Teks("(tidak ada)")); continue; }
            var t = new Tabel(new double[] { 1.1, 3.4, 1.4, 1.3, 1.4, 1.1, 1.8, 0.8, 0.7 },
                new[] { "Kode", "Kegiatan", "Lokasi", "Volume", "Sasaran", "Waktu", "Pagu Indikatif (Rp)", "SD", "Prio" },
                new[] { false, false, false, true, false, false, true, false, true });
            foreach (var bid in daftar.GroupBy(k => k.KodeSubBidang.Split('.')[0]).OrderBy(g => g.Key, KodeComparer.Instance))
            {
                t.Grup($"{bid.Key}. {Lookup.NamaBidang(bid.Key).ToUpperInvariant()}");
                foreach (var k in bid.OrderBy(k => k.Kode, KodeComparer.Instance))
                    t.Tambah(k.Kode + ".", k.Nama, k.Lokasi, $"{N(k.Volume)} {k.Satuan}", k.SasaranManfaat, k.Waktu, N0(k.PaguIndikatif), k.SumberDana, k.Prioritas.ToString());
            }
            t.Tebal("", "JUMLAH", "", "", "", "", N0(daftar.Sum(k => k.PaguIndikatif)), "", "");
            doc.Blocks.Add(t.T);
        }
    }

    static void IsiRak(FlowDocument doc, DataSkenario d)
    {
        doc.Blocks.Add(Teks("Pilih orientasi Landscape saat mencetak. Nilai dalam ribuan rupiah (Rp 000).", 11.5));
        decimal[] Bulan(string kunci) => d.AnggaranKas.FirstOrDefault(x => x.Kunci == kunci)?.Bulan.Take(12).ToArray() ?? new decimal[12];
        string R(decimal v) => v == 0 ? "-" : Math.Round(v / 1000m).ToString("#,##0", Budaya.Id);
        void Bagian(string judul, IEnumerable<(string kode, string nama, decimal pagu, decimal[] bulan)> baris)
        {
            doc.Blocks.Add(Judul(judul));
            var lebar = new List<double> { 0.9, 2.4, 1.1 };
            lebar.AddRange(Enumerable.Repeat(0.75, 12));
            var kepala = new List<string> { "Kode", "Uraian", "Anggaran" };
            kepala.AddRange(new[] { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agu", "Sep", "Okt", "Nov", "Des" });
            var kanan = new List<bool> { false, false, true };
            kanan.AddRange(Enumerable.Repeat(true, 12));
            var t = new Tabel(lebar.ToArray(), kepala.ToArray(), kanan.ToArray());
            t.T.FontSize = 10;
            var total = new decimal[12];
            decimal totalPagu = 0;
            foreach (var (kode, nama, pagu, bulan) in baris)
            {
                var isi = new List<string> { kode, nama, R(pagu) };
                isi.AddRange(bulan.Select(R));
                t.Tambah(isi.ToArray());
                for (int i = 0; i < 12; i++) total[i] += bulan[i];
                totalPagu += pagu;
            }
            var jml = new List<string> { "", "JUMLAH", R(totalPagu) };
            jml.AddRange(total.Select(R));
            t.Tebal(jml.ToArray());
            doc.Blocks.Add(t.T);
        }
        Bagian("A. Pendapatan", d.Pendapatan.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance)
            .Select(g => (g.Key + ".", Lookup.NamaRekening(g.Key), g.Sum(r => r.Jumlah), Bulan("R:" + g.Key))));
        Bagian("B. Belanja", d.Kegiatan.OrderBy(k => k.Kode, KodeComparer.Instance)
            .Select(k => (k.Kode + ".", k.Nama, k.Total, Bulan("K:" + k.Id))));
        Bagian("C. Pembiayaan", d.PenerimaanPembiayaan.Concat(d.PengeluaranPembiayaan).GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance)
            .Select(g => (g.Key + ".", Lookup.NamaRekening(g.Key), g.Sum(r => r.Jumlah), Bulan("R:" + g.Key))));
    }

    // ------------------------------------------------------------ Perubahan (Semula – Menjadi)

    /// <summary>Rekapitulasi perubahan seperti cetakan Siskeudes: per kegiatan, pendapatan, dan pembiayaan.</summary>
    public static FlowDocument BuatPerubahan(DataDesa desa, SkenarioInfo semula, DataSkenario dSemula, SkenarioInfo menjadi, DataSkenario dMenjadi)
    {
        var doc = Buat(Ringkasan, desa, menjadi, new DataSkenario(), Kalkulasi.Hitung(new DataSkenario(), AppState.Instance.Referensi));
        doc.Blocks.Clear();
        Kop(doc, Perubahan, desa, menjadi);
        doc.Blocks.Add(Teks($"Semula: skenario \"{semula.Nama}\" (tahun {semula.Tahun}).  Menjadi: skenario \"{menjadi.Nama}\" (tahun {menjadi.Tahun}).", 12));
        string S0(decimal v) => v < 0 ? $"({N0(-v)})" : N0(v);

        doc.Blocks.Add(Judul("A. Rekapitulasi Perubahan RAB per Kegiatan"));
        var t = new Tabel(new double[] { 0.6, 1.2, 4.2, 1.1, 1.9, 1.9, 1.9 },
            new[] { "No", "Kode", "Bidang / Sub Bidang / Nama Kegiatan", "Waktu", "Semula (Rp)", "Menjadi (Rp)", "Bertambah / (Berkurang)" },
            new[] { false, false, false, false, true, true, true });
        var ka = dSemula.Kegiatan.GroupBy(k => k.Kode).ToDictionary(g => g.Key, g => g.Sum(k => k.Total));
        var kb = dMenjadi.Kegiatan.GroupBy(k => k.Kode).ToDictionary(g => g.Key, g => g.Sum(k => k.Total));
        var semua = dSemula.Kegiatan.Concat(dMenjadi.Kegiatan).GroupBy(k => k.Kode).Select(g => g.Last()).OrderBy(k => k.Kode, KodeComparer.Instance).ToList();
        int no = 1;
        foreach (var bid in semua.GroupBy(k => k.KodeSubBidang.Split('.')[0]))
        {
            t.Grup($"{bid.Key}. {Lookup.NamaBidang(bid.Key).ToUpperInvariant()}");
            foreach (var k in bid)
            {
                var a = ka.GetValueOrDefault(k.Kode); var b = kb.GetValueOrDefault(k.Kode);
                t.Tambah((no++).ToString(), k.Kode + ".", k.Nama, k.Waktu, N0(a), N0(b), S0(b - a));
            }
        }
        var ta = ka.Values.Sum(); var tb = kb.Values.Sum();
        t.Tebal("", "", "JUMLAH BELANJA", "", N0(ta), N0(tb), S0(tb - ta));
        doc.Blocks.Add(t.T);

        void PerRekening(string judul, IEnumerable<RincianAnggaran> a, IEnumerable<RincianAnggaran> b)
        {
            doc.Blocks.Add(Judul(judul));
            var ga = a.GroupBy(r => r.KodeRekening).ToDictionary(g => g.Key, g => g.Sum(r => r.Jumlah));
            var gb = b.GroupBy(r => r.KodeRekening).ToDictionary(g => g.Key, g => g.Sum(r => r.Jumlah));
            var tt = new Tabel(new double[] { 1.4, 4.6, 1.9, 1.9, 1.9 },
                new[] { "Kode", "Uraian", "Semula (Rp)", "Menjadi (Rp)", "Bertambah / (Berkurang)" },
                new[] { false, false, true, true, true });
            foreach (var k in ga.Keys.Union(gb.Keys).OrderBy(x => x, KodeComparer.Instance))
            {
                var x = ga.GetValueOrDefault(k); var y = gb.GetValueOrDefault(k);
                tt.Tambah(KodeRek(k), Lookup.NamaRekening(k), N0(x), N0(y), S0(y - x));
            }
            var sa = ga.Values.Sum(); var sb = gb.Values.Sum();
            tt.Tebal("", "JUMLAH", N0(sa), N0(sb), S0(sb - sa));
            doc.Blocks.Add(tt.T);
        }
        PerRekening("B. Perubahan Pendapatan", dSemula.Pendapatan, dMenjadi.Pendapatan);
        PerRekening("C. Perubahan Penerimaan Pembiayaan", dSemula.PenerimaanPembiayaan, dMenjadi.PenerimaanPembiayaan);
        PerRekening("D. Perubahan Pengeluaran Pembiayaan", dSemula.PengeluaranPembiayaan, dMenjadi.PengeluaranPembiayaan);
        TandaTangan(doc, desa);
        return doc;
    }

    // ------------------------------------------------------------ RAP / pembiayaan (susunan seperti cetakan Siskeudes)

    static void IsiRekening(FlowDocument doc, string judul1, IEnumerable<RincianAnggaran> daftar1, string? judul2, IEnumerable<RincianAnggaran>? daftar2)
    {
        var t = new Tabel(new double[] { 1.5, 4.4, 0.9, 1.4, 1.9, 2.1 },
            new[] { "Kode", "Uraian", "SD", "Volume", "Harga Satuan (Rp)", "Jumlah (Rp)" },
            new[] { false, false, false, true, true, true });
        void Bagian(string judul, IEnumerable<RincianAnggaran> items)
        {
            var list = items.ToList();
            t.Tebal("", judul, "", "", "", N0(list.Sum(r => r.Jumlah)));
            foreach (var obj in list.GroupBy(r => string.Join('.', r.KodeRekening.Split('.').Take(3))).OrderBy(g => g.Key, KodeComparer.Instance))
            {
                t.Tebal(KodeRek(obj.Key), Lookup.NamaRekening(obj.Key), "", "", "", N0(obj.Sum(r => r.Jumlah)));
                foreach (var rek in obj.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
                {
                    t.Tambah(KodeRek(rek.Key), Lookup.NamaRekening(rek.Key), "", "", "", N0(rek.Sum(r => r.Jumlah)));
                    int no = 1;
                    foreach (var r in rek)
                        t.Tambah("", $"{no++:00}. {r.Uraian}", r.SumberDana, $"{N(r.Volume)} {r.Satuan}", N(r.HargaSatuan), N0(r.Jumlah));
                }
            }
        }
        Bagian(judul1, daftar1);
        if (judul2 != null && daftar2 != null) Bagian(judul2, daftar2);
        doc.Blocks.Add(t.T);
    }

    // ------------------------------------------------------------ RAB

    static void IsiRab(FlowDocument doc, DataDesa desa, DataSkenario d)
    {
        if (d.Kegiatan.Count == 0) { doc.Blocks.Add(Teks("Belum ada kegiatan.")); return; }
        foreach (var keg in d.Kegiatan.OrderBy(x => x.Kode, KodeComparer.Instance))
        {
            doc.Blocks.Add(Judul($"{KodeLengkap(desa, keg.Kode)}  {keg.Nama}"));
            var info = new Paragraph { Margin = new Thickness(0, 0, 0, 6), FontSize = 12 };
            info.Inlines.Add(new Run($"Bidang: {keg.KodeSubBidang.Split('.')[0]} {Lookup.NamaBidang(keg.KodeSubBidang.Split('.')[0])}"));
            info.Inlines.Add(new LineBreak());
            info.Inlines.Add(new Run($"Sub Bidang: {keg.KodeSubBidang} {Lookup.NamaSubBidang(keg.KodeSubBidang)}"));
            info.Inlines.Add(new LineBreak());
            info.Inlines.Add(new Run($"Lokasi: {keg.Lokasi} · Waktu: {keg.Waktu} · Pelaksana: {keg.Pelaksana}"));
            info.Inlines.Add(new LineBreak());
            info.Inlines.Add(new Run($"Keluaran: {keg.Keluaran} ({N(keg.VolumeKeluaran)} {keg.SatuanKeluaran})"));
            doc.Blocks.Add(info);

            var t = new Tabel(new double[] { 1.5, 4.2, 1, 1.4, 1.9, 2.1, 0.9 },
                new[] { "Kode Rek.", "Uraian", "Vol", "Satuan", "Harga Satuan", "Jumlah (Rp)", "SD" },
                new[] { false, false, true, false, true, true, false });
            foreach (var p in keg.Paket)
            {
                t.Grup($"Paket: {p.Nama}  ·  {p.Pola}, {p.Sifat}  ·  Nilai Rp {N0(p.Total)}");
                foreach (var rek in p.Rincian.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
                {
                    t.Tebal(KodeRek(rek.Key), Lookup.NamaRekening(rek.Key), "", "", "", N0(rek.Sum(r => r.Jumlah)), "");
                    foreach (var r in rek)
                        t.Tambah("", r.Uraian, N(r.Volume), r.Satuan, N(r.HargaSatuan), N0(r.Jumlah),
                            string.IsNullOrWhiteSpace(r.SumberDana) ? p.SumberDana : r.SumberDana);
                }
            }
            t.Tebal("", "JUMLAH KEGIATAN", "", "", "", N0(keg.Total), "");
            doc.Blocks.Add(t.T);
            if (!string.IsNullOrWhiteSpace(keg.Catatan))
                doc.Blocks.Add(Teks("Catatan rapat: " + keg.Catatan, 12));
        }
    }

    // ------------------------------------------------------------ siap input

    static void IsiSiapInput(FlowDocument doc, DataDesa desa, DataSkenario d)
    {
        doc.Blocks.Add(Teks("Urutan mengikuti menu Siskeudes: Data Entri ▸ Penganggaran ▸ Isian Data Anggaran. " +
                            "Centang (✓) setiap baris yang sudah diketik ke Siskeudes.", 12));

        void Rincian(string judul, IEnumerable<RincianAnggaran> items)
        {
            doc.Blocks.Add(Judul(judul));
            var list = items.ToList();
            if (list.Count == 0) { doc.Blocks.Add(Teks("(tidak ada)")); return; }
            var t = new Tabel(new double[] { 0.5, 0.7, 4.2, 1, 1.4, 1.9, 2.1, 0.9 },
                new[] { "✓", "No", "Uraian", "Vol", "Satuan", "Harga Satuan", "Jumlah (Rp)", "SD" },
                new[] { false, false, false, true, false, true, true, false });
            foreach (var g in list.GroupBy(r => r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            {
                t.Grup($"Rekening {KodeRek(g.Key)} {Lookup.NamaRekening(g.Key)} — Rp {N0(g.Sum(r => r.Jumlah))}");
                int no = 1;
                foreach (var r in g)
                    t.Tambah("☐", (no++).ToString("00"), r.Uraian, N(r.Volume), r.Satuan, N(r.HargaSatuan), N0(r.Jumlah), r.SumberDana);
            }
            doc.Blocks.Add(t.T);
        }

        Rincian("1. Pendapatan (RAP & RAP Rinci)", d.Pendapatan);
        Rincian("2. Penerimaan Pembiayaan", d.PenerimaanPembiayaan);
        Rincian("3. Pengeluaran Pembiayaan", d.PengeluaranPembiayaan);

        doc.Blocks.Add(Judul("4. Kegiatan (tab Kegiatan)"));
        var k = new Tabel(new double[] { 0.5, 2.2, 4, 1.8, 1.4, 2.2, 2 },
            new[] { "✓", "Kd Kegiatan", "Nama Kegiatan", "Lokasi", "Waktu", "Pelaksana / Keluaran", "Pagu (Rp)" },
            new[] { false, false, false, false, false, false, true });
        foreach (var keg in d.Kegiatan.OrderBy(x => x.Kode, KodeComparer.Instance))
            k.Tambah("☐", KodeLengkap(desa, keg.Kode), keg.Nama, keg.Lokasi, keg.Waktu,
                $"{keg.Pelaksana} / {keg.Keluaran} ({N(keg.VolumeKeluaran)} {keg.SatuanKeluaran})", N0(keg.Total));
        doc.Blocks.Add(k.T);

        doc.Blocks.Add(Judul("5. Paket, RAB, dan RAB Rinci (tab Paket Kegiatan & RAB)"));
        foreach (var keg in d.Kegiatan.OrderBy(x => x.Kode, KodeComparer.Instance))
        {
            doc.Blocks.Add(new Paragraph(new Run($"{KodeLengkap(desa, keg.Kode)}  {keg.Nama}")) { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 4), KeepWithNext = true });
            var p = new Tabel(new double[] { 0.5, 0.6, 3.2, 1.9, 0.9, 1.6, 1.2, 1.4, 2.6 },
                new[] { "✓", "ID", "Nama Paket", "Nilai (Rp)", "SD", "Pola", "Sifat", "Target", "Uraian Output" },
                new[] { false, false, false, true, false, false, false, false, false });
            int id = 1;
            foreach (var pk in keg.Paket)
                p.Tambah("☐", (id++).ToString("00"), pk.Nama, N0(pk.Total), pk.SumberDana, pk.Pola, pk.Sifat, $"{N(pk.TargetOutput)} {pk.Satuan}", pk.UraianOutput);
            doc.Blocks.Add(p.T);

            var semua = keg.Paket.SelectMany(pk => pk.Rincian.Select(r => (pk, r))).ToList();
            if (semua.Count == 0) continue;
            var t = new Tabel(new double[] { 0.5, 0.7, 3.8, 1, 1.4, 1.9, 2.1, 0.9 },
                new[] { "✓", "No", "Uraian", "Vol", "Satuan", "Harga Satuan", "Jumlah (Rp)", "SD" },
                new[] { false, false, false, true, false, true, true, false });
            foreach (var g in semua.GroupBy(x => x.r.KodeRekening).OrderBy(g => g.Key, KodeComparer.Instance))
            {
                t.Grup($"RAB {KodeRek(g.Key)} {Lookup.NamaRekening(g.Key)} — Rp {N0(g.Sum(x => x.r.Jumlah))}");
                int no = 1;
                foreach (var (pk, r) in g)
                    t.Tambah("☐", (no++).ToString("00"), r.Uraian, N(r.Volume), r.Satuan, N(r.HargaSatuan), N0(r.Jumlah),
                        string.IsNullOrWhiteSpace(r.SumberDana) ? pk.SumberDana : r.SumberDana);
            }
            doc.Blocks.Add(t.T);
        }
    }
}
