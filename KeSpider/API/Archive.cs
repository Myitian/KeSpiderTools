using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KeSpider.API;

public struct Archive
{
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    public static async Task<Archive> Request(HttpClient client, string domain, string hash, int retry = 10)
    {
        string url = $"https://{domain}/api/v1/file/{hash}";
        while (true)
        {
            Console.WriteLine($"GET {url}");
            using HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Archive);
            Console.WriteLine($"HTTP STATUS CODE {resp.StatusCode}");
            if (--retry == 0)
                return new();
            Thread.Sleep(1000);
        }
    }
}
