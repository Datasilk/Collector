# Speech

[Home](README.md) > Speech

`Collector.AI/Speech/` — two provider-backed helpers.

## TextToSpeech (`TextToSpeech.cs`)

ElevenLabs API (`ElevenLabsClient`):

- `ConvertTextToSpeech(text, voiceId, modelId)` — default model
  `eleven_flash_v2_5`, default voice `uhYnkYTBc711oAY590Ea`,
  `VoiceSpeed` 1.2.
- **Result-cached**: filename = `{modelId}_{MD5(text|voiceId|modelId)}.mp3`
  under `TextToSpeech/` next to the running assembly — repeat calls for
  the same text+voice return the existing file without hitting the API.
- `PrivateKey` + `Voices` populated from config; `VoiceId`/`VoiceSpeed`
  overridable per call.

## SpeechToText (`SpeechToText.cs`)

Google Cloud Speech-to-Text (`SpeechClient`):

- `ConvertSpeechToTextAsync(audioBytes, languageCode, sampleRateHertz,
  encoding)` — defaults `en-US`, 16kHz, `Linear16`.
- Uses the `latest_long` model with `EnableAutomaticPunctuation`; joins
  `response.Results[].Alternatives[0].Transcript`.
