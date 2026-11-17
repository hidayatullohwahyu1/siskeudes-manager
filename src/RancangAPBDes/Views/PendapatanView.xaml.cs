using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public partial class PendapatanView : UserControl
{
    public PendapatanView()
    {
        InitializeComponent();
        Loaded += (_, _) => { AppState.Instance.DataDiganti += Isi; Isi(); };
        Unloaded += (_, _) => AppState.Instance.DataDiganti -= Isi;
    }

    void Isi()
    {
        var d = AppState.Instance.Data;
        EdPendapatan.Items = d?.Pendapatan;
        EdTerima.Items = d?.PenerimaanPembiayaan;
        EdKeluar.Items = d?.PengeluaranPembiayaan;
    }
}
