using System.Text;
using Google.Cloud.Speech.V1;
using Google.Protobuf;

namespace Collector.Common
{
    public static class SpeechToText
    {
        /// <summary>
        /// Converts speech from audio bytes to text using Google Cloud Speech-to-Text API
        /// </summary>
        /// <param name="audioBytes">Audio data as byte array</param>
        /// <param name="languageCode">Language code (e.g., "en-US", "es-ES")</param>
        /// <param name="sampleRateHertz">Sample rate of the audio in Hz</param>
        /// <param name="encoding">Audio encoding format</param>
        /// <returns>Transcribed text from the audio</returns>
        public static async Task<string> ConvertSpeechToTextAsync(byte[] audioBytes, string languageCode = "en-US", int sampleRateHertz = 16000, RecognitionConfig.Types.AudioEncoding encoding = RecognitionConfig.Types.AudioEncoding.Linear16)
        {
            if (audioBytes == null || audioBytes.Length == 0)
            {
                throw new ArgumentException("Audio bytes cannot be null or empty.");
            }

            try
            {
                // Create a Speech client
                var speechClient = SpeechClient.Create();

                // Configure the recognition request
                var config = new RecognitionConfig
                {
                    Encoding = encoding,
                    SampleRateHertz = sampleRateHertz,
                    LanguageCode = languageCode,
                    EnableAutomaticPunctuation = true,
                    Model = "latest_long"
                };

                var audio = new RecognitionAudio
                {
                    Content = ByteString.CopyFrom(audioBytes)
                };

                // Perform the speech recognition
                var response = await speechClient.RecognizeAsync(config, audio);

                // Extract and return the transcribed text
                var transcription = new StringBuilder();
                foreach (var result in response.Results)
                {
                    if (result.Alternatives.Count > 0)
                    {
                        transcription.AppendLine(result.Alternatives[0].Transcript);
                    }
                }

                return transcription.ToString().Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Speech-to-text conversion failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Converts speech from an audio file to text using Google Cloud Speech-to-Text API
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file</param>
        /// <param name="languageCode">Language code (e.g., "en-US", "es-ES")</param>
        /// <param name="sampleRateHertz">Sample rate of the audio file in Hz</param>
        /// <returns>Transcribed text from the audio</returns>
        public static async Task<string> ConvertSpeechToTextAsync(string audioFilePath, string languageCode = "en-US", int sampleRateHertz = 16000)
        {
            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                throw new ArgumentException("Audio file path is invalid or file does not exist.");
            }
            // Read the audio file
            var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
            return await ConvertSpeechToTextAsync(audioBytes, languageCode, sampleRateHertz);
        }

        /// <summary>
        /// Converts speech from a stream to text using Google Cloud Speech-to-Text API
        /// </summary>
        /// <param name="audioStream">Audio stream</param>
        /// <param name="languageCode">Language code (e.g., "en-US", "es-ES")</param>
        /// <param name="sampleRateHertz">Sample rate of the audio in Hz</param>
        /// <param name="encoding">Audio encoding format</param>
        /// <returns>Transcribed text from the audio</returns>
        public static async Task<string> ConvertSpeechToTextAsync(Stream audioStream, string languageCode = "en-US", int sampleRateHertz = 16000, RecognitionConfig.Types.AudioEncoding encoding = RecognitionConfig.Types.AudioEncoding.Linear16)
        {
            if (audioStream == null)
            {
                throw new ArgumentNullException(nameof(audioStream));
            }

            try
            {
                // Read stream to byte array
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                return await ConvertSpeechToTextAsync(audioBytes, languageCode, sampleRateHertz, encoding);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Speech-to-text conversion failed: {ex.Message}", ex);
            }
        }
    }
}
