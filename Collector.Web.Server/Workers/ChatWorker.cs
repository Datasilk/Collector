using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Web.Server.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace Collector.Web.Server.Workers
{
    public class ChatWorker : IWorker
    {
        private readonly IChatsRepository _chatsRepo;
        private readonly IOllamaModelsRepository _ollamaModelsRepo;
        private readonly ILogger<ChatWorker> _logger;
        private readonly IHubContext<WorkerHub> _hubContext;
        private readonly OllamaApiClient _ollama;

        public ChatWorker(IChatsRepository chatsRepo, IOllamaModelsRepository ollamaModelsRepo, ILogger<ChatWorker> logger, IHubContext<WorkerHub> hubContext, OllamaApiClient ollama)
        {
            _chatsRepo = chatsRepo;
            _ollamaModelsRepo = ollamaModelsRepo;
            _logger = logger;
            _hubContext = hubContext;
            _ollama = ollama;
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task Progress(string appUserId, Guid workerId)
        {
            // No progress to report for chat
            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a message to Ollama and processes the response
        /// </summary>
        public async Task SendMessage(string appUserId, Guid workerId, Guid? chatId, string message, string model, string mode, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("ChatWorker SendMessage: chatId={ChatId}, model={Model}, mode={Mode}", chatId, model, mode);

                Guid _chatId = chatId ?? Guid.Empty;
                bool isNewChat = chatId == null || chatId == Guid.Empty;

                // If chat exists, verify ownership
                if (!isNewChat)
                {
                    var existingChat = _chatsRepo.GetById(_chatId);
                    if (existingChat == null || existingChat.AppUserId.ToString() != appUserId)
                    {
                        await SendWorkerMessage(appUserId, workerId, "ChatError", new { message = "Chat not found or unauthorized" }, cancellationToken);
                        return;
                    }
                }

                // Build system prompt for Ollama to return JSON
                string systemPrompt = isNewChat 
                    ? @"You are a helpful AI assistant. You must respond ONLY with valid JSON in this exact format:
{
  ""title"": ""a short 3-5 word title for this conversation"",
  ""message"": ""your response to the user"",
  ""action"": ""none or new-entry""
}

Rules:
- 'title' should be a brief, descriptive title (3-5 words) that summarizes the conversation topic
- 'action' should be 'none' for normal conversation
- 'action' should be 'new-entry' ONLY if the user explicitly asks you to create a new journal entry
- Always provide a helpful 'message' field with your response
- Do not include any text outside the JSON object"
                    : @"You are a helpful AI assistant. You must respond ONLY with valid JSON in this exact format:
{
  ""message"": ""your response to the user"",
  ""action"": ""none or new-entry""
}

Rules:
- 'action' should be 'none' for normal conversation
- 'action' should be 'new-entry' ONLY if the user explicitly asks you to create a new journal entry
- Always provide a helpful 'message' field with your response
- Do not include any text outside the JSON object";

                // Get RAG context using vector similarity search
                string contextPrompt = "";
                float[]? messageEmbedding = null;
                try
                {
                    // Generate embedding for user message
                    messageEmbedding = await GetEmbeddingAsync(new List<string>() { message });
                    
                    // Retrieve relevant context from user's conversation history
                    var relevantContext = _chatsRepo.GetContext(Guid.Parse(appUserId), messageEmbedding, topK: 5);
                    
                    if (relevantContext.Count > 0)
                    {
                        var contextParts = relevantContext.Select(c => 
                            $"[Relevant Context - Distance: {c.Distance:F3}]\n{c.Content}");
                        contextPrompt = "\n\nRelevant context from previous conversations:\n" + 
                                       string.Join("\n\n", contextParts) + "\n";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to retrieve RAG context for user {AppUserId}", appUserId);
                }

                // Get active model from database, fallback to config
                string activeModel = LLMOllama.Model;
                try
                {
                    var dbModel = _ollamaModelsRepo.GetActive();
                    if (dbModel != null)
                    {
                        activeModel = dbModel.Id;
                        _logger.LogInformation("ChatWorker: Using active model from database: {Model}", activeModel);
                    }
                    else
                    {
                        _logger.LogInformation("ChatWorker: No active model in database, using config fallback: {Model}", activeModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to get active model from database, using config fallback: {Model}", activeModel);
                }

                // Build the full prompt with system and user messages
                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = activeModel,
                    System = systemPrompt + contextPrompt,
                    Prompt = message,
                    Stream = false
                };

                var responseContent = "";
                await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
                {
                    if (response?.Response != null)
                    {
                        responseContent += response.Response;
                    }
                }

                // Parse JSON response
                OllamaResponse parsedResponse;
                try
                {
                    parsedResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, treat entire response as message
                    parsedResponse = new OllamaResponse
                    {
                        Message = responseContent,
                        Action = "none"
                    };
                }

                // Validate response content
                if (string.IsNullOrWhiteSpace(parsedResponse?.Message))
                {
                    await SendWorkerMessage(appUserId, workerId, "ChatError", new 
                    { 
                        message = "AI response was empty. Please try again or check if Ollama is running properly."
                    }, cancellationToken);
                    _logger.LogWarning("ChatWorker: Empty response from Ollama for chatId={ChatId}", chatId);
                    return;
                }

                // Create chat if needed
                if (isNewChat)
                {
                    var chatTitle = !string.IsNullOrEmpty(parsedResponse.Title) 
                        ? parsedResponse.Title 
                        : (message.Length > 50 ? message.Substring(0, 50) + "..." : message);
                    
                    var newChat = new Collector.Data.Entities.Chat
                    {
                        AppUserId = Guid.Parse(appUserId),
                        Title = chatTitle,
                        Status = 1
                    };
                    _chatId = _chatsRepo.Add(newChat);

                    // Send new chat ID to frontend
                    await SendWorkerMessage(appUserId, workerId, "ChatCreated", new 
                    { 
                        id = _chatId,
                        title = newChat.Title
                    }, cancellationToken);

                    _logger.LogInformation("ChatWorker: Created new chat {ChatId} with title '{Title}'", _chatId, chatTitle);
                }

                // Save user message to history
                var userMessage = new ChatHistory
                {
                    ChatId = _chatId,
                    Role = 0, // 0 = user
                    Content = message,
                    Model = model,
                    Status = 1
                };
                var userMessageId = _chatsRepo.AddMessage(userMessage);

                // Send user message confirmation
                await SendWorkerMessage(appUserId, workerId, "MessageSaved", new 
                { 
                    id = userMessageId,
                    role = 0,
                    content = message,
                    model = model
                }, cancellationToken);

                // Save assistant response to history
                var assistantMessage = new ChatHistory
                {
                    ChatId = _chatId,
                    Role = 1, // 1 = assistant
                    Content = parsedResponse.Message,
                    Model = LLMOllama.Model,
                    Status = 1
                };
                var assistantMessageId = _chatsRepo.AddMessage(assistantMessage);

                // Update chat modified time
                _chatsRepo.UpdateModified(_chatId);

                // Send response to client
                await SendWorkerMessage(appUserId, workerId, "ChatResponse", new 
                { 
                    id = assistantMessageId,
                    role = 1,
                    content = parsedResponse.Message,
                    model = LLMOllama.Model,
                    action = parsedResponse.Action
                }, cancellationToken);

                // Extract and store facts from conversation for future RAG retrieval
                try
                {
                    var userGuid = Guid.Parse(appUserId);
                    var facts = await ExtractFactsFromConversationAsync(message, parsedResponse.Message, cancellationToken);
                    
                    if (facts != null && facts.Count > 0)
                    {
                        int storedCount = 0;
                        int duplicateCount = 0;
                        
                        foreach (var fact in facts)
                        {
                            // Generate embedding for the fact
                            var factEmbedding = await GetEmbeddingAsync(new List<string>() { fact });
                            
                            // Check if similar fact already exists
                            if (!_chatsRepo.HasSimilarContext(userGuid, factEmbedding, similarityThreshold: 0.1f))
                            {
                                var factMetadata = JsonSerializer.Serialize(new
                                {
                                    type = "fact",
                                    timestamp = DateTime.UtcNow,
                                    chatId = _chatId,
                                    userMessageId = userMessageId,
                                    assistantMessageId = assistantMessageId
                                });
                                
                                _chatsRepo.StoreContext(userGuid, _chatId, fact, factEmbedding, factMetadata);
                                storedCount++;
                            }
                            else
                            {
                                duplicateCount++;
                            }
                        }
                        
                        _logger.LogInformation("ChatWorker: Extracted {TotalFacts} facts, stored {StoredCount} new facts, skipped {DuplicateCount} duplicates for user {AppUserId}", 
                            facts.Count, storedCount, duplicateCount, appUserId);
                    }
                    else
                    {
                        _logger.LogInformation("ChatWorker: No facts extracted from conversation for user {AppUserId}", appUserId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to extract and store facts for user {AppUserId}", appUserId);
                }

                // Handle actions
                if (parsedResponse.Action == "new-entry")
                {
                    // TODO: Implement journal entry creation
                    _logger.LogInformation("ChatWorker: Action 'new-entry' requested but not yet implemented");
                }

                _logger.LogInformation("ChatWorker SendMessage completed for chatId={ChatId}, action={Action}", _chatId, parsedResponse.Action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatWorker SendMessage error");
                await SendWorkerMessage(appUserId, workerId, "ChatError", new { message = ex.Message }, CancellationToken.None);
            }
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }

        private async Task<float[]> GetEmbeddingAsync(List<string> text)
        {
            var response = await _ollama.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = text
            });
            return response.Embeddings[0];
        }

        private async Task<List<string>> ExtractFactsFromConversationAsync(string userMessage, string assistantMessage, CancellationToken cancellationToken)
        {
            try
            {
                var systemPrompt = @"You are a fact extraction system. Extract key facts from conversations and return them as a JSON array of strings.

Rules:
- Extract only factual information, preferences, or important details
- Each fact should be a complete, standalone statement
- Ignore greetings and conversational filler
- Return ONLY a JSON array of strings

Example output:
[""The user prefers dark mode"", ""The project uses PostgreSQL database"", ""The user is working on a chat feature""]";

                var conversationPrompt = $@"User said: {userMessage}
Assistant replied: {assistantMessage}

Extract facts from this conversation:";

                // Get active model from database, fallback to config
                string activeModel = LLMOllama.Model;
                try
                {
                    var dbModel = _ollamaModelsRepo.GetActive();
                    if (dbModel != null)
                    {
                        activeModel = dbModel.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to get active model for fact extraction, using config fallback");
                }

                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = activeModel,
                    System = systemPrompt,
                    Prompt = conversationPrompt,
                    Stream = false,
                    Format = "json"
                };

                var responseContent = "";
                await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
                {
                    if (response?.Response != null)
                    {
                        responseContent += response.Response;
                    }
                }

                // Clean up response - sometimes Ollama adds extra text
                responseContent = responseContent.Trim();
                
                // Find JSON array in response
                var startIndex = responseContent.IndexOf('[');
                var endIndex = responseContent.LastIndexOf(']');
                
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    responseContent = responseContent.Substring(startIndex, endIndex - startIndex + 1);
                }

                // Parse JSON array of facts
                var facts = JsonSerializer.Deserialize<List<string>>(responseContent);
                return facts ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to extract facts from conversation");
                return new List<string>();
            }
        }

        private class OllamaResponse
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            
            [JsonPropertyName("message")]
            public string Message { get; set; }
            
            [JsonPropertyName("action")]
            public string Action { get; set; }
        }
    }
}
