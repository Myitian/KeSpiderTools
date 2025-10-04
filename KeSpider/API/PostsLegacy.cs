using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KeSpider.API;

public struct PostsProps
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public struct PostsResult
{
    [JsonPropertyName("id")]
    public string ID { get; set; }
    [JsonPropertyName("user")]
    public string User { get; set; }
    [JsonPropertyName("service")]
    public string Service { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }

    public static async Task<List<PostsResult>?> Request(HttpClient client, string domain, string service, string user, int offset = 0)
    {
        string url = $"https://{domain}/api/v1/{service}/user/{user}/posts?o={offset}";
        while (true)
        {
            Console.WriteLine($"GET {url}");
            using HttpResponseMessage resp = await client.GetAsync(url/*, HttpCompletionOption.ResponseHeadersRead*/);
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync(SourceGenerationContext.Default.ListPostsResult);
            Console.WriteLine($"HTTP STATUS CODE {resp.StatusCode}");
            Thread.Sleep(1000);
        }
    }
}

public struct PostsLegacy
{
    [JsonPropertyName("props")]
    public PostsProps Props { get; set; }

    [JsonPropertyName("results")]
    public List<PostsResult>? Results { get; set; }

    public static async Task<PostsLegacy> Request(HttpClient client, string domain, string service, string user, int offset = 0)
    {
        string url = $"https://{domain}/api/v1/{service}/user/{user}/posts-legacy?o={offset}";
        while (true)
        {
            Console.WriteLine($"GET {url}");
            using HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync(SourceGenerationContext.Default.PostsLegacy);
            Console.WriteLine($"HTTP STATUS CODE {resp.StatusCode}");
            Thread.Sleep(1000);
        }
    }
}