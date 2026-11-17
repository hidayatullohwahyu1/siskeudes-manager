using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Petunjuk singkat pemakaian.</summary>
public class BantuanView : UserControl
{
    public BantuanView()
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 8, 16), MaxWidth = 900, HorizontalAlignment = HorizontalAlignment.Left };
        sp.Children.Add(new TextBlock { Text = "Petunjuk Singkat", Style = (Style)Application.Current.FindResource("Judul") });

        void Bagian(string judul, params string[] butir)
        {
            sp.Children.Add(new TextBlock { Text = judul, Style = (Style)Application.Current.FindResource("SubJudul") });
            foreach (var b in butir)
            {
                var g = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.Children.Add(new TextBlock { Text = "•", FontWeight = FontWeights.Bold });
                var t = new TextBlock { Text = b, TextWrapping = TextWrapping.Wrap };
                Grid.SetColumn(t, 1);
                g.Children.Add(t);
                sp.Children.Add(g);
            }
        }

        Bagian("Tujuan aplikasi",
            "Rancang APBDes dipakai untuk menyusun dan mencoba-coba rencana anggaran desa saat musyawarah, SEBELUM diinput dan diposting di Siskeudes.",
            "Aplikasi ini tidak terhubung ke Siskeudes dan tidak mengubah data Siskeudes. Hasil yang sudah disepakati diketik ulang ke Siskeudes memakai laporan \"Daftar Siap Input Siskeudes\".",
            "Bekerja penuh tanpa internet.");

        Bagian("Urutan kerja yang disarankan",
            "1. Data Desa: isi nama desa, kode desa (seperti di Siskeudes, mis. 04.2002), dan daftar perangkat/pelaksana.",
            "2. Referensi & Aturan: cocokkan kode rekening, sumber dana, dan referensi kegiatan dengan Siskeudes. Bisa impor dari Excel.",
            "3. Pendapatan & Pembiayaan: isi perkiraan pendapatan per sumber dana (DDS, ADD, PBH, PAD, dll.) serta SiLPA.",
            "4. Belanja: pilih sub bidang → + Kegiatan → isi paket → isi rincian RAB (volume × harga satuan).",
            "5. Ringkasan: lihat sisa dana per sumber dana, keseimbangan APBDes, hasil cek aturan, dan peringatan.",
            "6. Pratinjau & Cetak / Ekspor Excel: bagikan hasil ke peserta musyawarah, lalu jadikan dasar input Siskeudes.");

        Bagian("Skenario – bebas mencoba",
            "Satu tahun anggaran boleh punya beberapa skenario, misalnya \"Usulan Kepala Desa\", \"Hasil Musdes\", \"Alternatif DD turun 10%\".",
            "Gunakan Skenario ▸ Salin skenario ini sebelum mencoba perubahan besar. Skenario asli tetap aman.",
            "Bandingkan Skenario menunjukkan apa saja yang berubah antara dua skenario.",
            "Setelah disepakati, pilih Skenario ▸ Tandai FINAL agar tidak berubah tanpa sengaja.");

        Bagian("Keamanan data",
            "Semua perubahan tersimpan otomatis ke file .apbdes (default di folder Dokumen\\Rancang APBDes).",
            "Tombol ↶ Batalkan (Ctrl+Z) mengembalikan perubahan terakhir; ↷ Ulangi (Ctrl+Y) memajukannya lagi.",
            "Setiap kali file dibuka, salinan cadangan otomatis dibuat (30 terakhir disimpan). Lihat File ▸ Buka folder cadangan otomatis.",
            "Untuk dibawa ke komputer lain, pakai File ▸ Simpan salinan sebagai… lalu buka file itu di komputer tujuan.");

        Bagian("Tampilan untuk rapat",
            "A+ / A− (atau Ctrl + roda mouse) memperbesar/memperkecil tulisan. Ctrl+0 kembali ke 100%.",
            "Layar Penuh (F11) cocok untuk ditampilkan ke proyektor. Tekan F11 atau Esc untuk keluar.",
            "Mengubah isi tabel: klik dua kali sel (atau tekan F2), ketik, lalu Enter. Desimal memakai koma, contoh 2,5.");

        sp.Children.Add(new TextBlock
        {
            Text = $"Lokasi log (untuk melaporkan masalah): {Log.FilePath}",
            Style = (Style)Application.Current.FindResource("TeksRedup"),
            Margin = new Thickness(0, 16, 0, 0),
            FontSize = 13
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"Versi {typeof(BantuanView).Assembly.GetName().Version}",
            Style = (Style)Application.Current.FindResource("TeksRedup"),
            FontSize = 13
        });

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = sp
        };
    }
}
