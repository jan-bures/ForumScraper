namespace ForumScraper;

public class FakeHttpClient(string baseDirectory = "input")
{
    public string GetString(string url)
    {
        string localPath = UrlUtils.ConvertUrlToLocalPath(url, baseDirectory);
        if (!File.Exists(localPath))
        {
            throw new HttpRequestException(
                $"Local file not found: {localPath}",
                null,
                System.Net.HttpStatusCode.NotFound
            );
        }

        return File.ReadAllText(localPath);
    }

    public async Task<string> GetStringAsync(string url)
    {
        string localPath = UrlUtils.ConvertUrlToLocalPath(url, baseDirectory);
        if (!File.Exists(localPath))
        {
            throw new HttpRequestException(
                $"Local file not found: {localPath}",
                null,
                System.Net.HttpStatusCode.NotFound
            );
        }

        return await File.ReadAllTextAsync(localPath);
    }
}