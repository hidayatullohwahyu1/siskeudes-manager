using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RancangAPBDes.Core;

namespace RancangAPBDes.Views;

/// <summary>Bantuan › Petunjuk Penggunaan.</summary>
public class BantuanView : UserControl
{
    public BantuanView()
    {
        var sp = new StackPanel { Margin = new Thickness(16, 12, 16, 16), MaxWidth = 860, HorizontalAlignment = HorizontalAlignment.Left };


        void Bagian(string judul, params string[] butir)
        {
            sp.Children.Add(new TextBlock { Text = judul, Style = (Style)Application.Current.FindResource("SubJudul") });
            foreach (var b in butir)
            {
                var g = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
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
            "Aplikasi ini tidak terhubung ke Siskeudes dan tidak mengubah data Siskeudes. Penatausahaan (SPP, pencairan, dll.) tetap dilakukan di Siskeudes.",
            "Bekerja penuh tanpa internet. Semua perubahan tersimpan otomatis.");

        Bagian("Urutan kerja (sama seperti Siskeudes)",
            "1. Data Entri › Data Umum Desa: nama desa, kepala desa, dan daftar Pelaksana Kegiatan (PKA).",
            "2. Data Entri › Penganggaran › Isian Data Anggaran (F3):",
            "    • Kegiatan: Bidang › Sub Bidang › Kegiatan › Paket Kegiatan.",
            "    • Pendapatan: RAP (pilih rekening) › RAP Rinci (uraian, volume, satuan, harga, sumber dana).",
            "    • Belanja: Kegiatan › Paket › RAB (pilih rekening) › RAB Rinci.",
            "    • Pembiayaan 1 (penerimaan, mis. SiLPA) dan Pembiayaan 2 (pengeluaran, mis. penyertaan modal).",
            "3. Beranda: lihat sisa dana per sumber dana, keseimbangan APBDes, dan peringatan.",
            "4. Data Entri › Penganggaran › Posting APBDes: kunci skenario yang sudah disepakati.",
            "5. Laporan: cetak/PDF atau ekspor Excel, termasuk Daftar Siap Input Siskeudes dan Perubahan APBDes (Semula – Menjadi).");

        Bagian("Perencanaan (sebelum penganggaran)",
            "Data Entri › Perencanaan › RPJMDes dan RKPDes: isi periode & visi, misi, lalu Kegiatan RPJMDes beserta tahun pelaksanaannya.",
            "RKPDes mengikuti tahun skenario aktif: isi Pagu Indikatif per sumber dana, lalu tekan \"Ambil dari RPJMDes\" dan tandai kegiatan Disepakati atau Cadangan.",
            "Tekan \"Kirim ke Isian Data Anggaran\" untuk membuat kegiatan & paket di skenario aktif; rincian RAB diisi di Isian Data Anggaran › 3. Belanja.");

        Bagian("Anggaran Kas Desa",
            "Data Entri › Penganggaran › Anggaran Kas Desa: jadwalkan anggaran setiap kegiatan dan rekening pendapatan per bulan.",
            "Klik dua kali sel bulan untuk mengisi, atau gunakan \"Bagi Rata 12 Bulan\". Kolom Belum Dijadwalkan harus 0 (hijau).");

        Bagian("Mulai dari APBDes yang sudah ada",
            "File › Impor APBDes dari Excel Siskeudes: pilih file RAB per kegiatan, Rencana Anggaran Pendapatan, dan Rencana Anggaran Pembiayaan (boleh sekaligus).",
            "Jika file berisi kolom SEMULA dan MENJADI, aplikasi membuat dua skenario sehingga bisa langsung dibandingkan.",
            "Baris yang jumlahnya tidak sama dengan Volume × Harga Satuan ditampilkan untuk dicek, karena Siskeudes selalu memakai rumus tersebut.");

        Bagian("Cara mengisi tabel",
            "Pilih baris di tabel, isinya tampil lengkap di bawah tabel.",
            "Tambah (Insert) untuk data baru, Ubah (F2 atau klik dua kali) untuk mengubah, Hapus (Delete) untuk menghapus.",
            "Saat mengisi, tekan Simpan (Ctrl+S) untuk menyimpan atau Batal (Esc) untuk membatalkan.",
            "Tombol … di samping kode membuka daftar pilihan yang bisa dicari (rekening, referensi kegiatan).",
            "Angka desimal memakai koma, contoh 2,5. Arahkan kursor ke teks yang terpotong untuk melihat isi lengkapnya.");

        Bagian("Simulasi dengan skenario",
            "Satu tahun anggaran boleh punya beberapa skenario, misalnya \"Usulan Kades\", \"Hasil Musdes\", \"Alternatif DD turun\".",
            "Simulasi › Salin Skenario Ini sebelum mencoba perubahan besar; skenario asli tetap aman.",
            "Simulasi › Bandingkan Skenario menampilkan Semula – Menjadi – Bertambah/(Berkurang).",
            "↶ Batalkan (Ctrl+Z) dan ↷ Ulangi (Ctrl+Y) untuk mengembalikan perubahan.");

        Bagian("Keamanan data",
            "File data .apbdes tersimpan di Dokumen\\Rancang APBDes (bisa dipindah lewat File › Simpan Salinan Sebagai).",
            "Setiap kali file dibuka, cadangan otomatis dibuat (30 terakhir disimpan): File › Buka Folder Cadangan Otomatis.");

        Bagian("Tampilan untuk rapat",
            "Tampilan › Perbesar / Perkecil (Ctrl + roda mouse), Ctrl+0 kembali ke 100%.",
            "Tampilan › Layar Penuh (F11) untuk proyektor; tekan F11 atau Esc untuk keluar.");

        sp.Children.Add(new TextBlock
        {
            Text = $"Versi {typeof(BantuanView).Assembly.GetName().Version}  ·  Log: {Log.FilePath}",
            Style = (Style)Application.Current.FindResource("TeksRedup"),
            Margin = new Thickness(0, 16, 0, 0),
            FontSize = Huruf.Kecil
        });

        Content = new Border
        {
            Background = Brushes.White, BorderBrush = (Brush)Application.Current.FindResource("Garis"), BorderThickness = new Thickness(1),
            Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Visible, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = sp }
        };
    }
}
