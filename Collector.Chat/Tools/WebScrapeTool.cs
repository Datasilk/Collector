using System.Collections.Generic;
using System.Text.Json.Serialization;
using Collector.Chat.Models;
using Collector.Common;
using Collector.Common.DOM;
using Collector.Common.Models;
using Collector.Common.Models.Articles;

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
        List<Dictionary<string, List<string>>> data,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        Guid appUserId,
        Collector.Chat.Models.ExecutionPlan? plan = null,
        int currentStepIndex = -1,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null)
    {
        try
        {
            onProgress(10, "Preparing to scrape web page...");

            // Get URL from plan args or check if this is a follow-up call with URL in data
            string url = string.Empty;
            if (plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count)
            {
                var step = plan.Steps[currentStepIndex];
                if (step.Args != null && step.Args.TryGetValue("url", out var urlObj) && urlObj != null)
                {
                    url = urlObj.ToString() ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(url))
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
            List<string> headers = new();
            List<string> contentSections = new();
            List<string> links = new();
            List<string> images = new();
            List<string> layout = new();
            List<DomElement> bodyElements = new();

            try
            {
                var analyzedArticle = Html.DeserializeArticle(domJson);
                Html.GetArticleInfoFromDOM(analyzedArticle);
                
                // Get best element indexes and populate article body elements
                var indexes = new List<AnalyzedElement>();
                Html.GetBestElementIndexes(analyzedArticle, indexes);
                Html.GetArticleElements(analyzedArticle, indexes);
                
                title = analyzedArticle.title ?? string.Empty;
                bodyElements = analyzedArticle.bodyElements ?? new List<DomElement>();

                // Extract structured content from body elements
                int headerIndex = 0;
                int contentIndex = 0;
                int imageIndex = 0;

                foreach (var element in bodyElements)
                {
                    // Skip script and style elements
                    if (element.tagName == "script" || element.tagName == "style" || element.tagName == "!--")
                        continue;

                    // Handle header elements (h1-h6)
                    if (element.tagName.StartsWith("h") && element.tagName.Length == 2 &&
                        char.IsDigit(element.tagName[1]))
                    {
                        var headerText = element.text?.Trim();
                        if (!string.IsNullOrWhiteSpace(headerText))
                        {
                            headers.Add(headerText);
                            layout.Add($"header:{headerIndex}");
                            headerIndex++;
                        }
                        continue;
                    }

                    // Handle image elements
                    if (element.tagName == "img")
                    {
                        var src = element.attribute?.GetValueOrDefault("src", "");
                        if (!string.IsNullOrWhiteSpace(src))
                        {
                            // Convert relative URLs to absolute
                            if (src.StartsWith("/"))
                            {
                                var baseUri = new Uri(resolvedUrl ?? url);
                                src = new Uri(baseUri, src).ToString();
                            }
                            images.Add(src);
                            layout.Add($"image:{imageIndex}");
                            imageIndex++;
                        }
                        continue;
                    }

                    // Handle anchor elements (links)
                    if (element.tagName == "a")
                    {
                        var href = element.attribute?.GetValueOrDefault("href", "");
                        var linkText = element.text?.Trim();
                        if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("#") && !href.StartsWith("javascript:"))
                        {
                            // Convert relative URLs to absolute
                            if (href.StartsWith("/"))
                            {
                                var baseUri = new Uri(resolvedUrl ?? url);
                                href = new Uri(baseUri, href).ToString();
                            }
                            var linkEntry = !string.IsNullOrWhiteSpace(linkText)
                                ? $"{linkText}|{href}"
                                : href;
                            links.Add(linkEntry);
                        }
                        continue;
                    }

                    // Handle content elements (p, div, article, section, etc.)
                    if (element.tagName is "p" or "div" or "article" or "section" or "span" or "li" or "td" or "blockquote")
                    {
                        var text = element.text?.Trim();
                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 20)
                        {
                            contentSections.Add(text);
                            layout.Add($"content:{contentIndex}");
                            contentIndex++;
                        }
                    }
                }

                // If no structured content found, fall back to raw text
                if (contentSections.Count == 0)
                {
                    if (!string.IsNullOrEmpty(analyzedArticle.summary))
                    {
                        contentSections.Add(analyzedArticle.summary);
                        layout.Add("content:0");
                    }
                    else if (!string.IsNullOrEmpty(analyzedArticle.rawText))
                    {
                        contentSections.Add(analyzedArticle.rawText);
                        layout.Add("content:0");
                    }
                }

                // Extract all images from the article (including those not in bodyElements)
                if (analyzedArticle.images != null)
                {
                    foreach (var img in analyzedArticle.images)
                    {
                        if (!string.IsNullOrWhiteSpace(img.url) && !images.Contains(img.url))
                        {
                            // Convert relative URLs to absolute
                            var src = img.url;
                            if (src.StartsWith("/"))
                            {
                                var baseUri = new Uri(resolvedUrl ?? url);
                                src = new Uri(baseUri, src).ToString();
                            }
                            images.Add(src);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                onError($"Error analyzing content: {ex.Message}");
                return;
            }

            onProgress(90, "Storing extracted data...");

            // Build structured data dictionary
            var scrapedData = new Dictionary<string, List<string>>
            {
                ["type"] = new List<string> { "web-scrape" },
                ["url"] = new List<string> { resolvedUrl ?? url },
                ["title"] = new List<string> { title }
            };

            if (headers.Count > 0)
                scrapedData["headers"] = headers;

            if (contentSections.Count > 0)
                scrapedData["content"] = contentSections;

            if (links.Count > 0)
                scrapedData["links"] = links;

            if (images.Count > 0)
                scrapedData["images"] = images;

            if (layout.Count > 0)
                scrapedData["layout"] = layout;

            // Add to data list
            data.Add(scrapedData);

            // Save scraped content to chat history for fact extraction
            var allContent = string.Join("\n\n", contentSections);
            if (!string.IsNullOrWhiteSpace(allContent))
            {
                onSaveChatHistory?.Invoke($"Web scraped from {resolvedUrl ?? url}:\n{allContent}");
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
