using System.Text.Json.Serialization;

namespace Collector.Chat.Models;

/// <summary>
/// Ollama planning response - if steps are present, it's a plan; if not, it's a direct answer
/// </summary>
public class OllamaPlanResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<PlanStep>? Steps { get; set; }

    /// <summary>
    /// Returns true if this response contains steps (indicating a plan), false for direct answer
    /// </summary>
    public bool IsPlan => Steps?.Count > 0;
}

/// <summary>
/// Tool parameter definition for Ollama context
/// </summary>
public class ToolParameter
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("datatype")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Tool response field definition for Ollama context
/// </summary>
public class ToolResponseField
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("datatype")]
    public string DataType { get; set; } = string.Empty;
}

/// <summary>
/// Tool definition for Ollama context building
/// </summary>
public class ToolDefinition
{
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public List<ToolParameter> Parameters { get; set; } = new();

    [JsonPropertyName("response")]
    public List<ToolResponseField> Response { get; set; } = new();
}

/// <summary>
/// A step in the execution plan
/// </summary>
public class PlanStep
{
    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();

    /// <summary>
    /// Results from executing this step, available to subsequent steps
    /// </summary>
    [JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; set; }
}

/// <summary>
/// The execution plan from Ollama
/// </summary>
public class ExecutionPlan
{
    [JsonPropertyName("steps")]
    public List<PlanStep> Steps { get; set; } = new();
}
