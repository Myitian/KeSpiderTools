using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KeSpider.API;

public struct Attachment
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("stem")]
    public string Stem { get; set; }

    [JsonPropertyName("server")]
    public string? Server { get; set; }
}

public struct Embed
{
    [JsonPropertyName("url")]
    public string? URL { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("description")]
    public object? Description { get; set; }
}

public struct Post
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("published")]
    public DateTime Published { get; set; }

    [JsonPropertyName("edited")]
    public DateTime? Edited { get; set; }

    [JsonPropertyName("file")]
    public Attachment File { get; set; }

    [JsonPropertyName("embed")]
    public Embed Embed { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment> Attachments { get; set; }
}

public class PostRoot
{
    [JsonPropertyName("post")]
    public Post Post { get; set; }

    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }

    [JsonPropertyName("previews")]
    public List<Attachment>? Previews { get; set; }

    public static async Task<(byte[], PostRoot?)> Request(HttpClient client, string domain, string service, string user, string post)
    {
        string url = $"https://{domain}/api/v1/{service}/user/{user}/post/{post}";
        while (true)
        {
            Console.WriteLine($"GET {url}");
            using HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            if (resp.IsSuccessStatusCode)
            {
                return (
                    await resp.Content.ReadAsByteArrayAsync(),
                    await resp.Content.ReadFromJsonAsync(SourceGenerationContext.Default.PostRoot));
            }
            Console.WriteLine($"HTTP STATUS CODE {resp.StatusCode}");
            Thread.Sleep(1000);
        }
    }
}