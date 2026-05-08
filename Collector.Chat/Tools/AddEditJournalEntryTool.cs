using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using OllamaSharp;
using Collector.Chat.Models;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Common.Models.JournalModules;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for adding or editing journal entries. Uses vector similarity to find existing entries
/// with similar titles. If found, updates the existing entry. Otherwise creates a new entry
/// in the journal that Ollama determines is most appropriate.
/// </summary>
public class AddEditJournalEntryTool : IChatTool
{
    private readonly IJournalEntriesRepository _entriesRepository;
    private readonly IJournalsRepository _journalsRepository;
    private readonly IJournalCategoriesRepository _categoriesRepository;
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;

    public AddEditJournalEntryTool(
        IJournalEntriesRepository entriesRepository,
        IJournalsRepository journalsRepository,
        IJournalCategoriesRepository categoriesRepository,
        OllamaApiClient ollama,
        string ollamaModel = "llama3.2")
    {
        _entriesRepository = entriesRepository;
        _journalsRepository = journalsRepository;
        _categoriesRepository = categoriesRepository;
        _ollama = ollama;
        _ollamaModel = ollamaModel;
    }

    public string ToolKey => "add-edit-journal-entry";

    public string Description => "Add or edit a journal entry. Searches for existing entries with similar titles (updates if found within 0.3 similarity), otherwise creates new entry in the best-matching journal.";

    public bool AvailableToOllama => false;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "title",
            DataType = "string",
            Description = "The title for the journal entry (used for similarity matching)"
        },
        new ToolParameter
        {
            Key = "journalId",
            DataType = "int",
            Description = "The ID of the journal to create the entry in (optional, Ollama will select best journal if not provided)"
        },
        new ToolParameter
        {
            Key = "description",
            DataType = "string",
            Description = "Brief description/summary of the entry content"
        },
        new ToolParameter
        {
            Key = "content",
            DataType = "string",
            Description = "The content to add to the journal entry (optional, uses scraped_content from data if available)"
        },
        new ToolParameter
        {
            Key = "updateTitle",
            DataType = "boolean",
            Description = "Whether to update the title when editing an existing entry"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "entryId",
            DataType = "guid"
        },
        new ToolResponseField
        {
            Key = "isNew",
            DataType = "boolean"
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
            onProgress(10, "Preparing to add/edit journal entry...");

            // Get parameters from plan args
            var args = plan?.Steps.Count > currentStepIndex && currentStepIndex >= 0
                ? plan.Steps[currentStepIndex].Args
                : new Dictionary<string, object>();

            var title = args?.GetValueOrDefault("title", "")?.ToString() ?? "";
            var description = args?.GetValueOrDefault("description", "")?.ToString() ?? "";
            var updateTitle = (args?.GetValueOrDefault("updateTitle", "false")?.ToString() ?? "false").ToLower() == "true";

            // Get cleaned content for title/description generation
            var cleanedContent = GetCleanedContent(data);
            var sourceUrl = GetCleanedSourceUrl(data);

            // Generate title and description via Ollama if not provided
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                onProgress(15, "Generating title and description from content...");
                var (generatedTitle, generatedDescription) = await GenerateTitleAndDescriptionAsync(
                    userMessage, 
                    cleanedContent, 
                    sourceUrl,
                    title,
                    description);
                
                if (string.IsNullOrWhiteSpace(title))
                    title = generatedTitle;
                if (string.IsNullOrWhiteSpace(description))
                    description = generatedDescription;
            }

            // Get or determine journal ID from args (if provided)
            int journalId = 0;
            var journalIdStr = args?.GetValueOrDefault("journalId", "")?.ToString();
            if (int.TryParse(journalIdStr, out var parsedId))
            {
                journalId = parsedId;
            }

            // Generate embedding for title similarity search
            onProgress(20, "Generating embedding for title similarity search...");
            var titleEmbedding = await GetEmbeddingAsync(title);

            // If journalId not provided, search all journals for similar entries
            // If journalId provided, search only that journal
            onProgress(30, "Searching for existing entries with similar titles...");
            var similarEntry = await FindSimilarEntryAsync(appUserId, journalId, titleEmbedding, onProgress, onError);

            if (similarEntry.HasValue && similarEntry.Value.EntryId.HasValue)
            {
                // Found similar entry - update it
                await UpdateExistingEntryAsync(
                    similarEntry.Value.EntryId.Value,
                    similarEntry.Value.Title,
                    title,
                    description,
                    updateTitle,
                    data,
                    titleEmbedding,
                    onProgress,
                    onError,
                    onComplete,
                    sendMessage);
                
                // Store entryId in plan for subsequent steps
                StoreEntryIdInPlan(plan, currentStepIndex, similarEntry.Value.EntryId.Value);
                return;
            }

            // No similar entry found - need to determine journal and create new entry
            if (journalId == 0)
            {
                onProgress(40, "No similar entry found. Selecting best journal...");
                journalId = await SelectJournalWithOllamaAsync(appUserId, userMessage, ragContext, onProgress, onError, onRawRequest, onRawResponse);
                if (journalId == 0)
                {
                    return;
                }
            }

            // Create new journal entry
            await CreateNewEntryAsync(
                journalId,
                title,
                description,
                data,
                titleEmbedding,
                onProgress,
                onError,
                onComplete,
                sendMessage,
                plan,
                currentStepIndex);
        }
        catch (Exception ex)
        {
            onError($"Failed to add/edit journal entry: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Find an existing entry with similar title within the specified journal or across all user's journals
    /// </summary>
    private async Task<(Guid? EntryId, string Title, float Distance)?> FindSimilarEntryAsync(
        Guid appUserId, 
        int journalId, 
        float[] titleEmbedding,
        OnProgress onProgress,
        OnError onError)
    {
        try
        {
            if (journalId > 0)
            {
                // Search within specific journal
                var result = _entriesRepository.FindSimilarByTitle(journalId, titleEmbedding, maxDistance: 0.3f);
                return result;
            }
            else
            {
                // Search across all user's journals
                var journals = _journalsRepository.GetAllByUserId(appUserId);
                foreach (var journal in journals)
                {
                    var result = _entriesRepository.FindSimilarByTitle(journal.Id, titleEmbedding, maxDistance: 0.3f);
                    if (result.HasValue)
                    {
                        return result;
                    }
                }
                return null;
            }
        }
        catch (Exception ex)
        {
            onError($"Error searching for similar entries: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Update an existing journal entry with new content
    /// </summary>
    private async Task UpdateExistingEntryAsync(
        Guid entryId,
        string existingTitle,
        string newTitle,
        string description,
        bool updateTitle,
        List<Dictionary<string, List<string>>> data,
        float[] titleEmbedding,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        SendMessage? sendMessage)
    {
        onProgress(50, $"Found similar entry '{existingTitle}'. Updating...");

        var entry = _entriesRepository.GetById(entryId);
        if (entry == null)
        {
            onError($"Journal entry with ID {entryId} not found.", null);
            return;
        }

        // Update title if requested
        if (updateTitle && !string.IsNullOrWhiteSpace(newTitle) && newTitle != existingTitle)
        {
            entry.Title = newTitle;
            // Update embedding for new title
            _entriesRepository.UpdateEmbedding(entryId, titleEmbedding);
        }

        // Update description if provided
        if (!string.IsNullOrWhiteSpace(description))
        {
            entry.Description = description;
            _entriesRepository.Modify(entryId);
        }

        onProgress(60, "Preparing content modules...");

        var modules = await BuildContentModulesAsync(data, entryId);

        onProgress(80, "Saving entry content...");

        // Append new modules to existing content if any
        var filePath = $"{entryId:N}.json";
        var existingContent = Files.GetFile(Files.Paths.Journal, filePath);
        
        List<object> allModules = new();
        if (!string.IsNullOrEmpty(existingContent))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<JournalContent>(existingContent);
                if (existing?.modules != null)
                {
                    allModules.AddRange(existing.modules);
                }
            }
            catch { /* ignore deserialization errors */ }
        }
        allModules.AddRange(modules);

        var entryContent = new JournalContent
        {
            id = entryId.ToString(),
            modules = allModules.ToArray()
        };

        var contentJson = JsonSerializer.Serialize(entryContent);
        Files.SaveFile(Files.Paths.Journal, filePath, contentJson);

        onProgress(90, "Updating entry metadata...");
        _entriesRepository.UpdateLastModified(entryId);

        onProgress(100, "Journal entry updated successfully");

        var finalTitle = updateTitle ? newTitle : existingTitle;
        onComplete($"Updated journal entry '{finalTitle}' with {modules.Count} new content module(s).");

        sendMessage?.Invoke($"Journal entry '{finalTitle}' has been updated.", $"/journal/{entry.JournalId}/entry/{entryId}", "View Entry");
    }

    /// <summary>
    /// Create a new journal entry
    /// </summary>
    private async Task CreateNewEntryAsync(
        int journalId,
        string title,
        string description,
        List<Dictionary<string, List<string>>> data,
        float[] titleEmbedding,
        OnProgress onProgress,
        OnError onError,
        OnComplete onComplete,
        SendMessage? sendMessage,
        ExecutionPlan? plan,
        int currentStepIndex)
    {
        onProgress(50, $"Creating new entry in journal {journalId}...");

        // Get URL from cleaned content if available
        var sourceUrl = GetCleanedSourceUrl(data);

        // Create the journal entry
        var entry = new JournalEntry
        {
            JournalId = journalId,
            Title = title,
            Description = description,
            Url = sourceUrl ?? ""
        };

        onProgress(60, "Saving entry to database...");
        var entryId = _entriesRepository.Add(entry);

        onProgress(70, "Generating embedding for title...");
        // Embedding is set during entry creation or updated separately
        _entriesRepository.UpdateEmbedding(entryId, titleEmbedding);

        onProgress(80, "Building content modules...");
        var modules = await BuildContentModulesAsync(data, entryId);

        onProgress(90, "Saving entry content to file...");
        var filePath = $"{entryId:N}.json";
        var content = new JournalContent
        {
            id = entryId.ToString(),
            modules = modules.ToArray()
        };
        var json = JsonSerializer.Serialize(content);
        Files.SaveFile(Files.Paths.Journal, filePath, json);

        onProgress(100, "Entry created successfully");

        var finalTitle = entry.Title;
        onComplete($"Created new journal entry '{finalTitle}' with {modules.Count} content module(s).");

        sendMessage?.Invoke($"Journal entry '{finalTitle}' has been created.", $"/journal/{journalId}/entry/{entryId}", "View Entry");

        // Store entryId in plan for subsequent steps
        StoreEntryIdInPlan(plan, currentStepIndex, entryId);
    }

    /// <summary>
    /// Build content modules from cleaned content for journal entry, following the layout order
    /// </summary>
    private async Task<List<object>> BuildContentModulesAsync(List<Dictionary<string, List<string>>> data, Guid entryId)
    {
        var modules = new List<object>();
        var random = new Random();
        string GenerateRandomId() => random.Next(1, 10000000).ToString();

        // Process each cleaned content item
        foreach (var item in data)
        {
            if (!item.TryGetValue("type", out var typeList) || !typeList.Contains("content"))
                continue;

            // Get layout, headers, content sections, and images
            var layout = item.TryGetValue("layout", out var layoutList) ? layoutList : new List<string>();
            var headers = item.TryGetValue("headers", out var headersList) ? headersList : new List<string>();
            var contentSections = item.TryGetValue("content", out var contentList) ? contentList : new List<string>();
            var images = item.TryGetValue("images", out var imagesList) ? imagesList : new List<string>();

            // Process each item in the layout order
            foreach (var layoutItem in layout)
            {
                var parts = layoutItem.Split(':');
                if (parts.Length != 2) continue;

                var itemType = parts[0];
                if (!int.TryParse(parts[1], out var itemIndex)) continue;

                if (itemType == "header" && itemIndex < headers.Count)
                {
                    var header = headers[itemIndex];
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        modules.Add(new
                        {
                            id = GenerateRandomId(),
                            type = "title",
                            text = header,
                            level = 2,
                            manuallyAdded = false
                        });
                    }
                }
                else if (itemType == "content" && itemIndex < contentSections.Count)
                {
                    var content = contentSections[itemIndex];
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    var encodedContent = System.Net.WebUtility.HtmlEncode(content);
                    var paragraphs = encodedContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var html = string.Join("", paragraphs.Select(p => $"<p>{p.Replace("\n", "<br/>")}</p>"));

                    modules.Add(new
                    {
                        id = GenerateRandomId(),
                        type = "text-editor",
                        html,
                        manuallyAdded = false
                    });
                }
                else if (itemType == "image" && itemIndex < images.Count)
                {
                    var imageUrl = images[itemIndex];
                    if (string.IsNullOrWhiteSpace(imageUrl))
                        continue;

                    modules.Add(new
                    {
                        id = GenerateRandomId(),
                        type = "image",
                        url = imageUrl,
                        caption = "",
                        manuallyAdded = false
                    });
                }
            }
        }

        return modules;
    }

    /// <summary>
    /// Extract content from cleaned content items in the data list (type='content')
    /// </summary>
    private string GetCleanedContent(List<Dictionary<string, List<string>>> data)
    {
        var allContent = new List<string>();
        
        foreach (var item in data)
        {
            // Check if this is a cleaned content item
            if (item.TryGetValue("type", out var typeList) && typeList.Contains("content"))
            {
                // Get headers
                if (item.TryGetValue("headers", out var headersList))
                {
                    allContent.AddRange(headersList);
                }
                // Get content sections
                if (item.TryGetValue("content", out var contentList))
                {
                    allContent.AddRange(contentList);
                }
            }
        }
        
        return string.Join("\n\n", allContent);
    }

    /// <summary>
    /// Extract URL from cleaned content items in the data list
    /// </summary>
    private string? GetCleanedSourceUrl(List<Dictionary<string, List<string>>> data)
    {
        foreach (var item in data)
        {
            if (item.TryGetValue("type", out var typeList) && typeList.Contains("content"))
            {
                if (item.TryGetValue("sourceUrl", out var urlList) && urlList.Count > 0)
                {
                    return urlList[0];
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Store entry ID in plan for subsequent steps
    /// </summary>
    private void StoreEntryIdInPlan(ExecutionPlan? plan, int currentStepIndex, Guid entryId)
    {
        if (plan == null || currentStepIndex < 0 || currentStepIndex >= plan.Steps.Count)
            return;

        // Add entryId to the current step's result so subsequent steps can access it
        var currentStep = plan.Steps[currentStepIndex];
        if (currentStep.Result == null)
        {
            currentStep.Result = new Dictionary<string, object>();
        }
        currentStep.Result["entryId"] = entryId.ToString();
    }

    /// <summary>
    /// Generate embedding vector for a text string using Ollama
    /// </summary>
    private async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            var response = await _ollama.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = new List<string> { text }
            });
            return response.Embeddings[0];
        }
        catch
        {
            // Return empty embedding if generation fails
            return new float[768];
        }
    }

    /// <summary>
    /// Use Ollama to intelligently select the appropriate journal based on user context
    /// </summary>
    private async Task<int> SelectJournalWithOllamaAsync(
        Guid appUserId, 
        string userMessage, 
        string ragContext, 
        OnProgress onProgress, 
        OnError onError,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null)
    {
        try
        {
            // Step 1: Get all journal categories with their journals
            onProgress(55, "Analyzing available journal categories...");
            var categoriesWithJournals = _categoriesRepository.GetAllWithJournalsByUserId(appUserId);

            // Step 2: Ask Ollama to select or suggest a category
            onProgress(60, "Asking AI to select the best journal category...");
            var selectedCategoryId = await AskOllamaForCategoryAsync(categoriesWithJournals, userMessage, ragContext, onProgress, onError, appUserId, onRawRequest, onRawResponse);

            if (selectedCategoryId == 0)
            {
                onError("No suitable category found or created.", null);
                return 0;
            }

            onProgress(65, $"Selected category ID: {selectedCategoryId}");

            // Step 3: Get the selected category with its journals
            var selectedCategory = categoriesWithJournals.FirstOrDefault(c => c.Id == selectedCategoryId);
            if (selectedCategory == null)
            {
                onError($"Selected category (ID: {selectedCategoryId}) not found.", null);
                return 0;
            }

            // Step 4: Ask Ollama to select or suggest a journal within the category
            onProgress(70, "Asking AI to select the best journal...");
            var selectedJournalId = await AskOllamaForJournalAsync(selectedCategory, userMessage, ragContext, onProgress, onError, onRawRequest, onRawResponse);

            if (selectedJournalId == 0 && selectedCategory.Journals.Count > 0)
            {
                selectedJournalId = selectedCategory.Journals.First().Id;
            }
            
            onProgress(75, $"Selected journal ID: {selectedJournalId}");
            return selectedJournalId;
        }
        catch (Exception ex)
        {
            onError($"Failed to select journal with Ollama: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Ask Ollama to select the best journal category based on context
    /// </summary>
    private async Task<int> AskOllamaForCategoryAsync(
        List<JournalCategory> categories, 
        string userMessage, 
        string ragContext, 
        OnProgress onProgress, 
        OnError onError,
        Guid appUserId,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null)
    {
        try
        {
            var categoryList = categories.Count > 0 
                ? string.Join("\n", categories.Select(c => $"- ID {c.Id}: {c.Title}"))
                : "(No existing categories - you must create one)";

            var systemPrompt = @"You are a helpful assistant that selects or creates the most appropriate journal category for a user's content. 
Respond ONLY with a valid JSON object in this exact format:
{
  ""categoryId"": 123,
  ""reason"": ""brief explanation"",
  ""newCategoryTitle"": ""suggested title if creating new category (only if categoryId is 0)""
}

Rules:
- Select the category that best matches the topic and intent of the user's request
- Use the conversation context to understand what the user wants to accomplish
- If no existing categories OR none are appropriate, set categoryId to 0 and provide a newCategoryTitle
- The newCategoryTitle should be concise (1-3 words) and descriptive of the content type
- Respond with ONLY the JSON object, no additional text";

            var userPrompt = "Available Journal Categories:\n" +
                categoryList +
                "\n\nConversation Context:\n" +
                ragContext +
                "\n\nUser's Request:\n" +
                userMessage +
                "\n\nBased on the context and the user's intent, which category ID should be selected? If no categories exist or none are appropriate, return categoryId: 0 and provide a newCategoryTitle to create.";

            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _ollamaModel,
                System = systemPrompt,
                Prompt = userPrompt,
                Stream = false,
                Format = "json"
            };

            onRawRequest?.Invoke("category-selection", $"System:\n{systemPrompt}\n\nUser:\n{userPrompt}");

            var responseContent = "";
            await foreach (var resp in _ollama.GenerateAsync(request))
            {
                if (resp?.Response != null)
                {
                    responseContent += resp.Response;
                }
            }

            onRawResponse?.Invoke("category-selection", responseContent);

            var result = JsonSerializer.Deserialize<CategorySelectionResponse>(responseContent);
            
            if (result?.CategoryId > 0)
            {
                return result.CategoryId;
            }
            
            if (result?.CategoryId == 0 && !string.IsNullOrWhiteSpace(result.NewCategoryTitle))
            {
                onProgress(63, $"Creating new category: {result.NewCategoryTitle.Trim()}...");
                var newCategory = new JournalCategory
                {
                    AppUserId = appUserId,
                    Title = result.NewCategoryTitle.Trim(),
                    Color = "#4A90D9"
                };
                var newCategoryId = _categoriesRepository.Add(newCategory);
                return newCategoryId;
            }

            onError("Ollama failed to return a valid category selection.", null);
            return 0;
        }
        catch (Exception ex)
        {
            onError($"Error asking Ollama for category: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Ask Ollama to select the best journal within a category based on context
    /// </summary>
    private async Task<int> AskOllamaForJournalAsync(
        JournalCategory category, 
        string userMessage, 
        string ragContext, 
        OnProgress onProgress, 
        OnError onError,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null)
    {
        try
        {
            var journalList = category.Journals.Count > 0
                ? string.Join("\n", category.Journals.Select(j => $"- ID {j.Id}: {j.Title}"))
                : "(No existing journals in this category - you must create one)";

            var systemPrompt = @"You are a helpful assistant that selects or creates the most appropriate journal for a user's content. 
Respond ONLY with a valid JSON object in this exact format:
{
  ""journalId"": 123,
  ""reason"": ""brief explanation"",
  ""newJournalTitle"": ""suggested title if creating new journal (only if journalId is 0)""
}

Rules:
- Select the journal that best matches the topic and intent of the user's request
- Use the conversation context to understand what the user wants to accomplish
- If no existing journals OR none are appropriate, set journalId to 0 and provide a newJournalTitle
- The newJournalTitle should be concise (1-3 words) and descriptive of the content
- Respond with ONLY the JSON object, no additional text";

            var userPrompt = "Selected Category: " + category.Title +
                "\n\nAvailable Journals in this Category:\n" +
                journalList +
                "\n\nConversation Context:\n" +
                ragContext +
                "\n\nUser's Request:\n" +
                userMessage +
                "\n\nBased on the context and the user's intent, which journal ID should be selected? If no journals exist in this category or none are appropriate, return journalId: 0 and provide a newJournalTitle to create.";

            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = _ollamaModel,
                System = systemPrompt,
                Prompt = userPrompt,
                Stream = false,
                Format = "json"
            };

            onRawRequest?.Invoke("journal-selection", $"System:\n{systemPrompt}\n\nUser:\n{userPrompt}");

            var responseContent = "";
            await foreach (var resp in _ollama.GenerateAsync(request))
            {
                if (resp?.Response != null)
                {
                    responseContent += resp.Response;
                }
            }

            onRawResponse?.Invoke("journal-selection", responseContent);

            var result = JsonSerializer.Deserialize<JournalSelectionResponse>(responseContent);
            
            if (result?.JournalId > 0)
            {
                return result.JournalId;
            }
            
            if (result?.JournalId == 0 && !string.IsNullOrWhiteSpace(result.NewJournalTitle))
            {
                onProgress(73, $"Creating new journal: {result.NewJournalTitle.Trim()}...");
                var newJournal = new Journal
                {
                    AppUserId = category.AppUserId,
                    CategoryId = category.Id,
                    Title = result.NewJournalTitle.Trim(),
                    Color = "#4A90D9"
                };
                var newJournalId = _journalsRepository.Add(newJournal);
                return newJournalId;
            }

            onError("Ollama failed to return a valid journal selection.", null);
            return 0;
        }
        catch (Exception ex)
        {
            onError($"Error asking Ollama for journal: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Helper class for deserializing existing journal content
    /// </summary>
    private class JournalContent
    {
        public string? id { get; set; }
        public object[]? modules { get; set; }
    }

    private class CategorySelectionResponse
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("newCategoryTitle")]
        public string? NewCategoryTitle { get; set; }
    }

    /// <summary>
    /// Generate title and description using Ollama based on content and user context
    /// </summary>
    private async Task<(string Title, string Description)> GenerateTitleAndDescriptionAsync(
        string userMessage,
        string cleanedContent,
        string? sourceUrl,
        string existingTitle,
        string existingDescription)
    {
        try
        {
            var hasContent = !string.IsNullOrWhiteSpace(cleanedContent);
            var contentPreview = hasContent 
                ? string.Join(" ", cleanedContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Take(500))
                : "(No content available)";

            var systemPrompt = @"You are a helpful assistant that creates concise, descriptive titles and summaries for journal entries.

Rules:
1. Title should be 3-8 words, descriptive, and capture the main topic
2. Description should be 1-2 sentences summarizing the key points
3. Use the user's question and available content to understand the topic
4. Return ONLY a valid JSON object in this exact format:
{
  ""title"": ""The Generated Title"",
  ""description"": ""A brief description of the content.""
}
5. Do not include any other text, markdown, or explanation";

            var urlContext = !string.IsNullOrWhiteSpace(sourceUrl) 
                ? $"\n\nSource URL: {sourceUrl}" 
                : "";

            var userPrompt = $"User's request/question: {userMessage}{urlContext}\n\nAvailable content:\n{contentPreview}\n\n";

            if (!string.IsNullOrWhiteSpace(existingTitle))
            {
                userPrompt += $"Existing title (improve if needed): {existingTitle}\n";
            }

            if (!string.IsNullOrWhiteSpace(existingDescription))
            {
                userPrompt += $"Existing description (improve if needed): {existingDescription}\n";
            }

            userPrompt += "\nGenerate a title and description for this journal entry. Return ONLY JSON:";

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

            var result = JsonSerializer.Deserialize<TitleDescriptionResponse>(responseContent);
            
            var title = result?.Title?.Trim() ?? "New Entry";
            var description = result?.Description?.Trim() ?? "";

            // Ensure we have a valid title
            if (string.IsNullOrWhiteSpace(title) || title.Length < 3)
            {
                title = "New Entry";
            }

            // Limit title length
            if (title.Length > 100)
            {
                title = title.Substring(0, 97) + "...";
            }

            // Limit description length
            if (description.Length > 500)
            {
                description = description.Substring(0, 497) + "...";
            }

            return (title, description);
        }
        catch (Exception ex)
        {
            // Return defaults if generation fails
            return ("New Entry", "Journal entry created from web research.");
        }
    }

    private class TitleDescriptionResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class JournalSelectionResponse
    {
        [JsonPropertyName("journalId")]
        public int JournalId { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("newJournalTitle")]
        public string? NewJournalTitle { get; set; }
    }
}
