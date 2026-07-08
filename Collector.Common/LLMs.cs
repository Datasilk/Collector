using Collector.Common.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Collector.Common
{
    public static class LLMs
    {
        public enum Models
        {
            Unknown, Qwen, ChatGPT, Gemini
        }

        /// <summary>
        /// The preferred model is set by the user to determine which model should be used in any given situation
        /// </summary>
        public static Models PreferredModel { get; set; } = Models.Unknown;

        public static Dictionary<Models, LLMInfo> Available = new Dictionary<Models, LLMInfo>()
        {
            {Models.Qwen, new LLMInfo(){
                Model = "qwen-flash",
                Endpoint = "https://dashscope-us.aliyuncs.com/compatible-mode/v1",
                PrivateKey = "",
                MaxInputTokens = 1000000
            }},
            {Models.ChatGPT, new LLMInfo(){
                Model = "gpt-4o-mini",
                Endpoint = "https://api.openai.com/v1/",
                PrivateKey = ""
            }},
            {Models.Gemini, new LLMInfo(){
                Model = "gemini-2.0-flash-lite",
                Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
                PrivateKey = ""
            }}
        };

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> Prompt(string system, string assistant, string user, Models llm = Models.Unknown)
        {
            var preferredModel = llm != Models.Unknown ? llm : PreferredModel != Models.Unknown ? PreferredModel : Models.Qwen;
            var myLLM = Available[preferredModel];
            
            if (string.IsNullOrEmpty(myLLM.PrivateKey))
            {
                throw new Exception("LLM private key is missing");
            }

            var messages = new List<ChatMessage>();
            
            if (!string.IsNullOrEmpty(system))
            {
                messages.Add(new ChatMessage { Role = "system", Content = system });
            }
            
            if (!string.IsNullOrEmpty(assistant))
            {
                messages.Add(new ChatMessage { Role = "assistant", Content = assistant });
            }
            
            messages.Add(new ChatMessage { Role = "user", Content = user });

            var requestBody = new ChatCompletionRequest
            {
                Model = myLLM.Model,
                Messages = messages
            };

            if (myLLM.MaxInputTokens.HasValue)
            {
                requestBody.ExtraBody = new Dictionary<string, object>
                {
                    { "max_input_tokens", myLLM.MaxInputTokens.Value }
                };
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{myLLM.Endpoint.TrimEnd('/')}/chat/completions");
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", myLLM.PrivateKey);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"LLM API request failed: {response.StatusCode} - {responseContent}");
            }

            var completionResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, jsonOptions);
            
            if (completionResponse?.Choices == null || completionResponse.Choices.Count == 0)
            {
                throw new Exception("No response from LLM");
            }

            return completionResponse.Choices[0].Message.Content;
        }

        private class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        private class ChatCompletionRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("messages")]
            public List<ChatMessage> Messages { get; set; }

            [JsonPropertyName("extra_body")]
            public Dictionary<string, object> ExtraBody { get; set; }
        }

        private class ChatCompletionResponse
        {
            [JsonPropertyName("choices")]
            public List<ChatChoice> Choices { get; set; }
        }

        private class ChatChoice
        {
            [JsonPropertyName("message")]
            public ChatMessage Message { get; set; }
        }
    }
}
