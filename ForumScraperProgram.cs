using System.Collections.Concurrent;
using ForumScraper;

const string outputDirectory = "output";
const string websiteUrl = "https://forum.kerbalspaceprogram.com/";

Console.WriteLine("Starting forum scraping...");

using var client = new HttpClient();
var parser = new Parser();

try
{
    string response = await client.GetStringAsync(websiteUrl);
    var forumUrls = parser.ParseHomePageUrls(response);

    await Task.WhenAll(forumUrls.Select(url => ProcessForum(url)));
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Error fetching main page: {ex.Message}");
    Console.WriteLine($"Status code: {ex.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error processing main page: {ex.GetType().Name} - {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("Forum scraping completed.");
return;

async Task ProcessForum(string forumUrl, string parentPath = outputDirectory)
{
    try
    {
        Console.WriteLine($"Processing forum: {forumUrl}");
        string forumHtml = await client.GetStringAsync(forumUrl);
        var (pageCount, subforumUrls, topicUrls) = parser.ParseForum(forumHtml);
        string path = Path.Combine(parentPath, UrlUtils.GetForumSection(forumUrl));

        var subforumTasks = subforumUrls.Select(url => ProcessForum(url, path));
        await Task.WhenAll(subforumTasks);

        var allTopicUrls = new ConcurrentBag<string>(topicUrls);

        var pageTasks = Enumerable.Range(2, pageCount - 1)
            .Select(async i =>
            {
                try
                {
                    string forumPageHtml = await client.GetStringAsync($"{forumUrl}page/{i}");
                    var newTopicUrls = parser.ParseForumPage(forumPageHtml);
                    foreach (string url in newTopicUrls)
                    {
                        allTopicUrls.Add(url);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching forum page {i} of {forumUrl}: {ex.Message}");
                    Console.WriteLine($"Status code: {ex.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing forum page {i} of {forumUrl}: {ex.GetType().Name} - {ex.Message}");
                }
            });

        await Task.WhenAll(pageTasks);

        await Task.WhenAll(allTopicUrls.Select(url => ProcessTopic(url, path)));
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Error fetching forum {forumUrl}: {ex.Message}");
        Console.WriteLine($"Status code: {ex.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing forum {forumUrl}: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

async Task ProcessTopic(string topicUrl, string parentPath)
{
    try
    {
        Console.WriteLine($"Processing topic: {topicUrl}");
        string topicPage = await client.GetStringAsync(topicUrl);
        int pageCount = parser.ParseTopic(topicPage);
        string safeName = UrlUtils.GetTopicSection(topicUrl);

        string path = Path.Combine(parentPath, safeName + ".html");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, topicPage);

        var pageTasks = Enumerable.Range(2, pageCount - 1)
            .Select(async i =>
            {
                try
                {
                    string topicPageHtml = await client.GetStringAsync($"{topicUrl}page/{i}");
                    string pagePath = Path.Combine(parentPath, safeName + $"_page_{i}.html");
                    await File.WriteAllTextAsync(pagePath, topicPageHtml);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error fetching topic page {i} of {topicUrl}: {ex.Message}");
                    Console.WriteLine($"Status code: {ex.StatusCode}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error writing topic page {i} of {topicUrl}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing topic page {i} of {topicUrl}: {ex.GetType().Name} - {ex.Message}");
                }
            });

        await Task.WhenAll(pageTasks);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Error fetching topic {topicUrl}: {ex.Message}");
        Console.WriteLine($"Status code: {ex.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing topic {topicUrl}: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}