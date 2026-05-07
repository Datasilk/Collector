using Collector.Chat.Models;
using Collector.Common;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for downloading an HTML web page and extracting its contents
/// </summary>
public class WebScrapeTool : IChatTool
{
    public string ToolKey => "web-scrape";

    public string Description => "Download an HTML web page and extract its contents";

    public bool AvailableToOllama => false;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "url",
            DataType = "string",
            Description = "A URL to scrape"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "title",
            DataType = "string"
        },
        new ToolResponseField
        {
            Key = "content",
            DataType = "string"
        },
        new ToolResponseField
        {
            Key = "url",
            DataType = "string"
        }
    };

    public async Task Run(
        string userMessage,
        string ragContext,
        Dictionary<string, string> data,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        ExecutionPlan? plan = null,
        int currentStepIndex = -1,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null)
    {
        try
        {
            onProgress(10, "Preparing to scrape web page...");

            // Get URL from data dictionary
            if (!data.TryGetValue("url", out var url) || string.IsNullOrWhiteSpace(url))
            {
                onError("No URL provided for web scraping");
                return;
            }

            onProgress(30, $"Downloading content from {url}...");

            // Download article DOM JSON via Charlotte router
            string resolvedUrl = url;
            var domJson = await Task.Run(() => Charlotte.Download(url, out resolvedUrl));

            if (string.IsNullOrWhiteSpace(domJson) || domJson.StartsWith("file:") || domJson.StartsWith("Error"))
            {
                onError($"Failed to download or parse content from URL: {url}");
                return;
            }

            onProgress(60, "Analyzing HTML document...");

            string title = string.Empty;
            string content = string.Empty;

            try
            {
                var analyzedArticle = Html.DeserializeArticle(domJson);
                Html.GetArticleInfoFromDOM(analyzedArticle);
                title = analyzedArticle.title ?? string.Empty;

                // Extract main content text using the correct method
                if (analyzedArticle.bodyElements.Count > 0)
                {
                    content = Html.GetArticleText(analyzedArticle);
                }
                else if (!string.IsNullOrEmpty(analyzedArticle.summary))
                {
                    content = analyzedArticle.summary;
                }
                else if (!string.IsNullOrEmpty(analyzedArticle.rawText))
                {
                    content = analyzedArticle.rawText;
                }
            }
            catch (Exception ex)
            {
                onError($"Error analyzing content: {ex.Message}");
                return;
            }

            onProgress(90, "Storing extracted data...");

            // Store results in data dictionary for other tools to use
            data["scraped_title"] = title;
            data["scraped_content"] = content;
            data["scraped_url"] = resolvedUrl ?? url;

            // Save scraped content to chat history for fact extraction
            if (!string.IsNullOrWhiteSpace(content))
            {
                onSaveChatHistory?.Invoke($"Web scraped from {resolvedUrl ?? url}:\n{content}");
            }

            onProgress(100, "Web scraping completed successfully");

            var summary = !string.IsNullOrEmpty(title)
                ? $"Successfully scraped: {title}"
                : "Successfully scraped web page content";

            onComplete(summary);
        }
        catch (Exception ex)
        {
            onError($"Unexpected error during web scraping: {ex.Message}", ex);
        }
    }
}
