using KeSpider.API;
using System.Text.RegularExpressions;

namespace KeSpider.OutlinkHandlers;

public class SimpleOutlinkHandler(string name, Regex pattern) : IOutlinkHandler
{
    public Regex Pattern => pattern;
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

            Console.WriteLine($"    @O - Find Outlink of ${name}: {text}");

            if (Program.SavemodeContent == SaveMode.Skip && File.Exists(path))
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
