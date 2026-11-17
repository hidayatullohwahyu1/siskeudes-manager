using System.IO;
using System.Text;

namespace RancangAPBDes.Core;

/// <summary>
/// Mengisi file data dari ekspor Excel Siskeudes tanpa jendela:
/// RancangAPBDes.exe --impor-awal "file.apbdes" "RAB.xlsx" "Pendapatan.xlsx" "Pembiayaan.xlsx".
/// Membuat skenario Semula & Menjadi (dikunci/posting) dan rancangan tahun berikutnya dari Menjadi.
/// </summary>
public static class ImporAwal
{
    public static int Jalankan(string fileData, List<string> excel)
    {
        var log = new StringBuilder();
        var S = AppState.Instance;
        int kode = 0;
        try
        {
            if (!S.BukaFile(fileData)) throw new InvalidOperationException("File data tidak dapat dibuka.");
            // Argumen "Kunci=Nilai" mengisi data desa yang tidak ada di Excel (mis. Kabupaten=…, KodeDesa=…)
            var tambahan = excel.Where(a => a.Contains('=') && !a.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Split('=', 2)).ToDictionary(p => p[0], p => p[1]);
            excel = excel.Where(a => a.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)).ToList();
            var h = ImporSiskeudes.Baca(excel);
            foreach (var (k, isi) in tambahan) h.InfoDesa[k] = isi;
            if (!h.Versi.Any()) throw new InvalidOperationException("Tidak ada data anggaran di file Excel.");

            // Identitas desa
            var d = S.Desa;
            var info = h.InfoDesa;
            if (info.TryGetValue("NamaDesa", out var v)) d.NamaDesa = v;
            if (info.TryGetValue("Kecamatan", out v)) d.Kecamatan = v;
            if (info.TryGetValue("KepalaDesa", out v)) d.KepalaDesa = v;
            if (info.TryGetValue("Sekdes", out v)) d.Sekdes = v;
            if (info.TryGetValue("TempatTtd", out v)) d.TempatTtd = v;
            if (info.TryGetValue("Kabupaten", out v)) d.Kabupaten = v;
            if (info.TryGetValue("KodeDesa", out v)) d.KodeDesa = v;
            if (info.TryGetValue("KaurKeuangan", out v)) d.KaurKeuangan = v;
            foreach (var nama in h.Pelaksana.Values.Distinct(StringComparer.OrdinalIgnoreCase))
                if (!d.Perangkat.Any(p => string.Equals(p.Nama, nama, StringComparison.OrdinalIgnoreCase)))
                    d.Perangkat.Add(new Perangkat { Nama = nama, Jabatan = "Pelaksana Kegiatan" });

            // Referensi
            var r = S.Referensi;
            foreach (var (k, n) in h.RekeningBaru) if (!r.Rekening.Any(x => x.Kode == k)) r.Rekening.Add(new RefRekening { Kode = k, Nama = n });
            foreach (var (k, n) in h.KegiatanBaru) if (!r.Kegiatan.Any(x => x.Kode == k)) r.Kegiatan.Add(new RefKegiatan { Kode = k, Nama = n });
            foreach (var s in h.Satuan) if (!r.Satuan.Any(x => string.Equals(x.Nama, s, StringComparison.OrdinalIgnoreCase))) r.Satuan.Add(new RefSatuan { Nama = s });
            S.PerbaruiDaftarReferensi();

            var tahun = info.TryGetValue("Tahun", out v) && int.TryParse(v, out var t) ? t : DateTime.Today.Year;
            foreach (var data in h.Versi.Values)
                foreach (var k in data.Kegiatan)
                {
                    if (string.IsNullOrWhiteSpace(k.Lokasi)) k.Lokasi = d.NamaDesa;
                    foreach (var p in k.Paket) if (string.IsNullOrWhiteSpace(p.Lokasi)) p.Lokasi = k.Lokasi;
                }

            // Skenario lama yang masih kosong dihapus
            var kosong = S.SemuaSkenario.Where(s =>
            {
                var x = S.MuatDataSkenarioLain(s);
                return x != null && x.Kegiatan.Count == 0 && x.Pendapatan.Count == 0 && x.PenerimaanPembiayaan.Count == 0 && x.PengeluaranPembiayaan.Count == 0;
            }).ToList();

            SkenarioInfo? terakhir = null;
            DataSkenario? acuan = null;
            foreach (var (versi, nama) in new[] { ("Semula", $"APBDes {tahun} (Semula)"), ("Menjadi", $"Perubahan APBDes {tahun} (Menjadi)"), ("Anggaran", $"APBDes {tahun}") })
            {
                if (!h.Versi.TryGetValue(versi, out var data)) continue;
                var info2 = S.BuatSkenarioDari(nama, tahun, data, pilih: false);
                if (info2 == null) continue;
                S.SetFinal(info2, true);
                terakhir = info2;
                acuan = data;
                var hit = Kalkulasi.Hitung(data, r);
                log.AppendLine($"{nama}: {data.Kegiatan.Count} kegiatan, pendapatan {hit.TotalPendapatan:N0}, belanja {hit.TotalBelanja:N0}, " +
                               $"penerimaan pembiayaan {hit.PenerimaanPembiayaan:N0}, pengeluaran pembiayaan {hit.PengeluaranPembiayaan:N0} (dikunci/posting)");
            }
            if (acuan != null)
            {
                var rancangan = S.BuatSkenarioDari($"Rancangan APBDes {tahun + 1}", tahun + 1, AppState.SalinDalam(acuan), pilih: true);
                if (rancangan != null) log.AppendLine($"Rancangan APBDes {tahun + 1}: salinan dari {terakhir?.Nama} sebagai titik awal musyawarah (draft).");
            }
            foreach (var s in kosong)
                if (S.SemuaSkenario.Count > 1) S.HapusSkenario(s);
            if (S.SemuaSkenario.FirstOrDefault(s => s.Nama == $"Rancangan APBDes {tahun + 1}") is { } pilih)
                S.PilihTahunDanSkenario(pilih.Tahun, pilih.Id);

            log.AppendLine($"Desa: {d.NamaDesa}, Kec. {d.Kecamatan}; pelaksana {d.Perangkat.Count}; satuan {r.Satuan.Count}");
            log.AppendLine($"Baris perlu dicek (jumlah ≠ volume × harga): {h.Selisih.Count}");
            foreach (var s in h.Selisih) log.AppendLine("  " + s);
            if (!S.SimpanSegera()) throw new IOException("Gagal menyimpan.");
            S.TutupFile();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Impor awal");
            log.AppendLine("GALAT: " + ex.Message);
            kode = 1;
        }
        File.WriteAllText(Path.Combine(Folder.DataAplikasi, "impor-awal.txt"), log.ToString());
        return kode;
    }
}
