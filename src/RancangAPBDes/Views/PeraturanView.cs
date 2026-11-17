using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Data Entri › Penganggaran › Peraturan APBDes: nomor & tanggal Perdes APBDes dan Perkades Penjabaran.</summary>
public class PeraturanView : UserControl
{
    readonly AppState S = AppState.Instance;
    readonly HalamanData _h;

    public PeraturanView()
    {
        _h = new HalamanData
        {
            JudulData = "Peraturan APBDes",
            TanpaTabel = true,
            Syarat = () => S.Data == null ? "Belum ada skenario." : null,
            Konteks = () => $"skenario \"{S.SkenarioAktif?.Nama}\" tahun {S.SkenarioAktif?.Tahun} · dipakai di kop Lampiran 1a, 1b, 1c",
            Tampilan = () => S.Data == null ? null : new List<DataSkenario> { S.Data },
            AksiCetak = () => IsianAnggaranView.BukaLaporan?.Invoke(Laporan.Lampiran1a)
        };
        _h.Isian.AddRange(new[]
        {
            new Isian { Label = "Nomor Perdes APBDes", Path = "NomorPerdes", Petunjuk = "Contoh: 5 Tahun 2026" },
            new Isian { Label = "Tanggal Perdes", Path = "TanggalPerdes", Petunjuk = "Contoh: 30 Desember 2026" },
            new Isian { Label = "Nomor Perkades Penjabaran", Path = "NomorPerkades", Petunjuk = "Perkades tentang Penjabaran APBDes" },
            new Isian { Label = "Tanggal Perkades", Path = "TanggalPerkades" },
        });
        var nav = Gaya.Nav();
        nav.Items.Add(Gaya.ItemNav("Peraturan APBDes"));
        nav.SelectedIndex = 0;
        Content = Gaya.DenganNav(nav, _h);
        Loaded += (_, _) => { S.DataDiganti += _h.Segarkan; _h.Segarkan(); };
        Unloaded += (_, _) => { if (_h.SedangMengisi) _h.Batal(); S.DataDiganti -= _h.Segarkan; };
    }
}
