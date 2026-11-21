# manage-ollama-models

[Home](../README.md) > [Chat](../Chat.md) > manage-ollama-models

`ManageOllamaModelsTool` — Ollama model management from chat.

## Args

`action` (list/active/set-active/pull/parser-models/…), `model`.

## Behavior

- Lists installed Ollama models and which is active.
- Switches the active chat/planning model (`LLMs` rows).
- Pulls new models.
- Manages the **HTML parser models** (`LLM_HtmlParsers`) used by
  `web-scrape`.
- Mirrors **Admin → Ollama** in the web client.
