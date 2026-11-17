using System.Text.Json.Serialization;

namespace RancangAPBDes.Core;

/// <summary>Perencanaan desa (RPJMDes & RKPDes). Berlaku untuk seluruh file, lintas tahun dan skenario.</summary>
public class DataPerencanaan : Model
{
    int _awal = DateTime.Today.Year;
    public int TahunAwal { get => _awal; set { if (Set(ref _awal, Math.Clamp(value, 2000, 2100))) OnPropertyChanged(nameof(Periode)); } }

    int _akhir = DateTime.Today.Year + 5;
    public int TahunAkhir { get => _akhir; set { if (Set(ref _akhir, Math.Clamp(value, 2000, 2100))) OnPropertyChanged(nameof(Periode)); } }

    string _visi = "";
    public string Visi { get => _visi; set => Set(ref _visi, value ?? ""); }

    string _nomorPerdes = "";
    public string NomorPerdes { get => _nomorPerdes; set => Set(ref _nomorPerdes, value ?? ""); }

    [JsonIgnore] public string Periode => $"{_awal} – {_akhir}";

    public TrackedCollection<Misi> Misi { get; set; } = new();
    public TrackedCollection<KegiatanRpjm> KegiatanRpjm { get; set; } = new();
    public TrackedCollection<PaguIndikatif> Pagu { get; set; } = new();
    public TrackedCollection<KegiatanRkp> KegiatanRkp { get; set; } = new();
}

public class Misi : Model
{
    int _no = 1;
    public int No { get => _no; set => Set(ref _no, Math.Clamp(value, 1, 99)); }
    string _uraian = "";
    public string Uraian { get => _uraian; set => Set(ref _uraian, value ?? ""); }
}

/// <summary>Kegiatan dalam RPJMDes (rencana enam/delapan tahun).</summary>
public class KegiatanRpjm : Model
{
    public Guid Id { get; set; } = Guid.NewGuid();

    string _sub = "";
    public string KodeSubBidang { get => _sub; set { if (Set(ref _sub, Batas.KodeKeg(value))) OnPropertyChanged(nameof(Kode)); } }
    int _no = 1;
    public int NoUrut { get => _no; set { if (Set(ref _no, Math.Clamp(value, 0, 99))) OnPropertyChanged(nameof(Kode)); } }
    [JsonIgnore] public string Kode => $"{_sub}.{_no:00}";

    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
    string _lokasi = "";
    public string Lokasi { get => _lokasi; set => Set(ref _lokasi, value ?? ""); }
    string _keluaran = "";
    public string Keluaran { get => _keluaran; set => Set(ref _keluaran, value ?? ""); }
    decimal _vol = 1;
    public decimal Volume { get => _vol; set => Set(ref _vol, Batas.Volume(value)); }
    string _sat = "";
    public string Satuan { get => _sat; set => Set(ref _sat, (value ?? "").Trim()); }
    string _sasaran = "";
    /// <summary>Penerima manfaat, mis. "120 KK".</summary>
    public string SasaranManfaat { get => _sasaran; set => Set(ref _sasaran, value ?? ""); }
    string _tahun = "";
    /// <summary>Tahun pelaksanaan dipisah koma, mis. "2027, 2029".</summary>
    public string TahunPelaksanaan { get => _tahun; set => Set(ref _tahun, value ?? ""); }
    decimal _biaya;
    public decimal PerkiraanBiaya { get => _biaya; set => Set(ref _biaya, Batas.Harga(value)); }
    string _sd = "";
    public string SumberDana { get => _sd; set => Set(ref _sd, (value ?? "").Trim()); }
    string _pola = "Swakelola";
    public string Pola { get => _pola; set => Set(ref _pola, value ?? ""); }
    string _asal = "";
    /// <summary>Asal usulan: dusun / RT / lembaga.</summary>
    public string AsalUsulan { get => _asal; set => Set(ref _asal, value ?? ""); }

    public IEnumerable<int> DaftarTahun() =>
        _tahun.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
              .Select(t => int.TryParse(t, out var x) ? x : 0).Where(x => x > 0).Distinct().OrderBy(x => x);
}

/// <summary>Pagu indikatif desa per tahun & sumber dana (dari pemerintah kabupaten).</summary>
public class PaguIndikatif : Model
{
    int _tahun = DateTime.Today.Year + 1;
    public int Tahun { get => _tahun; set => Set(ref _tahun, Math.Clamp(value, 2000, 2100)); }
    string _sd = "";
    public string SumberDana { get => _sd; set { if (Set(ref _sd, (value ?? "").Trim().ToUpperInvariant())) OnPropertyChanged(nameof(NamaSumberDana)); } }
    [JsonIgnore] public string NamaSumberDana => Lookup.NamaSumberDana(_sd);
    decimal _nilai;
    public decimal Nilai { get => _nilai; set => Set(ref _nilai, Batas.Harga(value)); }
    string _ket = "";
    public string Keterangan { get => _ket; set => Set(ref _ket, value ?? ""); }

    [JsonIgnore] public decimal Terpakai { get; set; }
    [JsonIgnore] public decimal Sisa => _nilai - Terpakai;
}

/// <summary>Kegiatan RKPDes satu tahun.</summary>
public class KegiatanRkp : Model
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? IdRpjm { get; set; }

    int _tahun = DateTime.Today.Year + 1;
    public int Tahun { get => _tahun; set => Set(ref _tahun, Math.Clamp(value, 2000, 2100)); }
    string _sub = "";
    public string KodeSubBidang { get => _sub; set { if (Set(ref _sub, Batas.KodeKeg(value))) OnPropertyChanged(nameof(Kode)); } }
    int _no = 1;
    public int NoUrut { get => _no; set { if (Set(ref _no, Math.Clamp(value, 0, 99))) OnPropertyChanged(nameof(Kode)); } }
    [JsonIgnore] public string Kode => $"{_sub}.{_no:00}";

    string _nama = "";
    public string Nama { get => _nama; set => Set(ref _nama, value ?? ""); }
    string _lokasi = "";
    public string Lokasi { get => _lokasi; set => Set(ref _lokasi, value ?? ""); }
    string _keluaran = "";
    public string Keluaran { get => _keluaran; set => Set(ref _keluaran, value ?? ""); }
    decimal _vol = 1;
    public decimal Volume { get => _vol; set => Set(ref _vol, Batas.Volume(value)); }
    string _sat = "";
    public string Satuan { get => _sat; set => Set(ref _sat, (value ?? "").Trim()); }
    string _sasaran = "";
    public string SasaranManfaat { get => _sasaran; set => Set(ref _sasaran, value ?? ""); }
    string _waktu = "12 Bulan";
    public string Waktu { get => _waktu; set => Set(ref _waktu, value ?? ""); }
    decimal _pagu;
    public decimal PaguIndikatif { get => _pagu; set => Set(ref _pagu, Batas.Harga(value)); }
    string _sd = "";
    public string SumberDana { get => _sd; set => Set(ref _sd, (value ?? "").Trim()); }
    string _pola = "Swakelola";
    public string Pola { get => _pola; set => Set(ref _pola, value ?? ""); }
    string _pelaksana = "";
    public string Pelaksana { get => _pelaksana; set => Set(ref _pelaksana, value ?? ""); }
    int _prioritas = 1;
    public int Prioritas { get => _prioritas; set => Set(ref _prioritas, Math.Clamp(value, 1, 9)); }
    string _status = "Disepakati";
    /// <summary>"Disepakati" (masuk APBDes) atau "Cadangan".</summary>
    public string Status { get => _status; set => Set(ref _status, string.IsNullOrWhiteSpace(value) ? "Disepakati" : value); }
}

/// <summary>Rencana anggaran kas per bulan untuk satu kegiatan (belanja) atau rekening (pendapatan/pembiayaan).</summary>
public class BarisKas : Model
{
    /// <summary>"K:{id kegiatan}" atau "R:{kode rekening}".</summary>
    public string Kunci { get; set; } = "";
    public TrackedCollection<decimal> Bulan { get; set; } = new(Enumerable.Repeat(0m, 12));

    public void Pastikan12()
    {
        while (Bulan.Count < 12) Bulan.Add(0);
        while (Bulan.Count > 12) Bulan.RemoveAt(Bulan.Count - 1);
    }
}
