using System;

using System.Linq;

using System.Text;

using System.Text.Json;

using System.Text.Json.Serialization;

using System.Threading;

using System.Threading.Tasks;

using Collector.Chat;

using Collector.Chat.Models;

using Collector.Chat.Tools;

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

        private readonly IJournalEntriesRepository _entriesRepository;

        private readonly IJournalsRepository _journalsRepository;

        private readonly IJournalCategoriesRepository _categoriesRepository;

        private readonly ILogger<ChatWorker> _logger;

        private readonly IHubContext<WorkerHub> _hubContext;

        private readonly OllamaApiClient _ollama;

        private readonly ChatToolRegistry _toolRegistry;

        private readonly IHostEnvironment _environment;



        public ChatWorker(

            IChatsRepository chatsRepo,

            IOllamaModelsRepository ollamaModelsRepo,

            IJournalEntriesRepository entriesRepository,

            IJournalsRepository journalsRepository,

            IJournalCategoriesRepository categoriesRepository,

            ILogger<ChatWorker> logger,

            IHubContext<WorkerHub> hubContext,

            OllamaApiClient ollama,

            IHostEnvironment environment)

        {

            _chatsRepo = chatsRepo;

            _ollamaModelsRepo = ollamaModelsRepo;

            _entriesRepository = entriesRepository;

            _journalsRepository = journalsRepository;

            _categoriesRepository = categoriesRepository;

            _logger = logger;

            _hubContext = hubContext;

            _ollama = ollama;

            _environment = environment;



            // Get active Ollama model for tool use

            string activeModel = LLMOllama.Model;

            try

            {

                var dbModel = _ollamaModelsRepo.GetActive();

                if (dbModel != null)

                {

                    activeModel = dbModel.Id;

                }

            }

            catch

            {

                // Use default from config

            }



            // Initialize tool registry

            _toolRegistry = new ChatToolRegistry();

            _toolRegistry.RegisterTool(new WebScrapeTool());

            _toolRegistry.RegisterTool(new NewJournalEntryTool(entriesRepository, journalsRepository, categoriesRepository, ollama, activeModel));

            _toolRegistry.RegisterTool(new EditJournalEntryTool(entriesRepository));

            _toolRegistry.RegisterTool(new GatherResearchTool());

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

        /// Sends a message to Ollama and processes the response using the planning system

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
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to get active model from database, using config fallback: {Model}", activeModel);
                }



                // Gather and summarize RAG context

                string ragContext = "";
                try
                {
                    var messageEmbedding = await GetEmbeddingAsync(new List<string> { message });

                    // Log before getting chunks (dev only)
                    if (_environment.IsDevelopment())
                    {
                        await _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "Debug",
                            new { message = "Getting RAG context chunks...", isDevelopment = true }, cancellationToken);
                    }

                    var relevantContext = _chatsRepo.GetContext(Guid.Parse(appUserId), messageEmbedding, topK: 20);

                    // Log the chunks (dev only)
                    if (_environment.IsDevelopment())
                    {
                        var chunkDetails = relevantContext.Select((c, i) => new
                        {
                            index = i,
                            distance = c.Distance,
                            content = c.Content,
                            metadata = c.Metadata
                        }).ToList();

                        await _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "Debug",
                            new { message = $"Retrieved {relevantContext.Count} context chunks", chunks = chunkDetails, isDevelopment = true }, cancellationToken);
                    }

                    if (relevantContext.Count > 0)
                    {
                        // Summarize the context chunks using Ollama
                        var rawContext = string.Join("\n\n", relevantContext.Select(c => c.Content));
                        var summarizedContext = await SummarizeContextAsync(message, rawContext, activeModel, cancellationToken);
                        ragContext = $"Relevant context from previous conversations (summarized):\n{summarizedContext}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ChatWorker: Failed to retrieve or summarize RAG context for user {AppUserId}", appUserId);
                    ragContext = "";
                }



                // Step 1: Ask Ollama what the user wants (planning phase)

                await SendWorkerMessage(appUserId, workerId, "Planning", new { message = "Analyzing your request..." }, cancellationToken);



                var planningPrompt = _toolRegistry.BuildPlanningPrompt(message, ragContext);

                var planResponse = await GetOllamaResponseAsync(planningPrompt, activeModel, appUserId, workerId, cancellationToken, debugType: "planning");



                OllamaPlanResponse? plan;

                try

                {

                    plan = JsonSerializer.Deserialize<OllamaPlanResponse>(planResponse);

                    _logger.LogDebug("ChatWorker: Deserialized plan - IsPlan={IsPlan}, Message={Message}, StepsCount={StepsCount}", 

                        plan?.IsPlan, plan?.Message, plan?.Steps?.Count ?? 0);

                }

                catch (JsonException ex)

                {

                    _logger.LogWarning(ex, "ChatWorker: Failed to deserialize plan response, treating as answer");

                    // If parsing fails, treat as direct answer

                    plan = new OllamaPlanResponse

                    {
                        Message = planResponse

                    };

                }



                if (plan == null)

                {

                    _logger.LogWarning("ChatWorker: Plan deserialized to null, treating as answer");

                    plan = new OllamaPlanResponse

                    {
                        Message = planResponse

                    };

                }



                // Create chat if needed

                if (isNewChat)

                {

                    var chatTitle = message.Length > 50 ? message.Substring(0, 50) + "..." : message;



                    var newChat = new Collector.Data.Entities.Chat

                    {

                        AppUserId = Guid.Parse(appUserId),

                        Title = chatTitle,

                        Status = 1

                    };

                    _chatId = _chatsRepo.Add(newChat);



                    await SendWorkerMessage(appUserId, workerId, "ChatCreated", new

                    {

                        id = _chatId,

                        title = newChat.Title

                    }, cancellationToken);



                    _logger.LogInformation("ChatWorker: Created new chat {ChatId} with title '{Title}'", _chatId, chatTitle);

                }



                // Save user message to history

                var userMessageEntity = new ChatHistory

                {

                    ChatId = _chatId,

                    Role = 0,

                    Content = message,

                    Model = model,

                    Status = 1

                };

                var userMessageId = _chatsRepo.AddMessage(userMessageEntity);



                await SendWorkerMessage(appUserId, workerId, "MessageSaved", new

                {

                    id = userMessageId,

                    role = 0,

                    content = message,

                    model = model

                }, cancellationToken);



                // Handle based on response type (plan has steps, answer does not)

                if (plan.IsPlan)

                {

                    // Execute the plan (fact extraction happens inside ExecutePlanAsync after plan completion)

                    await ExecutePlanAsync(plan, appUserId, workerId, _chatId, message, userMessageId.ToString(), ragContext, activeModel, cancellationToken);

                }

                else

                {

                    // Direct answer - generate content with Ollama or remote LLM

                    await SendDirectAnswerAsync(appUserId, workerId, _chatId, plan.Message, activeModel, cancellationToken);

                    

                    // For direct answers, extract facts from the user message and response

                    await ExtractAndStoreFactsAsync(appUserId, _chatId, userMessageId, message, plan.Message ?? "", cancellationToken);

                }



                // Update chat modified time

                _chatsRepo.UpdateModified(_chatId);



                _logger.LogInformation("ChatWorker SendMessage completed for chatId={ChatId}, isPlan={IsPlan}", _chatId, plan.IsPlan);

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "ChatWorker SendMessage error");

                var errorDetails = new

                {

                    message = ex.Message,

                    stackTrace = ex.StackTrace,

                    type = ex.GetType().Name,

                    innerException = ex.InnerException != null ? new

                    {

                        message = ex.InnerException.Message,

                        stackTrace = ex.InnerException.StackTrace,

                        type = ex.InnerException.GetType().Name

                    } : null

                };

                await SendWorkerMessage(appUserId, workerId, "ChatError", errorDetails, CancellationToken.None);

            }

        }

        private async Task<string> GetOllamaResponseAsync(string prompt, string model, string appUserId, Guid workerId, CancellationToken cancellationToken, string? debugType = null)

        {

            var request = new OllamaSharp.Models.GenerateRequest

            {

                Model = model,

                System = "You are a helpful AI assistant.",

                Prompt = prompt,

                Stream = false,

                Format = "json"

            };



            // Send raw request to client for debugging

            if (debugType != null && _environment.IsDevelopment())

            {

                await SendWorkerMessage(appUserId, workerId, "OllamaRequest", new 

                { 

                    type = debugType,

                    prompt = prompt,

                    isDevelopment = true

                }, cancellationToken);

            }



            try

            {

                var responseContent = "";

                await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))

                {

                    if (response?.Response != null)

                    {

                        responseContent += response.Response;

                    }

                }



                // Send raw response to client for debugging

                if (debugType != null && _environment.IsDevelopment())

                {

                    await SendWorkerMessage(appUserId, workerId, "OllamaResponse", new 

                    { 

                        type = debugType,

                        rawResponse = responseContent,

                        isDevelopment = true

                    }, cancellationToken);

                }



                return responseContent;

            }

            catch (HttpRequestException ex) when (ex.Message.Contains("404"))

            {

                // Model not found, try to pull it

                _logger.LogInformation("Model {Model} not found, attempting to pull...", model);

                await SendWorkerMessage(appUserId, workerId, "Planning", new { message = $"Model '{model}' not found. Downloading from Ollama..." }, cancellationToken);

                

                try

                {

                    await PullModelWithProgressAsync(model, appUserId, workerId, cancellationToken);

                    

                    // Retry the request

                    var responseContent = "";

                    await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))

                    {

                        if (response?.Response != null)

                        {

                            responseContent += response.Response;

                        }

                    }



                    // Send raw response for retry

                    if (debugType != null && _environment.IsDevelopment())

                    {

                        await SendWorkerMessage(appUserId, workerId, "OllamaResponse", new 

                        { 

                            type = debugType,

                            rawResponse = responseContent,

                            isDevelopment = true

                        }, cancellationToken);

                    }



                    return responseContent;

                }

                catch (Exception pullEx)

                {

                    _logger.LogError(pullEx, "Failed to pull model {Model}", model);

                    throw new Exception($"Failed to download model '{model}'. Please ensure Ollama is running and you have an internet connection. Error: {pullEx.Message}", pullEx);

                }

            }

        }



        private async Task PullModelWithProgressAsync(string model, string appUserId, Guid workerId, CancellationToken cancellationToken)

        {

            var pullRequest = new OllamaSharp.Models.PullModelRequest { Model = model };

            int lastPercent = 0;

            

            await foreach (var status in _ollama.PullModelAsync(pullRequest, cancellationToken))

            {

                if (status?.Status != null)

                {

                    _logger.LogInformation("Pulling model {Model}: {Status}", model, status.Status);

                    

                    // Try to parse progress from status message (e.g., "pulling 256/1024 MB" or "45%")

                    var percent = ParseProgressPercent(status.Status, lastPercent);

                    if (percent > lastPercent)

                    {

                        lastPercent = percent;

                    }

                    

                    await SendWorkerMessage(appUserId, workerId, "PlanProgress", new { percent = lastPercent, message = $"Downloading {model}: {status.Status}" }, cancellationToken);

                }

            }

            

            _logger.LogInformation("Model {Model} pulled successfully", model);

        }



        private int ParseProgressPercent(string status, int currentPercent)

        {

            // Try to extract percentage from status message

            // Examples: "45%", "pulling 256/1024 MB (25%)", "downloading 50%"

            

            // Look for explicit percentage

            var percentMatch = System.Text.RegularExpressions.Regex.Match(status, @"(\d+)%");

            if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out var explicitPercent))

            {

                return explicitPercent;

            }

            

            // Look for "X/Y" pattern and calculate percentage

            var fractionMatch = System.Text.RegularExpressions.Regex.Match(status, @"(\d+(?:\.\d+)?)\s*/\s*(\d+(?:\.\d+)?)");

            if (fractionMatch.Success && 

                double.TryParse(fractionMatch.Groups[1].Value, out var current) &&

                double.TryParse(fractionMatch.Groups[2].Value, out var total) &&

                total > 0)

            {

                return (int)((current / total) * 100);

            }

            

            // If parsing fails, increment slightly to show activity

            return Math.Min(currentPercent + 1, 99);

        }



        private async Task ExecutePlanAsync(OllamaPlanResponse plan, string appUserId, Guid workerId, Guid chatId,

            string userMessage, string userMessageId, string ragContext, string model, CancellationToken cancellationToken)

        {

            // Create execution plan from deserialized response

            ExecutionPlan? executionPlan = null;

            if (plan.Steps != null && plan.Steps.Count > 0)

            {

                executionPlan = new ExecutionPlan

                {

                    Steps = plan.Steps

                };

            }



            if (executionPlan?.Steps == null || executionPlan.Steps.Count == 0)

            {

                // No executable steps - this is an error, planner should have returned steps

                await SendWorkerMessage(appUserId, workerId, "ChatError", new 

                { 

                    message = "The AI planner returned an empty plan with no executable steps. Please try rephrasing your request." 

                }, cancellationToken);

                return;

            }



            // First, send the plan message to chat window so user knows what the assistant is doing

            await SendWorkerMessage(appUserId, workerId, "AssistantPlan", new

            {

                message = plan.Message

            }, cancellationToken);



            // Plan is starting - show progress indicator (frontend will replace any existing progress)

            await SendWorkerMessage(appUserId, workerId, "PlanStarted", new

            {

                stepCount = executionPlan.Steps.Count

            }, cancellationToken);



            // Shared data dictionary for passing values between tools

            var data = new Dictionary<string, string>

            {

                ["appUserId"] = appUserId,

                ["chatId"] = chatId.ToString(),

                ["workerId"] = workerId.ToString()

            };



            // Execute each step

            await _toolRegistry.ExecutePlan(

                executionPlan,

                userMessage,

                ragContext,

                data,

                (percent, message) =>

                {

                    // Send progress with replace flag so frontend updates existing progress label

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "PlanProgress",

                        new { percent, message, replace = true }, cancellationToken);

                },

                (tool, error, ex) =>

                {

                    var errorDetails = new

                    {

                        tool,

                        error,

                        exception = ex != null ? new

                        {

                            message = ex.Message,

                            stackTrace = ex.StackTrace,

                            type = ex.GetType().Name

                        } : null

                    };

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ToolError",

                        errorDetails, cancellationToken);

                },

                (tool, message) =>

                {

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ToolComplete",

                        new { tool, message }, cancellationToken);

                },

                // Raw request callback - sends OllamaRequest event for debugging

                _environment.IsDevelopment() ? (type, prompt) =>

                {

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "OllamaRequest",

                        new { type, prompt, isDevelopment = true }, cancellationToken);

                } : null,

                // Raw response callback - sends OllamaResponse event for debugging

                _environment.IsDevelopment() ? (type, response) =>

                {

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "OllamaResponse",

                        new { type, rawResponse = response, isDevelopment = true }, cancellationToken);

                } : null,

                // OnSaveChatHistory callback - extracts facts immediately when web-scrape provides context

                async (context) =>

                {

                    await ExtractAndStoreFactsAsync(appUserId, chatId, Guid.Parse(userMessageId), userMessage, context, cancellationToken);

                },

                // SendMessage callback - sends message with optional link to chat window

                (message, linkUrl, linkText) =>

                {

                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ChatMessage",

                        new { message, linkUrl, linkText }, cancellationToken);

                }

            );



            // Send completion message

            await SendWorkerMessage(appUserId, workerId, "PlanCompleted", new

            {

                message = $"Completed execution of plan: {plan.Message}"

            }, cancellationToken);



            // Generate final summary with Ollama

            var summaryPrompt = $"The following plan has been executed successfully:\n{plan.Message}\n\n" +

                               $"Available data from execution: {string.Join(", ", data.Keys)}\n\n" +

                               $"Provide a brief, friendly summary to the user about what was accomplished.";



            var summary = await GetOllamaResponseAsync(summaryPrompt, model, appUserId, workerId, cancellationToken);

            await SaveAndSendAssistantMessage(appUserId, workerId, chatId, summary, model, cancellationToken);

        }



        private async Task SendDirectAnswerAsync(string appUserId, Guid workerId, Guid chatId,

            string responseMessage, string model, CancellationToken cancellationToken)

        {

            // If using remote LLM for content generation, we would call it here

            // For now, use Ollama for the actual response

            await SaveAndSendAssistantMessage(appUserId, workerId, chatId, responseMessage, model, cancellationToken);

        }



        private async Task<string> SummarizeContextAsync(string userMessage, string rawContext, string model, CancellationToken cancellationToken)

        {

            try

            {

                var systemPrompt = "You are a helpful assistant that summarizes conversation context. Given a user's question and relevant context from previous conversations, create a concise summary (1-3 paragraphs) that captures the key information needed to answer the user's question. Focus on facts, preferences, and relevant history. Be concise but informative.";

                var userPrompt = $"User Question: {userMessage}\n\nRelevant Context from previous conversations:\n{rawContext}\n\nPlease summarize the key information from the context that would help answer the user's question:";

                var request = new OllamaSharp.Models.GenerateRequest

                {

                    Model = model,

                    System = systemPrompt,

                    Prompt = userPrompt,

                    Stream = false

                };

                var responseContent = "";

                await foreach (var resp in _ollama.GenerateAsync(request, cancellationToken))

                {

                    if (resp?.Response != null)

                    {

                        responseContent += resp.Response;

                    }

                }

                _logger.LogDebug("ChatWorker: Summarized context, result length: {Length}", responseContent.Length);

                return responseContent.Trim();

            }

            catch (Exception ex)

            {

                _logger.LogWarning(ex, "ChatWorker: Failed to summarize context, returning raw context");

                // If summarization fails, return a truncated version of the raw context

                return rawContext.Length > 2000 ? rawContext.Substring(0, 2000) + "..." : rawContext;

            }

        }



        private async Task SaveAndSendAssistantMessage(string appUserId, Guid workerId, Guid chatId,

            string content, string model, CancellationToken cancellationToken)

        {

            var assistantMessage = new ChatHistory

            {

                ChatId = chatId,

                Role = 1,

                Content = content,

                Model = model,

                Status = 1

            };

            var assistantMessageId = _chatsRepo.AddMessage(assistantMessage);



            await SendWorkerMessage(appUserId, workerId, "ChatResponse", new

            {

                id = assistantMessageId,

                role = 1,

                content = content,

                model = model

            }, cancellationToken);

        }



        private async Task ExtractAndStoreFactsAsync(string appUserId, Guid chatId, Guid userMessageId,

            string userMessage, string assistantMessage, CancellationToken cancellationToken)

        {

            try

            {

                var userGuid = Guid.Parse(appUserId);

                var facts = await ExtractFactsFromConversationAsync(userMessage, assistantMessage, cancellationToken);



                if (facts != null && facts.Count > 0)

                {

                    int storedCount = 0;

                    int duplicateCount = 0;



                    foreach (var fact in facts)

                    {

                        var factEmbedding = await GetEmbeddingAsync(new List<string> { fact });



                        if (!_chatsRepo.HasSimilarContext(userGuid, factEmbedding, similarityThreshold: 0.1f))

                        {

                            var factMetadata = JsonSerializer.Serialize(new

                            {

                                type = "fact",

                                timestamp = DateTime.UtcNow,

                                chatId = chatId,

                                userMessageId = userMessageId

                            });



                            _chatsRepo.StoreContext(userGuid, chatId, fact, factEmbedding, factMetadata);

                            storedCount++;

                        }

                        else

                        {

                            duplicateCount++;

                        }

                    }



                    _logger.LogInformation("ChatWorker: Extracted {TotalFacts} facts, stored {StoredCount} new facts for user {AppUserId}",

                        facts.Count, storedCount, appUserId);

                }

            }

            catch (Exception ex)

            {

                _logger.LogWarning(ex, "ChatWorker: Failed to extract and store facts for user {AppUserId}", appUserId);

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



    }

}

