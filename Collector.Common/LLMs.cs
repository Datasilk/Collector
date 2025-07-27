using OpenAI.Chat;
using Collector.Common.Models;

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
                Model = "qwen-turbo-latest",
                Endpoint = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1",
                PrivateKey = ""
            }},
            {Models.ChatGPT, new LLMInfo(){
                Model = "gpt-4o-mini",
                Endpoint = "https://api.openai.com/v1",
                PrivateKey = ""
            }},
            {Models.Gemini, new LLMInfo(){
                Model = "gemini-2.0-flash-lite",
                Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/",
                PrivateKey = ""
            }}
        };

        public static async Task<string> Prompt(string system, string assistant, string user, Models llm = Models.Unknown)
        {
            var preferredModel = llm != Models.Unknown ? llm : PreferredModel != Models.Unknown ? PreferredModel : Models.Qwen;
            var myLLM = Available[preferredModel];
            if (string.IsNullOrEmpty(myLLM.PrivateKey))
            {
                throw new Exception("LLM private key is missing");
            }
            ChatClient client = new ChatClient(myLLM.Model, new System.ClientModel.ApiKeyCredential(myLLM.PrivateKey), new OpenAI.OpenAIClientOptions()
            {
                Endpoint = new Uri(myLLM.Endpoint)
            });

            List<ChatMessage> prompt = new List<ChatMessage>()
            {
                new SystemChatMessage(system),
                new AssistantChatMessage(assistant),
                new UserChatMessage(user)
            };
            var results = await client.CompleteChatAsync(prompt);
            return results.Value.Content[0].Text;
        }
    }
}
