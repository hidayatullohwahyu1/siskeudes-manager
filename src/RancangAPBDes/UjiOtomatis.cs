using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RancangAPBDes.Core;
using RancangAPBDes.Views;

namespace RancangAPBDes;

/// <summary>Uji mandiri: jalankan "RancangAPBDes.exe --uji &lt;folder&gt;". Tidak menyentuh data pengguna.</summary>
public static class UjiOtomatis
{
    static readonly StringBuilder Hasil = new();
    static int _gagal;

    static void Cek(bool ok, string pesan)
    {
        Hasil.AppendLine((ok ? "OK    " : "GAGAL ") + pesan);
        if (!ok) _gagal++;
    }

    static void Tunggu()
    {
        for (int i = 0; i < 3; i++)
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    public static int Jalankan(string folder)
    {
        Directory.CreateDirectory(folder);
        var S = AppState.Instance;
        AppState.ModeUji = true;
        var file = Path.Combine(folder, "uji.apbdes");
        if (File.Exists(file)) File.Delete(file);

        try
        {
            Cek(S.BukaFile(file), "Buka file baru");
            IsiContoh(S);
            Cek(S.SimpanSegera(), "Simpan otomatis");
            S.Hitung();
            var h = S.Hasil!;
            Cek(h.TotalPendapatan == 2_412_000_000m, $"Total pendapatan = {h.TotalPendapatan:N0}");
            Cek(h.TotalBelanja > 0, $"Total belanja = {h.TotalBelanja:N0}");
            Cek(h.PerSumberDana.Any(b => b.Kode == "DDS"), "Ada baris sumber dana DDS");

            // Batalkan / ulangi
            var r = S.Data!.Kegiatan[0].Paket[0].Rincian[0];
            var lama = r.HargaSatuan;
            r.HargaSatuan = 123;
            S.SimpanSegera();
            S.Batalkan();
            var r2 = S.Data!.Kegiatan[0].Paket[0].Rincian[0];
            Cek(r2.HargaSatuan == lama, "Batalkan mengembalikan harga satuan");
            S.Ulangi();
            Cek(S.Data!.Kegiatan[0].Paket[0].Rincian[0].HargaSatuan == 123, "Ulangi menerapkan lagi");
            S.Batalkan();

            // Skenario salinan + final
            var asli = S.SkenarioAktif!;
            S.BuatSkenario("Alternatif DD turun", asli.Tahun, true);
            Cek(S.SkenarioAktif != asli && S.Data!.Kegiatan.Count == 4, "Salin skenario");
            S.Data!.Pendapatan.First(x => x.SumberDana == "DDS").HargaSatuan = 900_000_000m;
            S.SimpanSegera();
            S.SetFinal(S.SkenarioAktif!, true);
            Cek(!S.BisaUbah, "Skenario final terkunci");

            // Buka ulang file: data harus tetap
            var path = S.FilePath!;
            S.TutupFile();
            Cek(S.BukaFile(path), "Buka ulang file");
            Cek(S.SemuaSkenario.Count == 2, $"Jumlah skenario setelah dibuka ulang = {S.SemuaSkenario.Count}");
            S.SkenarioAktif = S.SkenarioTahunIni.First(s => s.Nama == "Hasil Musdes");
            Cek(S.Data!.Kegiatan.Count == 4, "Data kegiatan tersimpan");

            // Ekspor & laporan
            var xlsx = Path.Combine(folder, "uji.xlsx");
            EksporExcel.Simpan(xlsx, S.Desa, S.SkenarioAktif!, S.Data!, S.Hasil!);
            Cek(File.Exists(xlsx) && new FileInfo(xlsx).Length > 5000, "Ekspor Excel");
            foreach (var j in Laporan.Jenis)
            {
                var doc = Laporan.Buat(j, S.Desa, S.SkenarioAktif!, S.Data!, S.Hasil!);
                Cek(doc.Blocks.Count > 2, "Laporan: " + j);
            }

            UjiImpor(folder, S);

            // File rusak / bukan data
            var palsu = Path.Combine(folder, "palsu.apbdes");
            File.WriteAllText(palsu, "ini bukan database");
            try { Database.Open(palsu).Dispose(); Cek(false, "File palsu harus ditolak"); }
            catch { Cek(true, "File palsu ditolak dengan aman"); }

            // Tampilan
            AmbilGambar(folder, S);
        }
        catch (Exception ex)
        {
            Cek(false, "Pengecualian: " + ex);
        }
        finally
        {
            try { S.TutupFile(); } catch { /* abaikan */ }
            Hasil.AppendLine(AuditTampilan.Ringkasan());
            var ukuranLain = AuditTampilan.Ukuran.Keys.Where(u => u is not (12 or 13 or 15 or 18)).ToList();
            Cek(ukuranLain.Count == 0, "Ukuran font hanya 12/13/15/18" + (ukuranLain.Count > 0 ? " — ditemukan: " + string.Join(", ", ukuranLain) : ""));
            Cek(AuditTampilan.Jenis.Count == 1, "Satu jenis font");
            Cek(AuditTampilan.KontrasRendah.Count == 0, "Kontras warna memenuhi WCAG");
            Cek(AuditTampilan.Terpotong.Count == 0, "Tidak ada teks terpotong");
            Cek(AuditTampilan.TanpaAksi.Count == 0, "Semua tombol & menu punya aksi");
            Cek(AuditTampilan.GalatKlik.Count == 0, "Klik semua tombol tanpa galat");
            Hasil.AppendLine(_gagal == 0 ? "SEMUA UJI LULUS" : $"{_gagal} UJI GAGAL");
            File.WriteAllText(Path.Combine(folder, "hasil.txt"), Hasil.ToString());
        }
        return _gagal;
    }

    /// <summary>Foto & audit semua halaman memakai SALINAN file data nyata: --foto "data.apbdes" "folder".</summary>
    public static int Foto(string fileData, string folder)
    {
        Directory.CreateDirectory(folder);
        var salinan = Path.Combine(folder, "salinan.apbdes");
        File.Copy(fileData, salinan, overwrite: true);
        AppState.ModeUji = true;
        var S = AppState.Instance;
        try
        {
            Cek(S.BukaFile(salinan), "Buka salinan data nyata");
            var draft = S.SemuaSkenario.FirstOrDefault(s => !s.IsFinal);
            if (draft != null) S.PilihTahunDanSkenario(draft.Tahun, draft.Id);
            S.Hitung();
            AmbilGambar(folder, S);
        }
        catch (Exception ex) { Cek(false, "Pengecualian: " + ex); }
        finally
        {
            try { S.TutupFile(); } catch { /* abaikan */ }
            Hasil.AppendLine(AuditTampilan.Ringkasan());
            Cek(AuditTampilan.KontrasRendah.Count == 0 && AuditTampilan.Terpotong.Count == 0 && AuditTampilan.TanpaAksi.Count == 0 && AuditTampilan.GalatKlik.Count == 0, "Audit tampilan dengan data nyata");
            Hasil.AppendLine(_gagal == 0 ? "SEMUA UJI LULUS" : $"{_gagal} UJI GAGAL");
            File.WriteAllText(Path.Combine(folder, "hasil.txt"), Hasil.ToString());
        }
        return _gagal;
    }

    /// <summary>Impor dari Excel tiruan berformat Siskeudes (RAB, pendapatan, pembiayaan, rekap PKA).</summary>
    static void UjiImpor(string folder, AppState S)
    {
        var path = Path.Combine(folder, "tiruan_siskeudes.xlsx");
        using (var wb = new ClosedXML.Excel.XLWorkbook())
        {
            var rk = wb.AddWorksheet("Ringkasan");
            rk.Cell(1, 1).Value = "REKAPITULASI";
            object[] judulRk = { "NO", "KODE", "BIDANG / SUB BIDANG / NAMA KEGIATAN", "WAKTU", "PKA", "ANGGARAN SEMULA (Rp)", "ANGGARAN MENJADI (Rp)" };
            for (int i = 0; i < judulRk.Length; i++) rk.Cell(3, i + 1).Value = judulRk[i].ToString();
            rk.Cell(4, 1).Value = 1; rk.Cell(4, 2).Value = "2.3.10."; rk.Cell(4, 3).Value = "Jalan Desa"; rk.Cell(4, 5).Value = "Pelaksana Uji";

            var rab = wb.AddWorksheet("2.3.10 Jalan");
            rab.Cell(3, 1).Value = "Kegiatan"; rab.Cell(3, 2).Value = ":"; rab.Cell(3, 3).Value = "2.3.10. Pembangunan Jalan Desa **)";
            rab.Cell(4, 1).Value = "Waktu Pelaksanaan"; rab.Cell(4, 2).Value = ":"; rab.Cell(4, 3).Value = "6 Bulan";
            string[] j = { "KODE REKENING", "URAIAN KEGIATAN / AKUN / RINCIAN", "SUMBER DANA", "VOLUME SEMULA", "HARGA SATUAN SEMULA (Rp)", "JUMLAH SEMULA (Rp)", "VOLUME MENJADI", "HARGA SATUAN MENJADI (Rp)", "JUMLAH MENJADI (Rp)", "BERTAMBAH / (BERKURANG) (Rp)" };
            for (int i = 0; i < j.Length; i++) rab.Cell(6, i + 1).Value = j[i];
            void Baris(int r, string a, string b, string c, string d, double e, double f, string g, double h, double i)
            {
                rab.Cell(r, 1).Value = a; rab.Cell(r, 2).Value = b; rab.Cell(r, 3).Value = c;
                if (d != "") rab.Cell(r, 4).Value = d;
                if (e != 0) rab.Cell(r, 5).Value = e;
                rab.Cell(r, 6).Value = f;
                if (g != "") rab.Cell(r, 7).Value = g;
                if (h != 0) rab.Cell(r, 8).Value = h;
                rab.Cell(r, 9).Value = i;
            }
            Baris(7, "5.", "BELANJA", "", "", 0, 982_500, "", 0, 1_332_500);
            Baris(8, "", "01. Rabat Beton RT 01", "", "", 0, 982_500, "", 0, 1_332_500);
            Baris(9, "5.3.5.", "Belanja Modal Jalan/Prasarana Jalan", "", "", 0, 982_500, "", 0, 1_332_500);
            Baris(10, "5.3.5.03.", "Belanja Modal Jalan Bahan Baku/Material", "", "", 0, 982_500, "", 0, 1_332_500);
            Baris(11, "", "01. Semen 50 kg", "DDS", "10 sak", 80_000, 800_000, "12 sak", 80_000, 960_000);
            Baris(12, "", "02. Air", "DDS", "3.050 liter", 50, 152_500, "3.050 liter", 50, 152_500);
            Baris(13, "", "03. Paku", "DDS", "0.5 kg", 20_000, 10_000, "1 kg", 20_000, 20_000);
            Baris(14, "", "04. Plastik (salah ketik)", "DDS", "4 rol", 50_000, 20_000, "4 rol", 50_000, 200_000);
            rab.Cell(15, 1).Value = "JUMLAH TOTAL (Rp)";

            var pd = wb.AddWorksheet("Pendapatan");
            pd.Cell(4, 1).Value = "KODE"; pd.Cell(4, 2).Value = "URAIAN"; pd.Cell(4, 3).Value = "SUMBER DANA"; pd.Cell(4, 4).Value = "SEMULA"; pd.Cell(4, 7).Value = "MENJADI"; pd.Cell(4, 10).Value = "BERTAMBAH / (BERKURANG) (Rp)";
            pd.Cell(5, 4).Value = "VOLUME"; pd.Cell(5, 5).Value = "HARGA SATUAN (Rp)"; pd.Cell(5, 6).Value = "JUMLAH (Rp)"; pd.Cell(5, 7).Value = "VOLUME"; pd.Cell(5, 8).Value = "HARGA SATUAN (Rp)"; pd.Cell(5, 9).Value = "JUMLAH (Rp)";
            pd.Cell(6, 1).Value = "4."; pd.Cell(6, 2).Value = "PENDAPATAN"; pd.Cell(6, 6).Value = 1_000_000; pd.Cell(6, 9).Value = 900_000;
            pd.Cell(7, 1).Value = "4.2.1.01."; pd.Cell(7, 2).Value = "Dana Desa";
            pd.Cell(8, 2).Value = "01. Dana Desa"; pd.Cell(8, 3).Value = "DDS"; pd.Cell(8, 4).Value = "1 tahun"; pd.Cell(8, 5).Value = 1_000_000; pd.Cell(8, 6).Value = 1_000_000;
            pd.Cell(8, 7).Value = "1 tahun"; pd.Cell(8, 8).Value = 900_000; pd.Cell(8, 9).Value = 900_000;

            var pb = wb.AddWorksheet("Pembiayaan");
            pb.Cell(4, 1).Value = "KODE"; pb.Cell(4, 2).Value = "URAIAN"; pb.Cell(4, 3).Value = "SUMBER DANA"; pb.Cell(4, 4).Value = "SEMULA"; pb.Cell(4, 8).Value = "MENJADI"; pb.Cell(4, 12).Value = "BERTAMBAH / (BERKURANG) (Rp)";
            string[] sub = { "VOL", "SAT", "HARGA SATUAN (Rp)", "JUMLAH (Rp)" };
            for (int i = 0; i < 4; i++) { pb.Cell(5, 4 + i).Value = sub[i]; pb.Cell(5, 8 + i).Value = sub[i]; }
            for (int i = 1; i <= 12; i++) pb.Cell(6, i).Value = i;
            pb.Cell(7, 1).Value = "6."; pb.Cell(7, 2).Value = "PEMBIAYAAN";
            pb.Cell(8, 1).Value = "6.1."; pb.Cell(8, 2).Value = "Penerimaan Pembiayaan"; pb.Cell(8, 7).Value = 250_000; pb.Cell(8, 11).Value = 250_000;
            pb.Cell(9, 1).Value = "6.1.1.01."; pb.Cell(9, 2).Value = "SILPA Tahun Sebelumnya";
            pb.Cell(10, 1).Value = "01."; pb.Cell(10, 2).Value = "SILPA Dana Desa"; pb.Cell(10, 3).Value = "DDS";
            pb.Cell(10, 4).Value = 1; pb.Cell(10, 5).Value = "Ls"; pb.Cell(10, 6).Value = 250_000; pb.Cell(10, 7).Value = 250_000;
            pb.Cell(10, 8).Value = 1; pb.Cell(10, 9).Value = "Ls"; pb.Cell(10, 10).Value = 250_000; pb.Cell(10, 11).Value = 250_000;
            pb.Cell(11, 2).Value = "PEMBIAYAAN NETTO";
            wb.SaveAs(path);
        }

        var h = ImporSiskeudes.Baca(new[] { path });
        Cek(h.Versi.ContainsKey("Semula") && h.Versi.ContainsKey("Menjadi"), "Impor: versi Semula dan Menjadi dikenali");
        var s = h.Versi["Semula"]; var m = h.Versi["Menjadi"];
        Cek(s.Kegiatan.Count == 1 && s.Kegiatan[0].Kode == "2.3.10" && s.Kegiatan[0].Waktu == "6 Bulan", "Impor: kegiatan, kode & waktu");
        Cek(s.Kegiatan[0].Nama == "Pembangunan Jalan Desa", $"Impor: nama kegiatan tanpa tanda **) ({s.Kegiatan[0].Nama})");
        Cek(s.Kegiatan[0].Pelaksana == "Pelaksana Uji", "Impor: pelaksana dari lembar rekap");
        Cek(s.Kegiatan[0].Paket.Count == 1 && s.Kegiatan[0].Paket[0].Nama == "Rabat Beton RT 01", "Impor: paket kegiatan");
        var rin = s.Kegiatan[0].Paket[0].Rincian;
        Cek(rin.Count == 4 && rin[1].Volume == 3050 && rin[2].Volume == 0.5m && rin[0].Satuan == "sak", "Impor: volume 3.050 / 0.5 / satuan terbaca benar");
        Cek(m.Kegiatan[0].Total == 1_332_500m, $"Impor: total Menjadi {m.Kegiatan[0].Total:N0}");
        Cek(h.Selisih.Any(x => x.Contains("Plastik")), "Impor: salah ketik volume × harga terdeteksi");
        Cek(s.Pendapatan.Sum(r => r.Jumlah) == 1_000_000 && m.Pendapatan.Sum(r => r.Jumlah) == 900_000 && s.Pendapatan[0].SumberDana == "DDS", "Impor: pendapatan semula/menjadi");
        Cek(s.PenerimaanPembiayaan.Sum(r => r.Jumlah) == 250_000 && s.PenerimaanPembiayaan[0].Satuan == "Ls", "Impor: pembiayaan (kolom VOL/SAT terpisah)");

        var tahun = S.SkenarioAktif!.Tahun;
        var a = S.BuatSkenarioDari("Impor Semula", tahun, s, false);
        var b = S.BuatSkenarioDari("Impor Menjadi", tahun, m, false);
        Cek(a != null && b != null && S.MuatDataSkenarioLain(b!)!.Kegiatan.Count == 1, "Impor: skenario tersimpan");
        var doc = Laporan.BuatPerubahan(S.Desa, a!, s, b!, m);
        Cek(doc.Blocks.Count > 5, "Laporan Perubahan (Semula – Menjadi)");
        S.HapusSkenario(a!);
        S.HapusSkenario(b!);
        S.SkenarioAktif = S.SkenarioTahunIni.First(x => x.Nama == "Hasil Musdes");
    }

    static void IsiContoh(AppState S)
    {
        var d = S.Desa;
        d.NamaDesa = "Desa Contoh Makmur";
        d.KodeDesa = "04.2002";
        d.Kecamatan = "Kecamatan Contoh";
        d.Kabupaten = "Kabupaten Contoh";
        d.KepalaDesa = "Kepala Desa Contoh";
        d.KaurKeuangan = "Bendahara Contoh";
        d.Perangkat.Add(new Perangkat { Nama = "Perangkat Satu", Jabatan = "Kaur Perencanaan" });
        d.Perangkat.Add(new Perangkat { Nama = "Perangkat Dua", Jabatan = "Kasi Kesejahteraan" });
        S.PerbaruiDaftarReferensi();

        S.GantiNama(S.SkenarioAktif!, "Hasil Musdes");
        var data = S.Data!;
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.1.01", Uraian = "Dana Desa tahap I-III", Volume = 1, Satuan = "Tahun", HargaSatuan = 1_200_000_000, SumberDana = "DDS" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.3.01", Uraian = "Alokasi Dana Desa", Volume = 1, Satuan = "Tahun", HargaSatuan = 800_000_000, SumberDana = "ADD" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.2.2.01", Uraian = "Bagi hasil pajak dan retribusi daerah kabupaten", Volume = 1, Satuan = "Tahun", HargaSatuan = 112_000_000, SumberDana = "PBH" });
        data.Pendapatan.Add(new RincianAnggaran { KodeRekening = "4.1.2.01", Uraian = "Hasil lelang tanah kas desa", Volume = 1, Satuan = "Tahun", HargaSatuan = 300_000_000, SumberDana = "PAD" });
        data.PenerimaanPembiayaan.Add(new RincianAnggaran { KodeRekening = "6.1.1.01", Uraian = "SiLPA Dana Desa tahun lalu", Volume = 1, Satuan = "Paket", HargaSatuan = 25_000_000, SumberDana = "DDS" });

        Kegiatan K(string sub, int no, string nama, string sd, params (string rek, string uraian, decimal vol, string sat, decimal harga)[] rinci)
        {
            var k = new Kegiatan { KodeSubBidang = sub, NoUrut = no, Nama = nama, Lokasi = d.NamaDesa, Pelaksana = "Perangkat Satu", Keluaran = nama, SatuanKeluaran = "Kegiatan" };
            var p = new Paket { Nama = nama, Pola = "Swakelola", Sifat = "Non Fisik", Satuan = "Paket" };
            p.SumberDana = sd;
            foreach (var x in rinci)
                p.Rincian.Add(new RincianAnggaran { KodeRekening = x.rek, Uraian = x.uraian, Volume = x.vol, Satuan = x.sat, HargaSatuan = x.harga, SumberDana = sd });
            k.Paket.Add(p);
            data.Kegiatan.Add(k);
            return k;
        }
        K("01.01", 1, "Penyediaan Penghasilan Tetap dan Tunjangan Kepala Desa", "ADD",
            ("5.1.1.01", "Penghasilan tetap kepala desa", 12, "bulan", 3_714_500),
            ("5.1.1.02", "Tunjangan jabatan kepala desa", 12, "bulan", 1_000_000));
        K("1.1", 4, "Penyediaan Operasional Pemerintah Desa (ATK, Honor PKPKD dan PPKD dll)", "ADD",
            ("5.2.1.01", "Alat tulis kantor untuk pelayanan administrasi pemerintahan desa selama satu tahun penuh", 1, "paket", 15_000_000),
            ("5.2.5.01", "Langganan listrik kantor desa", 12, "bulan", 450_000));
        K("2.3", 10, "Pembangunan/Rehabilitasi/Peningkatan Jalan Desa Dusun Satu sampai Dusun Tiga beserta Drainase dan Talud", "DDS",
            ("5.3.5.03", "Batu pecah 2/3", 112.5m, "m3", 350_000),
            ("5.3.5.03", "Semen 50 kg", 1200, "sak", 80_000),
            ("5.3.5.02", "Upah tukang dan pekerja", 180, "hok", 120_000));
        K("5.3", 0, "Penanganan Keadaan Mendesak", "DDS",
            ("5.4.1.01", "BLT Dana Desa untuk keluarga penerima manfaat", 120, "KK", 3_600_000));
        data.Catatan = "Disepakati prioritas jalan Dusun Satu. Usulan lampu jalan ditunda ke tahun berikutnya.";
    }

    static void AmbilGambar(string folder, AppState S)
    {
        var ukuran = new (int w, int h, double zoom, string nama)[]
        {
            (1366, 728, 1.0, "1366x768"),
            (900, 580, 1.0, "minimum"),
            (1920, 1040, 1.3, "fullhd-zoom130"),
        };
        var w = new MainWindow { WindowStartupLocation = WindowStartupLocation.Manual, Left = 0, Top = 0, ShowActivated = false };
        w.Show();
        foreach (var (lebar, tinggi, zoom, nama) in ukuran)
        {
            w.WindowState = WindowState.Normal;
            w.Width = lebar;
            w.Height = tinggi;
            w.AturZoomUji(zoom);
            foreach (var kunci in MainWindow.DaftarHalaman)
            {
                var h = w.Buka(kunci);
                Tunggu();
                if (h is IsianAnggaranView iv)
                {
                    foreach (var (bagian, tab) in new[] { (0, 0), (0, 1), (0, 2), (0, 3), (1, 0), (1, 1), (2, 0), (2, 1), (2, 2), (2, 3), (2, 4), (3, 0), (3, 1), (4, 0) })
                    {
                        iv.Buka(bagian, tab);
                        Tunggu();
                        Render(w, Path.Combine(folder, $"{nama}_isian_{bagian}{tab}.png"));
                        CekTerpotong(w, $"{nama}/isian {bagian}.{tab}");
                        AuditTampilan.Periksa(w, $"{nama}/isian {bagian}.{tab}");
                        if (nama == "1366x768") AuditTampilan.KlikSemua(w, $"isian {bagian}.{tab}");
                    }
                    // Alur Ubah → Simpan pada kegiatan dan rincian RAB tanpa mengubah nilai
                    iv.Buka(0, 2); Tunggu();
                    var total = S.Hasil!.TotalBelanja;
                    iv.HalamanAktif!.Ubah(); Tunggu();
                    Render(w, Path.Combine(folder, $"{nama}_isian_mengubah.png"));
                    Cek(iv.HalamanAktif!.Simpan(), $"{nama}: Ubah → Simpan kegiatan");
                    iv.Buka(2, 4); Tunggu();
                    iv.HalamanAktif!.Ubah(); Tunggu();
                    Cek(iv.HalamanAktif!.Simpan(), $"{nama}: Ubah → Simpan rincian RAB");
                    S.Hitung();
                    Cek(S.Hasil!.TotalBelanja == total, $"{nama}: total belanja tetap setelah Ubah/Simpan");
                    continue;
                }
                if (h is ParameterView pv0)
                {
                    pv0.BukaTab(0); Tunggu();
                }
                Render(w, Path.Combine(folder, $"{nama}_{kunci}.png"));
                CekTerpotong(w, $"{nama}/{kunci}");
                AuditTampilan.Periksa(w, $"{nama}/{kunci}");
                if (nama == "1366x768") AuditTampilan.KlikSemua(w, kunci);
                if (h is RingkasanView rv)
                {
                    rv.Gulir.ScrollToVerticalOffset(380);
                    Tunggu();
                    Render(w, Path.Combine(folder, $"{nama}_{kunci}_bawah.png"));
                    CekTerpotong(w, $"{nama}/{kunci}-bawah");
                    rv.Gulir.ScrollToTop();
                }
                if (h is DataDesaView dv)
                {
                    dv.BukaTab(1); Tunggu();
                    Render(w, Path.Combine(folder, $"{nama}_{kunci}_pka.png"));
                    CekTerpotong(w, $"{nama}/{kunci}-pka");
                    AuditTampilan.Periksa(w, $"{nama}/{kunci}-pka");
                    dv.BukaTab(0);
                }
            }
        }
        var pv = new PratinjauWindow { Width = 1100, Height = 800, Left = 0, Top = 0, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        pv.Show();
        Tunggu();
        Render(pv, Path.Combine(folder, "pratinjau.png"));
        pv.Close();
        var pv2 = new PratinjauWindow(Laporan.Perubahan) { Width = 1100, Height = 800, Left = 0, Top = 0, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        pv2.Show();
        Tunggu();
        Render(pv2, Path.Combine(folder, "pratinjau_perubahan.png"));
        AuditTampilan.Periksa(pv2, "pratinjau perubahan");
        pv2.Close();
        var tiruan = Path.Combine(folder, "tiruan_siskeudes.xlsx");
        if (File.Exists(tiruan))
        {
            var hasilImpor = ImporSiskeudes.Baca(new[] { tiruan });
            var di = new DialogImpor(hasilImpor, DateTime.Today.Year) { Left = 0, Top = 0, WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
            di.Show();
            Tunggu();
            Render(di, Path.Combine(folder, "dialog_impor.png"));
            AuditTampilan.Periksa(di, "dialog impor");
            di.Close();
        }
        w.Close();
    }

    static void Render(Window w, string path)
    {
        var el = (FrameworkElement)w.Content;
        el.UpdateLayout();
        var rtb = new RenderTargetBitmap((int)Math.Ceiling(el.ActualWidth), (int)Math.Ceiling(el.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, el.ActualWidth, el.ActualHeight));
            dc.DrawRectangle(new VisualBrush(el), null, new Rect(0, 0, el.ActualWidth, el.ActualHeight));
        }
        rtb.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    /// <summary>Mencari TextBlock yang terlihat tetapi tidak mendapat ruang sebesar yang dibutuhkan.</summary>
    static void CekTerpotong(Window w, string halaman)
    {
        int n = 0;
        var contoh = new List<string>();
        foreach (var tb in UiBantu.Turunan<TextBlock>(w))
        {
            if (!tb.IsVisible || string.IsNullOrWhiteSpace(tb.Text) || tb.ActualWidth < 1) continue;
            var m = tb.Margin;
            tb.Measure(new Size(tb.ActualWidth + m.Left + m.Right, double.PositiveInfinity));
            var perlu = new Size(Math.Max(0, tb.DesiredSize.Width - m.Left - m.Right), Math.Max(0, tb.DesiredSize.Height - m.Top - m.Bottom));
            if (perlu.Height > tb.ActualHeight + 1.5 || (tb.TextWrapping == TextWrapping.NoWrap && perlu.Width > tb.ActualWidth + 1.5))
            {
                n++;
                if (contoh.Count < 8) contoh.Add($"\"{(tb.Text.Length > 50 ? tb.Text[..50] + "…" : tb.Text)}\" perlu {perlu.Width:0}x{perlu.Height:0}, dapat {tb.ActualWidth:0}x{tb.ActualHeight:0}");
                continue;
            }
            // Kata yang dipotong di tengah karena kolom terlalu sempit (mis. "Volum|e")
            if (tb.TextWrapping != TextWrapping.NoWrap)
            {
                var tf = new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
                var lebar = tb.ActualWidth - tb.Padding.Left - tb.Padding.Right + 0.5;
                var dpi = VisualTreeHelper.GetDpi(tb).PixelsPerDip;
                foreach (var kata in tb.Text.Split(new[] { ' ', '/', '\\', '-', '\n', '(', ')' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var ft = new FormattedText(kata, Budaya.Id, FlowDirection.LeftToRight, tf, tb.FontSize, Brushes.Black, dpi);
                    if (ft.Width > lebar + 1.5)
                    {
                        n++;
                        if (contoh.Count < 8) contoh.Add($"kata \"{kata}\" terpotong (perlu {ft.Width:0}, dapat {lebar:0})");
                        break;
                    }
                }
            }
        }
        // Tombol yang (sebagian) berada di luar jendela dan tidak bisa digulir
        var akar = (FrameworkElement)w.Content;
        foreach (var b in UiBantu.Turunan<Button>(w))
        {
            if (!b.IsVisible || b.ActualWidth < 1 || UiBantu.Leluhur<ScrollViewer>(VisualTreeHelper.GetParent(b)) != null) continue;
            var r = b.TransformToAncestor(akar).TransformBounds(new Rect(0, 0, b.ActualWidth, b.ActualHeight));
            for (var p = VisualTreeHelper.GetParent(b) as FrameworkElement; p != null; p = VisualTreeHelper.GetParent(p) as FrameworkElement)
            {
                var rp = p.TransformToAncestor(akar).TransformBounds(new Rect(0, 0, p.ActualWidth, p.ActualHeight));
                if (r.Bottom > rp.Bottom + 1 || r.Right > rp.Right + 1)
                {
                    n++;
                    if (contoh.Count < 8) contoh.Add($"tombol \"{b.Content}\" terpotong oleh {p.GetType().Name} ({r.Bottom:0} > {rp.Bottom:0})");
                    break;
                }
                if (ReferenceEquals(p, akar)) break;
            }
        }
        Cek(n == 0, $"Teks terpotong di {halaman}: {n}" + (contoh.Count > 0 ? "\n        " + string.Join("\n        ", contoh) : ""));
    }
}
