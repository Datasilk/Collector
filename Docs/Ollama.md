# Ollama

[Home](README.md) > Ollama

Collector's local model runtime — powers the chat system (planning,
relevance gates, topic assignment, edits) and the HTML parser models.

## Configuration (`Collector.AI/Ollama/Config.cs` + `Ollama:*` appsettings)

| Setting | Default | Effect |
|---|---|---|
| `Url` | `http://localhost:11434` | Ollama API endpoint |
| `Model` | `qwen2.5:0.5b` | default chat model |
| `UseGpu` / `NumGpu` | false / 1 | GPU offload |
| `Temperature` | 0.7 | sampling temperature |
| `AutoPullModel` | true | pull missing models at startup |

Startup registers a singleton `OllamaApiClient` (`OllamaSharp`) with a
**15-minute** `HttpClient` timeout, auto-pulls the configured model and
`nomic-embed-text` (embeddings) when missing, and degrades gracefully
(warn + continue) if Ollama is unreachable.

## Model roles

- **Chat/planning** — loaded from enabled `LLM_Models` rows at startup;
  `Collector.Chat/Helpers/LLMClient.cs` is the client. Context window is
  read per model (e.g. 131072 tokens).
- **HTML parser** — `LLM_HtmlParsers` rows, managed under **Admin → Ollama:
  HTML Parser Models** and via the `manage-ollama-models` chat tool.
  `web-scrape` sends cleaned HTML to the active parser; the JSON response
  becomes `research/{####}.json`.

## Operational notes

- Parser calls take several minutes per page — expected, not a hang.
- Ollama stays resident; CPU idles between calls.
