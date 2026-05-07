using Collector.Chat.Models;

namespace Collector.Chat;

/// <summary>
/// Callback for reporting progress during tool execution
/// </summary>
public delegate void OnProgress(int percent, string message);

/// <summary>
/// Callback for reporting errors during tool execution
/// </summary>
public delegate void OnError(string message, Exception? exception = null);

/// <summary>
/// Callback for reporting completion of tool execution
/// </summary>
public delegate void OnComplete(string message);

/// <summary>
/// Callback for reporting raw request data for debugging
/// </summary>
public delegate void OnRawRequest(string type, string prompt);

/// <summary>
/// Callback for reporting raw response data for debugging
/// </summary>
public delegate void OnRawResponse(string type, string response);

/// <summary>
/// Callback for saving chat history context (e.g., from web-scrape tool)
/// </summary>
public delegate void OnSaveChatHistory(string context);

/// <summary>
/// Callback for sending a message to the chat window
/// </summary>
public delegate void SendMessage(string message, string? linkUrl = null, string? linkText = null);

/// <summary>
/// Utility class for managing plan execution and adding steps dynamically
/// </summary>
public static class PlanUtility
{
    /// <summary>
    /// Add a new step to the plan immediately after the specified step index
    /// </summary>
    public static void AddStepToPlan(ExecutionPlan plan, int currentStepIndex, string tool, Dictionary<string, object> args)
    {
        var newStep = new PlanStep
        {
            Tool = tool,
            Args = args
        };
        
        // Insert after current step
        plan.Steps.Insert(currentStepIndex + 1, newStep);
    }

    /// <summary>
    /// Add multiple steps to the plan immediately after the specified step index
    /// </summary>
    public static void AddStepsToPlan(ExecutionPlan plan, int currentStepIndex, List<PlanStep> steps)
    {
        // Insert steps in reverse order so they appear in correct order
        for (int i = steps.Count - 1; i >= 0; i--)
        {
            plan.Steps.Insert(currentStepIndex + 1, steps[i]);
        }
    }
}

/// <summary>
/// Interface for chat tools that can be executed as part of an AI-generated plan
/// </summary>
public interface IChatTool
{
    /// <summary>
    /// Unique key identifying this tool (e.g., "web-scrape", "new-journal-entry")
    /// </summary>
    string ToolKey { get; }

    /// <summary>
    /// Human-readable description of what this tool does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Definition of parameters this tool accepts
    /// </summary>
    List<Models.ToolParameter> Parameters { get; }

    /// <summary>
    /// Definition of fields this tool returns in its response
    /// </summary>
    List<Models.ToolResponseField> ResponseFields { get; }

    /// <summary>
    /// Whether this tool is available to Ollama for planning (if false, only other tools can add this tool's steps)
    /// </summary>
    bool AvailableToOllama { get; }

    /// <summary>
    /// Execute the tool with the given context and callbacks
    /// </summary>
    /// <param name="userMessage">The original user chat message</param>
    /// <param name="ragContext">RAG context gathered for this conversation</param>
    /// <param name="data">Shared data dictionary for passing values between tools</param>
    /// <param name="onProgress">Callback for progress updates (percent, message)</param>
    /// <param name="onError">Callback for error reporting</param>
    /// <param name="onComplete">Callback for completion notification</param>
    /// <param name="plan">The execution plan that can be modified by the tool</param>
    /// <param name="currentStepIndex">The index of the current step being executed</param>
    /// <param name="onRawRequest">Callback for raw request debugging</param>
    /// <param name="onRawResponse">Callback for raw response debugging</param>
    /// <param name="onSaveChatHistory">Callback for saving chat history context</param>
    /// <param name="sendMessage">Callback for sending a message with optional link to the chat window</param>
    /// <returns>Task representing the async operation</returns>
    Task Run(
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
        SendMessage? sendMessage = null);
}
