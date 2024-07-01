using System.Collections.Concurrent;
using ForumScraper;

const string outputDirectory = "output";
const string websiteUrl = "https://forum.kerbalspaceprogram.com/";

Console.WriteLine("Starting forum scraping...");

// var client = new FakeHttpClient();
var client = new HttpClient();

string response = await client.GetStringAsync(websiteUrl);

var parser = new Parser();
var forumUrls = parser.ParseHomePageUrls(response);

await Task.WhenAll(forumUrls.Select(url => ProcessForum(url)));

return;

async Task ProcessForum(string forumUrl, string parentPath = outputDirectory)
{
    string forumHtml = await client.GetStringAsync(forumUrl);
    var (pageCount, subforumUrls, topicUrls) = parser.ParseForum(forumHtml);
    string path = Path.Combine(parentPath, UrlUtils.GetForumSection(forumUrl));

    var subforumTasks = subforumUrls.Select(url => ProcessForum(url, path));
    await Task.WhenAll(subforumTasks);

    var allTopicUrls = new ConcurrentBag<string>(topicUrls);

    var pageTasks = Enumerable.Range(2, pageCount - 1)
        .Select(async i =>
        {
            string forumPageHtml = await client.GetStringAsync($"{forumUrl}page/{i}");
            var newTopicUrls = parser.ParseForumPage(forumPageHtml);
            foreach (string url in newTopicUrls)
            {
                allTopicUrls.Add(url);
            }
        });

    await Task.WhenAll(pageTasks);

    await Task.WhenAll(allTopicUrls.Select(url => ProcessTopic(url, path)));
}

async Task ProcessTopic(string topicUrl, string parentPath)
{
    string topicPage = await client.GetStringAsync(topicUrl);
    int pageCount = parser.ParseTopic(topicPage);
    string safeName = UrlUtils.GetTopicSection(topicUrl);

    string path = Path.Combine(parentPath, safeName + ".html");
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await File.WriteAllTextAsync(path, topicPage);

    var pageTasks = Enumerable.Range(2, pageCount - 1)
        .Select(async i =>
        {
            string topicPageHtml = await client.GetStringAsync($"{topicUrl}page/{i}");
            string pagePath = Path.Combine(parentPath, safeName + $"_page_{i}.html");
            await File.WriteAllTextAsync(pagePath, topicPageHtml);
        });

    await Task.WhenAll(pageTasks);
}