using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public class BarisBanding
{
    public string Kelompok { get; init; } = "";
    public string Kode { get; init; } = "";
    public string Uraian { get; init; } = "";
    public decimal A { get; init; }
    public decimal B { get; init; }
    public decimal Selisih => B - A;
    public int Arah => Math.Sign(Selisih);
    public string Keterangan { get; init; } = "";
}

public partial class BandingView : UserControl
{
    readonly AppState S = AppState.Instance;

    public BandingView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (CmbB.SelectedItem == null) CmbB.SelectedItem = S.SkenarioAktif;
            if (CmbA.SelectedItem == null)
                CmbA.SelectedItem = S.SemuaSkenario.Where(s => s != S.SkenarioAktif)
                    .OrderBy(s => Math.Abs(s.Tahun - (S.SkenarioAktif?.Tahun ?? 0))).FirstOrDefault() ?? S.SkenarioAktif;
        };
    }

    void Bandingkan_Click(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        if (CmbA.SelectedItem is not SkenarioInfo a || CmbB.SelectedItem is not SkenarioInfo b)
        {
            if (sender is Button) Pesan.Info("Pilih skenario Semula dan Menjadi terlebih dahulu.");
            return;
        }
        S.SimpanSegera();
        var da = S.MuatDataSkenarioLain(a);
        var db = S.MuatDataSkenarioLain(b);
        if (da == null || db == null)
        {
            Pesan.Galat("Salah satu skenario tidak dapat dibaca.");
            return;
        }
        var baris = Hitung(da, db);
        if (ChkBeda.IsChecked == true) baris = baris.Where(x => x.Selisih != 0 || x.Kelompok.StartsWith("1")).ToList();
        var view = new ListCollectionView(baris);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(BarisBanding.Kelompok)));
        Tabel.ItemsSource = view;
    }

    List<BarisBanding> Hitung(DataSkenario a, DataSkenario b)
    {
        var ha = Kalkulasi.Hitung(a, S.Referensi);
        var hb = Kalkulasi.Hitung(b, S.Referensi);
        var list = new List<BarisBanding>();

        const string g1 = "1. Ringkasan APBDes";
        list.Add(new BarisBanding { Kelompok = g1, Kode = "4", Uraian = "Pendapatan", A = ha.TotalPendapatan, B = hb.TotalPendapatan });
        list.Add(new BarisBanding { Kelompok = g1, Kode = "5", Uraian = "Belanja", A = ha.TotalBelanja, B = hb.TotalBelanja });
        list.Add(new BarisBanding { Kelompok = g1, Kode = "6.1", Uraian = "Penerimaan pembiayaan", A = ha.PenerimaanPembiayaan, B = hb.PenerimaanPembiayaan });
        list.Add(new BarisBanding { Kelompok = g1, Kode = "6.2", Uraian = "Pengeluaran pembiayaan", A = ha.PengeluaranPembiayaan, B = hb.PengeluaranPembiayaan });
        list.Add(new BarisBanding { Kelompok = g1, Kode = "", Uraian = "Sisa belum dialokasikan (+) / kekurangan (−)", A = ha.SisaAnggaran, B = hb.SisaAnggaran });
        list.Add(new BarisBanding { Kelompok = g1, Kode = "", Uraian = "Jumlah kegiatan", A = ha.JumlahKegiatan, B = hb.JumlahKegiatan });

        const string g2 = "2. Belanja per Sumber Dana";
        foreach (var k in ha.PerSumberDana.Select(x => x.Kode).Union(hb.PerSumberDana.Select(x => x.Kode)))
        {
            var xa = ha.PerSumberDana.FirstOrDefault(x => x.Kode == k);
            var xb = hb.PerSumberDana.FirstOrDefault(x => x.Kode == k);
            list.Add(new BarisBanding
            {
                Kelompok = g2, Kode = k, Uraian = (xb ?? xa)!.Nama, A = xa?.Dipakai ?? 0, B = xb?.Dipakai ?? 0,
                Keterangan = $"Pagu semula {Rp(xa?.Pagu ?? 0)} · menjadi {Rp(xb?.Pagu ?? 0)}"
            });
        }

        const string g3 = "3. Pendapatan per Rekening";
        var pa = a.Pendapatan.GroupBy(r => r.KodeRekening).ToDictionary(g => g.Key, g => g.Sum(r => r.Jumlah));
        var pb = b.Pendapatan.GroupBy(r => r.KodeRekening).ToDictionary(g => g.Key, g => g.Sum(r => r.Jumlah));
        foreach (var k in pa.Keys.Union(pb.Keys).OrderBy(k => k, KodeComparer.Instance))
            list.Add(new BarisBanding { Kelompok = g3, Kode = k, Uraian = Lookup.NamaRekening(k), A = pa.GetValueOrDefault(k), B = pb.GetValueOrDefault(k) });

        const string g4 = "4. Belanja per Kegiatan";
        var ka = a.Kegiatan.GroupBy(k => k.Kode).ToDictionary(g => g.Key, g => g.ToList());
        var kb = b.Kegiatan.GroupBy(k => k.Kode).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var k in ka.Keys.Union(kb.Keys).OrderBy(k => k, KodeComparer.Instance))
        {
            var la = ka.GetValueOrDefault(k);
            var lb = kb.GetValueOrDefault(k);
            var nama = (lb ?? la)!.First().Nama;
            var ket = la == null ? "Kegiatan baru" : lb == null ? "Dihapus" :
                !string.Equals(la.First().Nama, lb.First().Nama, StringComparison.OrdinalIgnoreCase) ? $"Nama semula: {la.First().Nama}" : "";
            list.Add(new BarisBanding
            {
                Kelompok = g4, Kode = k, Uraian = nama,
                A = la?.Sum(x => x.Total) ?? 0, B = lb?.Sum(x => x.Total) ?? 0, Keterangan = ket
            });
        }
        return list;
    }

    static string Rp(decimal v) => v.ToString("#,##0", Budaya.Id);
}
