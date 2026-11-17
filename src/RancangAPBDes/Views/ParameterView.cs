using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Menu Parameter: rekening, sumber dana, bidang, sub bidang, referensi kegiatan, satuan, aturan batas.</summary>
public class ParameterView : UserControl
{
    static readonly string[] DaftarJenis = { "Maksimal", "Minimal" };
    static readonly string[] DaftarDasar = { "Total Belanja", "Pagu Sumber Dana" };

    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = Gaya.Nav();
    readonly ContentControl _isi = new() { Focusable = false };
    readonly List<HalamanData> _halaman = new();

    public ParameterView()
    {
        Content = Gaya.DenganNav(_nav, _isi);
        _nav.SelectionChanged += (_, _) =>
        {
            if (_nav.SelectedIndex < 0) return;
            _isi.Content = _halaman[_nav.SelectedIndex];
            _halaman[_nav.SelectedIndex].Segarkan();
        };

        Tambah("Rekening APB Desa", Rekening());
        Tambah("Sumber Dana", Sederhana("Sumber Dana", () => S.Referensi.SumberDana, () => new RefSumberDana(), "Kode", 90, "Nama Sumber Dana", o => ((RefSumberDana)o).Kode, (o, baru) => null));
        Tambah("Bidang", Sederhana("Bidang", () => S.Referensi.Bidang, () => new RefBidang(), "Kode", 80, "Nama Bidang", o => ((RefBidang)o).Kode, (o, baru) => null, urut: true));
        Tambah("Sub Bidang", Sederhana("Sub Bidang", () => S.Referensi.SubBidang, () => new RefSubBidang(), "Kode", 90, "Nama Sub Bidang", o => ((RefSubBidang)o).Kode, (o, baru) => null, urut: true));
        Tambah("Referensi Kegiatan", RefKegiatanHalaman());
        Tambah("Satuan", Satuan());
        Tambah("Aturan Batas Belanja", Aturan());

        _nav.SelectedIndex = 0;

        Unloaded += (_, _) =>
        {
            foreach (var h in _halaman) if (h.SedangMengisi) h.Batal();
            S.PerbaruiDaftarReferensi();
        };
    }

    public void BukaTab(int i) => _nav.SelectedIndex = i;

    void Tambah(string judul, HalamanData h)
    {
        h.BolehUbah = () => true;
        h.SetelahSimpan = _ => S.PerbaruiDaftarReferensi();
        _halaman.Add(h);
        _nav.Items.Add(Gaya.ItemNav(judul));
    }

    HalamanData Sederhana(string judul, Func<IList> daftar, Func<object> baru, string labelKode, double lebarKode, string labelNama,
        Func<object, string> kunci, Func<object, bool, string?> validasi, bool urut = false)
    {
        var h = new HalamanData
        {
            JudulData = judul,
            Daftar = daftar,
            Tampilan = urut ? () => daftar().Cast<object>().OrderBy(kunci, KodeComparer.Instance).ToList() : null,
            BuatBaru = baru,
            Kunci = kunci,
            Validasi = (o, b) => ValidasiTerpilih(o, b, daftar, kunci) ?? validasi(o, b),
            PesanHapus = o => $"Hapus {judul.ToLowerInvariant()} {kunci(o)}? Data anggaran yang sudah memakainya tidak ikut terhapus, tetapi akan muncul peringatan."
        };
        h.Kolom.Add(new Kolom(labelKode, "Kode", lebarKode));
        h.Kolom.Add(new Kolom { Judul = labelNama, Path = "Nama", DuaBaris = true, MinLebar = 220 });
        h.Isian.Add(new Isian { Label = labelKode, Path = "Kode", Wajib = true });
        h.Isian.Add(new Isian { Label = labelNama, Path = "Nama", Wajib = true, Penuh = true, Jenis = JenisIsian.TeksPanjang });
        return h;
    }

    /// <summary>Saat mengubah, salinan bukan objek asli; cegah kode bentrok dengan baris lain.</summary>
    static string? ValidasiTerpilih(object draf, bool baru, Func<IList> daftar, Func<object, string> kunci)
    {
        var k = kunci(draf);
        int sama = daftar().Cast<object>().Count(x => kunci(x) == k);
        if (baru ? sama > 0 : sama > 1) return $"Kode {k} sudah ada.";
        return null;
    }

    HalamanData Rekening()
    {
        var h = new HalamanData
        {
            JudulData = "Rekening APB Desa",
            Daftar = () => S.Referensi.Rekening,
            Tampilan = () => S.Referensi.Rekening.OrderBy(r => r.Kode, KodeComparer.Instance).ToList(),
            Konteks = () => "4 = Pendapatan · 5 = Belanja · 6.1/6.2 = Pembiayaan. Rincian anggaran memakai kode 4 tingkat (mis. 5.2.1.01). Kode bawaan diambil dari APBDes berjalan; lengkapi sesuai Siskeudes desa atau impor dari Excel.",
            BuatBaru = () => new RefRekening(),
            Kunci = o => ((RefRekening)o).Kode,
            Validasi = (o, b) =>
            {
                var r = (RefRekening)o;
                if (r.Kode == "" || !"456".Contains(r.Kode[0])) return "Kode rekening harus diawali 4, 5, atau 6.";
                return ValidasiTerpilih(o, b, () => S.Referensi.Rekening, x => ((RefRekening)x).Kode);
            },
            PesanHapus = o => $"Hapus rekening {((RefRekening)o).Kode}?"
        };
        h.Kolom.Add(new Kolom("Kode Rekening", "Kode", 110));
        h.Kolom.Add(new Kolom { Judul = "Nama Rekening", Path = "Nama", DuaBaris = true, MinLebar = 260 });
        h.Isian.Add(new Isian { Label = "Kode Rekening", Path = "Kode", Wajib = true, Petunjuk = "Contoh: 5.2.1.01" });
        h.Isian.Add(new Isian { Label = "Nama Rekening", Path = "Nama", Wajib = true, Penuh = true, Jenis = JenisIsian.TeksPanjang });
        h.TambahTombol("Impor dari Excel…", () => Impor(true), butuhUbah: false);
        h.TambahTombol("Lengkapi dari Bawaan", Lengkapi, butuhUbah: false);
        return h;
    }

    HalamanData RefKegiatanHalaman()
    {
        var h = new HalamanData
        {
            JudulData = "Referensi Kegiatan",
            Daftar = () => S.Referensi.Kegiatan,
            Tampilan = () => S.Referensi.Kegiatan.OrderBy(r => r.Kode, KodeComparer.Instance).ToList(),
            Konteks = () => "Kode = Bidang.Sub Bidang.Nomor, contoh 2.3.10",
            BuatBaru = () => new RefKegiatan(),
            Kunci = o => ((RefKegiatan)o).Kode,
            Validasi = (o, b) =>
            {
                var r = (RefKegiatan)o;
                if (r.Kode.Split('.').Length != 3) return "Kode kegiatan harus 3 tingkat, contoh 2.3.10";
                return ValidasiTerpilih(o, b, () => S.Referensi.Kegiatan, x => ((RefKegiatan)x).Kode);
            },
            PesanHapus = o => $"Hapus referensi kegiatan {((RefKegiatan)o).Kode}?"
        };
        h.Kolom.Add(new Kolom("Kode", "Kode", 90));
        h.Kolom.Add(new Kolom { Judul = "Nama Kegiatan", Path = "Nama", DuaBaris = true, MinLebar = 260 });
        h.Isian.Add(new Isian { Label = "Kode Kegiatan", Path = "Kode", Wajib = true });
        h.Isian.Add(new Isian { Label = "Nama Kegiatan", Path = "Nama", Wajib = true, Penuh = true, Jenis = JenisIsian.TeksPanjang });
        h.TambahTombol("Impor dari Excel…", () => Impor(false), butuhUbah: false);
        return h;
    }

    HalamanData Satuan()
    {
        var h = new HalamanData
        {
            JudulData = "Satuan",
            Daftar = () => S.Referensi.Satuan,
            BuatBaru = () => new RefSatuan(),
            Kunci = o => ((RefSatuan)o).Nama,
            Validasi = (o, b) => ValidasiTerpilih(o, b, () => S.Referensi.Satuan, x => ((RefSatuan)x).Nama.ToLowerInvariant())
        };
        h.Kolom.Add(new Kolom { Judul = "Satuan", Path = "Nama", MinLebar = 200 });
        h.Isian.Add(new Isian { Label = "Satuan", Path = "Nama", Wajib = true });
        return h;
    }

    HalamanData Aturan()
    {
        var h = new HalamanData
        {
            JudulData = "Aturan Batas Belanja",
            Konteks = () => "dipakai untuk peringatan di Beranda; kosongkan awalan/sumber dana untuk mencakup semua",
            Daftar = () => S.Referensi.Aturan,
            BuatBaru = () => new AturanBatas { Nama = "", Persen = 30 },
            Kunci = o => ((AturanBatas)o).Nama
        };
        h.Kolom.Add(new Kolom("Aktif", "Aktif", 60));
        h.Kolom.Add(new Kolom { Judul = "Nama Aturan", Path = "Nama", DuaBaris = true, MinLebar = 220 });
        h.Kolom.Add(new Kolom("Jenis", "Jenis", 85));
        h.Kolom.Add(new Kolom { Judul = "Persen", Path = "Persen", Lebar = 70, Angka = true });
        h.Kolom.Add(new Kolom("Sub Bidang", "AwalanSubBidang", 90));
        h.Kolom.Add(new Kolom("Rekening", "AwalanRekening", 85));
        h.Kolom.Add(new Kolom("Sumber Dana", "SumberDana", 85));
        h.Kolom.Add(new Kolom("Dibanding dengan", "Dasar", 140));
        h.Isian.AddRange(new[]
        {
            new Isian { Label = "Nama Aturan", Path = "Nama", Wajib = true, Penuh = true },
            new Isian { Label = "Aktif", Path = "Aktif", Jenis = JenisIsian.Centang },
            new Isian { Label = "Jenis Batas", Path = "Jenis", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarJenis },
            new Isian { Label = "Persen (%)", Path = "Persen", Jenis = JenisIsian.Angka },
            new Isian { Label = "Dibanding dengan", Path = "Dasar", Jenis = JenisIsian.Pilihan, Pilihan = () => DaftarDasar },
            new Isian { Label = "Awalan Sub Bidang", Path = "AwalanSubBidang", Petunjuk = "Contoh 1.1 atau 5.3" },
            new Isian { Label = "Awalan Rekening", Path = "AwalanRekening", Petunjuk = "Contoh 5.1" },
            new Isian { Label = "Sumber Dana", Path = "SumberDana", Petunjuk = "Contoh DDS" },
        });
        return h;
    }

    void Lengkapi()
    {
        var n = DefaultReferensi.Lengkapi(S.Referensi);
        S.PerbaruiDaftarReferensi();
        foreach (var h in _halaman) h.Segarkan();
        Pesan.Info(n == 0 ? "Semua isi bawaan sudah ada." : $"{n} isian bawaan ditambahkan.");
    }

    void Impor(bool rekening)
    {
        var dlg = new OpenFileDialog { Filter = "File Excel (*.xlsx)|*.xlsx", Title = "Pilih file Excel berisi kode dan nama" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
        List<(string Kode, string Nama)> hasil;
        try { hasil = ImporExcel.BacaKodeNama(dlg.FileName); }
        catch (Exception ex)
        {
            Log.Error(ex, "Impor");
            Pesan.Galat("File Excel tidak dapat dibaca. Pastikan formatnya .xlsx dan file tidak sedang dibuka di Excel.", ex);
            return;
        }
        var r = S.Referensi;
        int tambah = 0, ubah = 0;
        if (rekening)
        {
            foreach (var (kode, nama) in hasil.Where(h => "456".Contains(h.Kode[0])))
            {
                var ada = r.Rekening.FirstOrDefault(x => x.Kode == kode);
                if (ada == null) { r.Rekening.Add(new RefRekening { Kode = kode, Nama = nama }); tambah++; }
                else if (ada.Nama != nama) { ada.Nama = nama; ubah++; }
            }
        }
        else
        {
            foreach (var (k0, nama) in hasil)
            {
                var kode = Batas.KodeKeg(k0);
                var seg = kode.Split('.');
                if (seg.Length == 5) kode = Batas.KodeKeg(string.Join('.', seg[^3..]));
                else if (seg.Length != 3) continue;
                if (kode.Split('.').Any(x => x.Length == 0 || !x.All(char.IsDigit))) continue;
                var ada = r.Kegiatan.FirstOrDefault(x => x.Kode == kode);
                if (ada == null) { r.Kegiatan.Add(new RefKegiatan { Kode = kode, Nama = nama }); tambah++; }
                else if (ada.Nama != nama) { ada.Nama = nama; ubah++; }
            }
        }
        S.PerbaruiDaftarReferensi();
        foreach (var h in _halaman) h.Segarkan();
        Pesan.Info(tambah + ubah == 0
            ? "Tidak ada kode yang cocok ditemukan di file tersebut."
            : $"Impor selesai: {tambah} ditambahkan, {ubah} namanya diperbarui.");
    }
}
