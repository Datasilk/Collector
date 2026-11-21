using Collector.Chat.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Collector.Chat.Models;
using Collector.Data.Entities;
using Collector.Data.Interfaces;
using OllamaSharp;
using OllamaSharp.Models;

namespace Collector.Chat.Tools;

/// <summary>
/// Manages Ollama models for Collector: list locally installed models, report
/// which model is active, switch the active model, and download new models
/// (streaming pull progress back to the chat as a progress bar).
/// </summary>
public class ManageOllamaModelsTool : IChatTool
{
    private readonly OllamaApiClient _ollama;
    private readonly IOllamaModelsRepository _ollamaModelsRepo;
    private readonly ILLMHtmlParsersRepository? _htmlParsersRepo;

    public ManageOllamaModelsTool(OllamaApiClient ollama, IOllamaModelsRepository ollamaModelsRepo, ILLMHtmlParsersRepository? htmlParsersRepo = null)
    {
        _ollama = ollama;
        _ollamaModelsRepo = ollamaModelsRepo;
        _htmlParsersRepo = htmlParsersRepo;
    }

    public string ToolKey => "manage-ollama-models";

    public string Description => "Manage Ollama models: list installed models, show which model is active, switch the active model, download a new model, or manage HTML parser models. Use when the user asks about models (\"what models do I have\", \"which model is active\"), asks to run/use/switch to a model (\"ollama run <model>\", \"use <model>\", \"switch model to <model>\"), asks to download/install/pull a model, or asks to add/list/activate an html parser model (stored in LLM_HtmlParsers).";

    public bool AvailableToOllama => true;

    public List<ToolParameter> Parameters => new()
    {
        new ToolParameter
        {
            Key = "action",
            DataType = "string",
            Description = "one of: \"list\" (show installed models), \"active\" (show current model), \"switch\" (activate a model, downloading if needed), \"download\" (pull a model without switching). If omitted, inferred: a model name means switch; no model means list."
        },
        new ToolParameter
        {
            Key = "model",
            DataType = "string",
            Description = "the Ollama model reference, e.g. \"llama3.2\", \"llama3.2:latest\" or \"org/model-name\" — required for switch/download"
        }
    };

    public List<ToolResponseField> ResponseFields => new()
    {
        new ToolResponseField { Key = "action", DataType = "string" },
        new ToolResponseField { Key = "model", DataType = "string" },
        new ToolResponseField { Key = "models", DataType = "array" }
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
        SendMessage? sendMessage = null,
        CancellationToken cancellationToken = default)
    {
        ChatDetailLog.Fn("manage-ollama-models", "Run");
        try
        {
            var args = plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count
                ? plan.Steps[currentStepIndex].Args
                : null;
            var action = args?.GetValueOrDefault("action", "")?.ToString()?.Trim().ToLowerInvariant() ?? "";
            var model = CleanModelToken(args?.GetValueOrDefault("model", "")?.ToString());

            // "add a new html parser model" targets LLM_HtmlParsers, not OllamaModels
            var isParser = IsHtmlParserRequest(userMessage, args);

            // Infer missing pieces from the user's phrasing
            if (string.IsNullOrWhiteSpace(model))
                model = ParseModelFromMessage(userMessage);
            if (string.IsNullOrWhiteSpace(action))
                action = InferAction(userMessage, model);
            else
                action = NormalizeAction(action, model);

            // Everything below needs Ollama — reach it once up front
            List<Model> localModels;
            try
            {
                localModels = (await _ollama.ListLocalModelsAsync()).ToList();
            }
            catch (Exception ex)
            {
                onError($"Could not reach Ollama to list models: {ex.Message}", ex);
                return;
            }

            if (isParser)
            {
                await HandleParserRequestAsync(action, model, localModels,
                    onProgress, onError, onComplete, sendMessage);
                return;
            }

            switch (action)
            {
                case "list":
                    ReportModels(localModels, sendMessage, onComplete);
                    break;

                case "active":
                case "status":
                case "current":
                    ReportActive(localModels, sendMessage, onComplete);
                    break;

                case "download":
                case "pull":
                case "install":
                    await DownloadAsync(model, localModels, onProgress, onError, onComplete, sendMessage, activate: false);
                    break;

                case "switch":
                case "activate":
                case "use":
                case "run":
                    await SwitchAsync(model, localModels, onProgress, onError, onComplete, sendMessage, plan, currentStepIndex);
                    break;

                default:
                    onError($"Unknown action '{action}'. Valid actions: list, active, switch, download.", null);
                    break;
            }
        }
        catch (Exception ex)
        {
            onError($"Failed to manage Ollama models: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// "html parser model" phrasing routes model management to the
    /// LLM_HtmlParsers table instead of OllamaModels.
    /// </summary>
    private static bool IsHtmlParserRequest(string? userMessage, Dictionary<string, object>? args)
    {
        var msg = userMessage ?? "";
        if (Regex.IsMatch(msg, @"html\s*parser", RegexOptions.IgnoreCase)) return true;
        if (args != null)
            foreach (var kv in args)
                if (kv.Value != null && Regex.IsMatch(kv.Value.ToString()!, @"html\s*parser", RegexOptions.IgnoreCase))
                    return true;
        return false;
    }

    /// <summary>Add/activate/list/delete rows in LLM_HtmlParsers.</summary>
    private async Task HandleParserRequestAsync(
        string action, string? model, List<Model> localModels,
        OnProgress onProgress, OnError onError, OnComplete onComplete, SendMessage? sendMessage)
    {
        if (_htmlParsersRepo == null)
        {
            onError("The HTML parser model table isn't configured on this chat session.", null);
            return;
        }

        switch (action)
        {
            case "list":
            {
                var rows = _htmlParsersRepo.GetAll();
                if (rows.Count == 0)
                {
                    sendMessage?.Invoke("No HTML parser models are configured yet.", null, null);
                    onComplete("No HTML parser models configured");
                    return;
                }
                var sb = new StringBuilder("HTML parser models:");
                foreach (var m in rows)
                    sb.Append($"\n- {m.Id} ({m.Name}){(m.Status == 1 ? "  ← active" : "")}");
                sendMessage?.Invoke(sb.ToString(), null, null);
                onComplete($"Listed {rows.Count} HTML parser model(s)");
                return;
            }

            case "active":
            case "status":
            case "current":
            {
                var active = _htmlParsersRepo.GetActive();
                sendMessage?.Invoke(
                    active != null
                        ? $"The active HTML parser model is '{active.Id}'."
                        : "No HTML parser model is marked active.",
                    null, null);
                onComplete(active != null ? $"Active HTML parser model: {active.Id}" : "No active HTML parser model");
                return;
            }

            case "download":
            case "pull":
            case "install":
            case "switch":
            case "activate":
            case "use":
            case "run":
            case "add":
            {
                if (string.IsNullOrWhiteSpace(model))
                {
                    onError("No model name was provided. Ask the user which HTML parser model to add.", null);
                    return;
                }

                var activate = action is "switch" or "activate" or "use" or "run";

                if (!IsDownloaded(localModels, model))
                {
                    sendMessage?.Invoke($"Downloading Ollama model '{model}' — this may take a while…", null, null);
                    if (!await PullModelAsync(model, onProgress, sendMessage))
                    {
                        onError($"Failed to download model '{model}' from Ollama.", null);
                        return;
                    }
                }

                if (_htmlParsersRepo.GetById(model) == null)
                    _htmlParsersRepo.Add(new LLMHtmlParser { Id = model, Name = model, Status = 0 });

                if (activate)
                {
                    _htmlParsersRepo.SetActive(model);
                    sendMessage?.Invoke($"'{model}' is now the active HTML parser model.", null, null);
                    onComplete($"Active HTML parser model set to '{model}'");
                }
                else
                {
                    sendMessage?.Invoke($"Added '{model}' as an HTML parser model.", null, null);
                    onComplete($"Added HTML parser model '{model}'");
                }
                return;
            }

            default:
                onError($"Unknown action '{action}'. Valid actions: list, active, switch/add, download.", null);
                return;
        }
    }

    /// <summary>List every model installed in Ollama, marking the active one.</summary>
    private void ReportModels(List<Model> localModels, SendMessage? sendMessage, OnComplete onComplete)
    {
        var activeId = GetActiveId();
        if (localModels.Count == 0)
        {
            sendMessage?.Invoke("No models are installed in Ollama yet.", null, null);
            onComplete("No local Ollama models found");
            return;
        }

        var sb = new StringBuilder($"Ollama has {localModels.Count} model(s) installed:");
        foreach (var m in localModels.OrderBy(m => m.Name))
        {
            var isActive = m.Name == activeId || m.Name == activeId + ":latest";
            sb.Append($"\n- {m.Name}{(isActive ? "  ← active" : "")}");
        }
        if (string.IsNullOrWhiteSpace(activeId))
            sb.Append("\n\nNo model is marked active — Collector falls back to the configured default.");

        sendMessage?.Invoke(sb.ToString(), null, null);
        onComplete($"Listed {localModels.Count} installed model(s)");
    }

    /// <summary>Report which model Collector is currently using.</summary>
    private void ReportActive(List<Model> localModels, SendMessage? sendMessage, OnComplete onComplete)
    {
        var activeId = GetActiveId();
        if (!string.IsNullOrWhiteSpace(activeId))
        {
            var installed = localModels.Any(m => m.Name == activeId || m.Name == activeId + ":latest"
                || (!activeId.Contains(':') && m.Name.StartsWith(activeId + ":")));
            sendMessage?.Invoke(
                installed
                    ? $"The active model is '{activeId}'."
                    : $"The active model is set to '{activeId}' but it isn't installed in Ollama.",
                null, null);
            onComplete($"Active model: {activeId}");
        }
        else
        {
            sendMessage?.Invoke($"No model is marked active in Collector — using the configured default '{AI.Ollama.Config.Model}'.", null, null);
            onComplete($"No active model set; default is {AI.Ollama.Config.Model}");
        }
    }

    /// <summary>Download a model without changing which one is active.</summary>
    private async Task DownloadAsync(
        string? model, List<Model> localModels,
        OnProgress onProgress, OnError onError, OnComplete onComplete, SendMessage? sendMessage,
        bool activate)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            onError("No model name was provided. Ask the user which model to download.", null);
            return;
        }

        if (IsDownloaded(localModels, model))
        {
            sendMessage?.Invoke($"'{model}' is already downloaded.", null, null);
            onComplete($"'{model}' already installed");
            return;
        }

        sendMessage?.Invoke($"Downloading Ollama model '{model}' — this may take a while…", null, null);
        if (!await PullModelAsync(model, onProgress, sendMessage))
        {
            onError($"Failed to download model '{model}' from Ollama.", null);
            return;
        }

        // Register it in Collector's model table so it shows in admin/active list
        UpsertModelRow(model);
        onProgress(100, $"Downloaded '{model}'");
        sendMessage?.Invoke($"Finished downloading '{model}'.", null, null);
        onComplete($"Downloaded '{model}'");
    }

    /// <summary>Make a model the active one — downloading it first if needed.</summary>
    private async Task SwitchAsync(
        string? model, List<Model> localModels,
        OnProgress onProgress, OnError onError, OnComplete onComplete, SendMessage? sendMessage,
        ExecutionPlan? plan, int currentStepIndex)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            onError("No model name was provided. Ask the user which model they want to use.", null);
            return;
        }

        var alreadyDownloaded = IsDownloaded(localModels, model);
        if (!alreadyDownloaded)
        {
            sendMessage?.Invoke($"Downloading Ollama model '{model}' — this may take a while…", null, null);
            if (!await PullModelAsync(model, onProgress, sendMessage))
            {
                onError($"Failed to download model '{model}' from Ollama.", null);
                return;
            }
        }

        UpsertModelRow(model);
        _ollamaModelsRepo.SetActive(model);

        if (plan != null && currentStepIndex >= 0 && currentStepIndex < plan.Steps.Count)
        {
            plan.Steps[currentStepIndex].Result ??= new Dictionary<string, object>();
            plan.Steps[currentStepIndex].Result["model"] = model;
            plan.Steps[currentStepIndex].Result["downloaded"] = alreadyDownloaded ? "false" : "true";
        }

        onProgress(100, $"Active model is now '{model}'");
        sendMessage?.Invoke(
            alreadyDownloaded
                ? $"'{model}' is already downloaded — it's now the active model for Collector."
                : $"Finished downloading '{model}' — it's now the active model for Collector.",
            null, null);
        onComplete($"Active Ollama model switched to '{model}'");
    }

    private string? GetActiveId()
    {
        try { return _ollamaModelsRepo.GetActive()?.Id; }
        catch { return null; }
    }

    private void UpsertModelRow(string model)
    {
        if (_ollamaModelsRepo.GetById(model) == null)
        {
            _ollamaModelsRepo.Add(new OllamaModel { Id = model, Name = model, Status = 0 });
        }
    }

    private static bool IsDownloaded(List<Model> localModels, string model) =>
        localModels.Any(m => m.Name == model || m.Name == model + ":latest" ||
            (!model.Contains(':') && m.Name.StartsWith(model + ":")));

    /// <summary>
    /// Streams `ollama pull` status into onProgress so the chat window shows a
    /// live download bar. Returns false if the pull reports an error.
    /// </summary>
    private async Task<bool> PullModelAsync(string model, OnProgress onProgress, SendMessage? sendMessage)
    {
        try
        {
            var request = new PullModelRequest { Model = model };
            var lastStatus = "";
            var lastPercent = -1;

            await foreach (var status in _ollama.PullModelAsync(request))
            {
                if (status == null) continue;

                var pct = (int)Math.Round(status.Percent);
                var text = status.Status ?? "";

                if (pct > 0 && pct != lastPercent)
                {
                    lastPercent = pct;
                    onProgress(Math.Min(pct, 99), $"Downloading '{model}' — {text}");
                }
                else if (!string.IsNullOrWhiteSpace(text) && text != lastStatus && pct <= 0)
                {
                    onProgress(5, $"Downloading '{model}' — {text}");
                }
                lastStatus = text;
            }
            return true;
        }
        catch (Exception ex)
        {
            sendMessage?.Invoke($"Download of '{model}' failed: {ex.Message}", null, null);
            return false;
        }
    }

    /// <summary>
    /// Small planners invent action strings ("show_active_model",
    /// "get_available_models"). Substring-match them onto the four real
    /// actions instead of erroring; unknowns fall back to InferAction.
    /// </summary>
    private static string NormalizeAction(string action, string? model)
    {
        var a = action.ToLowerInvariant();
        // switch-verbs beat "active" when a model is named — "set_active_model X" means switch, not status
        var hasModel = !string.IsNullOrWhiteSpace(model);
        if (hasModel && (a.Contains("switch") || a.Contains("use") || a.Contains("run") || a.Contains("set") || a.Contains("make") || a.Contains("activat")))
            return "switch";
        if (a.Contains("active") || a.Contains("current") || a.Contains("status") || a.Contains("which"))
            return "active";
        if (a.Contains("list") || a.Contains("available") || a.Contains("installed") || a.Contains("all"))
            return "list";
        if (a.Contains("download") || a.Contains("pull") || a.Contains("install") || a.Contains("get"))
            return "download";
        if (a.Contains("switch") || a.Contains("activat") || a.Contains("use") || a.Contains("run") || a.Contains("set"))
            return "switch";
        return InferAction("", model);
    }

    /// <summary>Guess the action from phrasing when the planner omits it.</summary>
    private static string InferAction(string userMessage, string? model)
    {
        var msg = (userMessage ?? "").ToLowerInvariant();
        if (Regex.IsMatch(msg, @"\b(list|what|which|show|available|installed|have)\b.*\bmodels?\b") && !msg.Contains("active") && string.IsNullOrWhiteSpace(model))
            return "list";
        if (Regex.IsMatch(msg, @"\b(which|what|current|active)\b.*\bmodel\b") && string.IsNullOrWhiteSpace(model))
            return "active";
        if (Regex.IsMatch(msg, @"\b(download|install|pull)\b") && !string.IsNullOrWhiteSpace(model)
            && !Regex.IsMatch(msg, @"\b(use|switch|active|run)\b"))
            return "download";
        if (!string.IsNullOrWhiteSpace(model))
            return "switch";
        return "list";
    }

    /// <summary>
    /// Extract a model reference from the message: "ollama run/pull X",
    /// "use model X", "switch to X", or a bare name/name:tag/org/name token.
    /// </summary>
    private static string? ParseModelFromMessage(string userMessage)
    {
        var m = Regex.Match(userMessage ?? "",
            @"(?:ollama\s+(?:run|pull|use|install)|model\s+(?:to|called|named)?\s*[:=]?\s*|switch\s+to\s+|install\s+|use\s+|download\s+)['""]?([a-zA-Z0-9][a-zA-Z0-9._-]*(?:/[a-zA-Z0-9._-]+)*(?::[a-zA-Z0-9._-]+)?)['""]?",
            RegexOptions.IgnoreCase);
        if (m.Success && !IsFillerWord(m.Groups[1].Value)) return m.Groups[1].Value;
        return null;
    }

    /// <summary>Words that pattern-match like model refs but aren't (e.g. "use the model").</summary>
    private static bool IsFillerWord(string token) =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "the", "a", "an", "model", "models", "ollama", "active", "current", "list", "all", "installed", "available" }
            .Contains(token);

    private static string? CleanModelToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Trim('"', '\'', '`');
        var m = Regex.Match(s, @"(?:ollama\s+(?:run|pull|use|install)\s+)?([a-zA-Z0-9][a-zA-Z0-9._-]*(?:/[a-zA-Z0-9._-]+)*(?::[a-zA-Z0-9._-]+)?)");
        return m.Success && !IsFillerWord(m.Groups[1].Value) ? m.Groups[1].Value : null;
    }
}
