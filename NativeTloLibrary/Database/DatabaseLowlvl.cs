using System.Diagnostics;
using System.Text;

using SQLitePCL;

namespace TloSql;

public class DatabaseLowLvl
{
    private sqlite3 _connection;

    private Stopwatch _stopwatch = new Stopwatch();

    /// <summary>
    /// Сбрасывает и перезапускает таймер
    /// </summary>
    private void StartTimer()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    /// <summary>
    /// Останавливает таймер и пишет в консоль буквы
    /// </summary>
    /// <param name="text">Буквы которые писать</param>
    private void StopTimer(string text)
    {
        _stopwatch.Stop();
        Console.WriteLine($"{text} {_stopwatch.Elapsed}");
    }

    public DatabaseLowLvl()
    {
        SQLitePCL.raw.SetProvider(new SQLite3Provider_e_sqlite3());
        
        OpenDb();

        SetPragmas();

        BeginTransaction();

        StartTimer();
        
        Truncate("main.Topics");
        Truncate("main.KeepersSeeders");
        
        StopTimer("Clear all table");
    }

    private void ExecuteSql(string sql)
    {
        var rc = raw.sqlite3_exec(_connection, sql);

        CheckRc(rc);
    }

    private sqlite3_stmt PrepareStatement(string sql)
    {
        var rc = raw.sqlite3_prepare_v2(_connection, sql, out var stmt);
        
        CheckRc(rc);

        return stmt;
    }

    private void Bind(sqlite3_stmt stmt, int index, int value)
    {
        var rc = raw.sqlite3_bind_int(stmt, index, value);
        CheckRc(rc);
    }
    
    private void Bind(sqlite3_stmt stmt, int index, long value)
    {
        var rc = raw.sqlite3_bind_int64(stmt, index, value);
        CheckRc(rc);
    }
    
    private void Bind(sqlite3_stmt stmt, int index, string value)
    {
        var rc = raw.sqlite3_bind_text(stmt, index, value);
        CheckRc(rc);
    }

    private void OpenDb()
    {
        var rc = raw.sqlite3_open("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\webtlofordotnet.db", out _connection);
        
        CheckRc(rc);
    }
    
    private void CheckRc(int rc)
    {
        if (rc != raw.SQLITE_OK && rc != raw.SQLITE_DONE)
            throw new Exception(rc.ToString() + raw.sqlite3_errmsg(_connection).utf8_to_string());
    }
    
    // public void LoadSeedersByChunksWithParameter()
    // {
    //     StartTimer();
    //
    //     var chunkSize = 32766 / 3;
    //
    //     var chunks = ChunkArray(_seeders, chunkSize);
    //
    //     var sql = "INSERT INTO main.KeepersSeeders (topic_id, keeper_id, keeper_name) VALUES ";
    //     var paramsPart = "(?, ?, ?),";
    //
    //     ConstructStatement(chunkSize, sql, paramsPart, out var stmt);
    //
    //     for (var i = 0; i < chunks.Count; i++)
    //     {
    //         if (i == chunks.Count - 1)
    //         {
    //             ConstructStatement(chunks[i].Length, sql, paramsPart, out var stmtLast);
    //
    //             BindSeederParams(chunks, i, stmtLast);
    //
    //             ExecBindedStatement(stmtLast);
    //         }
    //         else
    //         {
    //             BindSeederParams(chunks, i, stmt);
    //
    //             ExecBindedStatement(stmt);
    //         }
    //     }
    //
    //     StopTimer($"Insert all seeders with chunk size {chunkSize}");
    // }

    private void ExecBindedStatement(sqlite3_stmt stmt)
    {
        var rc = raw.sqlite3_step(stmt);
        CheckRc(rc);
        rc = raw.sqlite3_clear_bindings(stmt);
        CheckRc(rc);
        rc =  raw.sqlite3_reset(stmt);
        CheckRc(rc);
    }

    private void BindSeederParams(List<KeepersSeeder[]> chunks, int ci, sqlite3_stmt stmt)
    {
        for (var i = 0; i < chunks[ci].Length; i++)
        {
            Bind(stmt, 3 * i + 1, chunks[ci][i].TopicId);
            Bind(stmt, 3 * i + 2, chunks[ci][i].KeeperId);
            Bind(stmt, 3 * i + 3, chunks[ci][i].KeeperName);
        }
    }

    // public void LoadTopicsByChunksWithParameter()
    // {
    //     StartTimer();
    //
    //     var chunkSize = 32766 / 13;
    //
    //     var chunks = ChunkArray(_topics, chunkSize);
    //
    //     var sql = "INSERT INTO main.Topics (id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen) VALUES ";
    //     var paramsPart = $"(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?),";
    //
    //     ConstructStatement(chunkSize, sql, paramsPart, out var stmt);
    //
    //     for (var i = 0; i < chunks.Count; i++)
    //     {
    //         if (i == chunks.Count - 1)
    //         {
    //             ConstructStatement(chunks[i].Length, sql, paramsPart, out var stmtLast);
    //
    //             BindKeeperParams(chunks, i, stmtLast);
    //
    //             ExecBindedStatement(stmtLast);
    //         }
    //         else
    //         {
    //             BindKeeperParams(chunks, i, stmt);
    //
    //             ExecBindedStatement(stmt);
    //         }
    //     }
    //
    //     StopTimer($"Insert all topics with chunk size {chunkSize}");
    // }

    private void BindKeeperParams(List<Topic[]> chunks, int ci, sqlite3_stmt stmt)
    {
        for (var i = 0; i < chunks[ci].Length; i++)
        {
            Bind(stmt, 13 * i + 1, chunks[ci][i].TopicId);
            Bind(stmt, 13 * i + 2, chunks[ci][i].ForumId);
            Bind(stmt, 13 * i + 3, chunks[ci][i].Name);
            Bind(stmt, 13 * i + 4, chunks[ci][i].InfoHash);
            Bind(stmt, 13 * i + 5, chunks[ci][i].Seeders);
            Bind(stmt, 13 * i + 6, chunks[ci][i].TorSizeBytes);
            Bind(stmt, 13 * i + 7, chunks[ci][i].TorStatus);
            Bind(stmt, 13 * i + 8, chunks[ci][i].RegTime);
            Bind(stmt, 13 * i + 9, chunks[ci][i].SeedersUpdatesToday);
            Bind(stmt, 13 * i + 10, chunks[ci][i].SeedersUpdatesDays);
            Bind(stmt, 13 * i + 11, chunks[ci][i].KeepingPriority);
            Bind(stmt, 13 * i + 12, chunks[ci][i].TopicPoster);
            Bind(stmt, 13 * i + 13, chunks[ci][i].SeederLastSeen);
        }
    }

    private void ConstructStatement(int chunkSize, string sql, string paramsPart, out sqlite3_stmt stmt)
    {
        var sb = new StringBuilder();
            
        sb.Append(sql);
        
        for(var i = 0; i < chunkSize; i++)
            sb.Append(paramsPart);

        sb.Remove(sb.Length - 1, 1);
        sb.Append(';');

        var rc = raw.sqlite3_prepare_v2(_connection, sb.ToString(), out stmt);
        CheckRc(rc);
    }
    
    public void LoadSeedersPreloaded()
    {
        var path = "C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\seeders.sql";
        
        LoadPreloaded(path);
    }

    public void LoadTopicsPreloaded()
    {
        var path = "C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\topics.sql";
        
        LoadPreloaded(path);
    }
    
    private void LoadPreloaded(string path)
    {
        var sql = File.ReadAllText(path);

        StartTimer();

        ExecuteSql(sql);
        
        StopTimer(path);
    }

    private void SetPragmas()
    {

        ExecuteSql("PRAGMA synchronous=OFF;");
        ExecuteSql("PRAGMA journal_mode=OFF;");
        ExecuteSql("PRAGMA count_changes=OFF;");
        ExecuteSql("PRAGMA temp_store=OFF;");
        ExecuteSql("PRAGMA page_size=65536;");
        ExecuteSql("PRAGMA cache_size=-16777216;");
        ExecuteSql("PRAGMA locking_mode = EXCLUSIVE;");
    }
    
    public void Close()
    {
        CommitTransaction();
        Optimize();
    }
    
    private void Truncate(string table) =>
        ExecuteSql($"delete from {table}");

    private void BeginTransaction() =>
        ExecuteSql("begin;");

    private void CommitTransaction() =>
        ExecuteSql("commit;");
    
    private void Optimize() =>
        ExecuteSql("PRAGMA optimize;");

    public static List<T[]> ChunkArray<T>(T[] array, int chunkSize)
    {
        var chunks = new List<T[]>();

        for (int i = 0; i < array.Length; i += chunkSize)
        {
            var chunk = new T[Math.Min(chunkSize, array.Length - i)];
            Array.Copy(array, i, chunk, 0, chunk.Length);
            chunks.Add(chunk);
        }

        return chunks;
    }
}
