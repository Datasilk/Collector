using System.Text.Json;
using System.Text.Json.Serialization;
using Collector.Chat.Models;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using OllamaSharp;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for creating a new journal entry
/// </summary>
public class NewJournalEntryTool : IChatTool
{
    private readonly IJournalEntriesRepository _entriesRepository;
    private readonly IJournalsRepository _journalsRepository;
    private readonly IJournalCategoriesRepository _categoriesRepository;
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;

    public NewJournalEntryTool(
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

    public string ToolKey => "new-journal-entry";

    public string Description => "Create a new journal entry. This must be called BEFORE edit-journal-entry when creating and populating a new entry. The entryId returned will be available in the data dictionary for subsequent steps.";

    public bool AvailableToOllama => true;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "title",
            DataType = "string",
            Description = "The title for the journal entry"
        },
        new ToolParameter
        {
            Key = "journalId",
            DataType = "int",
            Description = "The ID of the journal to create the entry in (optional, defaults to first available journal)"
        },
        new ToolParameter
        {
            Key = "description",
            DataType = "string",
            Description = "Optional initial description for the entry"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "entryId",
            DataType = "guid"
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
            onProgress(10, "Preparing to create journal entry...");

            // Get parameters from data dictionary
            var title = data.GetValueOrDefault("title", "New Entry");
            var description = data.GetValueOrDefault("description", string.Empty);

            // Get user ID from data dictionary (should be set by ChatWorker)
            if (!data.TryGetValue("appUserId", out var appUserIdStr) || !Guid.TryParse(appUserIdStr, out var appUserId))
            {
                onError("User ID not available. Cannot determine which journal to use.", null);
                return;
            }

            // Determine journal ID
            int journalId = 0;
            if (data.TryGetValue("journalId", out var journalIdStr) && int.TryParse(journalIdStr, out var parsedId))
            {
                journalId = parsedId;
            }
            else
            {
                // Use Ollama to intelligently select the appropriate journal
                journalId = await SelectJournalWithOllamaAsync(appUserId, userMessage, ragContext, onProgress, onError, onRawRequest, onRawResponse);
                if (journalId == 0)
                {
                    return;
                }
            }

            onProgress(40, $"Creating entry in journal {journalId}...");

            // Create the journal entry
            var entry = new JournalEntry
            {
                JournalId = journalId,
                Title = title,
                Description = description,
                Url = data.GetValueOrDefault("scraped_url", string.Empty)
            };

            onProgress(70, "Saving journal entry to database...");

            var entryId = _entriesRepository.Add(entry);

            onProgress(90, "Storing entry ID for subsequent steps...");

            // Store the entryId in the data dictionary for other tools to use
            data["entryId"] = entryId.ToString();

            onProgress(100, "Journal entry created successfully");

            onComplete($"Created journal entry '{title}' with ID: {entryId}");

            // Send a message to chat with link to the journal entry
            sendMessage?.Invoke($"Journal entry '{title}' has been created.", $"/journal/{entryId}", "View Entry");
        }
        catch (Exception ex)
        {
            onError($"Failed to create journal entry: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Use Ollama to intelligently select the appropriate journal based on user context
    /// </summary>
    private async Task<int> SelectJournalWithOllamaAsync(Guid appUserId, string userMessage, string ragContext, OnProgress onProgress, OnError onError, OnRawRequest? onRawRequest = null, OnRawResponse? onRawResponse = null)
    {
        try
        {
            // Step 1: Get all journal categories with their journals
            onProgress(20, "Analyzing available journal categories...");
            var categoriesWithJournals = _categoriesRepository.GetAllWithJournalsByUserId(appUserId);

            // Step 2: Ask Ollama to select or suggest a category
            onProgress(30, "Asking AI to select the best journal category...");
            var selectedCategoryId = await AskOllamaForCategoryAsync(categoriesWithJournals, userMessage, ragContext, onProgress, onError, appUserId, onRawRequest, onRawResponse);

            if (selectedCategoryId == 0)
            {
                onError("No suitable category found or created.", null);
                return 0;
            }

            onProgress(50, $"Selected category ID: {selectedCategoryId}");

            // Step 3: Get the selected category with its journals
            var selectedCategory = categoriesWithJournals.FirstOrDefault(c => c.Id == selectedCategoryId);
            if (selectedCategory == null)
            {
                onError($"Selected category (ID: {selectedCategoryId}) not found.", null);
                return 0;
            }

            // Step 4: Ask Ollama to select or suggest a journal within the category
            onProgress(60, "Asking AI to select the best journal...");
            var selectedJournalId = await AskOllamaForJournalAsync(selectedCategory, userMessage, ragContext, onProgress, onError, onRawRequest, onRawResponse);

            if (selectedJournalId == 0)
            {
                return 0;
            }

            // If Ollama couldn't select a journal but we have journals available, use the first one
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
    private async Task<int> AskOllamaForCategoryAsync(List<JournalCategory> categories, string userMessage, string ragContext, OnProgress onProgress, OnError onError, Guid appUserId, OnRawRequest? onRawRequest = null, OnRawResponse? onRawResponse = null)
    {
        try
        {
            // Build category list for the prompt (handle empty list)
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

            // Report raw request for debugging
            onRawRequest?.Invoke("category-selection", $"System:\n{systemPrompt}\n\nUser:\n{userPrompt}");

            var responseContent = "";
            await foreach (var resp in _ollama.GenerateAsync(request))
            {
                if (resp?.Response != null)
                {
                    responseContent += resp.Response;
                }
            }

            // Report raw response for debugging
            onRawResponse?.Invoke("category-selection", responseContent);

            // Parse the response
            var result = JsonSerializer.Deserialize<CategorySelectionResponse>(responseContent);
            
            // If Ollama selected an existing category, return it
            if (result?.CategoryId > 0)
            {
                return result.CategoryId;
            }
            
            // If Ollama suggested creating a new category
            if (result?.CategoryId == 0 && !string.IsNullOrWhiteSpace(result.NewCategoryTitle))
            {
                onProgress(35, $"Creating new category: {result.NewCategoryTitle.Trim()}...");
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
    private async Task<int> AskOllamaForJournalAsync(JournalCategory category, string userMessage, string ragContext, OnProgress onProgress, OnError onError, OnRawRequest? onRawRequest = null, OnRawResponse? onRawResponse = null)
    {
        try
        {
            // Build journal list for the prompt (handle empty list)
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

            // Report raw request for debugging
            onRawRequest?.Invoke("journal-selection", $"System:\n{systemPrompt}\n\nUser:\n{userPrompt}");

            var responseContent = "";
            await foreach (var resp in _ollama.GenerateAsync(request))
            {
                if (resp?.Response != null)
                {
                    responseContent += resp.Response;
                }
            }

            // Report raw response for debugging
            onRawResponse?.Invoke("journal-selection", responseContent);

            // Parse the response
            var result = JsonSerializer.Deserialize<JournalSelectionResponse>(responseContent);
            
            // If Ollama selected an existing journal, return it
            if (result?.JournalId > 0)
            {
                return result.JournalId;
            }
            
            // If Ollama suggested creating a new journal
            if (result?.JournalId == 0 && !string.IsNullOrWhiteSpace(result.NewJournalTitle))
            {
                onProgress(65, $"Creating new journal: {result.NewJournalTitle.Trim()}...");
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

    private class CategorySelectionResponse
    {
        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("newCategoryTitle")]
        public string? NewCategoryTitle { get; set; }
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
