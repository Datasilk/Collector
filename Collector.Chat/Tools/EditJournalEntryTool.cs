using System.Text.Json;
using System.Text.Encodings.Web;
using Collector.Chat.Models;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Common.Models.JournalModules;

namespace Collector.Chat.Tools;

/// <summary>
/// Tool for editing an existing journal entry and generating content
/// </summary>
public class EditJournalEntryTool : IChatTool
{
    private readonly IJournalEntriesRepository _entriesRepository;

    public EditJournalEntryTool(IJournalEntriesRepository entriesRepository)
    {
        _entriesRepository = entriesRepository;
    }

    public string ToolKey => "edit-journal-entry";

    public string Description => "Edit an existing journal entry and generate content using AI. Must be called AFTER new-journal-entry has created an entry, as it requires the entryId from the data dictionary.";

    public bool AvailableToOllama => true;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "content",
            DataType = "string",
            Description = "The content to add to the journal entry (optional, AI will generate if not provided)"
        },
        new ToolParameter
        {
            Key = "updateTitle",
            DataType = "boolean",
            Description = "Whether to update the title with AI-generated title"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField
        {
            Key = "success",
            DataType = "boolean"
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
            onProgress(10, "Preparing to edit journal entry...");

            // Get entryId from data dictionary (must have been set by new-journal-entry)
            if (!data.TryGetValue("entryId", out var entryIdStr) || !Guid.TryParse(entryIdStr, out var entryId))
            {
                onError("No entryId found in data. Make sure to run new-journal-entry before edit-journal-entry.", null);
                return;
            }

            onProgress(30, $"Loading journal entry {entryId}...");

            var entry = _entriesRepository.GetById(entryId);
            if (entry == null)
            {
                onError($"Journal entry with ID {entryId} not found.", null);
                return;
            }

            onProgress(50, "Preparing content modules...");

            var modules = new List<object>();
            var random = new Random();

            // Generate random module ID
            string GenerateRandomId() => random.Next(1, 10000000).ToString();

            // Add content from scraped data if available
            if (data.TryGetValue("scraped_content", out var scrapedContent) && !string.IsNullOrWhiteSpace(scrapedContent))
            {
                // Encode and format the content
                var encodedContent = System.Net.WebUtility.HtmlEncode(scrapedContent);
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
            else if (data.TryGetValue("content", out var providedContent) && !string.IsNullOrWhiteSpace(providedContent))
            {
                var encodedContent = System.Net.WebUtility.HtmlEncode(providedContent);
                modules.Add(new
                {
                    id = GenerateRandomId(),
                    type = "text-editor",
                    html = $"<p>{encodedContent}</p>",
                    manuallyAdded = false
                });
            }

            onProgress(70, "Saving entry content...");

            // Build entry content JSON and save it
            var entryContent = new
            {
                modules = modules.Count > 0 ? modules : null
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var contentJson = JsonSerializer.Serialize(entryContent, jsonOptions);
            var filePath = $"{entryId:N}.json";
            var saveSuccess = Files.SaveFile(Files.Paths.Journal, filePath, contentJson);

            if (!saveSuccess)
            {
                onError("Failed to save journal entry content.", null);
                return;
            }

            onProgress(90, "Updating entry metadata...");

            // Update the entry's modified timestamp
            _entriesRepository.UpdateLastModified(entryId);

            onProgress(100, "Journal entry updated successfully");

            onComplete($"Updated journal entry '{entry.Title}' with {modules.Count} content module(s).");
        }
        catch (Exception ex)
        {
            onError($"Failed to edit journal entry: {ex.Message}", ex);
        }
    }
}
