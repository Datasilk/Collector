using ElevenLabs;
using ElevenLabs.TextToSpeech;
using ElevenLabs.Voices;
using Collector.Common.Extensions.Strings;

namespace Collector.Common
{
    /// <summary>
    /// Text-to-Speech service using ElevenLabs API
    /// </summary>
    public static class TextToSpeech
    {
        private static string OutputDirectory { get; set; } = "TextToSpeech";
        public static string PrivateKey { get; set; } = "";
        public static List<Voice> Voices { get; set; } = new List<Voice>();
        public static string VoiceId { get; set; } = "uhYnkYTBc711oAY590Ea";

        /// <summary>
        /// Converts text to speech and saves the audio file with a timestamp
        /// </summary>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="voiceId">ElevenLabs voice ID (optional, uses default if not provided)</param>
        /// <param name="modelId">Model ID to use (optional, uses default if not provided)</param>
        /// <returns>Path to the generated audio file</returns>
        public static string ConvertTextToSpeech(string text, string voiceId = "", string modelId = "eleven_flash_v2_5")
        {
            if (string.IsNullOrEmpty(text)) { throw new ArgumentException("Text cannot be null or empty", nameof(text)); }


            if (voiceId != "") { VoiceId = voiceId; }
            var filehash = Generate.MD5Hash(text + "|" + VoiceId + "|" + modelId);
            var filename = $"{modelId}_{filehash}.mp3";
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OutputDirectory);
            var filePath = Path.Combine(folderPath, filename);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if(File.Exists(filePath))
            {
                //audio file already exists
                return filePath;
            }
            else
            {
                //generate audio file
                try
                {
                    var api = new ElevenLabsClient(new ElevenLabsAuthentication(PrivateKey));
                    var voice = GetVoice(VoiceId);
                    var Model = ElevenLabs.Models.Model.MultiLingualV2;
                    switch (modelId)
                    {
                        case "eleven_flash_v2_5":
                            Model = ElevenLabs.Models.Model.FlashV2_5;
                            break;
                    }
                    var request = new TextToSpeechRequest(voice, text, model: Model);
                    var voiceClip = api.TextToSpeechEndpoint.TextToSpeechAsync(request).Result;
                    File.WriteAllBytes(filePath, voiceClip.ClipData.ToArray());
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to convert text to speech: {ex.Message}", ex);
                }
            }

            return filePath;

        }

        private static Voice? GetVoice(string voiceId)
        {
            var api = new ElevenLabsClient(new ElevenLabsAuthentication(PrivateKey));
            if(Voices.Any(a => a.Id == voiceId))
            {
                return Voices.Where(a => a.Id == voiceId).FirstOrDefault();
            }
            var voice = api.VoicesEndpoint.GetVoiceAsync(voiceId).Result;
            Voices.Add(voice);
            return voice;
        }
    }
}