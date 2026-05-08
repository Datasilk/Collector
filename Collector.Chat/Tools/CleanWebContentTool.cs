using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Collector.Chat.Models;
using Collector.Common;
using OllamaSharp;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for cleaning and filtering web-scraped content using Ollama to determine relevance
/// Takes all web-scrape items from the data list and creates cleaned content items
/// </summary>
public class CleanWebContentTool : IChatTool
{
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;

    public CleanWebContentTool(OllamaApiClient ollama, string ollamaModel)
    {
        _ollama = ollama;
        _ollamaModel = ollamaModel;
    }

    public string ToolKey => "clean-web-content";

    public string Description => "Filter and clean web-scraped content by analyzing relevance to the user's question. Uses AI to determine which headers, content sections, and images are relevant.";

    public bool AvailableToOllama => false;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "userMessage",
            DataType = "string",
            Description = "The original user message/question to determine relevance against"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "cleanedCount",
            DataType = "int"
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
        ExecutionPlan? plan = null,
        int currentStepIndex = -1,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null)
    {
        try
        {
            onProgress(5, "Starting content cleaning process...");

            // Find all web-scrape items in the data list
            var webScrapeItems = data.FindAll(item => 
                item.TryGetValue("type", out var typeList) && 
                typeList.Count > 0 && 
                typeList[0] == "web-scrape"
            );

            if (webScrapeItems.Count == 0)
            {
                onProgress(100, "No web-scraped content found to clean");
                onComplete("No content to clean");
                return;
            }

            onProgress(10, $"Found {webScrapeItems.Count} web-scraped pages to process");

            int processedCount = 0;
            int totalPages = webScrapeItems.Count;

            foreach (var scrapeItem in webScrapeItems)
            {
                processedCount++;
                var progressPercent = 10 + (int)((processedCount / (double)totalPages) * 80);
                
                var url = scrapeItem.TryGetValue("url", out var urlList) && urlList.Count > 0 
                    ? urlList[0] 
                    : "unknown";
                var title = scrapeItem.TryGetValue("title", out var titleList) && titleList.Count > 0
                    ? titleList[0]
                    : "";
                
                onProgress(progressPercent, $"Cleaning content from: {title ?? url}");

                // Get layout, headers, content, and images
                var layout = scrapeItem.TryGetValue("layout", out var layoutList) ? layoutList : new List<string>();
                var headers = scrapeItem.TryGetValue("headers", out var headersList) ? headersList : new List<string>();
                var contentSections = scrapeItem.TryGetValue("content", out var contentList) ? contentList : new List<string>();
                var images = scrapeItem.TryGetValue("images", out var imagesList) ? imagesList : new List<string>();

                // Clean the content using Ollama
                var cleanedData = await CleanContentAsync(
                    userMessage, 
                    ragContext,
                    title,
                    layout, 
                    headers, 
                    contentSections, 
                    images,
                    onProgress,
                    progressPercent);

                if (cleanedData != null)
                {
                    // Add cleaned content to data list
                    cleanedData["sourceUrl"] = new List<string> { url };
                    cleanedData["sourceTitle"] = new List<string> { title };
                    data.Add(cleanedData);
                }
            }

            onProgress(95, "Content cleaning completed");

            // Count cleaned items
            var cleanedCount = data.FindAll(item => 
                item.TryGetValue("type", out var typeList) && 
                typeList.Count > 0 && 
                typeList[0] == "content"
            ).Count;

            onProgress(100, $"Successfully cleaned {cleanedCount} web page(s)");
            onComplete($"Cleaned {cleanedCount} web page(s) for relevant content");
        }
        catch (Exception ex)
        {
            onError($"Error cleaning web content: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clean content by asking Ollama about relevance of each section
    /// </summary>
    private async Task<Dictionary<string, List<string>>?> CleanContentAsync(
        string userMessage,
        string ragContext,
        string pageTitle,
        List<string> layout,
        List<string> headers,
        List<string> contentSections,
        List<string> images,
        OnProgress onProgress,
        int baseProgress)
    {
        try
        {
            var cleanedLayout = new List<string>();
            var cleanedHeaders = new List<string>();
            var cleanedContent = new List<string>();
            var cleanedImages = new List<string>();

            string? lastRelevantHeader = null;
            int relevantHeaderIndex = -1;

            // Process each item in the layout
            for (int i = 0; i < layout.Count; i++)
            {
                var layoutItem = layout[i];
                var parts = layoutItem.Split(':');
                if (parts.Length != 2) continue;

                var itemType = parts[0];
                if (!int.TryParse(parts[1], out var itemIndex)) continue;

                // Calculate progress within this page
                var itemProgress = baseProgress + (int)((i / (double)layout.Count) * 10);

                if (itemType == "header" && itemIndex < headers.Count)
                {
                    var header = headers[itemIndex];
                    onProgress(itemProgress, $"Evaluating header: {header.Substring(0, Math.Min(50, header.Length))}...");

                    // Ask Ollama if this header is relevant
                    var isRelevant = await IsContentRelevantAsync(userMessage, ragContext, pageTitle, header, lastRelevantHeader);
                    
                    if (isRelevant)
                    {
                        cleanedHeaders.Add(header);
                        cleanedLayout.Add($"header:{cleanedHeaders.Count - 1}");
                        lastRelevantHeader = header;
                        relevantHeaderIndex = cleanedHeaders.Count - 1;
                    }
                }
                else if (itemType == "content" && itemIndex < contentSections.Count)
                {
                    var content = contentSections[itemIndex];
                    onProgress(itemProgress, $"Evaluating content section {itemIndex + 1}...");

                    // Ask Ollama if this content is relevant
                    var isRelevant = await IsContentRelevantAsync(userMessage, ragContext, pageTitle, content, lastRelevantHeader);
                    
                    if (isRelevant)
                    {
                        cleanedContent.Add(content);
                        cleanedLayout.Add($"content:{cleanedContent.Count - 1}");
                    }
                }
                else if (itemType == "image" && itemIndex < images.Count)
                {
                    var image = images[itemIndex];
                    
                    // Include image if we have a relevant header or content
                    // Images are associated with the last relevant section
                    if (lastRelevantHeader != null || cleanedContent.Count > 0)
                    {
                        cleanedImages.Add(image);
                        cleanedLayout.Add($"image:{cleanedImages.Count - 1}");
                    }
                }
            }

            // If we found relevant content, create the cleaned data dictionary
            if (cleanedHeaders.Count > 0 || cleanedContent.Count > 0)
            {
                var cleanedData = new Dictionary<string, List<string>>
                {
                    ["type"] = new List<string> { "content" }
                };

                if (cleanedHeaders.Count > 0)
                    cleanedData["headers"] = cleanedHeaders;

                if (cleanedContent.Count > 0)
                    cleanedData["content"] = cleanedContent;

                if (cleanedImages.Count > 0)
                    cleanedData["images"] = cleanedImages;

                if (cleanedLayout.Count > 0)
                    cleanedData["layout"] = cleanedLayout;

                return cleanedData;
            }

            return null;
        }
        catch (Exception ex)
        {
            // Return null if cleaning fails
            return null;
        }
    }

    /// <summary>
    /// Ask Ollama if content is relevant to the user's question
    /// </summary>
    private async Task<bool> IsContentRelevantAsync(
        string userMessage, 
        string ragContext, 
        string pageTitle,
        string content, 
        string? previousHeader)
    {
        try
        {
            var systemPrompt = @"You are a content relevance evaluator. Your task is to determine if a piece of content is relevant to the user's question.

Rules:
1. Return ONLY a JSON object with a single boolean field: { ""relevant"": true } or { ""relevant"": false }
2. Content is relevant if it directly answers the user's question or provides important context
3. Content is relevant if it's related to the user's topic of interest
4. Headers provide context for subsequent content sections
5. Be inclusive - if the content might be useful, consider it relevant
6. Return ONLY the JSON, no other text";

            var contextInfo = string.IsNullOrEmpty(previousHeader) 
                ? "" 
                : $"\n\nPrevious section header for context: {previousHeader}";
            
            var pageContext = string.IsNullOrEmpty(pageTitle)
                ? ""
                : $"\n\nWeb page title: {pageTitle}";

            var userPrompt = $"User's question: {userMessage}\n\nPrevious conversation context:\n{ragContext}{pageContext}{contextInfo}\n\nContent to evaluate:\n{content.Substring(0, Math.Min(500, content.Length))}\n\nIs this content relevant to the user's question? Return ONLY JSON: {{\"relevant\": true}} or {{\"relevant\": false}}";

            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _ollamaModel,
                System = systemPrompt,
                Prompt = userPrompt,
                Stream = false,
                Format = "json"
            };

            var responseContent = "";
            await foreach (var response in _ollama.GenerateAsync(request))
            {
                if (response?.Response != null)
                {
                    responseContent += response.Response;
                }
            }

            // Parse the JSON response
            responseContent = responseContent.Trim();
            
            // Try to find and extract JSON object
            var jsonStart = responseContent.IndexOf('{');
            var jsonEnd = responseContent.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                responseContent = responseContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var result = JsonSerializer.Deserialize<RelevanceResponse>(responseContent);
            return result?.Relevant ?? false;
        }
        catch
        {
            // Default to true if evaluation fails
            return true;
        }
    }

    private class RelevanceResponse
    {
        [JsonPropertyName("relevant")]
        public bool Relevant { get; set; }
    }
}
