using System.Collections.Concurrent;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TloSql;

public class SubsectionsYoba
{
    private ConcurrentDictionary<string, int> concurentParsed;

    public async Task read()
    {
        var httpClient = new HttpClient();

        const string statsUrl = "https://api.rutracker.cc/v1/static/pvc/f-all.tar";

        var tarReader = new TarReader(new MemoryStream(await httpClient.GetByteArrayAsync(statsUrl)));

        var sw4 = Stopwatch.StartNew();

        var zippedBytes = new List<byte[]>();

        while (tarReader.GetNextEntry() is { } entryStream)
        {
            if (entryStream.DataStream == null)
                continue;

            using var ms = new MemoryStream();
            entryStream.DataStream.CopyTo(ms);
            zippedBytes.Add(ms.ToArray());
        }

        concurentParsed = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(zippedBytes, ExecuteRead);

        var parsed = concurentParsed
            .ToArray()
            .GroupBy(
                keyvalue => keyvalue.Key,
                (key, keyvalues) => new {
                    Key = key,
                    Value = keyvalues
                        .Sum(keyvalue => keyvalue.Value)
                }
            )
            .ToDictionary(
                x => x.Key,
                x => x.Value);
        
        File.WriteAllText("stats.json", JsonSerializer.Serialize(parsed));

        sw4.Stop();

        Console.WriteLine(sw4.Elapsed);

    }

    private void ExecuteRead(byte[] zipped)
    {
        using var ms1 = new MemoryStream(zipped);
        using var gzipArchive = new GZipStream(ms1, CompressionMode.Decompress);

        using var ms2 = new MemoryStream();
        gzipArchive.CopyTo(ms2);

        Read(ms2.ToArray());
    }

    private void Read(byte[] chars)
    {
        const string resultKey = "\"result\"";

        var i = 0;
        for (; i < chars.Length; i++)
        {
            var currentKey = chars[i];

            if (currentKey == '"' && chars[i + 1] == 'r' && IsIndexOfSubSequence(ref chars, resultKey, i))
                break;
        }

        i += resultKey.Length + 2;

        var nowInResult = true;
        var nowInTopicKey = false;
        var nowInTopic = false;
        var nowInKeepersList = false;
        var nowInEndKeepersList = false;

        var indexOfStart = 0;

        for (; i < chars.Length; i++)
        {
            var currentKey = chars[i];

            switch (nowInEndKeepersList)
            {
                case true when currentKey != ']': continue;
                case true:
                    nowInEndKeepersList = false;
                    nowInResult = true;
                    continue;
            }

            switch (nowInTopic)
            {
                case true when currentKey != '[': continue;
                case true:
                    nowInTopic = false;
                    nowInKeepersList = true;
                    indexOfStart = i + 1;
                    continue;
            }

            if (nowInResult && currentKey == '"')
            {
                nowInResult = false;
                nowInTopicKey = true;
                continue;
            }

            switch (nowInTopicKey)
            {
                case true when currentKey != '"': continue;
                case true:
                    nowInTopicKey = false;
                    nowInTopic = true;
                    i += 2;
                    continue;
            }

            switch (nowInKeepersList)
            {
                case true when currentKey == ',':
                {
                    var text = GetSubSequenceOfBytes(ref chars, indexOfStart, i - indexOfStart);

                    concurentParsed.AddOrUpdate(text, 1, (k, v) => v + 1);

                    indexOfStart = i + 1;
                    continue;
                }
                case true when currentKey == ']':
                {
                    if (i - 1 - indexOfStart > 0)
                    {
                        var text = GetSubSequenceOfBytes(ref chars, indexOfStart, i - indexOfStart);

                        if (!string.IsNullOrWhiteSpace(text))
                            concurentParsed.AddOrUpdate(text, 1, (k, v) => v + 1);
                    }

                    nowInKeepersList = false;
                    nowInEndKeepersList = true;
                    continue;
                }
                case true: continue;
            }
        }
    }

    private string GetSubSequenceOfBytes(ref byte[] bytes, int index, int length)
    {
        var subsequence = new char[length];

        for (var i = 0; i < length; i++)
            subsequence[i] = (char)bytes[index + i];

        return new string(subsequence);
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
