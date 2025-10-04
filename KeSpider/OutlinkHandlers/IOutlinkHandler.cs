using KeSpider.API;
using System.Text.RegularExpressions;

namespace KeSpider.OutlinkHandlers;

public interface IOutlinkHandler
{
    public abstract Regex Pattern { get; }
    public abstract ValueTask ProcessMatches(
        HttpClient client,
        Dictionary<Array256bit, string> dlCache,
        PostRoot post,
        DateTime datetime,
        DateTime datetimeEdited,
        string pageFolderPath,
        string content,
        HashSet<string> usedLinks,
        params IEnumerable<Match> matches);
}
