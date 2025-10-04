using KeSpider.API;
using System.Text.RegularExpressions;

namespace KeSpider.OutlinkHandlers;

public partial class BaiduPanOutlinkHandler : IOutlinkHandler
{
    [GeneratedRegex(@"(?<url>https://pan\.baidu\.com/s/[^<>]+\?[^<>\sp]*?pwd=(?<pwd>[\dA-Za-z]{4})[^<>""]+)|(?<url>https://pan\.baidu\.com/s/[^<>""]+)(?:[\S\s]*?(?:提取码|p(?:ass)?w(?:or)?d)\s*[：:=]\s*(?<pwd>[\dA-Za-z]{4}))?")]
    private static partial Regex RegBaiduPan();
    public static BaiduPanOutlinkHandler Instance { get; } = new();
    public Regex Pattern => RegBaiduPan();
    public ValueTask ProcessMatches(
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

            Console.WriteLine($"    @O - Find Outlink of BaiduPan: {text}");
            if (m.Groups.ContainsKey("pwd"))
                text += "\r\npwd=" + m.Groups["pwd"].Value;

            if (Program.SavemodeOutlink == SaveMode.Skip && File.Exists(path))
            {
                Console.WriteLine("    @O - Skipped");
                Utils.SetTime(path, datetime, datetimeEdited);
            }
            else
                Utils.SaveFile(text, fileName, pageFolderPath, datetime, datetimeEdited, Program.SavemodeOutlink);
        }
        return ValueTask.CompletedTask;
    }
}
