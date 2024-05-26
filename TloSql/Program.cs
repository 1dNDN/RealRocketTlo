// See https://aka.ms/new-console-template for more information


using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

using TloSql;

var sums = new SubsectionsClassic();


Stopwatch stopwatch = new Stopwatch();
stopwatch.Start();
// await sums.Read(true);

var db = new Database();

var chunkSize = 10_000;

db.LoadSeedersByChunks(chunkSize);
db.LoadTopicsByChunks(chunkSize);


db.Close();

stopwatch.Stop();
Console.WriteLine($"Total {stopwatch.Elapsed}");



// using System.Data.SqlClient;
// using System.Diagnostics;
//
// using Microsoft.Data.Sqlite;
// using Microsoft.EntityFrameworkCore.ChangeTracking;
//
// using TloSql;
// Stopwatch sw = Stopwatch.StartNew();
// Console.WriteLine("Hello, World!");
//
// String.Format("1243");
//
// var query1String = "drop table topicsTemp;";
// var query2String = "create temp table topicsTemp as\nSELECT * FROM Topics \nWHERE  \n  Topics.forum_id IN (5)\n  AND Topics.status IN (0, 2, 3, 8, 10)\n  AND Topics.keeping_priority IN (0, 1)\n  AND Topics.reg_time < 1714767421;";
// var query3String = "SELECT \n  topicsTemp.id AS topic_id, \n  topicsTemp.info_hash, \n  topicsTemp.name, \n  topicsTemp.size, \n  topicsTemp.reg_time, \n  topicsTemp.forum_id, \n  topicsTemp.keeping_priority AS priority, \n  Torrents.done, \n  Torrents.paused, \n  Torrents.error, \n  Torrents.client_id, \n  -1 AS days_seed, \n  topicsTemp.seeders / MAX(seeders_updates_today, 1) AS seed \nFROM topicsTemp \n  LEFT JOIN Torrents ON topicsTemp.info_hash = Torrents.info_hash \n  LEFT JOIN (\n    SELECT topic_id, MAX(complete) AS has_complete, MAX(posted) AS max_posted, MAX(NOT complete) AS has_download, MAX(seeding) AS has_seeding \n    FROM \n      (\n        SELECT topic_id, MAX(complete) AS complete, MAX(posted) AS posted, MAX(seeding) AS seeding \n        FROM \n          (\n            SELECT kl.topic_id, kl.keeper_id, kl.complete, CASE WHEN kl.complete = 1 THEN kl.posted END AS posted, 0 AS seeding \n            FROM KeepersLists kl \n            INNER JOIN topicsTemp t ON t.id = kl.topic_id \n            WHERE kl.posted > t.reg_time \n            UNION ALL \n            SELECT topic_id, keeper_id, 1 AS complete, NULL AS posted, 1 AS seeding \n            FROM KeepersSeeders k2\n\t\t\tINNER JOIN topicsTemp t ON t.id = k2.topic_id\n          ) \n        WHERE keeper_id != '43961998' \n        GROUP BY topic_id, keeper_id\n      ) \n    GROUP BY topic_id\n  ) Keepers ON topicsTemp.id = Keepers.topic_id \n  AND (\n    Keepers.max_posted IS NULL \n    OR topicsTemp.reg_time < Keepers.max_posted\n  ) \n  LEFT JOIN (SELECT info_hash FROM TopicsExcluded GROUP BY info_hash) TopicsExcluded \n  ON topicsTemp.info_hash = TopicsExcluded.info_hash \nWHERE \n  TopicsExcluded.info_hash IS NULL \n  AND (\n    CAST(done as INT) IS null\n  ) \n  AND Keepers.max_posted IS NOT NULL \n  AND Keepers.has_seeding = 1 \n  AND Keepers.has_download = 1;";
// var connectionString = "Data Source = C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\webtlotest.db";
//
// using var connection = new SqliteConnection(connectionString);
// connection.Open();
//
// var command = new SqliteCommand(query1String, connection);
// command.ExecuteNonQuery();
//
// var command2 = new SqliteCommand(query2String, connection);
// command2.ExecuteNonQuery();
//
// var command3 = new SqliteCommand(query3String, connection);
// var reader = command3.ExecuteReader();
//
// try
// {
//     while (reader.Read())
//     {
//         Console.WriteLine(123);
//     }
// }
// finally
// {
//     // Always call Close when done reading.
// }
// reader.Close();
// sw.Stop();
// Console.WriteLine(sw.Elapsed);

// var context = new WebtlotestContext();
// // собираем топики по первичным фильтрам
// Stopwatch sw = Stopwatch.StartNew();
//
// var topics = context.Topics
//     .Where(topic => topic.ForumId == 5 && (topic.Status == 0 || topic.Status == 2 || topic.Status == 3 || topic.Status == 8 || topic.Status == 10) && (topic.KeepingPriority == 0 || topic.KeepingPriority == 1) && topic.RegTime < 1714767421);
//
// var keepers =
//     context.KeepersLists
//         .Join(
//             topics,
//             list => list.TopicId,
//             topic => topic.Id,
//             (list, topic) =>
//                 new {
//                     TopicId = list.TopicId,
//                     KeeperId = list.KeeperId,
//                     Posted = list.Complete == 1
//                         ? list.Posted ?? 0
//                         : 0,
//                     Seeding = 0,
//                     RegTime = topic.RegTime})
//         .Where(horonitel => horonitel.Posted > horonitel.RegTime)
//         .Union(
//             context.KeepersSeeders
//                 .Join(
//                     topics,
//                     list => list.TopicId,
//                     topic => topic.Id,
//                     (seeder, topic) => new {
//                         TopicId = seeder.TopicId,
//                         KeeperId =seeder.KeeperId,
//                         Posted = 0,
//                         Seeding = 1,
//                         RegTime = (int?)0}));
//
//
//
//
//
// Console.WriteLine(keepers.Count());
// Console.WriteLine(topics.Count());
//
// sw.Stop();
// Console.WriteLine(sw.Elapsed);
//
// record KeeperSeederHoronitel(int TopicId, int KeeperId, int? Posted, int Seeding, int? RegTime);

// дальше можно плясать спокойно

// context.KeepersList
//     .Where()

// var query = context.Topics
//     .Where(t => !context.TopicsExcluded.Select(te => te.InfoHash).Contains(t.InfoHash)
//                 && t.ForumId == 5
//                 && new[] { 0, 2, 3, 8, 10 }.Contains((int)t.Status)
//                 && new[] { 0, 1 }.Contains((int)t.KeepingPriority)
//                 && t.done == null
//                 && t.reg_time < 1714767421)
//     .Join(context.Torrents, t => t.info_hash, tor => tor.info_hash, (t, tor) => new { t, tor })
//     .GroupJoin(
//         context.KeepersLists
//             .Join(context.Topics, kl => kl.topic_id, t => t.id, (kl, t) => new { kl, t })
//             .Where(kl => kl.kl.posted > kl.t.reg_time || kl.kl.complete == 1)
//             .Select(kl => new
//             {
//                 kl.kl.topic_id,
//                 kl.kl.keeper_id,
//                 kl.kl.complete,
//                 kl.kl.posted,
//                 seeding = kl.kl.complete == 1 ? 1 : 0
//             })
//             .Where(kl => kl.keeper_id != "43961998")
//             .GroupBy(kl => new { kl.topic_id, kl.keeper_id })
//             .Select(g => new
//             {
//                 topic_id = g.Key.topic_id,
//                 has_complete = g.Max(kl => kl.complete),
//                 max_posted = g.Max(kl => kl.posted),
//                 has_download = g.Max(kl => !kl.complete),
//                 has_seeding = g.Max(kl => kl.seeding)
//             }),
//         t => t.t.id,
//         k => k.topic_id,
//         (t, k) => new { t, k })
//     .SelectMany(tk => tk.k.DefaultIfEmpty(), (tk, k) => new
//     {
//         topic_id = tk.t.id,
//         tk.t.info_hash,
//         tk.t.name,
//         tk.t.size,
//         tk.t.reg_time,
//         tk.t.forum_id,
//         priority = tk.t.keeping_priority,
//         tk.torrent.done,
//         tk.torrent.paused,
//         tk.torrent.error,
//         tk.torrent.client_id,
//         days_seed = -1,
//         seed = tk.t.seeders / (k.has_seeding ? Math.Max(k.has_seeding, 1) : 1)
//     })
//     .Where(result => result.k.max_posted != null && result.k.has_seeding == 1 && result.k.has_download == 1);
