using System.Text.Json;
using Collector.Chat.Models;

namespace Collector.Chat;

/// <summary>
/// Registry for managing chat tools and building tool context for Ollama
/// </summary>
public class ChatToolRegistry
{
    private readonly Dictionary<string, IChatTool> _tools = new();

    /// <summary>
    /// Register a tool with the registry
    /// </summary>
    public void RegisterTool(IChatTool tool)
    {
        _tools[tool.ToolKey] = tool;
    }

    /// <summary>
    /// Get a tool by its key
    /// </summary>
    public IChatTool? GetTool(string toolKey)
    {
        return _tools.TryGetValue(toolKey, out var tool) ? tool : null;
    }

    /// <summary>
    /// Get all registered tools
    /// </summary>
    public IEnumerable<IChatTool> GetAllTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// Build the tools context JSON for Ollama planning
    /// </summary>
    public string BuildToolsContext()
    {
        var toolDefinitions = _tools.Values.Select(t => new ToolDefinition
        {
            Tool = t.ToolKey,
            Description = t.Description,
            Parameters = t.Parameters,
            Response = t.ResponseFields
        }).ToList();

        return JsonSerializer.Serialize(toolDefinitions, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Build the planning system prompt for Ollama
    /// </summary>
    public string BuildPlanningPrompt(string userMessage, string ragContext)
    {
        // Build dynamic tool list from registered tools (only include tools available to Ollama)
        var toolList = string.Join("\n", _tools.Values
            .Where(t => t.AvailableToOllama)
            .Select(t =>
            {
                var paramList = t.Parameters?.Any() == true
                    ? string.Join(", ", t.Parameters.Select(p => $"{p.Key}: {p.DataType}"))
                    : "none";
                return $"- {t.ToolKey}: {t.Description} (args: {paramList})";
            }));

        var prompt = "You are a planning assistant. Analyze the user's request and respond with ONLY valid JSON.\n\n" +
            "DECISION:\n" +
            "- If user wants a simple answer/conversation → return message only (no steps)\n" +
            "- If user needs actions performed → return message AND steps\n\n" +
            "AVAILABLE TOOLS (use their exact tool keys):\n" +
            toolList +
            "RESPONSE FORMAT:\n" +
            "{\n" +
            "  \"message\": \"What you will do or your answer\",\n" +
            "  \"steps\": [  // Include ONLY if actions are needed\n" +
            "    {\"tool\": \"tool-key\", \"args\": {\"param\": \"value\"}}\n" +
            "  ]\n" +
            "}\n\n" +
            "Conversation Context:\n" +
            ragContext +
            "\n\nUser Message: " + userMessage;

        return prompt;
    }

    /// <summary>
    /// Execute a plan step by step
    /// </summary>
    public async Task ExecutePlan(
        ExecutionPlan plan,
        string userMessage,
        string ragContext,
        List<Dictionary<string, List<string>>> data,
        Action<int, string> onStepProgress,
        Action<string, string, Exception?> onToolError,
        Action<string, string> onToolComplete,
        Guid appUserId,
        OnRawRequest? onRawRequest = null,
        OnRawResponse? onRawResponse = null,
        OnSaveChatHistory? onSaveChatHistory = null,
        SendMessage? sendMessage = null)
    {
        var totalSteps = plan.Steps.Count;

        for (int i = 0; i < totalSteps; i++)
        {
            var step = plan.Steps[i];
            var tool = GetTool(step.Tool);

            if (tool == null)
            {
                onToolError(step.Tool, $"Tool '{step.Tool}' not found in registry.", null);
                continue;
            }

            var stepNumber = i + 1;

            await tool.Run(
                userMessage,
                ragContext,
                data,
                (percent, message) => onStepProgress((int)((stepNumber - 1 + percent / 100.0) / totalSteps * 100), $"[{stepNumber}/{totalSteps}] {message}"),
                (message, ex) => onToolError(step.Tool, message, ex),
                (message) => onToolComplete(step.Tool, message),
                appUserId,
                plan,
                i,
                onRawRequest,
                onRawResponse,
                onSaveChatHistory,
                sendMessage
            );

            // Update total steps in case new steps were added
            totalSteps = plan.Steps.Count;
        }
    }
}
