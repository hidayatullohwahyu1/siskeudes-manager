using System.Windows;
using System.Windows.Controls;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

public partial class DesaView : UserControl
{
    public DesaView()
    {
        InitializeComponent();
        UiBantu.TeruskanRoda(TabelPerangkat);
        Loaded += (_, _) => DataContext = AppState.Instance.Desa;
        Unloaded += (_, _) =>
        {
            UiBantu.AkhiriEdit(TabelPerangkat);
            AppState.Instance.PerbaruiPelaksana();
        };
    }

    void Tambah_Click(object sender, RoutedEventArgs e)
    {
        UiBantu.AkhiriEdit(TabelPerangkat);
        var p = new Perangkat();
        AppState.Instance.Desa.Perangkat.Add(p);
        UiBantu.EditSel(TabelPerangkat, p, 0);
    }

    void Hapus_Click(object sender, RoutedEventArgs e)
    {
        if (TabelPerangkat.SelectedItem is not Perangkat p) return;
        UiBantu.AkhiriEdit(TabelPerangkat);
        AppState.Instance.Desa.Perangkat.Remove(p);
    }
}
