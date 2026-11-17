using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Collector.Chat;

/// <summary>
/// Centralized JSON extraction/parsing for LLM responses. Replaces the
/// ad-hoc IndexOf('{')/LastIndexOf('}') slicing scattered through the tools
/// with balanced-brace extraction that also strips markdown code fences.
/// </summary>
public static class LLMJson
{
    /// <summary>
    /// Extract the first balanced JSON object or array from raw LLM output.
    /// Returns null when no balanced structure is found.
    /// </summary>
    public static string? Extract(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var text = raw.Trim();

        // Strip a leading markdown code fence if present
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
            {
                text = text.Substring(firstNewline + 1);
                var fenceEnd = text.LastIndexOf("```", StringComparison.Ordinal);
                if (fenceEnd >= 0) text = text.Substring(0, fenceEnd);
                text = text.Trim();
            }
        }

        // Find the first '{' or '[' and walk for its balanced close
        var start = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' || text[i] == '[') { start = i; break; }
        }
        if (start < 0) return null;

        var open = text[start];
        var close = open == '{' ? '}' : ']';
        var depth = 0;
        var inString = false;
        var escape = false;

        for (int i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escape) { escape = false; }
                else if (c == '\\') { escape = true; }
                else if (c == '"') { inString = false; }
                continue;
            }

            if (c == '"') { inString = true; continue; }
            if (c == open || (open == '{' && c == '[') || (open == '[' && c == '{'))
            {
                // Track nesting across both bracket types uniformly
                if (c == '{' || c == '[') depth++;
            }
            else if (c == '}' || c == ']')
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(start, i - start + 1);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extract and deserialize JSON of type T from raw LLM output.
    /// </summary>
    public static bool TryParse<T>(string? raw, out T? value)
    {
        value = default;
        var json = Extract(raw);
        if (json == null) return false;

        try
        {
            value = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return value != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Call a prompt function, extract + deserialize the JSON response, and on
    /// failure retry once with feedback describing what went wrong.
    /// </summary>
    public static async Task<T?> ParseWithRetryAsync<T>(
        Func<string, Task<string>> promptFn,
        string prompt,
        Func<T?, string?> validate,
        int maxAttempts = 2)
    {
        string? lastError = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var effectivePrompt = attempt == 0 || lastError == null
                ? prompt
                : prompt + $"\n\nYour previous response could not be used: {lastError}\nRespond with ONLY valid JSON matching the required schema.";

            var raw = await promptFn(effectivePrompt);
            if (!TryParse<T>(raw, out var value))
            {
                lastError = "the output was not valid JSON";
                continue;
            }

            var validationError = validate(value);
            if (validationError == null)
            {
                return value;
            }

            lastError = validationError;
        }

        return default;
    }
}
