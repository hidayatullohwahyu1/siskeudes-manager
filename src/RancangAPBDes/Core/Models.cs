using System.Text.Json.Serialization;

namespace RancangAPBDes.Core;

static class Batas
{
    public const decimal MaksVolume = 1_000_000_000m;          // 1 miliar satuan
    public const decimal MaksHarga = 100_000_000_000_000m;      // 100 triliun
    public static decimal Volume(decimal v) => Math.Clamp(v, 0m, MaksVolume);
    public static decimal Harga(decimal v) => Math.Clamp(v, 0m, MaksHarga);
    public static string Kode(string? k) => (k ?? "").Trim().TrimEnd('.').Trim();

    /// <summary>Format kode bidang/sub bidang/kegiatan seperti cetakan Siskeudes: "01.01.1" → "1.1.01".</summary>
    public static string KodeKeg(string? k)
    {
        var s = Kode(k);
        if (s == "") return s;
        var p = s.Split('.');
        for (int i = 0; i < p.Length; i++)
        {
            if (!int.TryParse(p[i], out var n)) continue;
            p[i] = i < 2 ? n.ToString() : n.ToString("00");
        }
        return string.Join('.', p);
    }

    /// <summary>Kode cocok dengan awalan per segmen ("2.3" cocok dengan "2.3" dan "2.3.01", tidak dengan "2.31").</summary>
    public static bool Cocok(string kode, string awalan) =>
        awalan == "" || kode == awalan || kode.StartsWith(awalan + ".", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Satu baris rincian anggaran (RAP Rinci / RAB Rinci / Pembiayaan).</summary>
public class RincianAnggaran : Model
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _kode = "";
    public string KodeRekening
    {
        get => _kode;
        set { if (Set(ref _kode, Batas.Kode(value))) OnPropertyChanged(nameof(NamaRekening)); }
    }
    [JsonIgnore] public string NamaRekening => Lookup.NamaRekening(_kode);

    string _uraian = "";
    public string Uraian { get => _uraian; set => Set(ref _uraian, value ?? ""); }

    decimal _vol = 1;
    public decimal Volume
    {
        get => _vol;
        set { if (Set(ref _vol, Batas.Volume(value))) OnPropertyChanged(nameof(Jumlah)); }
    }

    string _sat = "";
    public string Satuan { get => _sat; set => Set(ref _sat, (value ?? "").Trim()); }

    decimal _harga;
    public decimal HargaSatuan
    {
        get => _harga;
        set { if (Set(ref _harga, Batas.Harga(value))) OnPropertyChanged(nameof(Jumlah)); }
    }

    [JsonIgnore] public decimal Jumlah => Math.Round(_vol * _harga, 2, MidpointRounding.AwayFromZero);

    string _sd = "";
    public string SumberDana { get => _sd; set => Set(ref _sd, (value ?? "").Trim()); }

    string _ket = "";
    public string Keterangan { get => _ket; set => Set(ref _ket, value ?? ""); }

    public void RefreshNama() => OnPropertyChanged(nameof(NamaRekening));

    int _no;
    /// <summary>Nomor urut tampilan di dalam kelompok rekening (tidak disimpan).</summary>
    [JsonIgnore] public int No { get => _no; set { _no = value; OnPropertyChanged(); } }

    public RincianAnggaran Salin() => new()
    {
        KodeRekening = KodeRekening, Uraian = Uraian, Volume = Volume, Satuan = Satuan,
        HargaSatuan = HargaSatuan, SumberDana = SumberDana, Keterangan = Keterangan
    };
}

public class Paket : Model
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }

    string _sd = "";
    /// <summary>Mengubah sumber dana paket ikut mengubah rincian yang sumber dananya sama/kosong.</summary>
    public string SumberDana
    {
        get => _sd;
        set
        {
            var lama = _sd;
            if (!Set(ref _sd, (value ?? "").Trim())) return;
            if (ChangeTracker.IsSuspended) return;
            foreach (var r in Rincian)
                if (r.SumberDana == lama || r.SumberDana == "") r.SumberDana = _sd;
        }
    }

    string _pola = "Swakelola";
    public string Pola { get => _pola; set => Set(ref _pola, value ?? ""); }

    string _sifat = "Non Fisik";
    public string Sifat { get => _sifat; set => Set(ref _sifat, value ?? ""); }

    decimal _target = 1;
    public decimal TargetOutput { get => _target; set => Set(ref _target, Batas.Volume(value)); }

    string _satuan = "";
    public string Satuan { get => _satuan; set => Set(ref _satuan, (value ?? "").Trim()); }

    string _lokasi = "";
    public string Lokasi { get => _lokasi; set => Set(ref _lokasi, value ?? ""); }

    string _uraianOutput = "";
    public string UraianOutput { get => _uraianOutput; set => Set(ref _uraianOutput, value ?? ""); }

    string _pktd = "Non PKTD";
    /// <summary>Padat Karya Tunai Desa: "PKTD" atau "Non PKTD".</summary>
    public string Pktd { get => _pktd; set => Set(ref _pktd, string.IsNullOrWhiteSpace(value) ? "Non PKTD" : value); }

    public TrackedCollection<RincianAnggaran> Rincian { get; set; } = new();

    [JsonIgnore] public decimal Total => Rincian.Sum(r => r.Jumlah);

    int _no;
    /// <summary>Nomor urut tampilan (tidak disimpan).</summary>
    [JsonIgnore] public int No { get => _no; set { _no = value; OnPropertyChanged(); } }

    public void RaiseTotals() => OnPropertyChanged(nameof(Total));

    public Paket Salin()
    {
        var p = new Paket
        {
            Nama = Nama, Pola = Pola, Sifat = Sifat, TargetOutput = TargetOutput,
            Satuan = Satuan, Lokasi = Lokasi, UraianOutput = UraianOutput, Pktd = Pktd
        };
        p._sd = _sd;
        foreach (var r in Rincian) p.Rincian.Add(r.Salin());
        return p;
    }
}

public class Kegiatan : Model
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _sub = "";
    public string KodeSubBidang
    {
        get => _sub;
        set { if (Set(ref _sub, Batas.KodeKeg(value))) OnPropertyChanged(nameof(Kode)); }
    }

    int _no = 1;
    public int NoUrut
    {
        get => _no;
        set { if (Set(ref _no, Math.Clamp(value, 0, 99))) OnPropertyChanged(nameof(Kode)); }
    }

    [JsonIgnore] public string Kode => $"{_sub}.{_no:00}";

    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }

    string _lokasi = "";
    public string Lokasi { get => _lokasi; set => Set(ref _lokasi, value ?? ""); }

    string _waktu = "12 Bulan";
    public string Waktu { get => _waktu; set => Set(ref _waktu, value ?? ""); }

    string _pelaksana = "";
    public string Pelaksana { get => _pelaksana; set => Set(ref _pelaksana, value ?? ""); }

    string _keluaran = "";
    public string Keluaran { get => _keluaran; set => Set(ref _keluaran, value ?? ""); }

    decimal _volKeluaran = 1;
    public decimal VolumeKeluaran { get => _volKeluaran; set => Set(ref _volKeluaran, Batas.Volume(value)); }

    string _satKeluaran = "";
    public string SatuanKeluaran { get => _satKeluaran; set => Set(ref _satKeluaran, (value ?? "").Trim()); }

    string _catatan = "";
    public string Catatan { get => _catatan; set => Set(ref _catatan, value ?? ""); }

    public TrackedCollection<Paket> Paket { get; set; } = new();

    [JsonIgnore] public decimal Total => Paket.Sum(p => p.Total);

    public void RaiseTotals()
    {
        foreach (var p in Paket) p.RaiseTotals();
        OnPropertyChanged(nameof(Total));
    }

    public Kegiatan Salin()
    {
        var k = new Kegiatan
        {
            KodeSubBidang = KodeSubBidang, NoUrut = NoUrut, Nama = Nama, Lokasi = Lokasi, Waktu = Waktu,
            Pelaksana = Pelaksana, Keluaran = Keluaran, VolumeKeluaran = VolumeKeluaran,
            SatuanKeluaran = SatuanKeluaran, Catatan = Catatan
        };
        foreach (var p in Paket) k.Paket.Add(p.Salin());
        return k;
    }
}

/// <summary>Seluruh isi satu skenario anggaran.</summary>
public class DataSkenario : Model
{
    public TrackedCollection<RincianAnggaran> Pendapatan { get; set; } = new();
    public TrackedCollection<RincianAnggaran> PenerimaanPembiayaan { get; set; } = new();
    public TrackedCollection<RincianAnggaran> PengeluaranPembiayaan { get; set; } = new();
    public TrackedCollection<Kegiatan> Kegiatan { get; set; } = new();
    /// <summary>Bidang & sub bidang yang ditambahkan desa (seperti tab Bidang/Sub Bidang di Siskeudes).</summary>
    public TrackedCollection<string> BidangDipakai { get; set; } = new();
    public TrackedCollection<string> SubBidangDipakai { get; set; } = new();
    /// <summary>Anggaran Kas Desa: rencana per bulan untuk tiap kegiatan dan rekening pendapatan/pembiayaan.</summary>
    public TrackedCollection<BarisKas> AnggaranKas { get; set; } = new();

    public BarisKas Kas(string kunci)
    {
        var b = AnggaranKas.FirstOrDefault(x => x.Kunci == kunci);
        if (b == null) { b = new BarisKas { Kunci = kunci }; AnggaranKas.Add(b); }
        b.Pastikan12();
        return b;
    }

    /// <summary>Bidang yang tampil: yang ditambahkan + yang sudah punya kegiatan.</summary>
    public List<string> DaftarBidang() =>
        BidangDipakai.Union(SubBidangDipakai.Select(s => s.Split('.')[0])).Union(Kegiatan.Select(k => k.KodeSubBidang.Split('.')[0]))
            .Where(s => s != "").Distinct().OrderBy(s => s, KodeComparer.Instance).ToList();

    public List<string> DaftarSubBidang(string bidang) =>
        SubBidangDipakai.Union(Kegiatan.Select(k => k.KodeSubBidang))
            .Where(s => s.Split('.')[0] == bidang).Distinct().OrderBy(s => s, KodeComparer.Instance).ToList();

    string _catatan = "";
    public string Catatan { get => _catatan; set => Set(ref _catatan, value ?? ""); }

    public void RaiseTotals()
    {
        foreach (var k in Kegiatan) k.RaiseTotals();
    }

    public IEnumerable<RincianAnggaran> SemuaRincian()
    {
        foreach (var r in Pendapatan) yield return r;
        foreach (var r in PenerimaanPembiayaan) yield return r;
        foreach (var r in PengeluaranPembiayaan) yield return r;
        foreach (var k in Kegiatan)
            foreach (var p in k.Paket)
                foreach (var r in p.Rincian) yield return r;
    }
}

public class SkenarioInfo : Bindable
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Tahun { get; set; }

    string _nama = "";
    public string Nama { get => _nama; set { if (Set(ref _nama, value ?? "")) OnPropertyChanged(nameof(Tampil)); } }

    string _status = "Draft";
    public string Status
    {
        get => _status;
        set
        {
            if (!Set(ref _status, value ?? "Draft")) return;
            OnPropertyChanged(nameof(IsFinal));
            OnPropertyChanged(nameof(Tampil));
        }
    }

    public DateTime Dibuat { get; set; } = DateTime.Now;

    DateTime _diubah = DateTime.Now;
    public DateTime Diubah { get => _diubah; set => Set(ref _diubah, value); }

    public bool IsFinal => _status == "Final";
    public string Tampil => IsFinal ? $"{Nama}  [FINAL]" : Nama;
    public string TampilLengkap => $"{Tahun} – {Tampil}";
}

public class Perangkat : Model
{
    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
    string _jabatan = "";
    public string Jabatan { get => _jabatan; set => Set(ref _jabatan, value ?? ""); }
}

public class DataDesa : Model
{
    string _nama = "";
    public string NamaDesa { get => _nama; set => Set(ref _nama, value ?? ""); }
    string _kode = "";
    /// <summary>Kode desa seperti di Siskeudes, mis. 04.2002</summary>
    public string KodeDesa { get => _kode; set => Set(ref _kode, Batas.Kode(value)); }
    string _kec = "";
    public string Kecamatan { get => _kec; set => Set(ref _kec, value ?? ""); }
    string _kab = "";
    public string Kabupaten { get => _kab; set => Set(ref _kab, value ?? ""); }
    string _kades = "";
    public string KepalaDesa { get => _kades; set => Set(ref _kades, value ?? ""); }
    string _sekdes = "";
    public string Sekdes { get => _sekdes; set => Set(ref _sekdes, value ?? ""); }
    string _kaur = "";
    public string KaurKeuangan { get => _kaur; set => Set(ref _kaur, value ?? ""); }
    string _tempat = "";
    /// <summary>Tempat penandatanganan untuk laporan cetak.</summary>
    public string TempatTtd { get => _tempat; set => Set(ref _tempat, value ?? ""); }

    public TrackedCollection<Perangkat> Perangkat { get; set; } = new();
}

// ---------------- Referensi ----------------

public class RefRekening : Model
{
    string _kode = "";
    public string Kode { get => _kode; set { if (Set(ref _kode, Batas.Kode(value))) OnPropertyChanged(nameof(Tampil)); } }
    string _nama = "";
    public string Nama { get => _nama; set { if (Set(ref _nama, value ?? "")) OnPropertyChanged(nameof(Tampil)); } }
    [JsonIgnore] public string Tampil => $"{Kode}  {Nama}";
}

public class RefSumberDana : Model
{
    string _kode = "";
    public string Kode { get => _kode; set { if (Set(ref _kode, (value ?? "").Trim().ToUpperInvariant())) OnPropertyChanged(nameof(Tampil)); } }
    string _nama = "";
    public string Nama { get => _nama; set { if (Set(ref _nama, value ?? "")) OnPropertyChanged(nameof(Tampil)); } }
    [JsonIgnore] public string Tampil => $"{Kode} – {Nama}";
}

public class RefBidang : Model
{
    string _kode = "";
    public string Kode { get => _kode; set => Set(ref _kode, Batas.KodeKeg(value)); }
    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
}

public class RefSubBidang : Model
{
    string _kode = "";
    public string Kode { get => _kode; set { if (Set(ref _kode, Batas.KodeKeg(value))) OnPropertyChanged(nameof(Tampil)); } }
    string _nama = "";
    public string Nama { get => _nama; set { if (Set(ref _nama, value ?? "")) OnPropertyChanged(nameof(Tampil)); } }
    [JsonIgnore] public string KodeBidang => Kode.Split('.')[0];
    [JsonIgnore] public string Tampil => $"{Kode}  {Nama}";
}

public class RefKegiatan : Model
{
    string _kode = "";
    public string Kode { get => _kode; set => Set(ref _kode, Batas.KodeKeg(value)); }
    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
    [JsonIgnore]
    public string KodeSubBidang
    {
        get
        {
            var i = Kode.LastIndexOf('.');
            return i > 0 ? Kode[..i] : "";
        }
    }
}

public class RefSatuan : Model
{
    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, (value ?? "").Trim()); }
}

public class AturanBatas : Model
{
    bool _aktif = true;
    public bool Aktif { get => _aktif; set => Set(ref _aktif, value); }
    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
    string _jenis = "Maksimal";
    /// <summary>"Maksimal" atau "Minimal"</summary>
    public string Jenis { get => _jenis; set => Set(ref _jenis, value ?? "Maksimal"); }
    decimal _persen;
    public decimal Persen { get => _persen; set => Set(ref _persen, Math.Clamp(value, 0m, 100m)); }
    string _sub = "";
    public string AwalanSubBidang { get => _sub; set => Set(ref _sub, Batas.KodeKeg(value)); }
    string _rek = "";
    public string AwalanRekening { get => _rek; set => Set(ref _rek, Batas.Kode(value)); }
    string _sd = "";
    public string SumberDana { get => _sd; set => Set(ref _sd, (value ?? "").Trim().ToUpperInvariant()); }
    string _dasar = "Total Belanja";
    /// <summary>"Total Belanja" atau "Pagu Sumber Dana"</summary>
    public string Dasar { get => _dasar; set => Set(ref _dasar, value ?? "Total Belanja"); }
}

public class Referensi : Model
{
    public TrackedCollection<RefRekening> Rekening { get; set; } = new();
    public TrackedCollection<RefSumberDana> SumberDana { get; set; } = new();
    public TrackedCollection<RefBidang> Bidang { get; set; } = new();
    public TrackedCollection<RefSubBidang> SubBidang { get; set; } = new();
    public TrackedCollection<RefKegiatan> Kegiatan { get; set; } = new();
    public TrackedCollection<RefSatuan> Satuan { get; set; } = new();
    public TrackedCollection<AturanBatas> Aturan { get; set; } = new();
}

/// <summary>Pencarian nama berdasarkan kode untuk tampilan.</summary>
public static class Lookup
{
    static Dictionary<string, string> _rek = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> _sd = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> _sub = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> _bid = new(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, string> _keg = new(StringComparer.OrdinalIgnoreCase);

    public static void Bangun(Referensi r)
    {
        _rek = Kamus(r.Rekening.Select(x => (x.Kode, x.Nama)));
        _sd = Kamus(r.SumberDana.Select(x => (x.Kode, x.Nama)));
        _sub = Kamus(r.SubBidang.Select(x => (x.Kode, x.Nama)));
        _bid = Kamus(r.Bidang.Select(x => (x.Kode, x.Nama)));
        _keg = Kamus(r.Kegiatan.Select(x => (x.Kode, x.Nama)));
    }

    static Dictionary<string, string> Kamus(IEnumerable<(string k, string n)> items)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, n) in items)
            if (!string.IsNullOrWhiteSpace(k)) d[k] = n;
        return d;
    }

    public static bool AdaRekening(string k) => _rek.ContainsKey(Batas.Kode(k));
    public static bool AdaKegiatan(string k) => _keg.ContainsKey(Batas.KodeKeg(k));
    public static bool AdaSumberDana(string k) => _sd.ContainsKey((k ?? "").Trim());

    public static string NamaRekening(string k)
    {
        if (string.IsNullOrWhiteSpace(k)) return "";
        return _rek.TryGetValue(Batas.Kode(k), out var n) ? n : "(kode tidak ada di referensi)";
    }

    public static string NamaSumberDana(string k) =>
        string.IsNullOrWhiteSpace(k) ? "(belum dipilih)" : _sd.TryGetValue(k.Trim(), out var n) ? n : "(tidak ada di referensi)";

    public static string NamaSubBidang(string k) => _sub.TryGetValue(Batas.Kode(k), out var n) ? n : "";
    public static string NamaBidang(string k) => _bid.TryGetValue(Batas.Kode(k), out var n) ? n : "";
}
