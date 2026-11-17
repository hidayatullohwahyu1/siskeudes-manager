using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Data Entri › Data Umum Desa: identitas desa dan Pelaksana Kegiatan (seperti Siskeudes).</summary>
public class DataDesaView : UserControl
{
    readonly AppState S = AppState.Instance;
    readonly ListBox _nav = Gaya.Nav();
    readonly ContentControl _isi = new() { Focusable = false };
    readonly HalamanData _umum, _pka;

    public DataDesaView()
    {
        _umum = new HalamanData
        {
            JudulData = "Data Umum Desa",
            TanpaTabel = true,
            BolehUbah = () => true,
            Konteks = () => "tekan Ubah untuk mengisi, lalu Simpan. Berlaku untuk semua skenario di file ini.",
            Tampilan = () => new List<DataDesa> { S.Desa },
            SetelahSimpan = _ => S.PerbaruiDaftarReferensi()
        };
        _umum.Isian.AddRange(new[]
        {
            new Isian { Label = "Nama Desa", Path = "NamaDesa", Wajib = true, Penuh = true, Petunjuk = "Contoh: Merden" },
            new Isian { Label = "Kode Desa", Path = "KodeDesa", Petunjuk = "Seperti di Siskeudes, contoh 04.2002 (tidak wajib)" },
            new Isian { Label = "Kecamatan", Path = "Kecamatan" },
            new Isian { Label = "Kabupaten", Path = "Kabupaten" },
            new Isian { Label = "Tempat Penandatanganan", Path = "TempatTtd", Petunjuk = "Muncul di laporan cetak" },
            new Isian { Label = "Nama Kepala Desa", Path = "KepalaDesa", Penuh = true },
            new Isian { Label = "Nama Sekretaris Desa", Path = "Sekdes", Penuh = true },
            new Isian { Label = "Nama Kaur Keuangan", Path = "KaurKeuangan", Penuh = true },
        });

        _pka = new HalamanData
        {
            JudulData = "Pelaksana Kegiatan",
            Konteks = () => "nama-nama ini menjadi pilihan Nama Pelaksana di Isian Data Anggaran",
            BolehUbah = () => true,
            Daftar = () => S.Desa.Perangkat,
            Tampilan = () =>
            {
                var list = S.Desa.Perangkat.ToList();
                return list;
            },
            BuatBaru = () => new Perangkat(),
            Kunci = o => ((Perangkat)o).Nama + "|" + ((Perangkat)o).Jabatan,
            PesanHapus = o => $"Hapus {((Perangkat)o).Nama} dari daftar pelaksana?",
            SetelahSimpan = _ => S.PerbaruiPelaksana()
        };
        _pka.Kolom.Add(new Kolom { Judul = "Nama", Path = "Nama", MinLebar = 200 });
        _pka.Kolom.Add(new Kolom { Judul = "Jabatan", Path = "Jabatan", MinLebar = 200 });
        _pka.Isian.Add(new Isian { Label = "Nama", Path = "Nama", Wajib = true });
        _pka.Isian.Add(new Isian { Label = "Jabatan", Path = "Jabatan", Wajib = true });

        _nav.Items.Add(Gaya.ItemNav("Data Umum"));
        _nav.Items.Add(Gaya.ItemNav("Pelaksana Kegiatan"));
        _nav.SelectionChanged += (_, _) =>
        {
            var h = _nav.SelectedIndex == 1 ? _pka : _umum;
            _isi.Content = h;
            h.Segarkan();
        };
        _nav.SelectedIndex = 0;
        Content = Gaya.DenganNav(_nav, _isi);

        Unloaded += (_, _) =>
        {
            if (_umum.SedangMengisi) _umum.Batal();
            if (_pka.SedangMengisi) _pka.Batal();
            S.PerbaruiPelaksana();
        };
    }

    public void BukaTab(int i) => _nav.SelectedIndex = i;
}
