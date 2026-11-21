using System;
using System.Threading;
using System.Threading.Tasks;
using Collector.AI;
using OllamaSharp;

namespace Collector.Chat.Helpers;

/// <summary>
/// Hybrid LLM client. Routes quick decisions (planning, relevance checks,
/// titles, selections) to the local Ollama model, and complex generation
/// (journal-entry module JSON, long-form synthesis, entry edits) to the
/// configured remote OpenAI-compatible endpoint — with graceful fallback
/// to local when no remote endpoint is configured or reachable.
/// </summary>
public class LLMClient
{
    private readonly OllamaApiClient _ollama;
    private readonly string _ollamaModel;

    public LLMClient(OllamaApiClient ollama, string ollamaModel)
    {
        _ollama = ollama;
        _ollamaModel = ollamaModel;
    }

    /// <summary>
    /// Whether a remote OpenAI-compatible endpoint is configured and usable.
    /// </summary>
    public static bool RemoteAvailable =>
        OpenAI.Available != null && OpenAI.Available.Count > 0 && OpenAI.PreferredModel > 0;

    /// <summary>
    /// Local Ollama generation — fast path for planning, checks and selections.
    /// </summary>
    public async Task<string> LocalAsync(
        string system,
        string prompt,
        bool json = false,
        CancellationToken cancellationToken = default,
        Action<string, string>? onRequest = null,
        Action<string, string>? onResponse = null,
        string? debugType = null,
        string? model = null,
        OllamaSharp.Models.RequestOptions? options = null)
    {
        var request = new OllamaSharp.Models.GenerateRequest
        {
            Model = model ?? _ollamaModel,
            System = system ?? "You are a helpful AI assistant.",
            Prompt = prompt,
            Stream = false,
            Format = json ? "json" : null,
            Options = options
        };

        if (debugType != null) onRequest?.Invoke(debugType, $"System:\n{request.System}\n\nUser:\n{prompt}");

        var responseContent = "";
        await foreach (var response in _ollama.GenerateAsync(request, cancellationToken))
        {
            if (response?.Response != null)
            {
                responseContent += response.Response;
            }
        }

        if (debugType != null) onResponse?.Invoke(debugType, responseContent);
        return responseContent;
    }

    /// <summary>
    /// Remote OpenAI-compatible generation — heavyweight path for structured
    /// module JSON and synthesis. Falls back to the local model when no remote
    /// endpoint is configured or the remote call fails.
    /// </summary>
    public async Task<string> RemoteAsync(
        string system,
        string user,
        bool json = false,
        CancellationToken cancellationToken = default,
        Action<string, string>? onRequest = null,
        Action<string, string>? onResponse = null,
        string? debugType = null)
    {
        if (debugType != null) onRequest?.Invoke(debugType, $"[remote]\nSystem:\n{system}\n\nUser:\n{user}");

        if (RemoteAvailable)
        {
            try
            {
                var result = await OpenAI.Prompt(system ?? "", "", user);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    if (debugType != null) onResponse?.Invoke(debugType, result);
                    return result;
                }
            }
            catch
            {
                // Remote endpoint unreachable/misconfigured — fall through to local
            }
        }

        return await LocalAsync(system, user, json, cancellationToken, onRequest, onResponse, debugType);
    }

    /// <summary>
    /// Generate an embedding vector for text using the local embedding model.
    /// </summary>
    public async Task<float[]> EmbedAsync(string text)
    {
        try
        {
            var response = await _ollama.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = new System.Collections.Generic.List<string> { text }
            });
            return response.Embeddings[0];
        }
        catch
        {
            return new float[768];
        }
    }

    /// <summary>
    /// Batch embeddings — one call for many texts (used by research dedup).
    /// Returns null on failure so callers can degrade to string matching.
    /// </summary>
    public async Task<float[][]?> EmbedBatchAsync(System.Collections.Generic.List<string> texts)
    {
        try
        {
            if (texts == null || texts.Count == 0) return new float[0][];
            var response = await _ollama.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = "nomic-embed-text",
                Input = texts
            });
            if (response?.Embeddings == null) return null;
            return response.Embeddings.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
