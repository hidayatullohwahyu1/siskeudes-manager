using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public partial class ReferensiView : UserControl
{
    readonly AppState S = AppState.Instance;

    public ReferensiView()
    {
        InitializeComponent();
        Loaded += (_, _) => Pasang();
        Unloaded += (_, _) =>
        {
            AkhiriSemua();
            S.PerbaruiDaftarReferensi();
        };
    }

    void Pasang()
    {
        var r = S.Referensi;
        TbRekening.ItemsSource = r.Rekening;
        TbSumberDana.ItemsSource = r.SumberDana;
        TbBidang.ItemsSource = r.Bidang;
        TbSubBidang.ItemsSource = r.SubBidang;
        TbKegiatan.ItemsSource = r.Kegiatan;
        TbSatuan.ItemsSource = r.Satuan;
        TbAturan.ItemsSource = r.Aturan;
    }

    DataGrid[] SemuaTabel => new[] { TbRekening, TbSumberDana, TbBidang, TbSubBidang, TbKegiatan, TbSatuan, TbAturan };

    void AkhiriSemua()
    {
        foreach (var g in SemuaTabel) UiBantu.AkhiriEdit(g);
    }

    (DataGrid tabel, IList daftar, Func<object> baru) Target(string tag)
    {
        var r = S.Referensi;
        return tag switch
        {
            "rekening" => (TbRekening, r.Rekening, () => new RefRekening()),
            "sumberdana" => (TbSumberDana, r.SumberDana, () => new RefSumberDana()),
            "bidang" => (TbBidang, r.Bidang, () => new RefBidang()),
            "subbidang" => (TbSubBidang, r.SubBidang, () => new RefSubBidang()),
            "kegiatan" => (TbKegiatan, r.Kegiatan, () => new RefKegiatan()),
            "satuan" => (TbSatuan, r.Satuan, () => new RefSatuan()),
            _ => (TbAturan, r.Aturan, () => new AturanBatas { Nama = "Aturan baru", Persen = 30 }),
        };
    }

    void Tambah_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        var (tabel, daftar, baru) = Target(tag);
        UiBantu.AkhiriEdit(tabel);
        var item = baru();
        var idx = tabel.SelectedItem != null ? daftar.IndexOf(tabel.SelectedItem) + 1 : daftar.Count;
        daftar.Insert(Math.Clamp(idx, 0, daftar.Count), item);
        UiBantu.EditSel(tabel, item, tag == "aturan" ? 1 : 0);
    }

    void Hapus_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        var (tabel, daftar, _) = Target(tag);
        if (tabel.SelectedItem is not { } item) return;
        UiBantu.AkhiriEdit(tabel);
        if (tag is "bidang" or "subbidang" or "sumberdana" &&
            !Pesan.Tanya("Menghapus referensi ini tidak menghapus kegiatan/rincian yang sudah memakainya, tetapi akan muncul peringatan \"tidak ada di referensi\". Lanjutkan?"))
            return;
        var idx = daftar.IndexOf(item);
        daftar.Remove(item);
        if (daftar.Count > 0) tabel.SelectedItem = daftar[Math.Clamp(idx, 0, daftar.Count - 1)];
    }

    void Urutkan_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        AkhiriSemua();
        var r = S.Referensi;
        if (tag == "rekening")
        {
            var urut = r.Rekening.OrderBy(x => x.Kode, KodeComparer.Instance).ToList();
            r.Rekening.Clear();
            foreach (var x in urut) r.Rekening.Add(x);
        }
        else if (tag == "kegiatan")
        {
            var urut = r.Kegiatan.OrderBy(x => x.Kode, KodeComparer.Instance).ToList();
            r.Kegiatan.Clear();
            foreach (var x in urut) r.Kegiatan.Add(x);
        }
    }

    void Lengkapi_Click(object sender, RoutedEventArgs e)
    {
        AkhiriSemua();
        var n = DefaultReferensi.Lengkapi(S.Referensi);
        S.PerbaruiDaftarReferensi();
        Pesan.Info(n == 0 ? "Semua isi bawaan sudah ada." : $"{n} isian bawaan ditambahkan.");
    }

    void Impor_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        AkhiriSemua();
        var dlg = new OpenFileDialog { Filter = "File Excel (*.xlsx)|*.xlsx", Title = "Pilih file Excel berisi kode dan nama" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;

        List<(string Kode, string Nama)> hasil;
        try
        {
            hasil = ImporExcel.BacaKodeNama(dlg.FileName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Impor");
            Pesan.Galat("File Excel tidak dapat dibaca. Pastikan formatnya .xlsx (bukan .xls lama) dan file tidak sedang dibuka di Excel.", ex);
            return;
        }

        var r = S.Referensi;
        int tambah = 0, ubah = 0;
        if (tag == "rekening")
        {
            foreach (var (kode, nama) in hasil.Where(h => h.Kode.Split('.').Length >= 4 && "456".Contains(h.Kode[0])))
            {
                var ada = r.Rekening.FirstOrDefault(x => x.Kode == kode);
                if (ada == null) { r.Rekening.Add(new RefRekening { Kode = kode, Nama = nama }); tambah++; }
                else if (ada.Nama != nama) { ada.Nama = nama; ubah++; }
            }
        }
        else
        {
            var awalanDesa = string.IsNullOrWhiteSpace(S.Desa.KodeDesa) ? null : S.Desa.KodeDesa + ".";
            foreach (var (k0, nama) in hasil)
            {
                var kode = k0;
                if (awalanDesa != null && kode.StartsWith(awalanDesa)) kode = kode[awalanDesa.Length..];
                var seg = kode.Split('.');
                // 01.01.01 atau 04.2002.01.01.01 (dengan kode desa)
                if (seg.Length == 5) seg = seg[^3..];
                else if (seg.Length != 3) continue;
                if (seg[0].Length != 2 || seg.Any(x => x.Length == 0 || !x.All(char.IsDigit))) continue;
                kode = string.Join('.', seg);
                var ada = r.Kegiatan.FirstOrDefault(x => x.Kode == kode);
                if (ada == null) { r.Kegiatan.Add(new RefKegiatan { Kode = kode, Nama = nama }); tambah++; }
                else if (ada.Nama != nama) { ada.Nama = nama; ubah++; }
            }
        }
        S.PerbaruiDaftarReferensi();
        Pesan.Info(tambah + ubah == 0
            ? "Tidak ada kode yang cocok ditemukan di file tersebut.\n\nFormat yang dibaca: satu baris per kode, kolom kode (contoh 5.2.1.01 atau 02.03.01) lalu kolom nama di sebelah kanannya."
            : $"Impor selesai: {tambah} ditambahkan, {ubah} namanya diperbarui.");
    }
}
