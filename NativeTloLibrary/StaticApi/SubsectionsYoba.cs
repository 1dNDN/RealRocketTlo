using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;

using NativeTloLibrary.Models;

// ReSharper disable CommentTypo

namespace NativeTloLibrary.StaticApi;

public partial class SubsectionsYoba
{
    public async Task<(List<Topic> topics, List<KeepersSeeder> keepers)> Read(Dictionary<long, string> keeperNames)
    {
        Speedometer.Start();
        var httpClient = new HttpClient();

        const string statsUrl = "https://api.rutracker.cc/v1/static/pvc/f-all.tar";

        var tarReader = new TarReader(new MemoryStream(await httpClient.GetByteArrayAsync(statsUrl)));

        var zippedBytes = new List<byte[]>();

        while (tarReader.GetNextEntry() is { } entryStream)
        {
            if (entryStream.DataStream == null)
                continue;

            using var ms = new MemoryStream();
            entryStream.DataStream.CopyTo(ms);
            zippedBytes.Add(ms.ToArray());
        }

        Speedometer.Stop("F-all скачан и распакован за");

        Speedometer.Start();

        var topics = new List<Topic>();
        var keepers = new List<KeepersSeeder>();
        var lockObject = new object();

        Parallel.ForEach(zippedBytes, (x) => ExecuteRead(x, keeperNames, ref topics, ref keepers, ref lockObject));

        Speedometer.Stop("F-all посчитан за");

        return (topics, keepers);

    }

    private void ExecuteRead(byte[] zipped, Dictionary<long, string> keeperNames, ref List<Topic> globalTopics, ref List<KeepersSeeder> globalKeepers, ref object lockObject)
    {
        using var ms1 = new MemoryStream(zipped);
        using var gzipArchive = new GZipStream(ms1, CompressionMode.Decompress);

        using var ms2 = new MemoryStream();
        gzipArchive.CopyTo(ms2);

        Read(ms2.ToArray(), keeperNames,  ref globalTopics, ref globalKeepers, ref lockObject);
    }

    enum Place
    {
        Result,
        TopicId,
        Topic,
        TorStatus,
        Seeders,
        RegTime,
        TorSizeBytes,
        KeepingPriority,
        Keepers,
        SeederLastSeen,
        InfoHash,
        TopicPoster,
        Leechers
    }

    /// <summary>
    /// Охуеть какой быстрый непонятный десериализатор f-all
    /// </summary>
    /// <param name="chars"></param>
    private void Read(byte[] chars, Dictionary<long, string> keeperNames, ref List<Topic> globalTopics, ref List<KeepersSeeder> globalKeepers, ref object lockObject)
    {
        var topics = new List<Topic>();
        var keepers = new List<KeepersSeeder>();

        // на входе JSON вида
        // {
        //      "format":
        //          {
        //              "topic_id":
        //                  ["tor_status","seeders","reg_time","tor_size_bytes","keeping_priority","keepers","seeder_last_seen","info_hash","topic_poster","leechers"]
        //          },
        //      "update_time":1734865523,
        //      "update_time_humn":"2024-12-22 14:05:23 MSK",
        //      "total_size_bytes":69509856966,
        //      "result":
        //          {
        //              "1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
        //              "1989338":[2,4,1287431560,41333086885,1,[369429,33209522],1734834061,"EA61FE807C062DC9B8B6412D1B909FA64DE1C2CA",6281469,3]
        //          }
        //  }

        // треба все это прочитать за приемлимое время

        // поле format в целом нас не ебет, потому что формат никогда не меняется
        // но в целом можно его прочитать и даже интерпретировать с соответствующим дебаффом к скорости десериализации

        // остальные поля метаданных тоже не используются, но если что не проблема их прочитать, когда понадобятся
        // поэтому ищем начало result и не ебемся
        const string resultKey = "\"result\"";

        // читаем всё в один проход, мы же не skill issue сделать это
        var i = 0;

        // где-то между началом и концом лежит result, ищем его
        for (; i < chars.Length; i++)
        {
            var currentKey = chars[i];

            if (currentKey == '"' && chars[i + 1] == 'r' // Йоба оптимизация, result всегда начинается с "r. Если мы щас не на "r, то можно не проверять, что мы на result
                && IsIndexOfSubSequence(ref chars, resultKey, i)) // проверяем, является ли текущая позиция индексом "result"
                break; // нашли "result"
        }

        // прыгаем вперед на длину "result":{"
        i += resultKey.Length + 3;

        // "result":{}
        // тут нихуя нет, хуйнёй не страдаем
        if (chars[i] == '}')
            return;

        var nowInResult = true;
        var nowInTopicKey = false;
        var nowInTopic = false;
        var nowInKeepersList = false;
        var nowInEndKeepersList = false;

        var currentPlace = Place.TopicId;

        // заполняем текущий элемент
        var currentTopic = new Topic();
        var currentTopicId = 0; // сепулек перфоманса тут поимеем

        // "result":{"1456361":[8
        //            |
        //          мы тут
        var indexOfStart = i;

        // отсюда и до обеда
        for (; i < chars.Length; i++)
        {
            var currentKey = chars[i];

            switch (currentPlace)
            {
                case Place.Result:

                    break;
                case Place.TopicId:
                    if (currentKey == '"')
                    {
                        currentTopic.TopicId = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8
                        //                   |
                        //                 были тут

                        // "result":{"1456361":[8
                        //                     |
                        //                 стали тут

                        // "result":{"1456361":[8
                        //                      |
                        //    на следующей итерации будем тут

                        i += 2;
                        indexOfStart = i + 1;

                        currentPlace = Place.TorStatus;
                    }

                    break;
                case Place.Topic:

                    break;
                case Place.TorStatus:
                    if (currentKey == ',')
                    {
                        currentTopic.TorStatus = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,17
                        //                       |
                        //                     мы тут

                        indexOfStart = i + 1;

                        currentPlace = Place.Seeders;
                    }

                    break;
                case Place.Seeders:
                    if (currentKey == ',')
                    {
                        currentTopic.Seeders = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,17
                        //                         |
                        //                      мы тут

                        indexOfStart = i + 1;

                        currentPlace = Place.RegTime;
                    }

                    break;
                case Place.RegTime:
                    if (currentKey == ',')
                    {
                        currentTopic.RegTime = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,5968138
                        //                                    |
                        //                                 мы тут

                        indexOfStart = i + 1;

                        currentPlace = Place.TorSizeBytes;
                    }

                    break;
                case Place.TorSizeBytes:
                    if (currentKey == ',')
                    {
                        currentTopic.TorSizeBytes = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,
                        //                                              |
                        //                                           мы тут

                        indexOfStart = i + 1;

                        currentPlace = Place.KeepingPriority;
                    }

                    break;
                case Place.KeepingPriority:
                    if (currentKey == ',')
                    {
                        currentTopic.KeepingPriority = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                        //                                                |
                        //                                              мы тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                        //                                                 |
                        //                                              стали тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                        //                                                  |
                        //                                   на следующей итерации будем тут

                        i++;
                        indexOfStart = i + 1;

                        currentPlace = Place.Keepers;
                    }

                    break;
                case Place.Keepers:
                    if (currentKey == ',')
                    {
                        var keeperId = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        var keeperName = keeperNames[keeperId];

                        keepers.Add(
                            new KeepersSeeder {
                                KeeperId = keeperId,
                                TopicId = currentTopicId,
                                KeeperName = keeperName
                            });

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                        //                                                          |
                        //                                                       мы тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                        //                                                                   |
                        //                                                                или тут

                        indexOfStart = i + 1;
                    } else if (currentKey == ']')
                    {
                        if (indexOfStart != i)
                        {
                            var keeperId = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                            var keeperName = keeperNames[keeperId];

                            keepers.Add(new KeepersSeeder {
                                KeeperId = keeperId,
                                TopicId = currentTopicId,
                                KeeperName = keeperName
                            });

                            // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                            //                                                                          |
                            //                                                                       мы тут

                            // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                            //                                                                           |
                            //                                                                     стали тут

                            // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061
                            //                                                                            |
                            //                                                          на следующей итерации будем тут

                            i++;
                            indexOfStart = i + 1;
                        }
                        else
                        {
                            // "result":{"1456361":[],1734834061
                            //                      |
                            //                    мы тут

                            // "result":{"1456361":[],1734834061
                            //                       |
                            //                    стали тут

                            // "result":{"1456361":[],1734834061
                            //                        |
                            //        на следующей итерации будем тут

                            i++;
                            indexOfStart = i + 1;
                        }

                        currentPlace = Place.SeederLastSeen;
                    }

                    break;
                case Place.SeederLastSeen:
                    if (currentKey == ',')
                    {
                        currentTopic.SeederLastSeen = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                      |
                        //                                                                                   мы тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                       |
                        //                                                                                   стали тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                        |
                        //                                                                            на следующей итерации будем тут

                        i++;
                        indexOfStart = i + 1;

                        currentPlace = Place.InfoHash;
                    }

                    break;
                case Place.InfoHash:
                    if (currentKey == '"')
                    {
                        currentTopic.InfoHash = GetSubSequenceOfBytesAsString(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                                                                |
                        //                                                                                                                             мы тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                                                                 |
                        //                                                                                                                             стали тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                                                                  |
                        //                                                                                                                   на следующей итерации будем тут

                        i++;
                        indexOfStart = i + 1;

                        currentPlace = Place.TopicPoster;
                    }

                    break;
                case Place.TopicPoster:
                    if (currentKey == ',')
                    {
                        currentTopic.TopicPoster = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],
                        //                                                                                                                                         |
                        //                                                                                                                                       мы тут

                        indexOfStart = i + 1;

                        currentPlace = Place.Leechers;
                    }

                    break;
                case Place.Leechers:
                    if (currentKey == ']')
                    {
                        currentTopic.TopicPoster = GetSubSequenceOfBytesAsLong(ref chars, indexOfStart, i - indexOfStart);

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],"1989338":
                        //                                                                                                                                           |
                        //                                                                                                                                         мы тут

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],"1989338":
                        // "result":{"1456361":[8                                                                                                                       |
                        //            |                                                                                                                                 |
                        //                                                                                                                                          нам надо сюда

                        // "result":{"1456361":[8,4,1702859387,596813824,1,[46916731,33209522,369429],1734834061,"54913BA8E950FC517FC568EDCF3CB54111AF5144",1427990,0],"1989338":
                        //                                                                                                                                              |
                        //                                                                                                                                         мы стали сюда

                        // не надо как-то особенно обрабатывать ситуацию, когда массив кончился, потому что тогда закончится и цикл

                        i += 3;
                        indexOfStart = i;

                        currentPlace = Place.TopicId;

                        topics.Add(currentTopic);
                        currentTopic = new Topic();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException("Медленно положи десериализатор на место");
            }
        }

        lock (lockObject)
        {
            globalTopics.AddRange(topics);
            globalKeepers.AddRange(keepers);
        }
    }

    /// <summary>
    /// Дернуть байты в строку
    /// </summary>
    /// <param name="bytes">Байты откуда дергать</param>
    /// <param name="index">Индекс начала последовательности</param>
    /// <param name="length">Длина последовательности</param>
    /// <returns>Строка</returns>
    private string GetSubSequenceOfBytesAsString(ref byte[] bytes, int index, int length)
    {
        var subsequence = new char[length];

        for (var i = 0; i < length; i++)
            subsequence[i] = (char)bytes[index + i];

        return new string(subsequence);
    }

    /// <summary>
    /// Дёрнуть последовательность байтов и интерпретировать ее как число
    /// </summary>
    /// <param name="bytes">Байты откуда дергать</param>
    /// <param name="index">Индекс начала последовательности</param>
    /// <param name="length">Длина последовательности</param>
    /// <returns>Число</returns>
    private static long GetSubSequenceOfBytesAsLong(ref byte[] bytes, int index, int length)
    {
        // Эта хуйня экономит 10% времени выполнения
        long result = length switch {
            1 => ((char)bytes[index] - '0'),

            2 => (long)((char)bytes[index + 0] - '0') * 10 +
                 (long)((char)bytes[index + 1] - '0'),

            3 => (long)((char)bytes[index + 0] - '0') * 100 +
                 (long)((char)bytes[index + 1] - '0') * 10 +
                 (long)((char)bytes[index + 2] - '0'),

            4 => (long)((char)bytes[index + 0] - '0') * 1000 +
                 (long)((char)bytes[index + 1] - '0') * 100 +
                 (long)((char)bytes[index + 2] - '0') * 10 +
                 (long)((char)bytes[index + 3] - '0'),

            5 => (long)((char)bytes[index + 0] - '0') * 10000 +
                 (long)((char)bytes[index + 1] - '0') * 1000 +
                 (long)((char)bytes[index + 2] - '0') * 100 +
                 (long)((char)bytes[index + 3] - '0') * 10 +
                 (long)((char)bytes[index + 4] - '0'),

            6 => (long)((char)bytes[index + 0] - '0') * 100000 +
                 (long)((char)bytes[index + 1] - '0') * 10000 +
                 (long)((char)bytes[index + 2] - '0') * 1000 +
                 (long)((char)bytes[index + 3] - '0') * 100 +
                 (long)((char)bytes[index + 4] - '0') * 10 +
                 (long)((char)bytes[index + 5] - '0'),

            7 => (long)((char)bytes[index + 0] - '0') * 1000000 +
                 (long)((char)bytes[index + 1] - '0') * 100000 +
                 (long)((char)bytes[index + 2] - '0') * 10000 +
                 (long)((char)bytes[index + 3] - '0') * 1000 +
                 (long)((char)bytes[index + 4] - '0') * 100 +
                 (long)((char)bytes[index + 5] - '0') * 10 +
                 (long)((char)bytes[index + 6] - '0'),

            8 => (long)((char)bytes[index + 0] - '0') * 10000000 +
                 (long)((char)bytes[index + 1] - '0') * 1000000 +
                 (long)((char)bytes[index + 2] - '0') * 100000 +
                 (long)((char)bytes[index + 3] - '0') * 10000 +
                 (long)((char)bytes[index + 4] - '0') * 1000 +
                 (long)((char)bytes[index + 5] - '0') * 100 +
                 (long)((char)bytes[index + 6] - '0') * 10 +
                 (long)((char)bytes[index + 7] - '0'),

            9 => (long)((char)bytes[index + 0] - '0') * 100000000 +
                 (long)((char)bytes[index + 1] - '0') * 10000000 +
                 (long)((char)bytes[index + 2] - '0') * 1000000 +
                 (long)((char)bytes[index + 3] - '0') * 100000 +
                 (long)((char)bytes[index + 4] - '0') * 10000 +
                 (long)((char)bytes[index + 5] - '0') * 1000 +
                 (long)((char)bytes[index + 6] - '0') * 100 +
                 (long)((char)bytes[index + 7] - '0') * 10 +
                 (long)((char)bytes[index + 8] - '0'),

            10 => (long)((char)bytes[index + 0] - '0') * 1000000000 +
                  (long)((char)bytes[index + 1] - '0') * 100000000 +
                  (long)((char)bytes[index + 2] - '0') * 10000000 +
                  (long)((char)bytes[index + 3] - '0') * 1000000 +
                  (long)((char)bytes[index + 4] - '0') * 100000 +
                  (long)((char)bytes[index + 5] - '0') * 10000 +
                  (long)((char)bytes[index + 6] - '0') * 1000 +
                  (long)((char)bytes[index + 7] - '0') * 100 +
                  (long)((char)bytes[index + 8] - '0') * 10 +
                  (long)((char)bytes[index + 9] - '0'),

            11 => (long)((char)bytes[index + 0] - '0') * 10000000000 +
                  (long)((char)bytes[index + 1] - '0') * 1000000000 +
                  (long)((char)bytes[index + 2] - '0') * 100000000 +
                  (long)((char)bytes[index + 3] - '0') * 10000000 +
                  (long)((char)bytes[index + 4] - '0') * 1000000 +
                  (long)((char)bytes[index + 5] - '0') * 100000 +
                  (long)((char)bytes[index + 6] - '0') * 10000 +
                  (long)((char)bytes[index + 7] - '0') * 1000 +
                  (long)((char)bytes[index + 8] - '0') * 100 +
                  (long)((char)bytes[index + 9] - '0') * 10 +
                  (long)((char)bytes[index + 10] - '0'),

            12 => (long)((char)bytes[index + 0] - '0') * 100000000000 +
                  (long)((char)bytes[index + 1] - '0') * 10000000000 +
                  (long)((char)bytes[index + 2] - '0') * 1000000000 +
                  (long)((char)bytes[index + 3] - '0') * 100000000 +
                  (long)((char)bytes[index + 4] - '0') * 10000000 +
                  (long)((char)bytes[index + 5] - '0') * 1000000 +
                  (long)((char)bytes[index + 6] - '0') * 100000 +
                  (long)((char)bytes[index + 7] - '0') * 10000 +
                  (long)((char)bytes[index + 8] - '0') * 1000 +
                  (long)((char)bytes[index + 9] - '0') * 100 +
                  (long)((char)bytes[index + 10] - '0') * 10 +
                  (long)((char)bytes[index + 11] - '0'),

            13 => (long)((char)bytes[index + 0] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 100000000000 +
                  (long)((char)bytes[index + 2] - '0') * 10000000000 +
                  (long)((char)bytes[index + 3] - '0') * 1000000000 +
                  (long)((char)bytes[index + 4] - '0') * 100000000 +
                  (long)((char)bytes[index + 5] - '0') * 10000000 +
                  (long)((char)bytes[index + 6] - '0') * 1000000 +
                  (long)((char)bytes[index + 7] - '0') * 100000 +
                  (long)((char)bytes[index + 8] - '0') * 10000 +
                  (long)((char)bytes[index + 9] - '0') * 1000 +
                  (long)((char)bytes[index + 10] - '0') * 100 +
                  (long)((char)bytes[index + 11] - '0') * 10 +
                  (long)((char)bytes[index + 12] - '0'),

            14 => (long)((char)bytes[index + 0] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 100000000000 +
                  (long)((char)bytes[index + 3] - '0') * 10000000000 +
                  (long)((char)bytes[index + 4] - '0') * 1000000000 +
                  (long)((char)bytes[index + 5] - '0') * 100000000 +
                  (long)((char)bytes[index + 6] - '0') * 10000000 +
                  (long)((char)bytes[index + 7] - '0') * 1000000 +
                  (long)((char)bytes[index + 8] - '0') * 100000 +
                  (long)((char)bytes[index + 9] - '0') * 10000 +
                  (long)((char)bytes[index + 10] - '0') * 1000 +
                  (long)((char)bytes[index + 11] - '0') * 100 +
                  (long)((char)bytes[index + 12] - '0') * 10 +
                  (long)((char)bytes[index + 13] - '0'),

            15 => (long)((char)bytes[index + 0] - '0') * 100000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 3] - '0') * 100000000000 +
                  (long)((char)bytes[index + 4] - '0') * 10000000000 +
                  (long)((char)bytes[index + 5] - '0') * 1000000000 +
                  (long)((char)bytes[index + 6] - '0') * 100000000 +
                  (long)((char)bytes[index + 7] - '0') * 10000000 +
                  (long)((char)bytes[index + 8] - '0') * 1000000 +
                  (long)((char)bytes[index + 9] - '0') * 100000 +
                  (long)((char)bytes[index + 10] - '0') * 10000 +
                  (long)((char)bytes[index + 11] - '0') * 1000 +
                  (long)((char)bytes[index + 12] - '0') * 100 +
                  (long)((char)bytes[index + 13] - '0') * 10 +
                  (long)((char)bytes[index + 14] - '0'),

            16 => (long)((char)bytes[index + 0] - '0') * 1000000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 100000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 3] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 4] - '0') * 100000000000 +
                  (long)((char)bytes[index + 5] - '0') * 10000000000 +
                  (long)((char)bytes[index + 6] - '0') * 1000000000 +
                  (long)((char)bytes[index + 7] - '0') * 100000000 +
                  (long)((char)bytes[index + 8] - '0') * 10000000 +
                  (long)((char)bytes[index + 9] - '0') * 1000000 +
                  (long)((char)bytes[index + 10] - '0') * 100000 +
                  (long)((char)bytes[index + 11] - '0') * 10000 +
                  (long)((char)bytes[index + 12] - '0') * 1000 +
                  (long)((char)bytes[index + 13] - '0') * 100 +
                  (long)((char)bytes[index + 14] - '0') * 10 +
                  (long)((char)bytes[index + 15] - '0'),

            17 => (long)((char)bytes[index + 0] - '0') * 10000000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 1000000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 100000000000000 +
                  (long)((char)bytes[index + 3] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 4] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 5] - '0') * 100000000000 +
                  (long)((char)bytes[index + 6] - '0') * 10000000000 +
                  (long)((char)bytes[index + 7] - '0') * 1000000000 +
                  (long)((char)bytes[index + 8] - '0') * 100000000 +
                  (long)((char)bytes[index + 9] - '0') * 10000000 +
                  (long)((char)bytes[index + 10] - '0') * 1000000 +
                  (long)((char)bytes[index + 11] - '0') * 100000 +
                  (long)((char)bytes[index + 12] - '0') * 10000 +
                  (long)((char)bytes[index + 13] - '0') * 1000 +
                  (long)((char)bytes[index + 14] - '0') * 100 +
                  (long)((char)bytes[index + 15] - '0') * 10 +
                  (long)((char)bytes[index + 16] - '0'),

            18 => (long)((char)bytes[index + 0] - '0') * 100000000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 10000000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 1000000000000000 +
                  (long)((char)bytes[index + 3] - '0') * 100000000000000 +
                  (long)((char)bytes[index + 4] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 5] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 6] - '0') * 100000000000 +
                  (long)((char)bytes[index + 7] - '0') * 10000000000 +
                  (long)((char)bytes[index + 8] - '0') * 1000000000 +
                  (long)((char)bytes[index + 9] - '0') * 100000000 +
                  (long)((char)bytes[index + 10] - '0') * 10000000 +
                  (long)((char)bytes[index + 11] - '0') * 1000000 +
                  (long)((char)bytes[index + 12] - '0') * 100000 +
                  (long)((char)bytes[index + 13] - '0') * 10000 +
                  (long)((char)bytes[index + 14] - '0') * 1000 +
                  (long)((char)bytes[index + 15] - '0') * 100 +
                  (long)((char)bytes[index + 16] - '0') * 10 +
                  (long)((char)bytes[index + 17] - '0'),

            19 => (long)((char)bytes[index + 0] - '0') * 1000000000000000000 +
                  (long)((char)bytes[index + 1] - '0') * 100000000000000000 +
                  (long)((char)bytes[index + 2] - '0') * 10000000000000000 +
                  (long)((char)bytes[index + 3] - '0') * 1000000000000000 +
                  (long)((char)bytes[index + 4] - '0') * 100000000000000 +
                  (long)((char)bytes[index + 5] - '0') * 10000000000000 +
                  (long)((char)bytes[index + 6] - '0') * 1000000000000 +
                  (long)((char)bytes[index + 7] - '0') * 100000000000 +
                  (long)((char)bytes[index + 8] - '0') * 10000000000 +
                  (long)((char)bytes[index + 9] - '0') * 1000000000 +
                  (long)((char)bytes[index + 10] - '0') * 100000000 +
                  (long)((char)bytes[index + 11] - '0') * 10000000 +
                  (long)((char)bytes[index + 12] - '0') * 1000000 +
                  (long)((char)bytes[index + 13] - '0') * 100000 +
                  (long)((char)bytes[index + 14] - '0') * 10000 +
                  (long)((char)bytes[index + 15] - '0') * 1000 +
                  (long)((char)bytes[index + 16] - '0') * 100 +
                  (long)((char)bytes[index + 17] - '0') * 10 +
                  (long)((char)bytes[index + 18] - '0'),
            _ => 0
        };

        return result;
    }


    private bool IsIndexOfSubSequence(ref byte[] bytes, string key, int index)
    {
        for (var j = 0; j < key.Length; j++)
        {
            var currentChar = bytes[index + j];
            if (currentChar == key[j])
                continue;

            return false;
        }

        return true;
    }
}
