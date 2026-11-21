using Collector.Chat.Helpers;
using System.Text.Json;
using Collector.Chat;
using Collector.Chat.Models;
using Collector.Chat.Tools;
using Collector.Common;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using Collector.Workers.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using OllamaSharp;

namespace Collector.Workers
{
    public class ChatWorker : IWorker
    {
        private readonly IChatsRepository _chatsRepo;
        private readonly IOllamaModelsRepository _ollamaModelsRepo;
        private readonly IJournalEntriesRepository _entriesRepository;
        private readonly IJournalsRepository _journalsRepository;
        private readonly IJournalCategoriesRepository _categoriesRepository;
        private readonly ISearchEnginesRepository _searchEnginesRepository;
        private readonly ILogger<ChatWorker> _logger;
        private readonly IHubContext<WorkerHub> _hubContext;
        private readonly OllamaApiClient _ollama;
        private readonly ChatToolRegistry _toolRegistry;
        private readonly IHostEnvironment _environment;

        // One active plan per chat: new messages cancel the running plan
        // (steering) then queue behind its semaphore so they never race.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _chatLocks = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, CancellationTokenSource> _chatPlanTokens = new();
        private Guid _activeChatId = Guid.Empty;
        private Guid _activeWorkerId = Guid.Empty;

        // Per-model context window, resolved once from /api/show (keyed by model name).
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _modelWindowCache = new();

        // Development-only per-session file log (logs/chat/{chatId}.log)
        private ChatDebugLog? _chatLog;
        // Debugging detail log (logs/chat/details/{chatId}.log + dumps)
        private ChatDetailLog? _detailLog;

        public ChatWorker(
            IChatsRepository chatsRepo,
            IOllamaModelsRepository ollamaModelsRepo,
            IJournalEntriesRepository entriesRepository,
            IJournalsRepository journalsRepository,
            IJournalCategoriesRepository categoriesRepository,
            ISearchEnginesRepository searchEnginesRepository,
            IJournalSnapshotsRepository snapshotsRepository,
            IDomainsRepository domainsRepository,
            ILLMHtmlParsersRepository htmlParsersRepository,
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
            _searchEnginesRepository = searchEnginesRepository;
            _logger = logger;
            _hubContext = hubContext;
            _ollama = ollama;
            _environment = environment;

            // Get active Ollama model for tool use
            string activeModel = AI.Ollama.Config.Model;

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

            // Initialize tool registry — the LLMClient routes quick decisions to
            // local Ollama and complex module-JSON generation to the remote endpoint.
            _toolRegistry = new ChatToolRegistry();
            var llmClient = new Collector.Chat.Helpers.LLMClient(ollama, activeModel);
            _toolRegistry.RegisterTool(new WebScrapeTool(llmClient, domainsRepository,
                // Same cookie refresh the video worker uses — asks the client's
                // Chrome extension for fresh cookies when a fetch is blocked.
                (uid, domain, ct) => SignalR.Workers.RequestCookiesFromClientAsync(
                    uid.ToString(), _activeWorkerId, domain, ct),
                htmlParsersRepository, searchEnginesRepository));
            _toolRegistry.RegisterTool(new AddEditJournalEntryTool(
                entriesRepository, journalsRepository, categoriesRepository, ollama, activeModel,
                llmClient,
                onEntrySaved: (entryId, journalId, appUserId, ops) => BroadcastEntryUpdated(appUserId, entryId, journalId, ops),
                snapshotsRepository, searchEnginesRepository));
            _toolRegistry.RegisterTool(new GatherResearchTool(ollama, activeModel, searchEnginesRepository));
            _toolRegistry.RegisterTool(new ResearchReviewTool(
                entriesRepository, ollama, activeModel, llmClient));
            _toolRegistry.RegisterTool(new CleanWebContentTool(ollama, activeModel));
            _toolRegistry.RegisterTool(new ManageOllamaModelsTool(ollama, _ollamaModelsRepo, htmlParsersRepository));
        }

        public Task Stop()
        {
            // Cancel the in-flight plan for this chat — SendMessage linked the
            // worker-level token to the per-chat CTS, so cancelling either
            // side stops tool execution promptly.
            if (_activeChatId != Guid.Empty &&
                _chatPlanTokens.TryRemove(_activeChatId, out var running))
            {
                try { running.Cancel(); } catch { /* already disposed */ }
                running.Dispose();
            }
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
        public async Task SendMessage(string appUserId, Guid workerId, Guid? chatId, string message, string model, string mode, Guid? journalEntryId = null, CancellationToken cancellationToken = default)
        {
            Guid _chatId = chatId ?? Guid.Empty;
            _activeChatId = _chatId;
            _activeWorkerId = workerId;
            _chatLog = new ChatDebugLog(_environment);
            _detailLog = new ChatDetailLog(_environment.ContentRootPath, _environment.IsDevelopment());
            ChatDetailLog.Current = _detailLog;
            if (_chatId != Guid.Empty)
            {
                _chatLog.SetChatId(_chatId);
                _detailLog.SetChatId(_chatId);
            }
            ChatDetailLog.Line($"user: {message}");
            SemaphoreSlim? chatLock = null;
            CancellationTokenSource? planCts = null;
            try
            {
                _logger.LogInformation("ChatWorker SendMessage: chatId={ChatId}, model={Model}, mode={Mode}", chatId, model, mode);

                // Receipt event — confirms to the browser console that the
                // message reached the worker before anything else can stall.
                if (_environment.IsDevelopment())
                {
                    await SendWorkerMessage(appUserId, workerId, "MessageReceived", new
                    {
                        workerId,
                        chatId,
                        preview = message?.Length > 80 ? message.Substring(0, 80) + "…" : message,
                        isDevelopment = true
                    }, cancellationToken);
                }
                bool isNewChat = chatId == null || chatId == Guid.Empty;
                // If chat exists, verify ownership
                Collector.Data.Entities.Chat? existingChat = null;
                if (!isNewChat)
                {
                    existingChat = _chatsRepo.GetById(_chatId);
                    if (existingChat == null || existingChat.AppUserId.ToString() != appUserId)
                    {
                        await SendWorkerMessage(appUserId, workerId, "ChatError", new { message = "Chat not found or unauthorized" }, cancellationToken);
                        return;
                    }
                }

                // Serialization + steering: a new message cancels any plan still
                // running for this chat, then queues behind the per-chat lock so
                // two plans can never race on the same entry.
                if (!isNewChat)
                {
                    if (_chatPlanTokens.TryRemove(_chatId, out var running))
                    {
                        try { running.Cancel(); } catch { }
                        running.Dispose();
                    }
                    chatLock = _chatLocks.GetOrAdd(_chatId, _ => new SemaphoreSlim(1, 1));
                    // Make the queue wait visible — if a previous request is still
                    // finishing (fact extraction, persistence), the user sees this
                    // instead of a silent spinner.
                    await SendWorkerMessage(appUserId, workerId, "PlanProgress", new
                    {
                        percent = 1,
                        message = "Waiting for the previous request to finish..."
                    }, cancellationToken);
                    await chatLock.WaitAsync(cancellationToken);
                    planCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _chatPlanTokens[_chatId] = planCts;
                    cancellationToken = planCts.Token;
                }

                // Get active model from database, fallback to config
                string activeModel = AI.Ollama.Config.Model;
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

                // Emit immediately so the chat never appears dead while the
                // context phase runs before "Planning".
                await SendWorkerMessage(appUserId, workerId, "PlanProgress", new
                {
                    percent = 2,
                    message = "Gathering conversation context..."
                }, cancellationToken);

                // === Session context (per-chat, no cross-session retrieval) ===
                // Verbatim history is the primary context. When the estimated
                // prompt crosses ~82% of the model's context window, older turns
                // get compacted into a persisted running summary — the same
                // Windsurf/Claude Code pattern, with a visible usage bar.
                var contextBudget = await ResolveModelWindowAsync(activeModel, cancellationToken);
                var estimatedTokens = Collector.Chat.Helpers.ContextBudget.PromptOverheadTokens
                    + Collector.Chat.Helpers.ContextBudget.EstimateTokens(message);
                string ragContext = "";
                var history = new List<Collector.Data.Entities.ChatHistory>();
                if (!isNewChat && existingChat != null)
                {
                    try
                    {
                        history = _chatsRepo.GetMessagesByChatId(_chatId) ?? new List<Collector.Data.Entities.ChatHistory>();

                        // Auto-compact before building context — checked at
                        // message start only, never mid-plan.
                        if (history.Count > 0)
                        {
                            await MaybeCompactSessionAsync(existingChat, history, activeModel,
                                contextBudget, appUserId, workerId, cancellationToken);
                        }

                        ragContext = BuildSessionContext(existingChat, history);
                        estimatedTokens += Collector.Chat.Helpers.ContextBudget.EstimateTokens(ragContext);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ChatWorker: Failed to build session context");
                        await SendWorkerExceptionAsync(appUserId, workerId, "session-context", ex, cancellationToken);
                    }
                }

                // Backfill the session's entry link when the chat window was
                // opened from a journal entry's toolbar but the chat row
                // predates (or missed) the association.
                if (journalEntryId.HasValue && journalEntryId != Guid.Empty
                    && existingChat != null
                    && (existingChat.JournalEntryId == null || existingChat.JournalEntryId == Guid.Empty))
                {
                    try
                    {
                        _chatsRepo.SetJournalEntryId(_chatId, journalEntryId);
                        existingChat.JournalEntryId = journalEntryId;
                    }
                    catch (Exception ex)
                    {
                        await SendWorkerExceptionAsync(appUserId, workerId, "session-entry-backfill", ex, cancellationToken);
                    }
                }

                // Tell the planner which journal entry this session is working on —
                // stored association first, then the toolbar-launched entry id.
                var sessionEntry = existingChat?.JournalEntryId ?? journalEntryId;
                if (sessionEntry is Guid targetEntryId && targetEntryId != Guid.Empty)
                {
                    var entryHint = $"This session is currently working on journal entry {targetEntryId}. " +
                        "If the user's request refers to 'the entry', 'it', or asks to rearrange/add/remove " +
                        "content, they mean that entry — prefer updating it over creating a new one.";
                    ragContext = string.IsNullOrEmpty(ragContext) ? entryHint : entryHint + "\n\n" + ragContext;
                }

                // Session-scoped memory notes (extracted from THIS chat only)
                if (!isNewChat)
                {
                    try
                    {
                        var notes = _chatsRepo.GetRecentContextByMetadataKind(_chatId, "note", 6);
                        if (notes.Count > 0)
                        {
                            var noteBlock = "Session notes:\n" + string.Join("\n", notes.Select(n => "- " + TruncateForContext(n.Content, 240)));
                            ragContext = string.IsNullOrEmpty(ragContext) ? noteBlock : ragContext + "\n\n" + noteBlock;
                            estimatedTokens += Collector.Chat.Helpers.ContextBudget.EstimateTokens(noteBlock);
                        }
                    }
                    catch (Exception ex)
                    {
                        await SendWorkerExceptionAsync(appUserId, workerId, "session-notes", ex, cancellationToken);
                    }
                }

                // Broadcast context usage for the Windsurf-style bar + persist it
                await SendWorkerMessage(appUserId, workerId, "ContextUsage", new
                {
                    usedTokens = estimatedTokens,
                    maxTokens = contextBudget,
                    percent = Math.Min(100, (int)Math.Round(100.0 * estimatedTokens / contextBudget))
                }, cancellationToken);
                try { if (!isNewChat) _chatsRepo.SetContextTokens(_chatId, estimatedTokens); }
                catch { /* cosmetic persistence — non-fatal */ }


                // Step 1: Ask Ollama what the user wants (planning phase)
                await SendWorkerMessage(appUserId, workerId, "Planning", new { message = "Analyzing your request..." }, cancellationToken);
                var planningPrompt = _toolRegistry.BuildPlanningPrompt(message, ragContext);
                var planResponse = await GetOllamaResponseAsync(planningPrompt, activeModel, appUserId, workerId, cancellationToken, debugType: "planning");
                // One retry on empty/degenerate output — small models flake, and
                // without this a broken active model can never be asked to switch.
                var degenerate = string.IsNullOrWhiteSpace(planResponse)
                    || new string(planResponse.Where(c => !char.IsWhiteSpace(c)).ToArray()) is var squashed
                        && (squashed == "{}" || squashed == "{\"message\":\"\"}");
                if (degenerate)
                {
                    _logger.LogWarning("ChatWorker: Empty planning response, retrying once");
                    await SendWorkerMessage(appUserId, workerId, "PlanProgress", new
                    {
                        percent = 5,
                        message = "The model returned an empty plan — retrying..."
                    }, cancellationToken);
                    planResponse = await GetOllamaResponseAsync(planningPrompt, activeModel, appUserId, workerId, cancellationToken, debugType: "planning-retry");
                    degenerate = string.IsNullOrWhiteSpace(planResponse)
                        || new string(planResponse.Where(c => !char.IsWhiteSpace(c)).ToArray()) is var squashed2
                            && (squashed2 == "{}" || squashed2 == "{\"message\":\"\"}");
                }
                if (degenerate)
                {
                    await SendWorkerMessage(appUserId, workerId, "ChatError", new
                    {
                        message = $"Model '{activeModel}' returned an empty response. It may not support Collector's planning format — try a different model (e.g. \"ollama run llama3.2\")."
                    }, cancellationToken);
                    return;
                }
                OllamaPlanResponse? plan = null;
                var planJson = Collector.Chat.Helpers.LLMJson.Extract(planResponse);
                if (planJson != null)
                {
                    try
                    {
                        plan = JsonSerializer.Deserialize<OllamaPlanResponse>(planJson);
                        _logger.LogDebug("ChatWorker: Deserialized plan - IsPlan={IsPlan}, Message={Message}, StepsCount={StepsCount}",
                            plan?.IsPlan, plan?.Message, plan?.Steps?.Count ?? 0);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "ChatWorker: Failed to deserialize plan response, treating as answer");
                        plan = new OllamaPlanResponse { Message = planResponse };
                    }
                    if (plan == null)
                    {
                        _logger.LogWarning("ChatWorker: Plan deserialized to null, treating as answer");
                        plan = new OllamaPlanResponse { Message = planResponse };
                    }
                    // Some models emit a bare single step — {"tool": "...", "args": {...}} —
                    // instead of {message, steps}. Normalize it into a one-step plan
                    // so a valid tool call isn't mistaken for an empty response.
                    NormalizeSingleStepPlan(plan, planJson);
                }
                else if (planResponse.TrimStart().StartsWith('{') || planResponse.TrimStart().StartsWith('['))
                {
                    // Starts as JSON but never closes — the model stopped
                    // generating mid-response. A stub truncation like
                    // {"message": " carries nothing worth repairing — retry
                    // the plan call once with a completion nudge first;
                    // substantial partial JSON still goes through repair.
                    _logger.LogWarning("ChatWorker: Truncated plan response from model {Model}: {Response}", activeModel, planResponse);
                    if (planResponse.Trim().Length < 120)
                    {
                        await SendWorkerMessage(appUserId, workerId, "PlanProgress", new
                        {
                            percent = 8,
                            message = "The model's plan was cut off — retrying..."
                        }, cancellationToken);
                        var retryResponse = await GetOllamaResponseAsync(
                            planningPrompt + "\n\nReturn ONLY the complete JSON — your previous response was cut off mid-generation.",
                            activeModel, appUserId, workerId, cancellationToken, debugType: "planning-nudge");
                        var retryJson = Collector.Chat.Helpers.LLMJson.Extract(retryResponse);
                        if (retryJson != null)
                        {
                            try
                            {
                                plan = JsonSerializer.Deserialize<OllamaPlanResponse>(retryJson);
                                if (plan != null) NormalizeSingleStepPlan(plan, retryJson);
                            }
                            catch (JsonException) { plan = null; }
                        }
                        else if (!retryResponse.TrimStart().StartsWith('{') && !retryResponse.TrimStart().StartsWith('['))
                        {
                            // Retry answered in plain prose — a direct answer.
                            plan = new OllamaPlanResponse { Message = retryResponse };
                        }
                        else
                        {
                            // Still truncated — hand the freshest output to repair.
                            planResponse = retryResponse;
                        }
                    }
                    var repairedPlan = plan == null
                        ? await TryRepairPlanAsync(planResponse, activeModel, appUserId, workerId, cancellationToken)
                        : null;
                    if (repairedPlan != null && (repairedPlan.IsPlan || !string.IsNullOrWhiteSpace(repairedPlan.Message)))
                    {
                        plan = repairedPlan;
                    }
                    else if (plan == null)
                    {
                        await SendWorkerMessage(appUserId, workerId, "ChatError", new
                        {
                            message = $"Model '{activeModel}' stopped mid-response and couldn't repair it. Its context window is {contextBudget} tokens (this prompt used ~{estimatedTokens}) — it may have run out of room, or it may just be terminating generation early. Try a different model."
                        }, cancellationToken);
                        return;
                    }
                }
                else if (LooksLikePromptEcho(planResponse))
                {
                    // The model parroted our prompt back instead of answering —
                    // saving it would surface internal instructions to the user.
                    _logger.LogWarning("ChatWorker: Model echoed the prompt: {Response}", planResponse);
                    await SendWorkerMessage(appUserId, workerId, "ChatError", new
                    {
                        message = $"Model '{activeModel}' echoed the prompt instead of responding. It may not support Collector's planning format — try a different model (e.g. \"ollama run llama3.2\")."
                    }, cancellationToken);
                    return;
                }
                else
                {
                    // plain prose — a direct answer, not a plan
                    plan = new OllamaPlanResponse { Message = planResponse };
                }

                // A structurally valid but empty response ({"steps":[]}, {}, etc.)
                // is the model failing the planning contract — tell the user
                // instead of saving a blank assistant message or spinning.
                if (!plan.IsPlan && string.IsNullOrWhiteSpace(plan.Message))
                {
                    _logger.LogWarning("ChatWorker: Model returned an empty plan/answer: {Response}", planResponse);
                    await SendWorkerMessage(appUserId, workerId, "ChatError", new
                    {
                        message = $"Model '{activeModel}' returned an empty response. It may not support Collector's planning format — try a different model (e.g. \"ollama run llama3.2\")."
                    }, cancellationToken);
                    return;
                }

                // Create chat if needed
                if (isNewChat)
                {
                    var chatTitle = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
                    var newChat = new Collector.Data.Entities.Chat
                    {
                        AppUserId = Guid.Parse(appUserId),
                        Title = chatTitle,
                        Status = 1,
                        JournalEntryId = journalEntryId
                    };
                    _chatId = _chatsRepo.Add(newChat);
                    _chatLog?.SetChatId(_chatId);
                    _detailLog?.SetChatId(_chatId);
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

                // Handle based on response type (plan has steps, answer does not)
                if (plan.IsPlan)
                {
                    ChatDetailLog.Line($"plan started: {plan.Steps!.Count} step(s) — " +
                        string.Join(", ", plan.Steps!.Select(s => s.Tool)));
                    // Execute the plan (fact extraction happens inside ExecutePlanAsync after plan completion)
                    await ExecutePlanAsync(plan, appUserId, workerId, _chatId, message, userMessageId.ToString(), ragContext, activeModel, cancellationToken);
                }
                else
                {
                    // Direct answer - generate content with Ollama or remote LLM
                    await SendDirectAnswerAsync(appUserId, workerId, _chatId, plan.Message, activeModel, cancellationToken);
                    // For direct answers, extract facts from the user message and response
                    await ExtractAndStoreFactsAsync(appUserId, workerId, _chatId, userMessageId, message, plan.Message ?? "", cancellationToken);
                }

                // Update chat modified time
                _chatsRepo.UpdateModified(_chatId);
                _logger.LogInformation("ChatWorker SendMessage completed for chatId={ChatId}, isPlan={IsPlan}", _chatId, plan.IsPlan);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // User hit stop (or a new message steered the plan) — not an
                // error. The chat window clears its thinking state on this.
                _logger.LogInformation("ChatWorker SendMessage cancelled for chatId={ChatId}", _chatId);
                await SendWorkerMessage(appUserId, workerId, "ChatStopped", new
                {
                    message = "Stopped."
                }, CancellationToken.None);
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
            finally
            {
                if (planCts != null)
                {
                    _chatPlanTokens.TryRemove(_chatId, out _);
                    planCts.Dispose();
                }
                chatLock?.Release();
            }
        }

        /// <summary>
        /// Normalizes a bare single-step object — {"tool": "...", "args": {...}} —
        /// into a one-step plan in place. Small models sometimes skip the
        /// {message, steps} envelope entirely.
        /// </summary>
        private void NormalizeSingleStepPlan(OllamaPlanResponse plan, string planJson)
        {
            if (plan.IsPlan || !string.IsNullOrWhiteSpace(plan.Message))
                return;
            try
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(planJson) as System.Text.Json.Nodes.JsonObject;
                var toolName = node?["tool"]?.GetValue<string>()
                    ?? node?["toolName"]?.GetValue<string>()
                    ?? node?["name"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(toolName))
                    return;
                var step = JsonSerializer.Deserialize<Collector.Chat.Models.PlanStep>(planJson);
                if (step == null)
                    return;
                step.Tool = toolName;
                plan.Steps = new List<Collector.Chat.Models.PlanStep> { step };
                plan.Message = $"Running {toolName}.";
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ChatWorker: Single-step normalization failed");
            }
        }

        /// <summary>
        /// Self-healing pass: when the model's plan is malformed or truncated,
        /// feed the broken output back and ask it to emit valid JSON. One shot —
        /// the caller still errors out if the repair itself is unparseable.
        /// </summary>
        /// <summary>Repair call + JSON extraction + prompt-echo guard. Returns
        /// the repaired plan, or null when repair produced nothing usable.</summary>
        private async Task<OllamaPlanResponse?> TryRepairPlanAsync(string brokenResponse, string model, string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            var repaired = await RepairPlanResponseAsync(brokenResponse, model, appUserId, workerId, cancellationToken);
            var repairedJson = repaired != null ? Collector.Chat.Helpers.LLMJson.Extract(repaired) : null;
            OllamaPlanResponse? repairedPlan = null;
            if (repairedJson != null)
            {
                try { repairedPlan = JsonSerializer.Deserialize<OllamaPlanResponse>(repairedJson); }
                catch (JsonException) { repairedPlan = null; }
                if (repairedPlan != null)
                    NormalizeSingleStepPlan(repairedPlan, repairedJson);
            }
            if (repairedPlan == null) return null;

            // The model sometimes regurgitates the repair prompt (template
            // JSON + instructions) instead of fixing the plan — strip steps
            // that name tools we don't have and reject message text that
            // echoes our own prompt.
            var validTools = new HashSet<string>(
                _toolRegistry.GetAllTools().Select(t => t.ToolKey),
                StringComparer.OrdinalIgnoreCase);
            repairedPlan.Steps?.RemoveAll(s =>
                s == null || string.IsNullOrWhiteSpace(s.Tool) || !validTools.Contains(s.Tool));
            if (repairedPlan.Message == "What you will do or your answer")
                repairedPlan.Message = "";
            return LooksLikePromptEcho(repairedPlan.Message) ? null : repairedPlan;
        }

        private async Task<string?> RepairPlanResponseAsync(string brokenResponse, string model, string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            var toolKeys = string.Join(", ", _toolRegistry.GetAllTools().Select(t => t.ToolKey));
            var broken = brokenResponse.Length > 4000 ? brokenResponse.Substring(0, 4000) : brokenResponse;
            var prompt =
                "You are a JSON repair assistant. The response below was meant to be a valid plan but was malformed or cut off.\n\n" +
                "BROKEN RESPONSE:\n" + broken + "\n\n" +
                "Fix it into EXACTLY this contract — keep the original intent; if a step was cut off mid-way, drop it rather than inventing args:\n" +
                "{\n" +
                "  \"message\": \"What you will do or your answer\",\n" +
                "  \"steps\": [\n" +
                "    {\"tool\": \"tool-key\", \"args\": {\"param\": \"value\"}}\n" +
                "  ]\n" +
                "}\n\n" +
                $"Valid tool keys: {toolKeys}\n" +
                "Return ONLY the repaired JSON — no commentary, no markdown fences.";

            try
            {
                return await GetOllamaResponseAsync(prompt, model, appUserId, workerId, cancellationToken, debugType: "plan-repair");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Plan-repair call failed");
                return null;
            }
        }

        /// <summary>
        /// Detects when a small model regurgitates our own prompt text instead
        /// of answering — sentinels from the planning and plan-repair prompts
        /// that can never legitimately appear in a user-facing response.
        /// </summary>
        private static bool LooksLikePromptEcho(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Contains("BROKEN RESPONSE")
                || text.Contains("EXACTLY this contract")
                || text.Contains("JSON repair assistant")
                || text.Contains("Return ONLY the repaired JSON")
                || text.Contains("planning assistant")
                || text.Contains("AVAILABLE TOOLS")
                || text.Contains("Conversation Context:");
        }

        /// <summary>
        /// The model's real context window, read "*.context_length" from
        /// /api/show. Drives both the context-usage meter and num_ctx —
        /// without num_ctx, Ollama silently clamps every request to 2048.
        /// Falls back to a sane default when /api/show doesn't report one.
        /// </summary>
        private async Task<int> ResolveModelWindowAsync(string model, CancellationToken cancellationToken)
        {
            if (_modelWindowCache.TryGetValue(model, out var cached))
                return cached;

            var window = Collector.Chat.Helpers.ContextBudget.DefaultBudget;
            try
            {
                var info = await _ollama.ShowModelAsync(model, cancellationToken);
                if (info?.Info?.ExtraInfo != null)
                {
                    long modelMax = 0;
                    foreach (var kv in info.Info.ExtraInfo)
                    {
                        if (!kv.Key.EndsWith("context_length", StringComparison.OrdinalIgnoreCase))
                            continue;
                        var v = kv.Value;
                        long n = v switch
                        {
                            long l => l,
                            int i => i,
                            double d => (long)d,
                            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.Number => je.GetInt64(),
                            _ => long.TryParse(v?.ToString(), out var p) ? p : 0
                        };
                        modelMax = Math.Max(modelMax, n);
                    }
                    if (modelMax > 0)
                        window = (int)Math.Min(modelMax, int.MaxValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: /api/show failed for {Model}, falling back to default window {Window}", model, window);
            }

            _logger.LogInformation("ChatWorker: Model '{Model}' context window = {Window} tokens", model, window);
            _modelWindowCache[model] = window;
            return window;
        }

        private async Task<string> GetOllamaResponseAsync(string prompt, string model, string appUserId, Guid workerId, CancellationToken cancellationToken, string? debugType = null, bool json = true)
        {
            var request = new OllamaSharp.Models.GenerateRequest
            {
                Model = model,
                System = "You are a helpful AI assistant.",
                Prompt = prompt,
                Stream = false,
                Format = json ? "json" : null,
                Options = new OllamaSharp.Models.RequestOptions
                {
                    NumCtx = await ResolveModelWindowAsync(model, cancellationToken)
                }
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
                // Bound the generation — a slow or incompatible model must never
                // leave the chat spinner running forever.
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(3));
                await foreach (var response in _ollama.GenerateAsync(request, timeoutCts.Token))
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
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new Exception($"Model '{model}' did not respond within 3 minutes. It may still be loading or may not support this request — try again, or switch to a different model.");
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
                // Add appUserId to first step's Args for tools to access
                if (executionPlan.Steps.Count > 0 && executionPlan.Steps[0].Args != null)
                {
                    executionPlan.Steps[0].Args["appUserId"] = appUserId;
                }
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

            // Shared data list for passing web-scraped content between tools
            // Each item represents a scraped web page with structured content
            var data = new List<Dictionary<string, List<string>>>();

            // Seed the session-entry marker from the chat's persisted target so
            // add-edit steps keep updating the same journal entry across sessions.
            try
            {
                var sessionChat = _chatsRepo.GetById(chatId);
                if (sessionChat?.JournalEntryId is Guid sessionEntryId && sessionEntryId != Guid.Empty)
                {
                    data.Add(new Dictionary<string, List<string>>
                    {
                        ["type"] = new List<string> { "session-entry" },
                        ["entryId"] = new List<string> { sessionEntryId.ToString() },
                        ["journalId"] = new List<string>()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to seed session entry marker");
                await SendWorkerExceptionAsync(appUserId, workerId, "session-marker", ex, cancellationToken);
            }

            // Restore research state (questions + asked queries) persisted by a
            // previous plan — steering a new plan keeps its coverage memory.
            try
            {
                var savedStates = _chatsRepo.GetRecentContextByMetadataKind(chatId, "research-state", 1);
                var stateJson = savedStates.FirstOrDefault().Content;
                if (!string.IsNullOrEmpty(stateJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(stateJson);
                    if (doc.RootElement.TryGetProperty("questions", out var q))
                    {
                        var list = q.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
                        if (list.Count > 0)
                            data.Add(new Dictionary<string, List<string>> { ["type"] = new List<string> { "research-questions" }, ["questions"] = list });
                    }
                    if (doc.RootElement.TryGetProperty("askedQueries", out var a))
                    {
                        var list = a.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
                        if (list.Count > 0)
                            data.Add(new Dictionary<string, List<string>> { ["type"] = new List<string> { "asked-queries" }, ["queries"] = list });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to restore research state");
                await SendWorkerExceptionAsync(appUserId, workerId, "research-state-restore", ex, cancellationToken);
            }

            // Collect step errors so the final summary can report honestly
            // instead of declaring success over failed tools.
            var toolErrors = new List<string>();
            var toolMessages = new List<string>();
            var lastStepTotal = executionPlan.Steps.Count;

            // Execute each step
            await _toolRegistry.ExecutePlan(
                executionPlan,
                userMessage,
                ragContext,
                data,
                (percent, message) =>
                {
                    // Send progress with replace flag so frontend updates existing progress label
                    _chatLog?.Log("PlanProgress", new { percent, message });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "PlanProgress",
                        new { percent, message, replace = true }, cancellationToken);
                },
                (tool, error, ex) =>
                {
                    toolErrors.Add($"{tool}: {error}");
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
                    _chatLog?.Log("ToolError", errorDetails);
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ToolError",
                        errorDetails, cancellationToken);
                },
                (tool, message) =>
                {
                    _chatLog?.Log("ToolComplete", new { tool, message });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ToolComplete",
                        new { tool, message }, cancellationToken);
                },
                Guid.Parse(appUserId),
                // Raw request callback - sends OllamaRequest event for debugging
                _environment.IsDevelopment() ? (type, prompt) =>
                {
                    _chatLog?.Log("OllamaRequest", new { type, prompt });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "OllamaRequest",
                        new { type, prompt, isDevelopment = true }, cancellationToken);
                } : null,
                // Raw response callback - sends OllamaResponse event for debugging
                _environment.IsDevelopment() ? (type, response) =>
                {
                    _chatLog?.Log("OllamaResponse", new { type, rawResponse = response });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "OllamaResponse",
                        new { type, rawResponse = response, isDevelopment = true }, cancellationToken);
                } : null,
                // OnSaveChatHistory callback - extracts facts immediately when web-scrape provides context
                async (context) =>
                {
                    await ExtractAndStoreFactsAsync(appUserId, workerId, chatId, Guid.Parse(userMessageId), userMessage, context, cancellationToken);
                },
                // SendMessage callback - sends message with optional link to chat window.
                // Texts are also collected so the final summary knows what the
                // tools actually told the user (they don't land in `data`).
                (message, linkUrl, linkText) =>
                {
                    if (!string.IsNullOrWhiteSpace(message)) toolMessages.Add(message);
                    _chatLog?.Log("ChatMessage", new { message, linkUrl, linkText });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ChatMessage",
                        new { message, linkUrl, linkText }, cancellationToken);
                },
                // Step-start callback - ToolStep event for browser-console debugging
                _environment.IsDevelopment() ? (stepNumber, stepTotal, step) =>
                {
                    if (stepTotal != lastStepTotal)
                    {
                        lastStepTotal = stepTotal;
                        ChatDetailLog.Line($"plan changed: now {stepTotal} step(s)");
                    }
                    ChatDetailLog.Line($"tool {stepNumber}/{stepTotal}: {step.Tool}");
                    _chatLog?.Log("ToolStep", new { step = stepNumber, total = stepTotal, tool = step.Tool, args = step.Args });
                    _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, "ToolStep",
                        new { step = stepNumber, total = stepTotal, tool = step.Tool, args = step.Args, isDevelopment = true }, cancellationToken);
                } : null,
                cancellationToken
            );

            // Persist the journal entry the plan worked on so future messages in
            // this session target the same entry (conversational editing).
            try
            {
                var producedEntryId = executionPlan.Steps
                    .Select(s => s.Result != null && s.Result.TryGetValue("entryId", out var v) ? v?.ToString() : null)
                    .LastOrDefault(v => Guid.TryParse(v, out _));
                if (Guid.TryParse(producedEntryId, out var sessionEntryId))
                {
                    _chatsRepo.SetJournalEntryId(chatId, sessionEntryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to persist session entry id");
                await SendWorkerExceptionAsync(appUserId, workerId, "session-entry-persist", ex, cancellationToken);
            }

            // Persist research coverage state so a steered/follow-up plan resumes
            // with the same question set and never re-asks answered queries.
            try
            {
                var questions = Collector.Chat.Tools.ResearchReviewTool.GetList(data, "research-questions", "questions");
                var asked = Collector.Chat.Tools.ResearchReviewTool.GetList(data, "asked-queries", "queries");
                if (questions.Count > 0 || asked.Count > 0)
                {
                    var stateJson = JsonSerializer.Serialize(new { questions, askedQueries = asked });
                    var embedding = await GetEmbeddingAsync(new List<string> { string.Join(" ", questions.Concat(asked).Take(6)) });
                    var meta = JsonSerializer.Serialize(new { kind = "research-state", chatId, timestamp = DateTime.UtcNow });
                    _chatsRepo.StoreContext(Guid.Parse(appUserId), chatId, stateJson, embedding, meta);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to persist research state");
                await SendWorkerExceptionAsync(appUserId, workerId, "research-state-persist", ex, cancellationToken);
            }

            // Send completion message
            await SendWorkerMessage(appUserId, workerId, "PlanCompleted", new
            {
                message = $"Completed execution of plan: {plan.Message}"
            }, cancellationToken);

            // Generate final summary with Ollama — include what the tools
            // already told the user so it doesn't claim "no data" when the
            // answer was delivered as chat messages rather than scraped data.
            var dataSummary = data.Count > 0
                ? $"{data.Count} web page(s) scraped"
                : "none (this plan did not involve web scraping)";
            var messagesSummary = toolMessages.Count > 0
                ? $"Information already reported to the user during execution:\n- {string.Join("\n- ", toolMessages.Take(8))}\n\n"
                : "";
            var errorsSummary = toolErrors.Count > 0
                ? $"However, {toolErrors.Count} step(s) reported errors:\n- {string.Join("\n- ", toolErrors)}\n\n"
                : "";
            var summaryPrompt = $"The following plan was just executed:\n{plan.Message}\n\n" +
                               $"Available data from execution: {dataSummary}\n\n" +
                               messagesSummary +
                               errorsSummary +
                               $"Provide a brief, honest summary to the user about what was accomplished. " +
                               $"Only mention scraping or research if scraping actually happened" +
                               (toolErrors.Count > 0 ? "; also report what went wrong." : ".");
            var summary = await GetOllamaResponseAsync(summaryPrompt, model, appUserId, workerId, cancellationToken, json: false);
            await SaveAndSendAssistantMessage(appUserId, workerId, chatId, summary, model, cancellationToken);
        }

        private async Task SendDirectAnswerAsync(string appUserId, Guid workerId, Guid chatId,
            string responseMessage, string model, CancellationToken cancellationToken)
        {
            // If using remote LLM for content generation, we would call it here
            // For now, use Ollama for the actual response
            await SaveAndSendAssistantMessage(appUserId, workerId, chatId, responseMessage, model, cancellationToken);
        }

        private static string TruncateForContext(string? content, int maxChars)
        {
            if (string.IsNullOrEmpty(content)) return "";
            return content.Length <= maxChars ? content : content.Substring(0, maxChars) + "…";
        }

        /// <summary>
        /// Forward a swallowed exception to the browser console — same dev-gated
        /// channel as the raw Ollama request/response debug events. Non-fatal
        /// catches stay silent in production but are visible while developing.
        /// </summary>
        private Task SendWorkerExceptionAsync(string appUserId, Guid workerId, string context, Exception ex, CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment()) return Task.CompletedTask;
            return SendWorkerMessage(appUserId, workerId, "ServerException", new
            {
                context,
                message = ex.Message,
                type = ex.GetType().Name,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException != null ? new
                {
                    message = ex.InnerException.Message,
                    type = ex.InnerException.GetType().Name
                } : null,
                isDevelopment = true
            }, cancellationToken);
        }

        /// <summary>
        /// Broadcast a journal-entry update so open viewers refetch and merge
        /// the new module tree while the research agent keeps working.
        /// </summary>
        private Task BroadcastEntryUpdated(Guid appUserId, Guid entryId, int journalId, List<string>? ops = null)
        {
            return _hubContext.Clients.All.SendAsync("JournalEntryUpdated", new
            {
                appUserId,
                entryId,
                journalId,
                ops = ops ?? new List<string>()
            });
        }

                /// <summary>
        /// Auto-compaction: when the estimated prompt crosses the threshold of
        /// the model's context window, fold all but the last few turns into the
        /// persisted running summary. Checked at message start only — never
        /// mid-plan. The summarize call can't recurse into planning, so no
        /// explicit recursion guard is needed.
        /// </summary>
        private async Task MaybeCompactSessionAsync(
            Collector.Data.Entities.Chat chat,
            List<Collector.Data.Entities.ChatHistory> history,
            string model, int contextBudget,
            string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            const int keepTail = 8;

            var uncompacted = history
                .Where(m => !chat.ContextSummarizedThrough.HasValue || m.Created > chat.ContextSummarizedThrough.Value)
                .ToList();
            if (uncompacted.Count == 0) return;

            var usedTokens = Collector.Chat.Helpers.ContextBudget.PromptOverheadTokens
                + uncompacted.Sum(m => Collector.Chat.Helpers.ContextBudget.EstimateTokens(m.Content))
                + Collector.Chat.Helpers.ContextBudget.EstimateTokens(chat.ContextSummary);
            if (!Collector.Chat.Helpers.ContextBudget.ShouldCompact(usedTokens, contextBudget)) return;
            if (!Collector.Chat.Helpers.ContextBudget.TryGetCompactionSplit(uncompacted, keepTail, out var head, out var tail)) return;

            await SendWorkerMessage(appUserId, workerId, "PlanProgress", new
            {
                percent = 4,
                message = $"Context is {(int)Math.Round(100.0 * usedTokens / contextBudget)}% full — compacting older conversation..."
            }, cancellationToken);

            var olderTurns = string.Join("\n", head.Select(m =>
                $"{(m.Role == 0 ? "User" : "Assistant")}: {TruncateForContext(m.Content, 300)}"));
            var foldInput = (string.IsNullOrWhiteSpace(chat.ContextSummary)
                ? ""
                : "Prior summary (keep its key facts, merge in the new material):\n" + chat.ContextSummary + "\n\n")
                + "Conversation turns to fold into the summary:\n" + olderTurns;
            var newSummary = await SummarizeContextAsync(
                "Summarize this conversation so far for continued work",
                foldInput, model, appUserId, workerId, cancellationToken);
            if (string.IsNullOrWhiteSpace(newSummary)) return;

            var through = head.Last().Created;
            var tailTokens = tail.Sum(m => Collector.Chat.Helpers.ContextBudget.EstimateTokens(m.Content));
            var newEstimate = Collector.Chat.Helpers.ContextBudget.PromptOverheadTokens
                + tailTokens + Collector.Chat.Helpers.ContextBudget.EstimateTokens(newSummary);
            _chatsRepo.SetContextCompaction(chat.Id, newSummary, through, newEstimate);
            chat.ContextSummary = newSummary;
            chat.ContextSummarizedThrough = through;
            chat.ContextTokens = newEstimate;
        }

        /// <summary>
        /// Build the session's prompt context purely from this chat: running
        /// summary (compacted older turns) + verbatim tail. Long messages beyond
        /// the last few turns collapse to a reference line — tool-result clearing.
        /// </summary>
        private static string BuildSessionContext(
            Collector.Data.Entities.Chat chat,
            List<Collector.Data.Entities.ChatHistory> history)
        {
            var verbatim = history
                .Where(m => !chat.ContextSummarizedThrough.HasValue || m.Created > chat.ContextSummarizedThrough.Value)
                .ToList();

            var turns = verbatim.Select((m, i) =>
            {
                var label = m.Role == 0 ? "User" : "Assistant";
                var len = m.Content?.Length ?? 0;
                // tool-result clearing: long older messages reduce to a reference
                var body = (i < verbatim.Count - 4 && len > 1200)
                    ? $"[earlier message — {len} chars]"
                    : TruncateForContext(m.Content, 600);
                return $"{label}: {body}";
            });

            return Collector.Chat.Helpers.ContextBudget.ComposeContext(chat.ContextSummary, turns);
        }

private async Task<string> SummarizeContextAsync(string userMessage, string rawContext, string model, string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            try
            {
                var systemPrompt = "You are a helpful assistant that summarizes conversation context. Given a user's question and relevant context from previous conversations, create a concise summary (1-3 paragraphs) of the conversation context that captures the key information that can be used later. Focus on facts, preferences, and relevant history. Be concise but informative.";
                var userPrompt = $"User Question: {userMessage}\n\nRelevant Context from previous conversations:\n{rawContext}\n\nPlease summarize the key information from the context that would help answer the user's question:";
                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = model,
                    System = systemPrompt,
                    Prompt = userPrompt,
                    Stream = false
                };
                var responseContent = "";

                // Bounded: a hanging model must fall back to raw context, not
                // stall the whole plan before "Planning" is ever emitted.
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
                await foreach (var resp in _ollama.GenerateAsync(request, timeoutCts.Token))
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
                await SendWorkerExceptionAsync(appUserId, workerId, "context-summarization", ex, cancellationToken);
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

        private async Task ExtractAndStoreFactsAsync(string appUserId, Guid workerId, Guid chatId, Guid userMessageId,
            string userMessage, string assistantMessage, CancellationToken cancellationToken)
        {
            try
            {
                var userGuid = Guid.Parse(appUserId);
                var notes = await ExtractFactsFromConversationAsync(userMessage, assistantMessage, appUserId, workerId, cancellationToken);

                if (notes != null && notes.Count > 0)
                {
                    int storedCount = 0;
                    int duplicateCount = 0;
                    foreach (var note in notes)
                    {
                        var noteEmbedding = await GetEmbeddingAsync(new List<string> { note.Content }, cancellationToken);
                        if (!_chatsRepo.HasSimilarContextForChat(chatId, noteEmbedding, similarityThreshold: 0.1f))
                        {
                            var noteMetadata = JsonSerializer.Serialize(new
                            {
                                kind = "note",
                                type = "note",
                                noteType = note.Type,
                                timestamp = DateTime.UtcNow,
                                chatId = chatId,
                                userMessageId = userMessageId
                            });
                            _chatsRepo.StoreContext(userGuid, chatId, note.Content, noteEmbedding, noteMetadata);
                            storedCount++;
                        }
                        else
                        {
                            duplicateCount++;
                        }
                    }
                    _logger.LogInformation("ChatWorker: Extracted {TotalNotes} memory notes, stored {StoredCount} new for user {AppUserId}",
                        notes.Count, storedCount, appUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to extract and store facts for user {AppUserId}", appUserId);
                await SendWorkerExceptionAsync(appUserId, workerId, "fact-extraction", ex, cancellationToken);
            }
        }

        private Task SendWorkerMessage(string appUserId, Guid workerId, string eventName, object payload, CancellationToken cancellationToken)
        {
            _chatLog?.Log(eventName, payload);
            return _hubContext.Clients.All.SendAsync("WorkerProgress", appUserId, workerId, eventName, payload, cancellationToken);
        }

        private async Task<float[]> GetEmbeddingAsync(List<string> text, CancellationToken cancellationToken = default)
        {
            // Bounded — an Ollama embed call that hangs must never hold the
            // per-chat lock hostage (it runs inside the post-response path).
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            var response = await _ollama.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = text
            }, timeoutCts.Token);

            return response.Embeddings[0];
        }

        /// <summary>
        /// A typed memory note extracted from a conversation turn — carries a
        /// noteType so retrieval can weigh decisions/preferences over raw facts.
        /// </summary>
        private class MemoryNote
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string Type { get; set; } = "fact";
            [System.Text.Json.Serialization.JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }

        private async Task<List<MemoryNote>> ExtractFactsFromConversationAsync(string userMessage, string assistantMessage, string appUserId, Guid workerId, CancellationToken cancellationToken)
        {
            try
            {
                var systemPrompt = @"You are a memory extraction system. Extract durable notes from conversations as a JSON array of typed objects.
Rules:
- Each note has ""type"" (one of: decision, question, preference, state, fact) and ""content"" (a complete, standalone statement)
- decision: something the user or assistant decided to do
- question: an unresolved question the user asked that matters later
- preference: a user preference (style, format, tools, approach)
- state: current working state (e.g. which entry/project is being worked on)
- fact: any other factual information worth remembering
- Extract 0-6 notes; ignore greetings and conversational filler
- Return ONLY a JSON array
Example output:
[{""type"":""preference"",""content"":""The user prefers dark mode""},{""type"":""state"",""content"":""The user is researching the history of PostgreSQL in a journal entry""}]";
                var conversationPrompt = $@"User said: {userMessage}
Assistant replied: {assistantMessage}
Extract facts from this conversation:";
                // Get active model from database, fallback to config
                string activeModel = AI.Ollama.Config.Model;
                
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

                // Bounded: fact extraction happens while holding the chat lock —
                // a hang here silently queues every subsequent message forever.
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));
                await foreach (var response in _ollama.GenerateAsync(request, timeoutCts.Token))
                {
                    if (response?.Response != null)
                    {
                        responseContent += response.Response;
                    }
                }
                
                return ParseMemoryNotes(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatWorker: Failed to extract facts from conversation");
                await SendWorkerExceptionAsync(appUserId, workerId, "fact-extraction", ex, cancellationToken);
                return new List<MemoryNote>();
            }
        }

        /// <summary>
        /// Shape-tolerant memory-note parser. Models return every variant:
        /// a bare array of strings, an array of note objects, or an object
        /// wrapping either under a key like "notes"/"facts". Normalizes all of
        /// them via JsonNode so a model can't wedge extraction with odd JSON.
        /// </summary>
        private static List<MemoryNote> ParseMemoryNotes(string raw)
        {
            try
            {
                // strip prose around the JSON before parsing
                var json = Collector.Chat.Helpers.LLMJson.Extract(raw) ?? raw?.Trim() ?? "";
                var node = System.Text.Json.Nodes.JsonNode.Parse(json);
                if (node == null) return new List<MemoryNote>();

                // unwrap objects: find the first array-valued property
                if (node is System.Text.Json.Nodes.JsonObject obj)
                {
                    var arrayProp = obj.FirstOrDefault(kv => kv.Value is System.Text.Json.Nodes.JsonArray).Value;
                    node = arrayProp as System.Text.Json.Nodes.JsonNode
                        ?? obj.FirstOrDefault(kv => kv.Value is System.Text.Json.Nodes.JsonValue).Value;
                }
                if (node == null) return new List<MemoryNote>();

                var items = new List<MemoryNote>();
                if (node is System.Text.Json.Nodes.JsonArray arr)
                {
                    foreach (var item in arr)
                    {
                        var note = ToMemoryNote(item);
                        if (note != null) items.Add(note);
                    }
                }
                else
                {
                    var note = ToMemoryNote(node);
                    if (note != null) items.Add(note);
                }
                return items.Take(6).ToList();
            }
            catch (System.Text.Json.JsonException)
            {
                // not JSON at all — treat the raw line(s) as a single fact note
                var text = raw?.Trim() ?? "";
                if (string.IsNullOrEmpty(text) || text.Length > 400) return new List<MemoryNote>();
                return new List<MemoryNote> { new MemoryNote { Type = "fact", Content = text } };
            }
        }

        private static MemoryNote? ToMemoryNote(System.Text.Json.Nodes.JsonNode? item)
        {
            if (item == null) return null;
            if (item is System.Text.Json.Nodes.JsonValue v && v.TryGetValue<string>(out var s))
            {
                return string.IsNullOrWhiteSpace(s) ? null : new MemoryNote { Type = "fact", Content = s.Trim() };
            }
            if (item is System.Text.Json.Nodes.JsonObject o)
            {
                var type = o["type"]?.GetValue<string>() ?? "fact";
                // models emit "content", "note", "fact", or "text" for the body
                var content = o["content"]?.GetValue<string>()
                    ?? o["note"]?.GetValue<string>()
                    ?? o["fact"]?.GetValue<string>()
                    ?? o["text"]?.GetValue<string>();
                return string.IsNullOrWhiteSpace(content) ? null
                    : new MemoryNote { Type = type, Content = content.Trim() };
            }
            return null;
        }
    }
}
