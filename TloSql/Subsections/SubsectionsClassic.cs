using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

namespace TloSql;

public class SubsectionsClassic
{
    public async Task Read(bool calc_average)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var topics = await GetTopics();
        var keepers = await GetKeepers();

        stopwatch.Stop();
        Console.WriteLine($"Get n parse topics and keepers from api {stopwatch.Elapsed}");

        var calculationsStopwatch = Stopwatch.StartNew();
        var db = new WebtlotestContext();

        var previousTopics = db.Topics.ToDictionary(topic => topic.Id);

        db.KeepersSeeders.ExecuteDelete();
        db.Topics.ExecuteDelete();

        db = new WebtlotestContext();

        var tempKeepersSeeder = new List<KeepersSeeder>();
        var tempTopics = new List<TloSql.Topic>();

        foreach (var topic in topics)
        {
            if (!topic.Valid)
                continue;

            foreach (var keeperId in topic.keepers)
            {
                if (!keepers.TryGetValue(keeperId, out var keeper))
                    continue;

                tempKeepersSeeder.Add(
                    new KeepersSeeder {
                        KeeperId = keeperId,
                        KeeperName = keeper.username,
                        TopicId = topic.id
                    });
            }

            previousTopics.TryGetValue(topic.id, out var previousTopic);

            // var daysUpdate = previousTopic.SeedersUpdatesDays ?? 0;
            // var sumUpdates = 1;
            // var sumSeeders = topic.seeders;
            //
            // if (calc_average)
            // {
            //     var 
            // }

            //TODO: avg seeds


            tempTopics.Add(
                new TloSql.Topic {
                    Id = topic.id,
                    ForumId = topic.forumId,
                    Name = previousTopic?.Name ?? "",
                    InfoHash = topic.info_hash,
                    Seeders = topic.seeders,
                    Size = topic.tor_size_bytes,
                    Status = topic.tor_status,
                    RegTime = topic.reg_time,
                    SeedersUpdatesToday = 0, //TODO: avg seeders
                    SeedersUpdatesDays = 0, //TODO: avg seeders
                    KeepingPriority = topic.keeping_priority,
                    Poster = topic.topic_poster,
                    SeederLastSeen = topic.seeder_last_seen
                });
        }
        calculationsStopwatch.Stop();
        Console.WriteLine($"All calculations {stopwatch.Elapsed}");
        
        Console.WriteLine("inserting");
        Console.WriteLine(DateTime.Now);

        File.WriteAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\seeders.json", JsonSerializer.Serialize(tempKeepersSeeder.ToArray()));
        File.WriteAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\topics.json", JsonSerializer.Serialize(tempTopics.ToArray()));

        // BulkInserdSeeders(tempKeepersSeeder.ToArray(), db);
        // BulkInserdTopics(tempTopics.ToArray(), db);

        // db.BulkInsert(tempKeepersSeeder);
        // db.BulkInsert(tempTopics);
        Console.WriteLine(DateTime.Now);
    }

    private void BulkInserdSeeders(KeepersSeeder[] tempKeepersSeeder, WebtlotestContext db)
    {
        var generateStopwatch = Stopwatch.StartNew();
        
        // KeepersSeeder[] buffer;
        //
        // var bufferLength = 1_000_000;
        // for(var i = 0; i < tempKeepersSeeder.Length; i += bufferLength)
        // {
        //     buffer = new KeepersSeeder[bufferLength];
        //     Array.Copy(tempKeepersSeeder, i, buffer, 0, bufferLength);
            
            var sb = new StringBuilder();

            sb.Append("INSERT INTO main.KeepersSeeders (topic_id, keeper_id, keeper_name) VALUES ");

            sb.AppendJoin(",", tempKeepersSeeder.Select(keepersSeeder => $"('{keepersSeeder.TopicId}', '{keepersSeeder.KeeperId}', '{keepersSeeder.KeeperName.Replace("'", "''")}')"));

            sb.Append(';');

            var sql = sb.ToString();
            generateStopwatch.Stop();
            Console.WriteLine($"Generate sql for seeders {generateStopwatch.Elapsed}");
            File.WriteAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\seeders.sql", sql);

            var executeStopwatch = Stopwatch.StartNew();
            db.Database.ExecuteSqlRaw(sql);
            executeStopwatch.Stop();
            Console.WriteLine($"Execute sql for seeders {executeStopwatch.Elapsed}");
        // }
    }
    
    private void BulkInserdTopics(TloSql.Topic[] tempTopics, WebtlotestContext db)
    {
        // Topic[] buffer;
        //  var bufferLength = 1_000_000;
        //  for (var i = 0; i < tempTopics.Length; i += bufferLength)
        //  {
        //      buffer = new Topic[bufferLength];
        //      Array.Copy(tempTopics, i, buffer, 0, bufferLength);
        var generateStopwatch = Stopwatch.StartNew();
             var sb = new StringBuilder();

             sb.Append("INSERT INTO main.Topics (id, forum_id, name, info_hash, seeders, size, status, reg_time, seeders_updates_today, seeders_updates_days, keeping_priority, poster, seeder_last_seen) VALUES ");

             sb.AppendJoin(
                 ", ", tempTopics.Select(
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
             
             var sql = sb.ToString();
             File.WriteAllText("C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\topics.sql", sql);
        
             generateStopwatch.Stop();
             Console.WriteLine($"Generate sql for topics {generateStopwatch.Elapsed}");
        
             var executeStopwatch = Stopwatch.StartNew();
             db.Database.ExecuteSqlRaw(sql);
             executeStopwatch.Stop();
             Console.WriteLine($"Execute sql for topics {executeStopwatch.Elapsed}");
         // }
    }

    private async Task<List<Topic>> GetTopics()
    {
        var proxy = new WebProxy {
            Address = new Uri("socks5://gateway.keeps.cyou:2081")
        };

        var handler = new HttpClientHandler {
            Proxy = proxy
        };

        var httpClient = new HttpClient(handler);
        const string statsUrl = "https://api.rutracker.cc/v1/static/pvc/f-all.tar";
        var stream = await httpClient.GetStreamAsync(statsUrl);

        var tarReader = new TarReader(stream);

        var topics = new List<Topic>();

        var pattern = @"\.\/(\d*)\.json\.gz";

        while (await tarReader.GetNextEntryAsync() is { } entryStream)
        {
            //TODO: filter by last update time
            //TODO: filter by ini

            if (entryStream.DataStream == null)
                continue;

            var gzipArchive = new GZipStream(entryStream.DataStream, CompressionMode.Decompress);

            //TODO: error handling

            var stats = JsonSerializer
                .Deserialize<ApiResponseModel>(gzipArchive);

            topics.AddRange(
                stats!
                    .Result
                    .Select(
                        pair => new Topic(
                            int.Parse(pair.Key),
                            ((JsonElement)pair.Value[0]).Deserialize<int>(),
                            ((JsonElement)pair.Value[1]).Deserialize<int>(),
                            ((JsonElement)pair.Value[2]).Deserialize<int>(),
                            ((JsonElement)pair.Value[3]).Deserialize<long>(),
                            ((JsonElement)pair.Value[4]).Deserialize<int>(),
                            ((JsonElement)pair.Value[5]).Deserialize<int[]>()!,
                            ((JsonElement)pair.Value[6]).Deserialize<int>(),
                            ((JsonElement)pair.Value[7]).Deserialize<string>()!,
                            ((JsonElement)pair.Value[8]).Deserialize<int>(),
                            GetForumId(entryStream.Name)
                        )));
        }

        return topics;
    }

    private int GetForumId(string gzName)
    {
        var forumdId = 0;

        for (var i = 2; i < gzName.Length; i++)
            if (char.IsDigit(gzName[i]))
            {
                forumdId *= 10;
                forumdId += gzName[i] - '0';
            }
            else
            {
                return forumdId;
            }

        return forumdId;
    }

    private async Task<Dictionary<int, Keeper>> GetKeepers()
    {
        var proxy = new WebProxy {
            Address = new Uri("socks5://gateway.keeps.cyou:2081")
        };

        var handler = new HttpClientHandler {
            Proxy = proxy
        };

        var httpClient = new HttpClient(handler);
        const string statsUrl = "https://api.rutracker.cc/v1/static/keepers_user_data";
        return (await httpClient
                .GetAsync(statsUrl)
                .Result
                .Content
                .ReadFromJsonAsync<ApiResponseModel>())!
            .Result
            .Select(
                pair => new Keeper(
                    int.Parse(pair.Key),
                    ((JsonElement)pair.Value[0]).Deserialize<string>()!,
                    ((JsonElement)pair.Value[1]).Deserialize<bool>()))
            .ToDictionary(keeper => keeper.id);

        //TODO: error handling
    }

    public record ApiResponseModel(
        [property: JsonPropertyName("result")]
        Dictionary<string, object[]> Result,
        long update_time,
        string update_time_humn,
        long total_size_bytes);

    public record Topic(int id, int tor_status, int seeders, int reg_time, long tor_size_bytes, int keeping_priority, int[] keepers, int seeder_last_seen, string info_hash, int topic_poster, int forumId)
    {
        public bool Valid => tor_status == 0 || tor_status == 2 || tor_status == 3 || tor_status == 8 || tor_status == 10;
    }

    public record Keeper(int id, string username, bool isCandidate);
}
