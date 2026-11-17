using System.IO;
using Microsoft.Data.Sqlite;

namespace RancangAPBDes.Core;

public sealed class Database : IDisposable
{
    const string TandaAplikasi = "RancangAPBDes";
    readonly SqliteConnection _c;
    SqliteTransaction? _tx;

    public string FilePath { get; }

    Database(string path, SqliteConnection c)
    {
        FilePath = path;
        _c = c;
    }

    public static Database Open(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 10
        }.ToString();

        var c = new SqliteConnection(cs);
        try
        {
            c.Open();
            var db = new Database(path, c);
            db.Exec("PRAGMA journal_mode=DELETE;");
            db.Exec("PRAGMA synchronous=FULL;");

            var cek = Convert.ToString(db.Scalar("PRAGMA quick_check;"));
            if (!string.Equals(cek, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("File data rusak (" + cek + ").");

            var jumlahTabel = Convert.ToInt64(db.Scalar("SELECT count(*) FROM sqlite_master WHERE type='table';"));
            var adaMeta = Convert.ToInt64(db.Scalar("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='meta';")) > 0;
            if (jumlahTabel > 0 && !adaMeta)
                throw new InvalidDataException("File ini bukan file data Rancang APBDes.");

            db.Exec(@"CREATE TABLE IF NOT EXISTS meta(kunci TEXT PRIMARY KEY, nilai TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS skenario(
  id TEXT PRIMARY KEY, tahun INTEGER NOT NULL, nama TEXT NOT NULL, status TEXT NOT NULL,
  dibuat TEXT NOT NULL, diubah TEXT NOT NULL, data TEXT NOT NULL);");

            var app = db.GetMeta("aplikasi");
            if (app == null)
            {
                db.SetMeta("aplikasi", TandaAplikasi);
                db.SetMeta("versi_skema", "1");
            }
            else if (app != TandaAplikasi)
            {
                throw new InvalidDataException("File ini bukan file data Rancang APBDes.");
            }
            return db;
        }
        catch
        {
            c.Dispose();
            throw;
        }
    }

    SqliteCommand Cmd(string sql, params (string, object?)[] p)
    {
        var cmd = _c.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = _tx;
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }

    public void Exec(string sql, params (string, object?)[] p)
    {
        using var cmd = Cmd(sql, p);
        cmd.ExecuteNonQuery();
    }

    public object? Scalar(string sql, params (string, object?)[] p)
    {
        using var cmd = Cmd(sql, p);
        return cmd.ExecuteScalar();
    }

    public void InTransaction(Action a)
    {
        if (_tx != null) { a(); return; }
        _tx = _c.BeginTransaction();
        try
        {
            a();
            _tx.Commit();
        }
        catch
        {
            try { _tx.Rollback(); } catch { /* abaikan */ }
            throw;
        }
        finally
        {
            _tx.Dispose();
            _tx = null;
        }
    }

    public string? GetMeta(string kunci)
    {
        var v = Scalar("SELECT nilai FROM meta WHERE kunci=$k;", ("$k", kunci));
        return v is string s ? s : null;
    }

    public void SetMeta(string kunci, string nilai) =>
        Exec("INSERT INTO meta(kunci,nilai) VALUES($k,$v) ON CONFLICT(kunci) DO UPDATE SET nilai=excluded.nilai;",
            ("$k", kunci), ("$v", nilai));

    public List<SkenarioInfo> DaftarSkenario()
    {
        var list = new List<SkenarioInfo>();
        using var cmd = Cmd("SELECT id,tahun,nama,status,dibuat,diubah FROM skenario ORDER BY tahun, dibuat;");
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new SkenarioInfo
            {
                Id = r.GetString(0),
                Tahun = r.GetInt32(1),
                Nama = r.GetString(2),
                Status = r.GetString(3),
                Dibuat = Tanggal(r.GetString(4)),
                Diubah = Tanggal(r.GetString(5)),
            });
        }
        return list;
    }

    static DateTime Tanggal(string s) =>
        DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var d) ? d : DateTime.Now;

    static string Teks(DateTime d) => d.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

    public string? MuatData(string id) => Scalar("SELECT data FROM skenario WHERE id=$id;", ("$id", id)) as string;

    public void TambahSkenario(SkenarioInfo s, string data) =>
        Exec("INSERT INTO skenario(id,tahun,nama,status,dibuat,diubah,data) VALUES($id,$t,$n,$s,$c,$u,$d);",
            ("$id", s.Id), ("$t", s.Tahun), ("$n", s.Nama), ("$s", s.Status),
            ("$c", Teks(s.Dibuat)), ("$u", Teks(s.Diubah)), ("$d", data));

    public void UbahInfo(SkenarioInfo s) =>
        Exec("UPDATE skenario SET nama=$n, status=$s, diubah=$u WHERE id=$id;",
            ("$id", s.Id), ("$n", s.Nama), ("$s", s.Status), ("$u", Teks(s.Diubah)));

    public void SimpanData(string id, string data, DateTime diubah) =>
        Exec("UPDATE skenario SET data=$d, diubah=$u WHERE id=$id;", ("$id", id), ("$d", data), ("$u", Teks(diubah)));

    public void HapusSkenario(string id) => Exec("DELETE FROM skenario WHERE id=$id;", ("$id", id));

    /// <summary>Salinan aman (memakai API backup SQLite, aman walau sedang dipakai).</summary>
    public void SalinKe(string tujuan)
    {
        var dir = Path.GetDirectoryName(tujuan);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = tujuan + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
        using (var d = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = tmp, Pooling = false }.ToString()))
        {
            d.Open();
            _c.BackupDatabase(d);
        }
        File.Move(tmp, tujuan, overwrite: true);
    }

    public void Dispose()
    {
        try { _tx?.Dispose(); } catch { /* abaikan */ }
        _c.Dispose();
    }
}
