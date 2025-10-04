using System.Text.RegularExpressions;

namespace KeSpider;

partial class Regexes
{
    [GeneratedRegex(@"https://(?<domain>[^/]+)/(?:api/v\d/)?(?<service>[^/]+)/user/(?<user>[^/\?#]+)")]
    internal static partial Regex RegMainPage();

    [GeneratedRegex(@"https://(?<domain>[^/]+)/(?:api/v\d/)?(?<service>[^/]+)/user/(?<user>[^/]+)/post/(?<id>[^/\?#]+)")]
    internal static partial Regex RegPostPage();

    [GeneratedRegex(@"(?<url>https?:[/\\]{2}(?:[^\x00-\x1f \x7f""<>\^`\{\|\}\.\\/\?#]+\.)+[^\x00-\x1f \x7f""<>\^`\{\|\}\.\\/\?#]+(?:[/\\\?#][^\x00-\x1f \x7f""<>\^`\{\|\}]*)*)")]
    internal static partial Regex RegUrl();

    [GeneratedRegex(@"(?<server>(?:https://[^/]+)?)(?<path>/(?:[0-9a-fA-F]{2}/){2}(?<name>[0-9a-fA-F]+\.[0-9A-Za-z]+))")]
    internal static partial Regex RegInlineFile();

    [GeneratedRegex(@"(?<url>https?://mega(?:\.co)?\.nz/[^""'<>\s]+)(?:<[^\>]+>)?(?<hash>#[a-zA-Z0-9\-_]+)")]
    internal static partial Regex RegMega();

    [GeneratedRegex(@"(?<url>https://www\.mediafire\.com/(?:\?|file/)[a-zA-Z0-9]+)")]
    internal static partial Regex RegMediaFire();


    [GeneratedRegex(@"\.(?<num>\d+)$")]
    internal static partial Regex RegMultiPartNumberOnly();

    [GeneratedRegex(@"\.part(?<num>\d+)\.rar$")]
    internal static partial Regex RegMultiPartRar();

    [GeneratedRegex(@"\.r(?<num>\d+)$")]
    internal static partial Regex RegMultiPartRxx();

    [GeneratedRegex(@"\.z(?<num>\d+)$")]
    internal static partial Regex RegMultiPartZxx();
}
