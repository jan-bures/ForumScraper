using AngleSharp;
using AngleSharp.Dom;

namespace ForumScraper;

public class Parser
{
    private readonly IBrowsingContext _context;

    public Parser()
    {
        IConfiguration config = Configuration.Default;
        _context = BrowsingContext.New(config);
    }

    /// <summary>
    /// Parses the homepage of the forum and returns a list of URLs to the forums.
    /// </summary>
    /// <param name="html">The HTML of the forum homepage.</param>
    /// <returns>A list of URLs to the forums.</returns>
    public List<string> ParseHomePageUrls(string html)
    {
        var document = GetDocument(html);

        var forumLinks = document.QuerySelectorAll("[data-forumid] .ipsDataItem_title a");

        return forumLinks.Select(link => link.GetAttribute("href") ?? "").Take(1).ToList();
    }

    /// <summary>
    /// Parses a forum first page and returns the number of pages, a list of URLs to the subforums, and a list of URLs
    /// to the topics.
    /// </summary>
    /// <param name="html">The HTML of the forum page.</param>
    /// <returns>The number of pages, a list of URLs to the subforums, and a list of URLs to the topics.</returns>
    public (int pageCount, List<string> subforumUrls, List<string> topicUrls) ParseForum(string html)
    {
        var document = GetDocument(html);

        int pageCount = GetPageCount(document);
        var subforumLinks = document.QuerySelectorAll("[data-forumid] .ipsDataItem_title a");
        var topicLinks = document.QuerySelectorAll(".cTopicList .ipsDataItem_title .ipsContained a");

        return (
            pageCount,
            subforumLinks.Select(link => link.GetAttribute("href") ?? "").Take(1).ToList(),
            topicLinks.Select(link => link.GetAttribute("href") ?? "").Take(1).ToList()
        );
    }

    /// <summary>
    /// Parses a non-first forum page and returns a list of URLs to the topics.
    /// </summary>
    /// <param name="html">The HTML of the forum page.</param>
    /// <returns>A list of URLs to the topics.</returns>
    public List<string> ParseForumPage(string html)
    {
        var document = GetDocument(html);

        var topicLinks = document.QuerySelectorAll(".ipsDataItem_title a");

        return topicLinks.Select(link => link.GetAttribute("href") ?? "").Take(1).ToList();
    }

    /// <summary>
    /// Parses a topic page and returns the number of pages.
    /// </summary>
    /// <param name="html">The HTML of the topic page.</param>
    /// <returns>The number of pages.</returns>
    public int ParseTopic(string html)
    {
        var document = GetDocument(html);

        int pageCount = GetPageCount(document);

        return pageCount;
    }

    private static int GetPageCount(IDocument document)
    {
        var pagination = document.QuerySelector(".ipsPagination");

        int postCount = 1;
        if (pagination != null && pagination.HasAttribute("data-pages"))
        {
            postCount = int.Parse(pagination.GetAttribute("data-pages")!);
        }

        return postCount;
    }

    private IDocument GetDocument(string html)
    {
        return _context.OpenAsync(req => req.Content(html)).Result;
    }
}