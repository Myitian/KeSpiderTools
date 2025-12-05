using KeSpider.API;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace KeSpider.OutlinkHandlers;

public partial class OneDriveOutlinkHandler : IOutlinkHandler, IDisposable
{
    private static readonly MediaTypeWithQualityHeaderValue Any = new("*/*");
    private static readonly MediaTypeWithQualityHeaderValue ApplicationJson = new(MediaTypeNames.Application.Json);
    [GeneratedRegex(@"(?<url>https://(?:1drv\.ms/[^""'<>\s]+|[^\.]+\.sharepoint\.com/[^""'<>\s]+))")]
    internal static partial Regex RegOneDrive();
    public static OneDriveOutlinkHandler Instance { get; } = new();

    private SocketsHttpHandler? handler = null;
    private HttpClient? client = null;
    private HttpClient Client
    {
        get
        {
            if (client is not null)
                return client;
            handler = new()
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = false,
                UseProxy = true
            };
            client = new(handler);
            Console.WriteLine("AuthenticationHeader:");
            ReadOnlySpan<char> span = Console.ReadLine().AsSpan().Trim();
            int space = span.IndexOf(' ');
            client.DefaultRequestHeaders.Authorization = space < 0 ?
                new(new(span)) :
                new(new(span[..space].Trim()), new(span[space..].Trim()));
            return client;
        }
    }
    public Regex Pattern => RegOneDrive();
    public async ValueTask ProcessMatches(
        HttpClient client,
        Dictionary<Array256bit, string> dlCache,
        PostRoot post,
        DateTime datetime,
        DateTime datetimeEdited,
        string pageFolderPath,
        string content,
        HashSet<string> usedLinks,
        params IEnumerable<Match> matches)
    {
        foreach (Match m in matches)
        {
            if (!m.Success)
                continue;
            string text = m.Groups["url"].Value;
            if (!usedLinks.Add(text))
                continue;
            string fileName = Utils.ReplaceInvalidFileNameChars(text) + ".placeholder.txt";
            string path = Path.Combine(pageFolderPath, fileName);

            Console.WriteLine($"    @O - Find Outlink of OneDrive: {text}");

            if (Program.SavemodeContent == SaveMode.Skip && File.Exists(path))
            {
                Console.WriteLine("    @O - Skipped");
                Utils.SetTime(path, datetime, datetimeEdited);
            }
            else
            {
                Utils.SaveFile(text, fileName, pageFolderPath, datetime, datetimeEdited, Program.SavemodeOutlink);
                string sharingToken = EncodeSharingUrl(text);
                string endpoint = $"https://graph.microsoft.com/v1.0/shares/u!{sharingToken}/driveItem";
                Console.WriteLine($"    @O - Metadata {endpoint}");
                DriveItem? driveItem;
                string? raw = null;
                using (HttpRequestMessage reqMetadata = new(HttpMethod.Get, endpoint))
                {
                    reqMetadata.Headers.Accept.Clear();
                    reqMetadata.Headers.Accept.Add(ApplicationJson);
                    using HttpResponseMessage respMetadata = await Client.SendAsync(reqMetadata, HttpCompletionOption.ResponseContentRead);
                    raw = await respMetadata.Content.ReadAsStringAsync();
                    driveItem = await respMetadata.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.DriveItem);
                }
                if (driveItem is not { Name: not null, File: not null })
                {
                    Console.WriteLine(raw);
                    return;
                }
                fileName = Program.FixSpecialExt(driveItem.Name);
                path = Path.Combine(pageFolderPath, fileName);
                Array256bit sha256url = new();
                if (driveItem.File.Hashes.SHA256Hash?.Length is SHA256.HashSizeInBits / 4)
                {
                    Convert.FromHexString(driveItem.File.Hashes.SHA256Hash, sha256url, out _, out _);
                    if (dlCache.TryGetValue(sha256url, out string? duplicated))
                    {
                        if (File.Exists(path))
                            File.Delete(path);
                        Console.WriteLine("    @O - Link");
                        Utils.MakeLink(path, duplicated);
                        goto E;
                    }
                    else if (File.Exists(path))
                    {
                        switch (Program.SavemodeFile)
                        {
                            case SaveMode.Skip:
                                Console.WriteLine($"    @O - Skipped");
                                goto E;
                            case SaveMode.Replace:
                                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    Array256bit sha256local = new();
                                    SHA256.HashData(fs, sha256local);
                                    if (sha256local == sha256url)
                                    {
                                        Console.WriteLine($"    @O - Skipped (SHA256)");
                                        goto E;
                                    }
                                }
                                break;
                        }
                    }
                }
                string? url;
                using (HttpRequestMessage reqContent = new(HttpMethod.Get, $"{endpoint}/content"))
                {
                    reqContent.Headers.Accept.Clear();
                    reqContent.Headers.Accept.Add(Any);
                    using HttpResponseMessage respContent = await Client.SendAsync(reqContent, HttpCompletionOption.ResponseHeadersRead);
                    url = respContent.Headers.Location?.ToString();
                }
                if (string.IsNullOrEmpty(url))
                    return;
                Console.WriteLine($"    @O - aria2c!");
                Program.Aria2cDownload(pageFolderPath, fileName, url);
            E:
                if (driveItem.File.Hashes.SHA256Hash?.Length is SHA256.HashSizeInBits / 4)
                    dlCache[sha256url] = fileName;
                Utils.SetTime(path,
                    driveItem.FileSystemInfo.CreatedDateTime ?? driveItem.CreatedDateTime ?? datetime,
                    driveItem.FileSystemInfo.LastModifiedDateTime ?? driveItem.LastModifiedDateTime ?? datetimeEdited);
                FileInfo fi = new(path);
                string d = Path.Combine(fi.DirectoryName ?? "", Path.GetFileNameWithoutExtension(fi.Name));
                if (!Directory.Exists(d) && !File.Exists(d))
                {
                    if (fi.Extension is ".zip" or ".rar" or ".7z" or ".gz" or ".tar")
                        Program.SevenZipExtract(d, path);
                }
            }
        }
    }

    public static string EncodeSharingUrl(string shareUrl)
    {
        const string table = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        if (string.IsNullOrEmpty(shareUrl))
            return string.Empty;
        int utf8Length = Encoding.UTF8.GetByteCount(shareUrl);
        int base64Length = (utf8Length * 4 + 2) / 3;
        Span<char> charBuffer = base64Length <= 1024 ? stackalloc char[base64Length] : new char[base64Length];
        int charIndex = 0;
        int byteCount = 0;
        Span<byte> byteBuffer = stackalloc byte[6];
        foreach (Rune rune in shareUrl.EnumerateRunes())
        {
            int count = rune.EncodeToUtf8(byteBuffer[byteCount..]);
            byteCount += count;
            if (byteCount >= 3)
            {
                int offset = 0;
                do
                {
                    int v0 = byteBuffer[offset++] << 16;
                    int v1 = byteBuffer[offset++] << 8;
                    int v2 = byteBuffer[offset++];
                    int value = v0 | v1 | v2;
                    charBuffer[charIndex++] = table[(value >> 18) & 0x3F];
                    charBuffer[charIndex++] = table[(value >> 12) & 0x3F];
                    charBuffer[charIndex++] = table[(value >> 6) & 0x3F];
                    charBuffer[charIndex++] = table[value & 0x3F];
                    byteCount -= 3;
                } while (byteCount >= 3);
                byteBuffer.Slice(offset, byteCount).CopyTo(byteBuffer);
            }
        }
        switch (byteCount)
        {
            case 2:
                int v1 = byteBuffer[0] << 16;
                int v2 = byteBuffer[1] << 8;
                int value = v1 | v2;
                charBuffer[charIndex++] = table[(value >> 18) & 0x3F];
                charBuffer[charIndex++] = table[(value >> 12) & 0x3F];
                charBuffer[charIndex++] = table[(value >> 6) & 0x3F];
                break;
            case 1:
                value = byteBuffer[0] << 16;
                charBuffer[charIndex++] = table[(value >> 18) & 0x3F];
                charBuffer[charIndex++] = table[(value >> 12) & 0x3F];
                break;
        }

        return new string(charBuffer);
    }

    public void Dispose()
    {
        client?.Dispose();
        handler?.Dispose();
        GC.SuppressFinalize(this);
    }

    public class DriveFile
    {
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("hashes")]
        public Hashes Hashes { get; set; }
    }

    public struct FileSystemInfo
    {
        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [JsonPropertyName("lastModifiedDateTime")]
        public DateTime? LastModifiedDateTime { get; set; }
    }

    public struct Hashes
    {
        [JsonPropertyName("crc32Hash")]
        public string? CRC32Hash { get; set; }

        [JsonPropertyName("quickXorHash")]
        public string? QuickXorHash { get; set; }

        [JsonPropertyName("sha1Hash")]
        public string? SHA1Hash { get; set; }

        [JsonPropertyName("sha256Hash")]
        public string? SHA256Hash { get; set; }
    }

    public class DriveItem
    {
        [JsonPropertyName("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        [JsonPropertyName("lastModifiedDateTime")]
        public DateTime? LastModifiedDateTime { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("file")]
        public DriveFile? File { get; set; }

        [JsonPropertyName("fileSystemInfo")]
        public FileSystemInfo FileSystemInfo { get; set; }
    }
}
