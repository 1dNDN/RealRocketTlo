using System.Text;

using NativeTloLibrary.Models;

using SQLitePCL;

namespace NativeTloLibrary.Database;

/// <summary>
///     Обертка вокруг низкоуровневого биндинга SQLITE
/// </summary>
public class DatabaseLowLvl
{
    // Поддержка многопоточности даже не в планах
    // Все взаимодействие строго синхронное

    // Запрос выполняется так:
    // ConstructStatement();
    // BindParams();
    // ExecBindedStatement();

    private sqlite3 _connection;



    public DatabaseLowLvl(string path)
    {
        raw.SetProvider(new SQLite3Provider_e_sqlite3());

        OpenDb(path);

        SetPragmas();

        BeginTransaction();

        Speedometer.Start();

        //TODO: Удолить, грешно
        Truncate("main.Topics");
        Truncate("main.KeepersSeeders");

        Speedometer.Stop("Clear all table");
    }

    /// <summary>
    ///     Выполнить SQL запрос
    /// </summary>
    /// <param name="sql">Текст SQL запроса</param>
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

    private void OpenDb(string path)
    {
        var rc = raw.sqlite3_open(path, out _connection);

        CheckRc(rc);
    }

    private void CheckRc(int rc)
    {
        if (rc != raw.SQLITE_OK && rc != raw.SQLITE_DONE)
            throw new Exception(rc + raw.sqlite3_errmsg(_connection).utf8_to_string());
    }

    /// <summary>
    ///     Грузит что угодно в бд
    /// </summary>
    /// <param name="data">Данные</param>
    /// <param name="tableName">Название таблицы, например <c>main.KeepersSeeders</c></param>
    /// <param name="columns">Столбцы, например <c>(topic_id, keeper_id, keeper_name)</c></param>
    /// <param name="countOfColumns">Число столбцов, например <c>3</c></param>
    /// <typeparam name="T">Чота</typeparam>
    public void LoadAnythingByChunksWithParameter<T>(T[] data, string tableName, string columns, Action<List<T[]>, int, sqlite3_stmt> bindSeederParamsFunc)
    {
        Speedometer.Start();

        var countOfColumns = columns.Split(',').Length;

        var chunkSize = 32766 / countOfColumns;

        var chunks = ChunkArray(data, chunkSize);

        var sql = $"INSERT INTO {tableName} {columns} VALUES ";
        var paramsPart = "(";

        for (var i = 0; i < countOfColumns - 1; i++)
            paramsPart += "?, ";

        paramsPart += "?),";

        ConstructStatement(chunkSize, sql, paramsPart, out var stmt);

        for (var i = 0; i < chunks.Count; i++)
            if (i == chunks.Count - 1)
            {
                ConstructStatement(chunks[i].Length, sql, paramsPart, out var stmtLast);

                bindSeederParamsFunc(chunks, i, stmtLast);

                ExecBindedStatement(stmtLast);
            }
            else
            {
                bindSeederParamsFunc(chunks, i, stmt);

                ExecBindedStatement(stmt);
            }

        Speedometer.Stop($"Insert all {tableName} with chunk size {chunkSize}");

    }

    public void LoadSeedersByChunksWithParameter(KeepersSeeder[] seeders) =>
        LoadAnythingByChunksWithParameter(seeders, "main.KeepersSeeders", "(topic_id, keeper_id, keeper_name)", BindParams);

    public void LoadTopicsByChunksWithParameter(Topic[] topics) =>
        LoadAnythingByChunksWithParameter(topics, "main.Topics", "(id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen)", BindParams);

    private void ExecBindedStatement(sqlite3_stmt stmt)
    {
        var rc = raw.sqlite3_step(stmt);
        CheckRc(rc);
        rc = raw.sqlite3_clear_bindings(stmt);
        CheckRc(rc);
        rc = raw.sqlite3_reset(stmt);
        CheckRc(rc);
    }

    private void BindParams(List<KeepersSeeder[]> chunks, int ci, sqlite3_stmt stmt)
    {
        for (var i = 0; i < chunks[ci].Length; i++)
        {
            Bind(stmt, 3 * i + 1, chunks[ci][i].TopicId);
            Bind(stmt, 3 * i + 2, chunks[ci][i].KeeperId);
            Bind(stmt, 3 * i + 3, chunks[ci][i].KeeperName);
        }
    }

    private void BindParams(List<Topic[]> chunks, int ci, sqlite3_stmt stmt)
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

        for (var i = 0; i < chunkSize; i++)
            sb.Append(paramsPart);

        sb.Remove(sb.Length - 1, 1);
        sb.Append(';');

        stmt = PrepareStatement(sb.ToString());
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

        Speedometer.Start();

        ExecuteSql(sql);

        Speedometer.Stop(path);
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

        for (var i = 0; i < array.Length; i += chunkSize)
        {
            var chunk = new T[Math.Min(chunkSize, array.Length - i)];
            Array.Copy(array, i, chunk, 0, chunk.Length);
            chunks.Add(chunk);
        }

        return chunks;
    }
}
