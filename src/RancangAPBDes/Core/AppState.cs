using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace RancangAPBDes.Core;

/// <summary>Keadaan aplikasi: file yang dibuka, skenario aktif, simpan otomatis, batalkan/ulangi.</summary>
public sealed class AppState : Bindable
{
    public static AppState Instance { get; } = new();

    /// <summary>Mode uji otomatis: tidak mengubah pengaturan pengguna dan tidak membuat cadangan.</summary>
    public static bool ModeUji { get; set; }

    static readonly JsonSerializerOptions JsonOpsi = new() { WriteIndented = false };

    Database? _db;
    readonly DispatcherTimer _timerSimpan;
    readonly DispatcherTimer _timerHitung;
    string _lastDesa = "", _lastRef = "", _lastData = "";
    readonly List<string> _undo = new();
    readonly List<string> _redo = new();
    bool _galatDitampilkan;
    bool _sedangIsi;

    public Pengaturan Pengaturan { get; } = Pengaturan.Muat();

    /// <summary>Dipanggil sebelum data skenario diganti (tampilan harus mengakhiri edit).</summary>
    public event Action? SebelumDataDiganti;
    /// <summary>Data skenario diganti total (ganti skenario, batalkan, ulangi).</summary>
    public event Action? DataDiganti;
    /// <summary>Hasil hitungan diperbarui.</summary>
    public event Action? HasilBerubah;
    /// <summary>Referensi dimuat ulang.</summary>
    public event Action? ReferensiBerubah;

    AppState()
    {
        _timerSimpan = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _timerSimpan.Tick += (_, _) => { _timerSimpan.Stop(); SimpanSekarang(); };
        _timerHitung = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timerHitung.Tick += (_, _) => { _timerHitung.Stop(); Hitung(); };
        ChangeTracker.Changed += SaatBerubah;
    }

    // ------------------------------------------------------------------ properti

    string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        private set
        {
            if (!Set(ref _filePath, value)) return;
            OnPropertyChanged(nameof(NamaFile));
            OnPropertyChanged(nameof(NamaFileSingkat));
        }
    }
    public string NamaFile => _filePath == null ? "(belum ada file)" : _filePath;
    public string NamaFileSingkat => _filePath == null ? "(belum ada file)" : "File: " + Path.GetFileName(_filePath);

    DataDesa _desa = new();
    public DataDesa Desa { get => _desa; private set => Set(ref _desa, value); }

    Referensi _ref = DefaultReferensi.Buat();
    public Referensi Referensi { get => _ref; private set => Set(ref _ref, value); }

    public ObservableCollection<SkenarioInfo> SemuaSkenario { get; } = new();
    public ObservableCollection<SkenarioInfo> SkenarioTahunIni { get; } = new();
    public ObservableCollection<int> DaftarTahun { get; } = new();

    // Daftar untuk pilihan (combo box)
    public ObservableCollection<RefRekening> RekeningPendapatan { get; } = new();
    public ObservableCollection<RefRekening> RekeningBelanja { get; } = new();
    public ObservableCollection<RefRekening> RekeningPenerimaanPembiayaan { get; } = new();
    public ObservableCollection<RefRekening> RekeningPengeluaranPembiayaan { get; } = new();
    public ObservableCollection<RefSumberDana> DaftarSumberDana { get; } = new();
    public ObservableCollection<string> DaftarSatuan { get; } = new();
    public ObservableCollection<string> DaftarPelaksana { get; } = new();

    int _tahun;
    public int TahunAktif
    {
        get => _tahun;
        set
        {
            if (_sedangIsi && value == 0) return;
            if (_tahun == value) return;
            _tahun = value;
            OnPropertyChanged();
            IsiSkenarioTahun(null);
        }
    }

    SkenarioInfo? _aktif;
    public SkenarioInfo? SkenarioAktif
    {
        get => _aktif;
        set
        {
            if (_sedangIsi && value == null) return;
            if (ReferenceEquals(_aktif, value)) return;
            MuatSkenario(value);
        }
    }

    DataSkenario? _data;
    public DataSkenario? Data { get => _data; private set => Set(ref _data, value); }

    HasilHitung? _hasil;
    public HasilHitung? Hasil { get => _hasil; private set => Set(ref _hasil, value); }

    public bool BisaUbah => _aktif != null && !_aktif.IsFinal && _data != null;
    public bool TidakBisaUbah => !BisaUbah;
    public bool SkenarioFinal => _aktif?.IsFinal == true;

    string _status = "";
    public string StatusSimpan { get => _status; private set => Set(ref _status, value); }

    bool _gagal;
    public bool GagalSimpan { get => _gagal; private set => Set(ref _gagal, value); }

    public bool BisaBatalkan => _undo.Count > 0 && BisaUbah;
    public bool BisaUlangi => _redo.Count > 0 && BisaUbah;

    // ------------------------------------------------------------------ serialisasi

    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, JsonOpsi);

    public static T? Deserialize<T>(string json) where T : class
    {
        using (ChangeTracker.Suspend())
            return JsonSerializer.Deserialize<T>(json, JsonOpsi);
    }

    public static T SalinDalam<T>(T obj) where T : class => Deserialize<T>(Serialize(obj))!;

    /// <summary>Salinan lengkap objek apa pun (untuk form isian).</summary>
    public static object SalinObjek(object obj)
    {
        var t = obj.GetType();
        using (ChangeTracker.Suspend())
            return JsonSerializer.Deserialize(JsonSerializer.Serialize(obj, t, JsonOpsi), t, JsonOpsi)!;
    }

    // ------------------------------------------------------------------ file

    public bool BukaFile(string path)
    {
        Database db;
        try
        {
            db = Database.Open(path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Buka file " + path);
            Pesan.Galat($"File tidak dapat dibuka:\n{path}\n\nPastikan file ini adalah file data Rancang APBDes (.apbdes) dan tidak sedang dipakai program lain.", ex);
            return false;
        }

        TutupFile();
        _db = db;
        FilePath = path;

        DataDesa desa;
        Referensi refr;
        try
        {
            var j = db.GetMeta("desa");
            desa = (j == null ? null : Deserialize<DataDesa>(j)) ?? new DataDesa();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Muat data desa");
            Pesan.Galat("Data desa di file ini tidak terbaca. Data desa diisi ulang dari awal.", ex);
            desa = new DataDesa();
        }
        try
        {
            var j = db.GetMeta("referensi");
            refr = (j == null ? null : Deserialize<Referensi>(j)) ?? DefaultReferensi.Buat();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Muat referensi");
            Pesan.Galat("Referensi di file ini tidak terbaca. Referensi dikembalikan ke bawaan.", ex);
            refr = DefaultReferensi.Buat();
        }

        // Versi 1 memakai kode contoh yang keliru (01.01, 5.1.2.01, …): ganti dengan referensi resmi.
        try
        {
            if (db.GetMeta("versi_referensi") != "2")
            {
                var baru = DefaultReferensi.Buat();
                using (ChangeTracker.Suspend())
                {
                    refr.Bidang = baru.Bidang;
                    refr.SubBidang = baru.SubBidang;
                    refr.Rekening = baru.Rekening;
                    refr.Kegiatan = baru.Kegiatan;
                    DefaultReferensi.Lengkapi(refr);
                }
                db.SetMeta("versi_referensi", "2");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migrasi referensi");
        }

        Desa = desa;
        Referensi = refr;
        _lastDesa = Serialize(desa);
        _lastRef = Serialize(refr);
        try
        {
            db.InTransaction(() =>
            {
                db.SetMeta("desa", _lastDesa);
                db.SetMeta("referensi", _lastRef);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan awal meta");
        }
        PerbaruiDaftarReferensi();

        SemuaSkenario.Clear();
        List<SkenarioInfo> daftar;
        try { daftar = db.DaftarSkenario(); }
        catch (Exception ex) { Log.Error(ex, "Daftar skenario"); daftar = new(); }
        foreach (var s in daftar) SemuaSkenario.Add(s);

        if (SemuaSkenario.Count == 0)
        {
            var tahun = DateTime.Today.Month >= 6 ? DateTime.Today.Year + 1 : DateTime.Today.Year;
            var info = new SkenarioInfo { Tahun = tahun, Nama = "Rancangan Awal" };
            try
            {
                db.TambahSkenario(info, Serialize(new DataSkenario()));
                SemuaSkenario.Add(info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Skenario awal");
            }
        }

        _tahun = 0;
        IsiDaftarTahun();
        var tahunPilih = DaftarTahun.Count > 0 ? DaftarTahun.Max() : DateTime.Today.Year;
        PilihTahunDanSkenario(tahunPilih, null);

        if (!ModeUji)
        {
            Cadangan.Otomatis(db);
            Pengaturan.FileTerakhir = path;
            Pengaturan.Simpan();
        }
        StatusSimpan = "File dibuka. Perubahan disimpan otomatis.";
        GagalSimpan = false;
        _galatDitampilkan = false;
        Log.Info("Buka file " + path);
        return true;
    }

    /// <summary>Membuat file data baru. File lama dengan nama sama akan ditimpa.</summary>
    public bool BuatFileBaru(string path)
    {
        try
        {
            if (string.Equals(path, _filePath, StringComparison.OrdinalIgnoreCase)) TutupFile();
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Hapus file lama");
            Pesan.Galat("File lama tidak dapat ditimpa (mungkin sedang dibuka program lain).", ex);
            return false;
        }
        return BukaFile(path);
    }

    /// <summary>Menutup file. Mengembalikan false bila ada data yang gagal disimpan.</summary>
    public bool TutupFile()
    {
        if (_db == null) return true;
        var ok = SimpanSegera();
        SebelumDataDiganti?.Invoke();
        try { _db.Dispose(); } catch (Exception ex) { Log.Error(ex, "Tutup db"); }
        _db = null;
        _aktif = null;
        Data = null;
        Hasil = null;
        FilePath = null;
        _undo.Clear();
        _redo.Clear();
        SemuaSkenario.Clear();
        SkenarioTahunIni.Clear();
        DaftarTahun.Clear();
        RaiseStatusEdit();
        return ok;
    }

    public void SalinFileKe(string tujuan)
    {
        if (_db == null) return;
        SimpanSegera();
        _db.SalinKe(tujuan);
    }

    // ------------------------------------------------------------------ simpan otomatis

    void SaatBerubah()
    {
        if (_db == null) return;
        _timerSimpan.Stop();
        _timerSimpan.Start();
        _timerHitung.Stop();
        _timerHitung.Start();
        if (!GagalSimpan) StatusSimpan = "Ada perubahan, menyimpan…";
    }

    public bool SimpanSegera()
    {
        _timerSimpan.Stop();
        return SimpanSekarang();
    }

    bool SimpanSekarang()
    {
        if (_db == null) return true;
        try
        {
            string desa = Serialize(Desa);
            string rf = Serialize(Referensi);
            string? data = _data != null && _aktif != null ? Serialize(_data) : null;
            bool ubahDesa = desa != _lastDesa;
            bool ubahRef = rf != _lastRef;
            bool ubahData = data != null && data != _lastData;
            if (!ubahDesa && !ubahRef && !ubahData)
            {
                if (GagalSimpan) { GagalSimpan = false; StatusSimpan = "Semua perubahan tersimpan."; }
                return true;
            }

            var now = DateTime.Now;
            _db.InTransaction(() =>
            {
                if (ubahDesa) _db.SetMeta("desa", desa);
                if (ubahRef) _db.SetMeta("referensi", rf);
                if (ubahData) _db.SimpanData(_aktif!.Id, data!, now);
            });

            if (ubahDesa) _lastDesa = desa;
            if (ubahRef) _lastRef = rf;
            if (ubahData)
            {
                _undo.Add(_lastData);
                if (_undo.Count > 300) _undo.RemoveAt(0);
                _redo.Clear();
                _lastData = data!;
                _aktif!.Diubah = now;
                RaiseStatusEdit();
            }
            GagalSimpan = false;
            _galatDitampilkan = false;
            StatusSimpan = $"Tersimpan otomatis pukul {now:HH:mm:ss}";
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan");
            GagalSimpan = true;
            StatusSimpan = "GAGAL MENYIMPAN – periksa file / flashdisk. Mencoba lagi…";
            _timerSimpan.Interval = TimeSpan.FromSeconds(5);
            _timerSimpan.Start();
            if (!_galatDitampilkan)
            {
                _galatDitampilkan = true;
                SimpanDarurat();
                Pesan.Galat("Perubahan gagal disimpan ke file data.\n\nJika file ada di flashdisk, pastikan flashdisk masih terpasang. " +
                            "Aplikasi akan terus mencoba menyimpan. Anda juga bisa memakai File ▸ Simpan Salinan Sebagai… ke lokasi lain.", ex);
            }
            return false;
        }
        finally
        {
            if (!GagalSimpan) _timerSimpan.Interval = TimeSpan.FromMilliseconds(1200);
        }
    }

    /// <summary>Bila file data tidak bisa ditulis, salin isi yang sedang dikerjakan ke folder darurat.</summary>
    void SimpanDarurat()
    {
        try
        {
            var dir = Path.Combine(Folder.DataAplikasi, "Darurat");
            Directory.CreateDirectory(dir);
            var isi = new Dictionary<string, object?>
            {
                ["file"] = _filePath,
                ["waktu"] = DateTime.Now,
                ["skenario"] = _aktif == null ? null : new { _aktif.Id, _aktif.Tahun, _aktif.Nama },
                ["desa"] = Desa,
                ["referensi"] = Referensi,
                ["data"] = _data
            };
            File.WriteAllText(Path.Combine(dir, $"darurat_{DateTime.Now:yyyyMMdd_HHmmss}.json"), Serialize(isi));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan darurat");
        }
    }

    // ------------------------------------------------------------------ hitung

    public void Hitung()
    {
        _timerHitung.Stop();
        try
        {
            if (_data == null) { Hasil = null; }
            else
            {
                _data.RaiseTotals();
                Hasil = Kalkulasi.Hitung(_data, Referensi);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Hitung");
        }
        HasilBerubah?.Invoke();
    }

    public void PerbaruiDaftarReferensi()
    {
        try
        {
            Lookup.Bangun(Referensi);
            Isi(RekeningPendapatan, Referensi.Rekening.Where(r => Rinci(r.Kode, "4")).OrderBy(r => r.Kode, KodeComparer.Instance));
            Isi(RekeningBelanja, Referensi.Rekening.Where(r => Rinci(r.Kode, "5")).OrderBy(r => r.Kode, KodeComparer.Instance));
            Isi(RekeningPenerimaanPembiayaan, Referensi.Rekening.Where(r => Rinci(r.Kode, "6.1")).OrderBy(r => r.Kode, KodeComparer.Instance));
            Isi(RekeningPengeluaranPembiayaan, Referensi.Rekening.Where(r => Rinci(r.Kode, "6.2")).OrderBy(r => r.Kode, KodeComparer.Instance));
            Isi(DaftarSumberDana, Referensi.SumberDana.Where(s => s.Kode != ""));
            Isi(DaftarSatuan, Referensi.Satuan.Select(s => s.Nama).Where(s => s != "").Distinct(StringComparer.OrdinalIgnoreCase));
            PerbaruiPelaksana();
            if (_data != null)
                foreach (var r in _data.SemuaRincian()) r.RefreshNama();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Perbarui referensi");
        }
        ReferensiBerubah?.Invoke();
        Hitung();
    }

    static bool Rinci(string kode, string awalan) => kode.Split('.').Length == 4 && Batas.Cocok(kode, awalan);

    public void PerbaruiPelaksana()
    {
        var nama = Desa.Perangkat
            .Where(p => !string.IsNullOrWhiteSpace(p.Nama))
            .Select(p => p.Nama.Trim())
            .Distinct();
        Isi(DaftarPelaksana, nama);
    }

    static void Isi<T>(ObservableCollection<T> target, IEnumerable<T> sumber)
    {
        var baru = sumber.ToList();
        if (target.SequenceEqual(baru)) return;
        target.Clear();
        foreach (var x in baru) target.Add(x);
    }

    // ------------------------------------------------------------------ skenario

    void IsiDaftarTahun()
    {
        var tahun = SemuaSkenario.Select(s => s.Tahun).Distinct().OrderBy(t => t).ToList();
        _sedangIsi = true;
        try { Isi(DaftarTahun, tahun); }
        finally { _sedangIsi = false; }
    }

    void IsiSkenarioTahun(string? pilihId)
    {
        _sedangIsi = true;
        try
        {
            SkenarioTahunIni.Clear();
            foreach (var s in SemuaSkenario.Where(s => s.Tahun == _tahun).OrderBy(s => s.Dibuat))
                SkenarioTahunIni.Add(s);
        }
        finally { _sedangIsi = false; }

        var pilih = SkenarioTahunIni.FirstOrDefault(s => s.Id == pilihId) ?? SkenarioTahunIni.FirstOrDefault();
        if (ReferenceEquals(pilih, _aktif)) OnPropertyChanged(nameof(SkenarioAktif));
        else MuatSkenario(pilih);
    }

    public void PilihTahunDanSkenario(int tahun, string? id)
    {
        _tahun = tahun;
        OnPropertyChanged(nameof(TahunAktif));
        IsiSkenarioTahun(id);
    }

    void MuatSkenario(SkenarioInfo? info)
    {
        SimpanSegera();
        SebelumDataDiganti?.Invoke();

        DataSkenario? data = null;
        if (info != null && _db != null)
        {
            try
            {
                var json = _db.MuatData(info.Id);
                data = (json == null ? null : Deserialize<DataSkenario>(json)) ?? new DataSkenario();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Muat skenario " + info.Id);
                Pesan.Galat($"Skenario \"{info.Nama}\" tidak dapat dibaca. Silakan pulihkan dari cadangan (File ▸ Buka Folder Cadangan).", ex);
                info = null;
            }
        }

        _aktif = info;
        Data = data;
        _lastData = data == null ? "" : Serialize(data);
        _undo.Clear();
        _redo.Clear();
        OnPropertyChanged(nameof(SkenarioAktif));
        RaiseStatusEdit();
        DataDiganti?.Invoke();
        Hitung();
    }

    void RaiseStatusEdit()
    {
        OnPropertyChanged(nameof(BisaUbah));
        OnPropertyChanged(nameof(TidakBisaUbah));
        OnPropertyChanged(nameof(SkenarioFinal));
        OnPropertyChanged(nameof(BisaBatalkan));
        OnPropertyChanged(nameof(BisaUlangi));
    }

    public void BuatSkenario(string nama, int tahun, bool salinDariAktif)
    {
        if (_db == null) return;
        SimpanSegera();
        var data = salinDariAktif && _data != null ? SalinDalam(_data) : new DataSkenario();
        var info = new SkenarioInfo { Tahun = tahun, Nama = nama.Trim() };
        try
        {
            _db.TambahSkenario(info, Serialize(data));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Buat skenario");
            Pesan.Galat("Skenario baru gagal dibuat.", ex);
            return;
        }
        SemuaSkenario.Add(info);
        IsiDaftarTahun();
        PilihTahunDanSkenario(tahun, info.Id);
    }

    /// <summary>Buat skenario dari data yang sudah disiapkan (mis. hasil impor). Mengembalikan skenario baru.</summary>
    public SkenarioInfo? BuatSkenarioDari(string nama, int tahun, DataSkenario data, bool pilih)
    {
        if (_db == null) return null;
        SimpanSegera();
        var info = new SkenarioInfo { Tahun = tahun, Nama = nama.Trim() };
        try
        {
            _db.TambahSkenario(info, Serialize(data));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Buat skenario impor");
            Pesan.Galat("Skenario gagal dibuat.", ex);
            return null;
        }
        SemuaSkenario.Add(info);
        IsiDaftarTahun();
        if (pilih) PilihTahunDanSkenario(tahun, info.Id);
        return info;
    }

    public void GantiNama(SkenarioInfo info, string nama)
    {
        if (_db == null) return;
        var lama = info.Nama;
        info.Nama = nama.Trim();
        info.Diubah = DateTime.Now;
        try { _db.UbahInfo(info); }
        catch (Exception ex) { info.Nama = lama; Log.Error(ex, "Ganti nama"); Pesan.Galat("Nama skenario gagal diubah.", ex); }
    }

    public void SetFinal(SkenarioInfo info, bool final)
    {
        if (_db == null) return;
        SimpanSegera();
        var lama = info.Status;
        info.Status = final ? "Final" : "Draft";
        info.Diubah = DateTime.Now;
        try { _db.UbahInfo(info); }
        catch (Exception ex) { info.Status = lama; Log.Error(ex, "Set final"); Pesan.Galat("Status skenario gagal diubah.", ex); }
        RaiseStatusEdit();
    }

    public void HapusSkenario(SkenarioInfo info)
    {
        if (_db == null) return;
        SimpanSegera();
        try { _db.HapusSkenario(info.Id); }
        catch (Exception ex) { Log.Error(ex, "Hapus skenario"); Pesan.Galat("Skenario gagal dihapus.", ex); return; }

        var tahun = info.Tahun;
        SemuaSkenario.Remove(info);
        if (ReferenceEquals(info, _aktif))
        {
            _aktif = null;
            Data = null;
        }
        if (SemuaSkenario.Count == 0)
        {
            var baru = new SkenarioInfo { Tahun = tahun, Nama = "Rancangan Awal" };
            try { _db.TambahSkenario(baru, Serialize(new DataSkenario())); SemuaSkenario.Add(baru); }
            catch (Exception ex) { Log.Error(ex, "Skenario pengganti"); }
        }
        IsiDaftarTahun();
        var t = SemuaSkenario.Any(s => s.Tahun == tahun) ? tahun : (DaftarTahun.Count > 0 ? DaftarTahun.Max() : tahun);
        _aktif = null;
        PilihTahunDanSkenario(t, null);
    }

    // ------------------------------------------------------------------ batalkan / ulangi

    public void Batalkan()
    {
        if (!BisaUbah) return;
        SimpanSegera();
        if (_undo.Count == 0) return;
        var sebelumnya = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(_lastData);
        Terapkan(sebelumnya);
    }

    public void Ulangi()
    {
        if (!BisaUbah) return;
        SimpanSegera();
        if (_redo.Count == 0) return;
        var berikut = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(_lastData);
        Terapkan(berikut);
    }

    void Terapkan(string json)
    {
        if (_db == null || _aktif == null) return;
        SebelumDataDiganti?.Invoke();
        DataSkenario data;
        try { data = Deserialize<DataSkenario>(json) ?? new DataSkenario(); }
        catch (Exception ex) { Log.Error(ex, "Terapkan riwayat"); return; }
        try
        {
            var now = DateTime.Now;
            _db.SimpanData(_aktif.Id, json, now);
            _aktif.Diubah = now;
            StatusSimpan = $"Tersimpan otomatis pukul {now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Simpan riwayat");
        }
        Data = data;
        _lastData = json;
        RaiseStatusEdit();
        DataDiganti?.Invoke();
        Hitung();
    }

    /// <summary>Memuat data skenario lain (untuk perbandingan) tanpa mengganti skenario aktif.</summary>
    public DataSkenario? MuatDataSkenarioLain(SkenarioInfo info)
    {
        if (ReferenceEquals(info, _aktif)) return _data;
        if (_db == null) return null;
        try
        {
            var json = _db.MuatData(info.Id);
            return json == null ? null : Deserialize<DataSkenario>(json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Muat skenario lain");
            return null;
        }
    }
}

/// <summary>Mengurutkan kode bertitik secara alami: 5.2.10 setelah 5.2.9.</summary>
public sealed class KodeComparer : IComparer<string>
{
    public static readonly KodeComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        var a = (x ?? "").Split('.');
        var b = (y ?? "").Split('.');
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            int c;
            if (int.TryParse(a[i], out var na) && int.TryParse(b[i], out var nb)) c = na.CompareTo(nb);
            else c = string.Compare(a[i], b[i], StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
        }
        return a.Length.CompareTo(b.Length);
    }
}
