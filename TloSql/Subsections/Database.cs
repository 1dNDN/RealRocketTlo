using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TloSql;

public class Database
{
    private TloSql.KeepersSeeder[] _seeders = JsonSerializer.Deserialize<KeepersSeeder[]>(File.ReadAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\seeders.json"))!;
    private TloSql.Topic[] _topics = JsonSerializer.Deserialize<Topic[]>(File.ReadAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\topics.json"))!;
    
    private SqliteConnection _connection = new SqliteConnection("Data Source = C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\webtlofordotnet.db");

    private WebtlotestContext _db = new WebtlotestContext();

    private Stopwatch _stopwatch = new Stopwatch();

    private void StartTimer()
    {
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    private void StopTimer(string text)
    {
        _stopwatch.Stop();
        Console.WriteLine($"{text} {_stopwatch.Elapsed}");
    }

    public Database()
    {
        _connection.Open();
        
        var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous=OFF; PRAGMA journal_mode=OFF; PRAGMA count_changes=OFF; PRAGMA temp_store=OFF; PRAGMA page_size=65536; PRAGMA cache_size=-16777216;locking_mode = EXCLUSIVE;";
        command.ExecuteNonQuery();
        
        Begin();

        StartTimer();
        
        Truncate("main.Topics");
        Truncate("main.KeepersSeeders");
        
        StopTimer("Clear all table");
    }

    public void LoadSeedersByChunks(int chunkSize)
    {
        StartTimer();
        
        var chunks = ChunkArray(_seeders, chunkSize);

        foreach (var chunk in chunks)
        {
            var command = _connection.CreateCommand();
            
            var sb = new StringBuilder();
            
            sb.Append("INSERT INTO main.KeepersSeeders (topic_id, keeper_id, keeper_name) VALUES ");
            sb.AppendJoin(",", chunk.Select(keepersSeeder => $"('{keepersSeeder.TopicId}', '{keepersSeeder.KeeperId}', '{keepersSeeder.KeeperName.Replace("'", "''")}')"));
            sb.Append(";");
            
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }
        
        StopTimer($"Insert all seeders with chunk size {chunkSize}");
    }
    
    public void LoadTopicsByChunks(int chunkSize)
    {
        StartTimer();
        
        var chunks = ChunkArray(_topics, chunkSize);

        foreach (var chunk in chunks)
        {
            var command = _connection.CreateCommand();
            
            var sb = new StringBuilder();
            sb.Append("INSERT INTO main.Topics (id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen) VALUES ");
            
            sb.AppendJoin(
                ", ", chunk.Select(
                    topic =>
                        new StringBuilder().Append("('")
                            .Append(topic.Id)
                            .Append("', '")
                            .Append(topic.ForumId)
                            .Append("', '")
                            .Append("hui" /*topic.Name?.Replace("'", "''")*/)
                            .Append("', '")
                            .Append(topic.InfoHash)
                            .Append("', '")
                            .Append(topic.Seeders)
                            .Append("', '")
                            .Append(topic.Size)
                            .Append("', '")
                            .Append(topic.Status)
                            .Append("', '")
                            .Append(topic.RegTime)
                            .Append("', '")
                            .Append(topic.SeedersUpdatesToday)
                            .Append("', '")
                            .Append(topic.SeedersUpdatesDays)
                            .Append("', '")
                            .Append(topic.KeepingPriority)
                            .Append("', '")
                            .Append(topic.Poster)
                            .Append("', '")
                            .Append(topic.SeederLastSeen)
                            .Append("')")
                            .ToString()));

            sb.Append(';');

            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }
        
        StopTimer($"Insert all topics with chunk size {chunkSize}");
    }
    
    public void LoadSeedersByChunksWithParameter(int chunkSize)
    {
        StartTimer();
        
        var chunks = ChunkArray(_seeders, chunkSize);

        var prepCommand = _connection.CreateCommand();
        
        var sbPrep = new StringBuilder();
            
        sbPrep.Append("INSERT INTO main.KeepersSeeders (topic_id, keeper_id, keeper_name) VALUES ");
        sbPrep.AppendJoin(",", Enumerable.Range(0, chunkSize).Select(i => $"(@t{i}, @i{i}, @n{i})"));
        sbPrep.Append(";");
        
        for (var i = 0; i < chunkSize; i++)
        {
            prepCommand.Parameters.Add("@t" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@i" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@n" + i, SqliteType.Text);
        }

        prepCommand.CommandText = sbPrep.ToString();
        
        for (var ci = 0; ci < chunks.Count; ci++)
        {
            if (ci == chunks.Count - 1)
            {
                var command = _connection.CreateCommand();
            
                var sb = new StringBuilder();
            
                sb.Append("INSERT INTO main.KeepersSeeders (topic_id, keeper_id, keeper_name) VALUES ");
                sb.AppendJoin(",", chunks[ci].Select(keepersSeeder => $"('{keepersSeeder.TopicId}', '{keepersSeeder.KeeperId}', '{keepersSeeder.KeeperName.Replace("'", "''")}')"));
                sb.Append(";");
            
                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
            else
            {
                for (var i = 0; i < chunks[ci].Length; i++)
                {
                    prepCommand.Parameters[3 * i].Value = chunks[ci][i].TopicId;
                    prepCommand.Parameters[3 * i + 1].Value = chunks[ci][i].KeeperId;
                    prepCommand.Parameters[3 * i + 2].Value = chunks[ci][i].KeeperName;
                }

                prepCommand.ExecuteNonQuery();
            }
        }

        StopTimer($"Insert all seeders with chunk size {chunkSize}");
    }
    
    public void LoadTopicsByChunksWithParameter(int chunkSize)
    {
        StartTimer();
        
        var chunks = ChunkArray(_topics, chunkSize);

        var prepCommand = _connection.CreateCommand();
        
        var sbPrep = new StringBuilder();
            
        sbPrep.Append("INSERT INTO main.Topics (id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen) VALUES ");
        sbPrep.AppendJoin(",", Enumerable.Range(0, chunkSize).Select(i => $"(@id{i}, @fid{i}, @na{i}, @ih{i}, @se{i}, @si{i}, @st{i}, @re{i}, @sut{i}, @sud{i}, @kp{i}, @po{i}, @sls{i})"));
        sbPrep.Append(";");

        prepCommand.CommandText = sbPrep.ToString();
        
        for (var i = 0; i < chunkSize; i++)
        {
            prepCommand.Parameters.Add("@id" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@fid" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@na" + i, SqliteType.Text);
            prepCommand.Parameters.Add("@ih" + i, SqliteType.Text);
            prepCommand.Parameters.Add("@se" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@si" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@st" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@re" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@sut" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@sud" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@kp" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@po" + i, SqliteType.Integer);
            prepCommand.Parameters.Add("@sls" + i, SqliteType.Integer);
        }
        
        for (var ci = 0; ci < chunks.Count; ci++)
        {
            if (ci == chunks.Count - 1)
            {
                var command = _connection.CreateCommand();
            
                var sb = new StringBuilder();
                sb.Append("INSERT INTO main.Topics (id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen) VALUES ");
            
                sb.AppendJoin(
                    ", ", chunks[ci].Select(
                        topic =>
                            new StringBuilder().Append("('")
                                .Append(topic.Id)
                                .Append("', '")
                                .Append(topic.ForumId)
                                .Append("', '")
                                .Append("hui" /*topic.Name?.Replace("'", "''")*/)
                                .Append("', '")
                                .Append(topic.InfoHash)
                                .Append("', '")
                                .Append(topic.Seeders)
                                .Append("', '")
                                .Append(topic.Size)
                                .Append("', '")
                                .Append(topic.Status)
                                .Append("', '")
                                .Append(topic.RegTime)
                                .Append("', '")
                                .Append(topic.SeedersUpdatesToday)
                                .Append("', '")
                                .Append(topic.SeedersUpdatesDays)
                                .Append("', '")
                                .Append(topic.KeepingPriority)
                                .Append("', '")
                                .Append(topic.Poster)
                                .Append("', '")
                                .Append(topic.SeederLastSeen)
                                .Append("')")
                                .ToString()));

                sb.Append(';');

                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
            else
            {
                for (var i = 0; i < chunks[ci].Length; i++)
                {
                    prepCommand.Parameters[13 * i].Value = chunks[ci][i].Id;
                    prepCommand.Parameters[13 * i + 1].Value = chunks[ci][i].ForumId;
                    prepCommand.Parameters[13 * i + 2].Value = chunks[ci][i].Name;
                    prepCommand.Parameters[13 * i + 3].Value = chunks[ci][i].InfoHash;
                    prepCommand.Parameters[13 * i + 4].Value = chunks[ci][i].Seeders;
                    prepCommand.Parameters[13 * i + 5].Value = chunks[ci][i].Size;
                    prepCommand.Parameters[13 * i + 6].Value = chunks[ci][i].Status;
                    prepCommand.Parameters[13 * i + 7].Value = chunks[ci][i].RegTime;
                    prepCommand.Parameters[13 * i + 8].Value = chunks[ci][i].SeedersUpdatesToday;
                    prepCommand.Parameters[13 * i + 9].Value = chunks[ci][i].SeedersUpdatesDays;
                    prepCommand.Parameters[13 * i + 10].Value = chunks[ci][i].KeepingPriority;
                    prepCommand.Parameters[13 * i + 11].Value = chunks[ci][i].Poster;
                    prepCommand.Parameters[13 * i + 12].Value = chunks[ci][i].SeederLastSeen;
                }

                prepCommand.ExecuteNonQuery();
            }
        }
        
        StopTimer($"Insert all topics with chunk size {chunkSize}");
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

    public void Close()
    {
        Commit();
        
        var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA optimize;";
        command.ExecuteNonQuery();
    }

    private void LoadPreloaded(string path)
    {
        var sql = File.ReadAllText(path);

        StartTimer();

        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
        
        StopTimer(path);
    }

    private void Truncate(string table)
    {
        var command = _connection.CreateCommand();
        command.CommandText = $"delete from {table}";
        command.ExecuteNonQuery();

    }

    private void Begin()
    {
        var command = _connection.CreateCommand();
        command.CommandText = $"begin;";
        command.ExecuteNonQuery();
    }
    
    private void Commit()
    {
        var command = _connection.CreateCommand();
        command.CommandText = $"commit;";
        command.ExecuteNonQuery();
    }
    
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
