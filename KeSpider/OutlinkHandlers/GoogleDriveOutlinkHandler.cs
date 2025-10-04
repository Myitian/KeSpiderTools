using KeSpider.API;
using System.Text;
using System.Text.RegularExpressions;

namespace KeSpider.OutlinkHandlers;

public partial class GoogleDriveOutlinkHandler : IOutlinkHandler
{
    [GeneratedRegex(@"(?<url>https://drive\.google\.com/(?:file/d|drive/folders)/[^""'<>\s]+)")]
    internal static partial Regex RegGoogleDrive();

    [GeneratedRegex(@"(?<url>https://drive\.google\.com/file/d/(?<id>[^""'<>\s?#/]+))")]
    internal static partial Regex RegGoogleDriveFile();
    public static OneDriveOutlinkHandler Instance { get; } = new();
    public Regex Pattern => RegGoogleDrive();
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
        HashSet<string> gDriveIDs = [];
        foreach (Match m in matches)
        {
            if (!m.Success)
                continue;
            string text = m.Groups["url"].Value;
            if (!usedLinks.Add(text))
                continue;
            string fileName = Utils.ReplaceInvalidFileNameChars(text) + ".placeholder.txt";
            string path = Path.Combine(pageFolderPath, fileName);

            Console.WriteLine($"    @O - Find Outlink of GoogleDrive: {text}");

            if (Program.SavemodeContent == SaveMode.Skip && File.Exists(path))
            {
                Console.WriteLine($"    @O - Skipped");
                Utils.SetTime(path, datetime, datetimeEdited);
            }
            else
            {
                Utils.SaveFile(text, fileName, pageFolderPath, datetime, datetimeEdited, Program.SavemodeOutlink);
                Match mm = RegGoogleDriveFile().Match(text);
                if (mm.Success)
                {
                    string gdid = mm.Groups["id"].Value;
                    if (gDriveIDs.Contains(gdid))
                        continue;
                    string urlDirect = $"https://drive.usercontent.google.com/download?export=download&authuser=0&confirm=t&id={gdid}";
                    using HttpRequestMessage headReq = new(HttpMethod.Head, urlDirect);
                    using HttpResponseMessage headResp = await client.SendAsync(headReq);
                    if (headResp.Content.Headers.ContentDisposition?.FileName is not null)
                    {
                        // In this API, GoogleDrive will send filename in "filename" and UTF-8, not Latin-1 or "filename*"
                        headResp.Content.Headers.ContentDisposition.FileName = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(headResp.Content.Headers.ContentDisposition.FileName));
                    }
                    string? name = headResp.Content.Headers.ContentDisposition?.FileNameStar
                                ?? headResp.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                                ?? headResp.RequestMessage?.RequestUri?.AbsolutePath
                                ?? "file";
                    name = Path.GetFileName(name);
                    string path2 = Path.Combine(pageFolderPath, name);
                    if (File.Exists(path2))
                    {
                        Console.WriteLine($"    @O - Skipped");
                        goto E;
                    }
                    Console.WriteLine($"    @O - aria2c!");
                    Program.Aria2cDownload(pageFolderPath, name, urlDirect);
                E:
                    Utils.SetTime(path2, datetime, datetimeEdited);
                    FileInfo fi = new(path2);
                    string d = Path.Combine(fi.DirectoryName ?? "", Path.GetFileNameWithoutExtension(fi.Name));
                    if (!Directory.Exists(d) && !File.Exists(d))
                    {
                        if (fi.Extension is ".zip" or ".rar" or ".7z" or ".gz" or ".tar")
                            Program.SevenZipExtract(d, path2);
                    }
                    gDriveIDs.Add(gdid);
                }
            }
        }
    }
}
