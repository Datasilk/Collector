# Open AI

[Home](README.md) > Open AI

`Collector.AI/LLMs.cs` — the `OpenAI` static class: an OpenAI-compatible
chat-completions client usable against any endpoint with the same API
shape (OpenAI, Azure, OpenRouter, local gateways).

## Registry

- `Available: Dictionary<int, LLMInfo>` — populated at startup from enabled
  `LLM_Models` rows; kept in sync via `AddModel`/`UpdateModel`/`RemoveModel`
  when the admin UI edits models (**Admin → OpenAI**, `LLMModelsController`
  / `LLMsController`).
- `PreferredModel` — default model id when callers don't specify one.
- `LLMModel` carries `Endpoint` + `PrivateKey` per model.

## `Prompt(system, assistant, user, modelId)`

Builds a chat-completions request — system/assistant/user messages, model
id, `ExtraBody` merged for provider-specific params — POSTs to
`{endpoint}/chat/completions` with `Bearer {PrivateKey}`, returns the first
choice's message content. Throws when no key is configured.

## Who uses it

`DomainWorker`/`DomainAnalyzer` pass `OpenAI.Prompt` as the `LlmCall`
delegate for domain classification; the chat research pipeline uses the
local [Ollama](Ollama.md) stack (`LLMClient`) instead.
