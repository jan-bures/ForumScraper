using System.Text.RegularExpressions;

namespace ForumScraper;

public static partial class UrlUtils
{
    [GeneratedRegex(@"/forum/(\d+[^/]+)")]
    private static partial Regex ForumRegex();

    [GeneratedRegex(@"/topic/(\d+[^/]+)")]
    private static partial Regex TopicRegex();

    [GeneratedRegex(@"^https?://")]
    private static partial Regex ProtocolRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9_-]")]
    private static partial Regex SpecialCharsRegex();

    public static string GetForumSection(string url)
    {
        Match match = ForumRegex().Match(url);
        return match.Groups[1].Value;
    }

    public static string GetTopicSection(string url)
    {
        Match match = TopicRegex().Match(url);
        return match.Groups[1].Value;
    }

    public static string ConvertUrlToLocalPath(string url, string baseDirectory)
    {
        if (url.EndsWith('/'))
        {
            url = url[..^1];
        }

        string withoutProtocol = ProtocolRegex().Replace(url, "");
        string underscored = withoutProtocol.Replace("/", "_");
        string sanitized = SpecialCharsRegex().Replace(underscored, "_");
        return Path.Combine(baseDirectory, sanitized + ".html");
    }
}