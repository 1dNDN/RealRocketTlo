using System.Text.Json;
using System.Text.Json.Serialization;

namespace NativeTloLibrary.StaticApi;

public class KeepersList
{
    public static async Task<Dictionary<long, string>> GetKeepersAsync()
    {
        var keeperNames = new Dictionary<long, string>();

        // var proxy = new WebProxy {
        //     Address = new Uri("socks5://gateway.keeps.cyou:2081")
        // };

        var handler = new HttpClientHandler {
            // Proxy = proxy
        };

        var httpClient = new HttpClient(handler);
        var data = await httpClient.GetStringAsync("https://api.rutracker.cc/v1/static/keepers_user_data");

        var result = JsonSerializer.Deserialize<Root>(data)?.Result;

        foreach (var (id, value) in result)
        {
            var name = value[0].ToString();

            keeperNames.Add(long.Parse(id), name);
        }


        return keeperNames;
    }

    private class Root
    {
        [property: JsonPropertyName("result")]
        public Dictionary<string, object[]> Result { get; set; }
    }
}
